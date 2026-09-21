using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA042Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Adjacent repeated segment",
            "namespace {|#0:My.Cool.Project.Models.Models|} { public class C { } }",
            "My.Cool.Project.Models.Models",
            "Models"
        ],
        [
            "Non-adjacent repeated segment",
            "namespace {|#0:My.Cool.Project.Models.Abstractions.Models|} { public class C { } }",
            "My.Cool.Project.Models.Abstractions.Models",
            "Models"
        ],
        [
            "File-scoped namespace with repeated segment",
            "namespace {|#0:My.Api.Api|};\npublic class C { }",
            "My.Api.Api",
            "Api"
        ],
        [
            // Single declaration whose repeat is NOT the trailing segment: must still fire.
            "Repeated segment before the last segment",
            "namespace {|#0:Dup.Dup.Middle|} { public class C { } }",
            "Dup.Dup.Middle",
            "Dup"
        ],
        [
            // Physically nested: the OUTER declaration introduces the repeat and is the only one flagged;
            // the inner `Inner` must NOT re-report the inherited "Dup" repetition (regression guard).
            "Nested declaration does not re-report an ancestor's repeat",
            "namespace {|#0:Dup.Dup|} { namespace Inner { public class C { } } }",
            "Dup.Dup",
            "Dup"
        ],
        [
            // Physically nested: a repeat the INNER declaration itself introduces still fires on the inner.
            "Nested declaration reports a repeat it introduces itself",
            "namespace Outer { namespace {|#0:Repeat.Repeat|} { public class C { } } }",
            "Outer.Repeat.Repeat",
            "Repeat"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string fullNamespace,
        string repeatedSegment
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA042Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA042Analyzer>.Diagnostic(DSA042Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(fullNamespace, repeatedSegment));

        await test.RunAsync().ConfigureAwait(false);
    }
}
