using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA040Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Security-named local (int)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        int token = {|#0:rnd.Next()|};
                        System.Console.WriteLine(token);
                    }
                }
            }"
        ],
        [
            "Security-named local via ToString (word-boundary apiKey)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        var apiKey = {|#0:rnd.Next()|}.ToString();
                        System.Console.WriteLine(apiKey);
                    }
                }
            }"
        ],
        [
            "Assignment to a security-named field",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    private int sessionId;
                    public void M(Random rnd)
                    {
                        sessionId = {|#0:rnd.Next()|};
                    }
                }
            }"
        ],
        [
            "Security-named method return (expression body)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public int GetOtp(Random rnd) => {|#0:rnd.Next()|};
                }
            }"
        ],
        [
            "NextBytes into a security-named buffer",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        var salt = new byte[16];
                        {|#0:rnd.NextBytes(salt)|};
                    }
                }
            }"
        ],
        [
            "Random.Shared for a security sink",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M()
                    {
                        var nonce = {|#0:Random.Shared.Next()|};
                        System.Console.WriteLine(nonce);
                    }
                }
            }"
        ],
        [
            "One assignment hop into a security-named field",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    private int token;
                    public void M(Random rnd)
                    {
                        var t = {|#0:rnd.Next()|};
                        token = t;
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA040Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA040Analyzer>.Diagnostic(DSA040Analyzer.DiagnosticId)
                .WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
