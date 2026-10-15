using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA043 end to end: a complex class that neither injects nor uses a logger.</summary>
[TestClass]
public partial class DSA043Tests
{
    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA043Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>
    /// A class with 4 ifs, a try/catch and the given members. <c>[[Name]]</c> in the header is the class the diagnostic is expected on.
    /// </summary>
    internal static string BuildClass(string pre, string header, string members, string activity, bool expectDiagnostic)
    {
        var isStatic = header.Contains("static class", System.StringComparison.Ordinal);
        var name = header.Substring(header.IndexOf("[[", System.StringComparison.Ordinal) + 2, header.IndexOf("]]", System.StringComparison.Ordinal) - header.IndexOf("[[", System.StringComparison.Ordinal) - 2);
        var marked = header.Replace("[[" + name + "]]", expectDiagnostic ? "{|#0:" + name + "|}" : name);
        var ifs = string.Concat(Enumerable.Range(1, 4).Select(i => "if (x == " + i + ") { x++; }\n            "));

        return "using System;\nusing Microsoft.Extensions.Logging;\nnamespace TestApp\n{\n" +
               pre + "\n" +
               "    " + marked + "\n    {\n" +
               members + "\n" +
               "        public " + (isStatic ? "static " : string.Empty) + "int Run(int x)\n        {\n            " + ifs + activity + "\n            return x;\n        }\n    }\n}";
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = DSA043TestSupport.LowThresholds.Concat(
            (settings ?? string.Empty).Split(new[] { ';' }, System.StringSplitOptions.RemoveEmptyEntries).Select(entry =>
            {
                var parts = entry.Split(new[] { '=' }, 2);
                return "dotnet_diagnostic.DSA043." + parts[0].Trim() + " = " + parts[1].Trim();
            }));

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    internal static CSharpAnalyzerVerifier<DSA043Analyzer>.Test CreateTest(string source, string settings, bool withLoggingReference = true)
    {
        var test = new CSharpAnalyzerVerifier<DSA043Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        if (withLoggingReference)
            test.TestState.Sources.Add(("Fakes.cs", DSA043TestSupport.FakeLoggingSource));

        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));
        return test;
    }

    internal static void ExpectDiagnostic(CSharpAnalyzerVerifier<DSA043Analyzer>.Test test)
    {
        // no message: the arguments (complexity, lines) are checked by their own test
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA043Analyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0));
    }
}
