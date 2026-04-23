using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA020Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Non-async lambda returning Task.FromResult (already correct)",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<CancellationToken, Task<int>> func = _ => Task.FromResult(42);
                    }
                }
            }"
        ],
        [
            "Async lambda with real async work (not just Task.FromResult)",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    private Task<int> GetValueAsync() => Task.FromResult(42);
                    public void Setup()
                    {
                        Func<Task<int>> func = async () => await GetValueAsync();
                    }
                }
            }"
        ],
        [
            "Async lambda with multiple statements",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task<int>> func = async () =>
                        {
                            var x = 42;
                            return await Task.FromResult(x);
                        };
                    }
                }
            }"
        ],
        [
            "Async lambda awaiting a different method",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task<int>> func = async () => await Task.Delay(100).ContinueWith(_ => 42);
                    }
                }
            }"
        ],
        [
            "Non-lambda async method (not in scope)",
            @"
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task<int> GetValueAsync()
                    {
                        return await Task.FromResult(42);
                    }
                }
            }"
        ],
        [
            "Async anonymous method delegate (not a lambda, out of scope)",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<CancellationToken, Task<int>> func = async delegate(CancellationToken ct) { return await Task.FromResult(42); };
                    }
                }
            }"
        ],
        [
            "Async lambda awaiting Task.CompletedTask (different method)",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task> func = async () => await Task.CompletedTask;
                    }
                }
            }"
        ],
        [
            "Lambda without async keyword",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task<int>> func = () => Task.FromResult(42);
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA020Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
