using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA041Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Switch over an enum with exactly max (4) members (boundary: not > max)",
            @"
            namespace TestApp
            {
                public enum Suit { Hearts, Diamonds, Clubs, Spades }
                public class MyType
                {
                    public int Rank(Suit s)
                    {
                        switch (s)
                        {
                            case Suit.Hearts: return 1;
                            default: return 0;
                        }
                    }
                }
            }"
        ],
        [
            "Switch over a 3-member enum",
            @"
            namespace TestApp
            {
                public enum Light { Red, Amber, Green }
                public class MyType
                {
                    public int Go(Light l)
                    {
                        switch (l)
                        {
                            case Light.Green: return 1;
                            default: return 0;
                        }
                    }
                }
            }"
        ],
        [
            "Switch over a non-enum (int) governing value",
            @"
            namespace TestApp
            {
                public class MyType
                {
                    public int Classify(int n)
                    {
                        switch (n)
                        {
                            case 1: return 1;
                            case 2: return 2;
                            case 3: return 3;
                            case 4: return 4;
                            case 5: return 5;
                            default: return 0;
                        }
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
        var test = new CSharpAnalyzerVerifier<DSA041Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConfiguredThreshold_IsHonoured()
    {
        // max_enum_members lowered to 2 => a 3-member enum switch is now flagged.
        var source = @"
            namespace TestApp
            {
                public enum Light { Red, Amber, Green }
                public class MyType
                {
                    public int Go(Light l)
                    {
                        {|#0:switch|} (l)
                        {
                            case Light.Green: return 1;
                            default: return 0;
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA041Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(
            ("/.editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.DSA041.max_enum_members = 2\n"));

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA041Analyzer>.Diagnostic(DSA041Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("Light", 3, 2));

        await test.RunAsync().ConfigureAwait(false);
    }
}
