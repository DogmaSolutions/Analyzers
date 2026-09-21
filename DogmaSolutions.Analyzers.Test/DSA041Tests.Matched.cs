using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA041Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Switch statement over a 5-member enum (default max 4)",
            @"
            namespace TestApp
            {
                public enum Color { Red, Green, Blue, Yellow, Orange }
                public class MyType
                {
                    public int Rank(Color c)
                    {
                        {|#0:switch|} (c)
                        {
                            case Color.Red: return 1;
                            case Color.Green: return 2;
                            case Color.Blue: return 3;
                            case Color.Yellow: return 4;
                            default: return 0;
                        }
                    }
                }
            }"
        ],
        [
            "Switch expression over a 5-member enum",
            @"
            namespace TestApp
            {
                public enum Color { Red, Green, Blue, Yellow, Orange }
                public class MyType
                {
                    public int Rank(Color c) => c {|#0:switch|}
                    {
                        Color.Red => 1,
                        Color.Green => 2,
                        _ => 0
                    };
                }
            }"
        ],
        [
            "Switch over a nullable 5-member enum (underlying enum counted)",
            @"
            namespace TestApp
            {
                public enum Color { Red, Green, Blue, Yellow, Orange }
                public class MyType
                {
                    public int Rank(Color? c)
                    {
                        {|#0:switch|} (c)
                        {
                            case Color.Red: return 1;
                            default: return 0;
                        }
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
        var test = new CSharpAnalyzerVerifier<DSA041Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA041Analyzer>.Diagnostic(DSA041Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("Color", 5, 4));

        await test.RunAsync().ConfigureAwait(false);
    }
}
