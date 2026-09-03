using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA038Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Parameterless constructor (the recommended form)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => new Random();
                }
            }"
        ],
        [
            "Seed from a method call (non-constant, non-time-based)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => new Random(GetSeed());
                    private int GetSeed() => 123;
                }
            }"
        ],
        [
            "Seed from a parameter",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make(int seed) => new Random(seed);
                }
            }"
        ],
        [
            "A user-defined Random type must not be matched",
            @"
            namespace TestApp
            {
                public sealed class Random
                {
                    public Random(int seed) { }
                }

                public class MyType
                {
                    public Random Make() => new Random(42);
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
        var test = new CSharpAnalyzerVerifier<DSA038Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
