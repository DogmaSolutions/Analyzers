using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA044: Console / Debug writes become calls of a Microsoft.Extensions.Logging logger.</summary>
[TestClass]
public class DSA044CodeFixTests
{
    private const string LoggerField = "private Microsoft.Extensions.Logging.ILogger _logger;";

    // title, member declarations before the method, parameters of the method, statement before, statement after
    private static IEnumerable<object[]> GetFixes =>
    [
        ["Console.WriteLine of a literal", LoggerField, "", "Console.WriteLine(\"hello\")", "_logger.LogInformation(\"hello\")"],
        ["Console.Write of a literal", LoggerField, "", "Console.Write(\"hello\")", "_logger.LogInformation(\"hello\")"],
        ["Format and arguments are kept", LoggerField, "int count", "Console.WriteLine(\"n={0}\", count)", "_logger.LogInformation(\"n={0}\", count)"],
        ["A value becomes the value of a template", LoggerField, "Item file", "Console.WriteLine(file.Name)", "_logger.LogInformation(\"{Value}\", file.Name)"],
        ["Interpolated string with names and a format", LoggerField, "int count, Item file", "Console.WriteLine($\"read {count:N0} of {file.Name} {{ok}}\")", "_logger.LogInformation(\"read {Count:N0} of {Name} {{ok}}\", count, file.Name)"],
        ["Interpolated string with an expression", LoggerField, "int count", "Console.WriteLine($\"next {count + 1}\")", "_logger.LogInformation(\"next {Value}\", count + 1)"],
        ["Interpolated string without holes", LoggerField, "", "Console.WriteLine($\"plain\")", "_logger.LogInformation(\"plain\")"],
        ["Interpolated string with an alignment", LoggerField, "int count", "Console.WriteLine($\"x {count,5}\")", "_logger.LogInformation(\"{Value}\", $\"x {count,5}\")"],
        ["Verbatim interpolated string", LoggerField, "int count", "Console.WriteLine($@\"x {count}\")", "_logger.LogInformation(\"{Value}\", $@\"x {count}\")"],
        ["Debug with a format and arguments", LoggerField, "int count", "Debug.WriteLine(\"n={0}\", count)", "_logger.LogDebug(\"n={0}\", count)"],
        ["Method call in a hole", LoggerField, "Item file", "Console.WriteLine($\"x {file.Name.ToUpper()}\")", "_logger.LogInformation(\"x {Value}\", file.Name.ToUpper())"],
        ["Conditional in a hole", LoggerField, "bool ok", "Console.WriteLine($\"x {(ok ? \"y\" : \"n\")}\")", "_logger.LogInformation(\"x {Value}\", (ok ? \"y\" : \"n\"))"],
        ["Concatenation is a value", LoggerField, "int count", "Console.WriteLine(\"a\" + count)", "_logger.LogInformation(\"{Value}\", \"a\" + count)"],
        ["Several holes of the same name", LoggerField, "int count", "Console.WriteLine($\"{count} {count}\")", "_logger.LogInformation(\"{Count} {Count}\", count, count)"],
        ["Debug.Write of a value", LoggerField, "int count", "Debug.Write(count)", "_logger.LogDebug(\"{Value}\", count)"],
        ["Escapes in an interpolated string", LoggerField, "int count", "Console.WriteLine($\"a\\t\\\"{count}\\\"\")", "_logger.LogInformation(\"a\\t\\\"{Count}\\\"\", count)"],
        ["Debug is a debug entry", LoggerField, "", "Debug.WriteLine(\"d\")", "_logger.LogDebug(\"d\")"],
        ["Console.Error is an error entry", LoggerField, "", "Console.Error.WriteLine(\"e\")", "_logger.LogError(\"e\")"],
        ["Console.Out is an information entry", LoggerField, "", "Console.Out.WriteLine(\"o\")", "_logger.LogInformation(\"o\")"],
        ["The logger is a parameter", string.Empty, "Microsoft.Extensions.Logging.ILogger log", "Console.WriteLine(\"p\")", "log.LogInformation(\"p\")"],
        ["The logger is a typed logger", "private Microsoft.Extensions.Logging.ILogger<Service> _typed;", "", "Console.WriteLine(\"t\")", "_typed.LogInformation(\"t\")"],
    ];

    private static string BuildSource(string members, string parameters, string statement, bool withUsing, bool flagged) =>
        "using System;\nusing System.Diagnostics;\n" + (withUsing ? "using Microsoft.Extensions.Logging;\n" : string.Empty) + "namespace TestApp\n{\n    public class Item { public string Name; }\n" +
        "    public class Service\n    {\n        " + members + "\n        public void Run(" + parameters + ")\n        {\n            " + (flagged ? "{|#0:" + statement + "|}" : statement) + ";\n        }\n    }\n}";

    private static CSharpCodeFixVerifier<DSA044Analyzer, DSA044CodeFixProvider>.Test CreateTest(string source, string fixedSource)
    {
        var test = new CSharpCodeFixVerifier<DSA044Analyzer, DSA044CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.Sources.Add(("Fakes.cs", DSA044TestSupport.FakeLoggersSource));
        test.FixedState.Sources.Add(("Fakes.cs", DSA044TestSupport.FakeLoggersSource));
        test.CodeActionEquivalenceKey = DSA044CodeFixProvider.ReplaceEquivalenceKey;
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA044Analyzer.DiagnosticId, DiagnosticSeverity.Info).WithLocation(0));
        return test;
    }

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(DSA044Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA044Tests))]
    public async Task Replaces_the_write_with_a_call_of_the_logger_and_adds_the_using(string title, string members, string parameters, string before, string after)
    {
        var test = CreateTest(
            BuildSource(members, parameters, before, withUsing: false, flagged: true),
            BuildSource(members, parameters, after, withUsing: true, flagged: false));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Does_not_add_a_using_that_is_already_there()
    {
        var test = CreateTest(
            BuildSource(LoggerField, string.Empty, "Console.WriteLine(\"hello\")", withUsing: true, flagged: true),
            BuildSource(LoggerField, string.Empty, "_logger.LogInformation(\"hello\")", withUsing: true, flagged: false));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Does_not_add_a_using_that_is_already_inside_the_namespace()
    {
        const string before = "using System;\nnamespace TestApp\n{\n    using Microsoft.Extensions.Logging;\n    public class Service\n    {\n        private ILogger _logger;\n        public void Run()\n        {\n            {|#0:Console.WriteLine(\"hello\")|};\n        }\n    }\n}";
        var test = CreateTest(before, before.Replace("{|#0:Console.WriteLine(\"hello\")|}", "_logger.LogInformation(\"hello\")"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_every_call_of_the_document_at_once()
    {
        const string members = "private Microsoft.Extensions.Logging.ILogger _logger;\n        public void Other() { {|#1:Debug.WriteLine(\"b\")|}; }";
        var before = BuildSource(members, string.Empty, "Console.WriteLine(\"a\")", withUsing: false, flagged: true);
        var after = BuildSource("private Microsoft.Extensions.Logging.ILogger _logger;\n        public void Other() { _logger.LogDebug(\"b\"); }", string.Empty, "_logger.LogInformation(\"a\")", withUsing: true, flagged: false);
        var test = CreateTest(before, after);
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA044Analyzer.DiagnosticId, DiagnosticSeverity.Info).WithLocation(1));

        await test.RunAsync().ConfigureAwait(false);
    }

    // ---- what is not fixed: the review comment is still offered ----------------------------------------------------

    [TestMethod]
    [DataRow("Console.WriteLine()", "Microsoft.Extensions.Logging.ILogger", "no argument")]
    [DataRow("Console.WriteLine(new char[] { 'a' }, 0, 1)", "Microsoft.Extensions.Logging.ILogger", "a buffer")]
    [DataRow("Console.WriteLine(format: \"{0}\", arg0: 1)", "Microsoft.Extensions.Logging.ILogger", "named arguments")]
    [DataRow("Debug.WriteLine(\"hello\", \"category\")", "Microsoft.Extensions.Logging.ILogger", "a debug category, not a value")]
    [DataRow("Debug.Write(\"hello\", \"category\")", "Microsoft.Extensions.Logging.ILogger", "a debug category, not a value")]
    [DataRow("Console.WriteLine(\"hello\")", "Serilog.ILogger", "a logger that is not a Microsoft one")]
    [DataRow("Console.WriteLine(\"hello\")", "Microsoft.Extensions.Logging.ILoggerFactory", "a factory, not a logger")]
    public async Task Offers_only_the_review_comment_when_there_is_no_safe_replacement(string statement, string loggerType, string reason)
    {
        var titles = await GetTitlesAsync(BuildSource("private " + loggerType + " _logger;", string.Empty, statement, withUsing: false, flagged: false)).ConfigureAwait(false);

        CollectionAssert.AreEqual(new[] { "[DSA044] Add comment for code review" }, titles, reason);
    }

    [TestMethod]
    public async Task Offers_the_replacement_and_the_review_comment_when_it_is_safe()
    {
        var titles = await GetTitlesAsync(BuildSource(LoggerField, string.Empty, "Console.WriteLine(\"hello\")", withUsing: false, flagged: false)).ConfigureAwait(false);

        CollectionAssert.AreEqual(new[] { "Replace with a call of the logger", "[DSA044] Add comment for code review" }, titles);
    }

    private static async Task<string[]> GetTitlesAsync(string source)
    {
        using var workspace = new AdhocWorkspace();
        var references = DSA043TestSupport.CreateCompilation().References;
        var project = workspace.AddProject("p", LanguageNames.CSharp).WithMetadataReferences(references);
        project = project.AddDocument("fakes.cs", DSA044TestSupport.FakeLoggersSource).Project;
        var document = project.AddDocument("a.cs", source);

        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var diagnostic = (await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DSA044Analyzer())).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false)).Single();

        var titles = new List<string>();
        await new DSA044CodeFixProvider()
            .RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => titles.Add(action.Title), CancellationToken.None))
            .ConfigureAwait(false);
        return titles.ToArray();
    }
}
