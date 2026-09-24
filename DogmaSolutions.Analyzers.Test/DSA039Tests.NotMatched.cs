using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA039Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Correct unbiased API GetInt32 (no modulo)",
            @"
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int n) => RandomNumberGenerator.GetInt32(0, n);
                }
            }"
        ],
        [
            "Modulo on a value not derived from RNG",
            @"
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int someInt, int n) => someInt % n;
                }
            }"
        ],
        [
            "System.Random modulo is a different rule",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int n) => new Random().Next() % n;
                }
            }"
        ],
        [
            "RNG bytes used without a modulo reduction",
            @"
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public int KeyLength()
                    {
                        byte[] key = RandomNumberGenerator.GetBytes(32);
                        return key.Length;
                    }
                }
            }"
        ],
        [
            "Local buffer never filled by an RNG (buffer-fill scan returns false)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public int Roll(int n)
                    {
                        byte[] b = new byte[4];
                        return BitConverter.ToInt32(b, 0) % n;
                    }
                }
            }"
        ],
        [
            "Buffer filled by a non-RNG method (IsRandomNumberGeneratorType returns false)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    private static void Fill(byte[] buffer) { }
                    public int Roll(int n)
                    {
                        byte[] b = new byte[4];
                        Fill(b);
                        return BitConverter.ToInt32(b, 0) % n;
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA039Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
