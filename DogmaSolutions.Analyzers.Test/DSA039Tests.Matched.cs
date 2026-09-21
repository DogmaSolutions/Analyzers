using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA039Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "BitConverter over a locally RNG-filled buffer",
            @"
            using System;
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int n)
                    {
                        byte[] b = new byte[4];
                        RandomNumberGenerator.Fill(b);
                        return {|#0:BitConverter.ToInt32(b, 0) % n|};
                    }
                }
            }"
        ],
        [
            "BitConverter directly over GetBytes",
            @"
            using System;
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int n) => {|#0:BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4), 0) % n|};
                }
            }"
        ],
        [
            "Indexing RNG bytes",
            @"
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll() => {|#0:RandomNumberGenerator.GetBytes(1)[0] % 6|};
                }
            }"
        ],
        [
            "One-hop local integer derived from RNG bytes",
            @"
            using System;
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int n)
                    {
                        int raw = BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4), 0);
                        return {|#0:raw % n|};
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
        var test = new CSharpAnalyzerVerifier<DSA039Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA039Analyzer>.Diagnostic(DSA039Analyzer.DiagnosticId)
                .WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
