using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA055: the comparison with zero becomes the comparison a key is expected to satisfy.</summary>
[TestClass]
public class DSA055CodeFixTests
{
    private static CSharpCodeFixVerifier<DSA055Analyzer, DSA055CodeFixProvider>.Test CreateTest(string members, string fixedMembers, int diagnostics)
    {
        var test = new CSharpCodeFixVerifier<DSA055Analyzer, DSA055CodeFixProvider>.Test();
        test.TestCode = DSA055Tests.BuildSource(string.Empty, DSA055Tests.Header, members, true);
        test.FixedCode = DSA055Tests.BuildSource(string.Empty, DSA055Tests.Header, fixedMembers, false);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA055CodeFixProvider.ReplaceEquivalenceKey;
        test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
        for (var i = 0; i < diagnostics; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA055Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        return test;
    }

    // title, members of the source ([[...]] marks the expected comparisons), members of the fixed source
    private static IEnumerable<object[]> GetFixes =>
    [
        ["Not equal", "public bool IsValidKey(long resolvedKey) { return [[resolvedKey != 0]]; }", "public bool IsValidKey(long resolvedKey) { return resolvedKey > 0; }"],
        ["Equal", "public bool IsMissing(long resolvedKey) { return [[resolvedKey == 0]]; }", "public bool IsMissing(long resolvedKey) { return resolvedKey <= 0; }"],
        ["In an if", "public bool IsValidKey(long resolvedKey) { if ([[resolvedKey != 0]]) return true; else return false; }", "public bool IsValidKey(long resolvedKey) { if (resolvedKey > 0) return true; else return false; }"],
        ["Zero on the left", "public bool Has(int id) { return [[0 != id]]; }", "public bool Has(int id) { return id > 0; }"],
        ["Zero on the left, equality", "public bool Has(int id) { return [[0 == id]]; }", "public bool Has(int id) { return id <= 0; }"],
        ["Typed zero", "public bool Has(long id) { return [[id != 0L]]; }", "public bool Has(long id) { return id > 0; }"],
        ["Parenthesized operand", "public bool Has(long id) { return [[(id) != 0]]; }", "public bool Has(long id) { return (id) > 0; }"],
        ["Member access", "public long CustomerId { get; set; }\npublic bool Has(Worker o) { return [[o.CustomerId != 0]]; }", "public long CustomerId { get; set; }\npublic bool Has(Worker o) { return o.CustomerId > 0; }"],
        ["Is pattern", "public bool Has(long id) { return [[id is 0]]; }", "public bool Has(long id) { return id <= 0; }"],
        ["Is not pattern", "public bool Has(long id) { return [[id is not 0]]; }", "public bool Has(long id) { return id > 0; }"],
        ["Unsigned key", "public bool Has(uint id) { return [[id != 0]]; }", "public bool Has(uint id) { return id > 0; }"],
        ["Spacing is normalized", "public bool Has(long id) { return [[id!=0]]; }", "public bool Has(long id) { return id > 0; }"],
        ["Comments around are kept", "public bool Has(long id) { return /* a */ [[id != 0]] /* b */; }", "public bool Has(long id) { return /* a */ id > 0 /* b */; }"],
        ["Several fixes", "public bool Has(long id, long parentKey) { return [[id != 0]] && [[parentKey == 0]]; }", "public bool Has(long id, long parentKey) { return id > 0 && parentKey <= 0; }"],
        ["In a negation", "public bool Has(long id) { return !([[id != 0]]); }", "public bool Has(long id) { return !(id > 0); }"],
    ];

    public static string GetCaseDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) => CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task The_comparison_is_replaced(string title, string members, string fixedMembers)
    {
        var diagnostics = members.Split("[[").Length - 1;
        await CreateTest(members, fixedMembers, diagnostics).RunAsync().ConfigureAwait(false);
    }
}
