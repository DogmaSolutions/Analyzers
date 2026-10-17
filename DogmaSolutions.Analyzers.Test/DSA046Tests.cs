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

/// <summary>DSA046 end to end: a read-modify-write of a volatile field.</summary>
[TestClass]
public partial class DSA046Tests
{
    internal const string Header = "public class Worker";

    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA046Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>One expected diagnostic: the scenario (the suffix of the message resource), the field and the operator the message talks about.</summary>
    internal static string E(string scenario, string field, string op) => "DSA046Message" + scenario + "~" + field + "~" + op;

    /// <summary><c>[[...]]</c> marks the code a diagnostic is expected on, in order (the markup is dropped when nothing is expected).</summary>
    internal static string BuildSource(string pre, string header, string members, bool expectDiagnostics)
    {
        var index = 0;
        var body = Regex.Replace(members, @"\[\[(.*?)\]\]", match => expectDiagnostics ? "{|#" + index++ + ":" + match.Groups[1].Value + "|}" : match.Groups[1].Value);
        return "using System;\nusing System.Threading;\nnamespace TestApp\n{\n" + pre + "\n    " + header + "\n    {\n" + body + "\n    }\n}";
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic.DSA046." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    /// <summary>The declared type of the field, as it is spelled in the source.</summary>
    internal static string GetFieldType(string source, string field)
    {
        var match = Regex.Match(source, @"volatile\s+([\w.]+)\s+" + Regex.Escape(field) + @"\b");
        Assert.IsTrue(match.Success, "Field " + field + " not found");
        return match.Groups[1].Value;
    }

    /// <summary>What the message must say about Interlocked for the type, written here independently of the analyzer.</summary>
    internal static string ExpectedRemedy(string type)
    {
        string F(string format, string name) => string.Format(Resources.Culture, format, name);
        switch (type)
        {
            case "int": return Resources.DSA046RemedyInt;
            case "uint": return Resources.DSA046RemedyUInt;
            case "float": return Resources.DSA046RemedyFloat;
            case "System.IntPtr": return F(Resources.DSA046RemedyNativeInt, "nint");
            case "System.UIntPtr": return F(Resources.DSA046RemedySmall, "nuint");
            case "byte": case "sbyte": case "short": case "ushort": return F(Resources.DSA046RemedySmall, type);
            case "bool": case "char": return F(Resources.DSA046RemedyNoOverload, type);
            case "Mode": return F(Resources.DSA046RemedyEnum, "Mode");
            default: return string.Empty;
        }
    }

    internal static CSharpAnalyzerVerifier<DSA046Analyzer>.Test CreateTest(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA046Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));

        var entries = (expected ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < entries.Length; i++)
        {
            var parts = entries[i].Split('~');
            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(parts[0], Resources.Culture), parts[1], parts[2], ExpectedRemedy(GetFieldType(source, parts[1])));
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA046Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i).WithMessage(message));
        }

        return test;
    }

    internal static async Task RunAsync(string pre, string header, string members, string settings, string expected)
    {
        var matched = !string.IsNullOrEmpty(expected);
        await CreateTest(BuildSource(pre, header, members, matched), settings, expected).RunAsync().ConfigureAwait(false);
    }
}
