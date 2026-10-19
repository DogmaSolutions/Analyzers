using System;
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

/// <summary>DSA051 end to end: a class without a ToString override.</summary>
[TestClass]
public partial class DSA051Tests
{
    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA051Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>The expected diagnostic: the scenario (the end of the resource name), the type and the members the message lists.</summary>
    internal static string E(string scenario, string type, string members = "") => scenario + "|" + type + "|" + members;

    internal static string BuildSource(string pre, string declaration, bool expectDiagnostic, string usings = "using System;\nusing System.Collections.Generic;\nusing System.Diagnostics;\n")
    {
        var body = System.Text.RegularExpressions.Regex.Replace(declaration, @"\[\[(.*?)\]\]", match => expectDiagnostic ? "{|#0:" + match.Groups[1].Value + "|}" : match.Groups[1].Value);
        return usings + "namespace TestApp\n{\n" + pre + "\n" + body + "\n}";
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic.DSA051." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    internal static string Message(string expected)
    {
        var parts = expected.Split('|');
        return string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA051Message" + parts[0], Resources.Culture), parts[1], parts[2]);
    }

    internal static CSharpAnalyzerVerifier<DSA051Analyzer>.Test CreateTest(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA051Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));

        if (!string.IsNullOrEmpty(expected))
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Info).WithLocation(0).WithMessage(Message(expected)));

        return test;
    }

    internal static async Task RunAsync(string pre, string declaration, string settings, string expected)
    {
        var source = BuildSource(pre, declaration, !string.IsNullOrEmpty(expected));
        await CreateTest(source, settings, expected).RunAsync().ConfigureAwait(false);
    }
}
