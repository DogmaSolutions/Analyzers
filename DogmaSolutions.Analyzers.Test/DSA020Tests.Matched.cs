using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA020Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "async (ct) => await Task.FromResult(value)",
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
                        Func<CancellationToken, Task<int>> func = {|#0:async (ct) => await Task.FromResult(42)|};
                    }
                }
            }"
        ],
        [
            "async ct => await Task.FromResult(value)",
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
                        Func<CancellationToken, Task<int>> func = {|#0:async ct => await Task.FromResult(42)|};
                    }
                }
            }"
        ],
        [
            "async _ => await Task.FromResult(value)",
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
                        Func<CancellationToken, Task<int>> func = {|#0:async _ => await Task.FromResult(42)|};
                    }
                }
            }"
        ],
        [
            "async () => await Task.FromResult(value)",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task<string>> func = {|#0:async () => await Task.FromResult(""hello"")|};
                    }
                }
            }"
        ],
        [
            "async (ct) => await Task.FromResult<int>(value) (generic variant)",
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
                        Func<CancellationToken, Task<int>> func = {|#0:async (ct) => await Task.FromResult<int>(42)|};
                    }
                }
            }"
        ],
        [
            "async () => await Task.FromResult<string>(value) (generic variant)",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task<string>> func = {|#0:async () => await Task.FromResult<string>(""hello"")|};
                    }
                }
            }"
        ],
        [
            "Block body: async (ct) => { return await Task.FromResult(value); }",
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
                        Func<CancellationToken, Task<int>> func = {|#0:async (ct) => { return await Task.FromResult(42); }|};
                    }
                }
            }"
        ],
        [
            "Block body with no params: async () => { return await Task.FromResult(value); }",
            @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Setup()
                    {
                        Func<Task<int>> func = {|#0:async () => { return await Task.FromResult(42); }|};
                    }
                }
            }"
        ],
        [
            "async with ConfigureAwait(false)",
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
                        Func<CancellationToken, Task<int>> func = {|#0:async (ct) => await Task.FromResult(42).ConfigureAwait(false)|};
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA020Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA020Analyzer>.Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
