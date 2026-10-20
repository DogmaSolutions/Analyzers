using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA053 end to end: a Count whose number is only compared with zero.</summary>
[TestClass]
public partial class DSA053Tests
{
    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA053Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>One expected diagnostic: the source, how the result reaches the test, the method, the replacement and the variable.</summary>
    internal static string E(string source, string shape, string method, string alternative, string local = "") => source + "|" + shape + "|" + method + "|" + alternative + "|" + local;

    internal static CSharpAnalyzerVerifier<DSA053Analyzer>.Test CreateTest(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA053Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ExistenceTestSupport.ToEditorConfig("DSA053", settings)));

        var entries = (expected ?? string.Empty).Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < entries.Length; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA053Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i).WithMessage(ExistenceTestSupport.Message("053", entries[i], null)));

        return test;
    }

    [TestMethod]
    public async Task A_variable_of_the_top_level_statements_is_not_looked_at()
    {
        // the variables of a program without a method are visible to the whole file: they are not local to a block
        var source = "using System.Linq;\nvar items = new[] { 1, 2 };\nvar n = items.Select(x => x).Count();\nSystem.Console.WriteLine(n > 0);\n";

        var test = CreateTest(source, string.Empty, string.Empty);
        test.TestState.OutputKind = OutputKind.ConsoleApplication;
        await test.RunAsync().ConfigureAwait(false);
    }

    internal static async Task RunAsync(string members, string settings, string expected, string nullable = "")
    {
        var source = ExistenceTestSupport.BuildSource(members, !string.IsNullOrEmpty(expected), nullable);
        await CreateTest(source, settings, expected).RunAsync().ConfigureAwait(false);
    }
}
