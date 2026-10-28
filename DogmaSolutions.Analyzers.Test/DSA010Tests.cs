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

/// <summary>DSA010 end to end: a lock around a single plain access of a field.</summary>
[TestClass]
public partial class DSA010Tests
{
    internal const string Header = "public class Worker";
    internal const string Gate = "private readonly object _gate = new object();\n";

    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA010Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>One expected diagnostic: the scenario (the suffix of the message resource), the field and the lock the message talks about.</summary>
    internal static string E(string scenario, string field, string lockName) => "DSA010Message" + scenario + "|" + field + "|" + lockName;

    /// <summary>A class with the given members. <c>[[...]]</c> marks the code a diagnostic is expected on, in order (the markup is dropped when nothing is expected).</summary>
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
            return "dotnet_diagnostic.DSA010." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    /// <summary>The declared type of the field, as it is spelled in the source.</summary>
    internal static string GetFieldType(string source, string field)
    {
        var match = Regex.Match(source, @"([\w.]+(?:<[\w, ]+>)?(?:\[\])?\??|\([\w, ]+\))\s+(?:\w+\s*,\s*)*" + Regex.Escape(field) + @"\b\s*[;=,]");
        Assert.IsTrue(match.Success, "Field " + field + " not found");
        return match.Groups[1].Value;
    }

    private static string F(string format, params object[] arguments) => string.Format(Resources.Culture, format, arguments);

    /// <summary>
    /// What the message must say about a type, written here independently of the analyzer: how the CLR reads and writes it, and on which platforms.
    /// </summary>
    internal static (string Sentence, string Remedy) ExpectedTypeText(string type, string source)
    {
        var other = F(Resources.DSA010RemedyOther, type);
        switch (type)
        {
            case "bool": case "byte": case "sbyte": return (F(Resources.DSA010TypeSmall, type, 8), string.Empty);
            case "short": case "ushort": case "char": return (F(Resources.DSA010TypeSmall, type, 16), string.Empty);
            case "int": case "uint": case "float": return (F(Resources.DSA010TypeSmall, type, 32), string.Empty);
            case "System.IntPtr": return (F(Resources.DSA010TypeNativeWord, "nint"), string.Empty);
            case "System.UIntPtr": return (F(Resources.DSA010TypeNativeWord, "nuint"), string.Empty);
            case "string": case "object": case "int[]": case "Worker": return (F(Resources.DSA010TypeReference, type), string.Empty);
            case "Action": return (F(Resources.DSA010TypeReference, "Action"), string.Empty);
            case "Mode": return (F(Resources.DSA010TypeSmallEnum, "Mode", "int", 32), string.Empty);
            case "EventHandler": return (F(Resources.DSA010TypeReference, "EventHandler"), string.Empty);
            case "Kind": return (F(Resources.DSA010TypeSmallEnum, "Kind", "int", 32), string.Empty);
            case "LongKind": return (F(Resources.DSA010TypeWide64Enum, "LongKind", "long"), F(Resources.DSA010RemedyOther, "LongKind"));
            case "long": return (F(Resources.DSA010TypeWide64, type), F(Resources.DSA010RemedyLong, type));
            case "ulong": return (F(Resources.DSA010TypeWide64, type), F(Resources.DSA010RemedyULong, type));
            case "double": return (F(Resources.DSA010TypeWide64, type), F(Resources.DSA010RemedyDouble, type));
            case "decimal": return (F(Resources.DSA010TypeDecimal, type), other);
            case "int?": return (F(Resources.DSA010TypeStruct, "int?"), other);
            case "(int, int)": return (F(Resources.DSA010TypeStruct, "(int, int)"), other);
            case "DateTime": case "TimeSpan": case "Guid": case "Pair": return (F(Resources.DSA010TypeStruct, type), other);
            case "T": return source.Contains("where T : class") ? (F(Resources.DSA010TypeReference, "T"), string.Empty) : (F(Resources.DSA010TypeParameter, "T"), other);
            default: throw new AssertFailedException("No expectation for the type " + type);
        }
    }

    internal static CSharpAnalyzerVerifier<DSA010Analyzer>.Test CreateTest(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA010Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));

        var entries = (expected ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < entries.Length; i++)
        {
            var parts = entries[i].Split('|');
            var (sentence, remedy) = ExpectedTypeText(GetFieldType(source, parts[1]), source);
            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(parts[0], Resources.Culture), parts[1], parts[2], sentence, remedy);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA010Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i).WithMessage(message));
        }

        return test;
    }

    internal static async Task RunAsync(string pre, string header, string members, string settings, string expected)
    {
        var matched = !string.IsNullOrEmpty(expected);
        await CreateTest(BuildSource(pre, header, members, matched), settings, expected).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task A_lock_is_shared_with_the_critical_section_of_another_file_of_a_partial_class()
    {
        var source = BuildSource(string.Empty, "public partial class Worker", Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", expectDiagnostics: true);
        var test = CreateTest(source, string.Empty, E("ReadShared", "_flag", "_gate"));
        test.TestState.Sources.Add(("Other.cs", "namespace TestApp\n{\n    public partial class Worker\n    {\n        private int _a, _b;\n        public void Work() { lock (_gate) { _a++; _b++; } }\n    }\n}"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public void The_messages_state_the_type_and_the_platforms_they_are_true_for()
    {
        // a claim about atomicity is never made without saying for which type, and on which platforms
        StringAssert.Contains(F(Resources.DSA010TypeSmall, "int", 32), "'int' is 32 bits wide");
        StringAssert.Contains(F(Resources.DSA010TypeSmall, "int", 32), "every platform it runs on (32-bit and 64-bit processes alike)");
        StringAssert.Contains(F(Resources.DSA010TypeWide64, "long"), "in a 64-bit process the CLR reads and writes it as one operation, but in a 32-bit process");
        StringAssert.Contains(F(Resources.DSA010TypeDecimal, "decimal"), "on any platform, 32-bit or 64-bit");
        StringAssert.Contains(F(Resources.DSA010TypeStruct, "Pair"), "on any platform, 32-bit or 64-bit");
        StringAssert.Contains(F(Resources.DSA010TypeReference, "string"), "on every platform");
        Assert.IsFalse(Resources.DSA010MessageReadDedicated.Contains("a single read is already atomic"));
        Assert.IsTrue(Resources.DSA010MessageReadDedicated.Contains("{2}"));
    }

    [TestMethod]
    public async Task A_pointer_has_the_size_of_the_native_word()
    {
        // a pointer cannot be a field of the scenarios without unsafe code: its symbol is made
        var references = await ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, default).ConfigureAwait(false);
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("pointers", Array.Empty<SyntaxTree>(), references);
        var pointer = compilation.CreatePointerTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));

        var (sentence, remedy) = DSA010Types.Describe(pointer);

        Assert.AreEqual(F(Resources.DSA010TypeNativeWord, "int*"), sentence);
        Assert.AreEqual(string.Empty, remedy);
    }
}
