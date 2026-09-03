using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA037Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Canonical [ThreadStatic] Random field with a new-expression initializer",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static Random _rnd {|#0:= new Random()|};
                }
            }",
            "_rnd"
        ],
        [
            "Attribute written with the explicit 'Attribute' suffix",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStaticAttribute]
                    private static Random _rnd {|#0:= new Random()|};
                }
            }",
            "_rnd"
        ],
        [
            "Attribute written fully qualified",
            @"
            namespace TestApp
            {
                public class MyType
                {
                    [System.ThreadStatic]
                    private static System.Random _rnd {|#0:= new System.Random()|};
                }
            }",
            "_rnd"
        ],
        [
            "Initializer is a factory call rather than a new-expression",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static Random _rnd {|#0:= CreateRng()|};

                    private static Random CreateRng() => new Random();
                }
            }",
            "_rnd"
        ],
        [
            "Target-typed new initializer",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static Random _rnd {|#0:= new()|};
                }
            }",
            "_rnd"
        ],
        [
            "Multiple declarators — only the initialized one is flagged",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    [ThreadStatic]
                    private static Random _a {|#0:= new Random()|}, _b;
                }
            }",
            "_a"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string fieldName
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA037Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA037Analyzer>.Diagnostic(DSA037Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(fieldName));

        await test.RunAsync().ConfigureAwait(false);
    }
}
