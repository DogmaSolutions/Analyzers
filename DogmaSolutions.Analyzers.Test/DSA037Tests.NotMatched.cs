using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA037Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "[ThreadStatic] Random field WITHOUT an initializer (the correct shape)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static Random _rnd;
                }
            }"
        ],
        [
            "Random field WITH an initializer but NOT [ThreadStatic]",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    private static Random _rnd = new Random();
                }
            }"
        ],
        [
            "[ThreadStatic] field with an initializer but NOT of type Random",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static int _counter = 0;
                }
            }"
        ],
        [
            "[ThreadStatic] Random with multiple declarators, none initialized",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static Random _a, _b;
                }
            }"
        ],
        [
            "Local Random variable with an initializer (not a field)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void Use()
                    {
                        Random rnd = new Random();
                        _ = rnd.Next();
                    }
                }
            }"
        ],
        [
            "User-defined attribute named ThreadStatic must not match the BCL one",
            @"
            namespace TestApp
            {
                public sealed class ThreadStaticAttribute : System.Attribute { }

                public class MyType
                {
                    [ThreadStatic]
                    private static System.Random _rnd = new System.Random();
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
        var test = new CSharpAnalyzerVerifier<DSA037Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
