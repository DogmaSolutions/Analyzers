using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA038Tests
{
    // Exact message arguments the analyzer substitutes; kept here so the tests lock the wording.
    private const string TimeBasedArg =
        "a time-based value, which is redundant with the parameterless 'new Random()' and makes instances created within the same clock tick share a sequence";

    private const string ConstantArg =
        "a compile-time constant, which produces a deterministic, predictable sequence";

    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Environment.TickCount",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => {|#0:new Random(Environment.TickCount)|};
                }
            }",
            TimeBasedArg
        ],
        [
            "Environment.TickCount64",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => {|#0:new Random((int)Environment.TickCount64)|};
                }
            }",
            TimeBasedArg
        ],
        [
            "(int)DateTime.UtcNow.Ticks",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => {|#0:new Random((int)DateTime.UtcNow.Ticks)|};
                }
            }",
            TimeBasedArg
        ],
        [
            "unchecked((int)DateTimeOffset.UtcNow.Ticks)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => {|#0:new Random(unchecked((int)DateTimeOffset.UtcNow.Ticks))|};
                }
            }",
            TimeBasedArg
        ],
        [
            "Stopwatch.GetTimestamp()",
            @"
            using System;
            using System.Diagnostics;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => {|#0:new Random((int)Stopwatch.GetTimestamp())|};
                }
            }",
            TimeBasedArg
        ],
        [
            "Compile-time constant literal",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make() => {|#0:new Random(42)|};
                }
            }",
            ConstantArg
        ],
        [
            "Compile-time constant via const field",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    private const int Seed = 7;
                    public Random Make() => {|#0:new Random(Seed)|};
                }
            }",
            ConstantArg
        ],
        [
            "Target-typed new with a constant seed",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public Random Make()
                    {
                        Random r = {|#0:new(42)|};
                        return r;
                    }
                }
            }",
            ConstantArg
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string expectedArg
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA038Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA038Analyzer>.Diagnostic(DSA038Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(expectedArg));

        await test.RunAsync().ConfigureAwait(false);
    }
}
