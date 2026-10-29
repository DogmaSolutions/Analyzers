using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA055 end to end: a key or an identifier compared with zero by equality.</summary>
[TestClass]
public partial class DSA055Tests
{
    internal const string Header = "public class Worker";

    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA055Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>One expected diagnostic: the scenario (the suffix of the message resource), the compared value as written and its type.</summary>
    internal static string E(string scenario, string operand, string type) => "DSA055Message" + scenario + "~" + operand + "~" + type;

    /// <summary><c>[[...]]</c> marks the code a diagnostic is expected on, in order (the markup is dropped when nothing is expected).</summary>
    internal static string BuildSource(string pre, string header, string members, bool expectDiagnostics)
    {
        var index = 0;
        var body = Regex.Replace(members, @"\[\[(.*?)\]\]", match => expectDiagnostics ? "{|#" + index++ + ":" + match.Groups[1].Value + "|}" : match.Groups[1].Value);
        return "using System;\nusing System.Linq;\nnamespace TestApp\n{\n" + pre + "\n    " + header + "\n    {\n" + body + "\n    }\n}";
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic.DSA055." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    internal static CSharpAnalyzerVerifier<DSA055Analyzer>.Test CreateTest(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA055Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));

        var entries = (expected ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < entries.Length; i++)
        {
            var parts = entries[i].Split('~');
            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(parts[0], Resources.Culture), parts[1], parts[2]);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA055Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i).WithMessage(message));
        }

        return test;
    }

    internal static async Task RunAsync(string pre, string header, string members, string settings, string expected)
    {
        var matched = !string.IsNullOrEmpty(expected);
        await CreateTest(BuildSource(pre, header, members, matched), settings, expected).RunAsync().ConfigureAwait(false);
    }
}
