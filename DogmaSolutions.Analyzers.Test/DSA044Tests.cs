using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA044 end to end: console or debug output where a logger is available.</summary>
[TestClass]
public partial class DSA044Tests
{
    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA044Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>
    /// A class <c>Service</c> with the given members. <c>[[...]]</c> in the members marks the code the diagnostic is expected on
    /// (the markup is kept only when the scenario is matched).
    /// </summary>
    internal static string BuildClass(string pre, string header, string members, bool matched)
    {
        var body = matched ? members.Replace("[[", "{|#0:").Replace("]]", "|}") : members.Replace("[[", string.Empty).Replace("]]", string.Empty);
        return DSA044TestSupport.Usings + "namespace TestApp\n{\n" + pre + "\n    " + header + "\n    {\n" + body + "\n    }\n}";
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic.DSA044." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    internal static CSharpAnalyzerVerifier<DSA044Analyzer>.Test CreateTest(string source, string settings)
    {
        var test = new CSharpAnalyzerVerifier<DSA044Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.Sources.Add(("Fakes.cs", DSA044TestSupport.FakeLoggersSource));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));
        return test;
    }

    internal static void ExpectDiagnostic(CSharpAnalyzerVerifier<DSA044Analyzer>.Test test)
    {
        // no message: the arguments (the call and the logger) are checked by their own test
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA044Analyzer.DiagnosticId, DiagnosticSeverity.Info).WithLocation(0));
    }
}
