using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA052 end to end: a FirstOrDefault whose element is only tested for existence.</summary>
[TestClass]
public partial class DSA052Tests
{
    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA052Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>One expected diagnostic: the source, how the result reaches the test, the method, the replacement and the variable.</summary>
    internal static string E(string source, string shape, string method, string alternative, string local = "") => source + "|" + shape + "|" + method + "|" + alternative + "|" + local;

    internal static CSharpAnalyzerVerifier<DSA052Analyzer>.Test CreateTest(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA052Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ExistenceTestSupport.ToEditorConfig("DSA052", settings)));

        var entries = (expected ?? string.Empty).Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < entries.Length; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA052Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i).WithMessage(ExistenceTestSupport.Message("052", entries[i], nameof(Resources.DSA052SingleNote))));

        return test;
    }

    internal static async Task RunAsync(string members, string settings, string expected, string nullable = "")
    {
        var source = ExistenceTestSupport.BuildSource(members, !string.IsNullOrEmpty(expected), nullable);
        await CreateTest(source, settings, expected).RunAsync().ConfigureAwait(false);
    }
}
