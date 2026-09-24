using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA042Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "No repeated segment",
            "namespace My.Cool.Project.Models { public class C { } }"
        ],
        [
            "Single-segment namespace",
            "namespace App { public class C { } }"
        ],
        [
            "Segments differing only by case (ordinal comparison)",
            "namespace My.models.Models { public class C { } }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA042Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
