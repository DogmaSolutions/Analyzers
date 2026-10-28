using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA054: the assignment becomes a memoization (<c>??=</c>) when nothing else has to change.</summary>
[TestClass]
public class DSA054CodeFixTests
{
    private const string Build = "private static string Build() => \"x\";\n";

    private static CSharpCodeFixVerifier<DSA054Analyzer, DSA054CodeFixProvider>.Test CreateTest(string source, string fixedSource, int diagnostics, LanguageVersion version = LanguageVersion.Latest)
    {
        var test = new CSharpCodeFixVerifier<DSA054Analyzer, DSA054CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA054CodeFixProvider.MemoizeEquivalenceKey;
        test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
        if (version != LanguageVersion.Latest)
            test.SolutionTransforms.Add((solution, projectId) => solution.WithProjectParseOptions(projectId, new CSharpParseOptions(version)));

        for (var i = 0; i < diagnostics; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA054Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        return test;
    }

    // title, members of the source ([[...]] marks the expected assignments), members of the fixed source
    private static IEnumerable<object[]> GetFixes =>
    [
        ["Property getter", "private string _label;\npublic string Label { get { [[_label = Build()]]; return _label; } }\n" + Build, "private string _label;\npublic string Label { get { _label ??= Build(); return _label; } }\n" + Build],
        ["Method", "private string _label;\npublic string GetLabel() { [[_label = Build()]]; return _label; }\n" + Build, "private string _label;\npublic string GetLabel() { _label ??= Build(); return _label; }\n" + Build],
        ["Returned assignment", "private string _label;\npublic string GetLabel() { return [[_label = Build()]]; }\n" + Build, "private string _label;\npublic string GetLabel() { return _label ??= Build(); }\n" + Build],
        ["Expression bodied property", "private string _label;\npublic string Label => [[_label = Build()]];\n" + Build, "private string _label;\npublic string Label => _label ??= Build();\n" + Build],
        ["Expression bodied accessor", "private string _label;\npublic string Label { get => [[_label = Build()]]; }\n" + Build, "private string _label;\npublic string Label { get => _label ??= Build(); }\n" + Build],
        ["Nullable value type", "private int? _count;\npublic int? Count { get { [[_count = Compute()]]; return _count; } }\nprivate static int Compute() => 3;", "private int? _count;\npublic int? Count { get { _count ??= Compute(); return _count; } }\nprivate static int Compute() => 3;"],
        ["Qualified with this", "private string _label;\npublic string Label { get { [[this._label = Build()]]; return this._label; } }\n" + Build, "private string _label;\npublic string Label { get { this._label ??= Build(); return this._label; } }\n" + Build],
        ["Static field", "private static string _label;\npublic static string Label { get { [[_label = Build()]]; return _label; } }\n" + Build, "private static string _label;\npublic static string Label { get { _label ??= Build(); return _label; } }\n" + Build],
        ["Trivia is kept", "private string _label;\npublic string Label { get { /* c */ [[_label   =   Build()]]; return _label; } }\n" + Build, "private string _label;\npublic string Label { get { /* c */ _label   ??=   Build(); return _label; } }\n" + Build],
        ["Several fixes", "private string _a;\nprivate string _b;\npublic string A => [[_a = Build()]];\npublic string B { get { [[_b = Build()]]; return _b; } }\n" + Build, "private string _a;\nprivate string _b;\npublic string A => _a ??= Build();\npublic string B { get { _b ??= Build(); return _b; } }\n" + Build],
    ];

    public static string GetCaseDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) => CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task The_assignment_becomes_a_memoization(string title, string members, string fixedMembers)
    {
        var diagnostics = members.Split("[[").Length - 1;
        await CreateTest(DSA054Tests.BuildSource(string.Empty, DSA054Tests.Header, members, true), DSA054Tests.BuildSource(string.Empty, DSA054Tests.Header, fixedMembers, false), diagnostics).RunAsync().ConfigureAwait(false);
    }

    // title, members of the source ([[...]] marks the expected assignment): the fix is not offered
    private static IEnumerable<object[]> GetNotOffered =>
    [
        ["A value type that cannot be null", "private int _count;\npublic int Count { get { [[_count = Compute()]]; return _count; } }\nprivate static int Compute() => 3;"],
        ["The assignment is not the first statement", "private string _label;\npublic string GetLabel() { Console.WriteLine(1); [[_label = Build()]]; return _label; }\n" + Build],
        ["The field is assigned twice", "private string _label;\npublic string GetLabel() { [[_label = Build()]]; Console.WriteLine(_label); _label = \"y\"; return _label; }\n" + Build],
        ["The value is not returned", "private string _label;\npublic void Show() { [[_label = Build()]]; Console.WriteLine(_label.Length); }\n" + Build],
        ["The assignment is in a block", "private string _label;\npublic string GetLabel() { { [[_label = Build()]]; } return _label; }\n" + Build],
        ["A constructor", "private string _label;\npublic Worker() { [[_label = Build()]]; Console.WriteLine(_label); }\n" + Build],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotOffered), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task The_memoization_is_not_offered(string title, string members)
    {
        var test = CreateTest(DSA054Tests.BuildSource(string.Empty, DSA054Tests.Header, members, true), DSA054Tests.BuildSource(string.Empty, DSA054Tests.Header, members, true), 1);
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA054Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task The_memoization_is_not_offered_before_C_Sharp_8()
    {
        var members = "private string _label;\npublic string GetLabel() { [[_label = Build()]]; return _label; }\n" + Build;
        var test = CreateTest(DSA054Tests.BuildSource(string.Empty, DSA054Tests.Header, members, true), DSA054Tests.BuildSource(string.Empty, DSA054Tests.Header, members, true), 1, LanguageVersion.CSharp7_3);
        test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA054Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }
}
