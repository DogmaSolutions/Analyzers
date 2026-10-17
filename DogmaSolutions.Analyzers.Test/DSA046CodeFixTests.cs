using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA046: the operation becomes an <c>Interlocked</c> call.</summary>
[TestClass]
public class DSA046CodeFixTests
{
    private static string Wrap(string members) => "namespace TestApp\n{\n    public class Worker\n    {\n        private volatile int _n;\n        private volatile uint _u;\n" + members + "\n    }\n}";

    // title, members of the source, members of the fixed source
    private static IEnumerable<object[]> GetFixes =>
    [
        ["Post increment", "        public void Hit() { {|#0:_n++|}; }", "        public void Hit() { System.Threading.Interlocked.Increment(ref _n); }"],
        ["Pre increment", "        public void Hit() { {|#0:++_n|}; }", "        public void Hit() { System.Threading.Interlocked.Increment(ref _n); }"],
        ["Post decrement", "        public void Hit() { {|#0:_n--|}; }", "        public void Hit() { System.Threading.Interlocked.Decrement(ref _n); }"],
        ["Pre decrement", "        public void Hit() { {|#0:--_n|}; }", "        public void Hit() { System.Threading.Interlocked.Decrement(ref _n); }"],
        ["Unsigned increment", "        public void Hit() { {|#0:_u++|}; }", "        public void Hit() { System.Threading.Interlocked.Increment(ref _u); }"],
        ["Add", "        public void Hit(int v) { {|#0:_n += v|}; }", "        public void Hit(int v) { System.Threading.Interlocked.Add(ref _n, v); }"],
        ["Subtract", "        public void Hit(int v) { {|#0:_n -= v + 1|}; }", "        public void Hit(int v) { System.Threading.Interlocked.Add(ref _n, -(v + 1)); }"],
        ["Unsigned add", "        public void Hit(uint v) { {|#0:_u += v|}; }", "        public void Hit(uint v) { System.Threading.Interlocked.Add(ref _u, v); }"],
        ["Through this", "        public void Hit() { {|#0:this._n++|}; }", "        public void Hit() { System.Threading.Interlocked.Increment(ref this._n); }"],
        ["Through another instance", "        public void Hit(Worker o) { {|#0:o._n += 2|}; }", "        public void Hit(Worker o) { System.Threading.Interlocked.Add(ref o._n, 2); }"],
    ];

    // title, members: the operations the fix is not offered for (the review comment still is)
    private static IEnumerable<object[]> GetNoFixes =>
    [
        ["Value is used", "        public int Hit() { return {|#0:_n++|}; }"],
        ["Unsigned subtract", "        public void Hit(uint v) { {|#0:_u -= v|}; }"],
        ["Multiply", "        public void Hit() { {|#0:_n *= 2|}; }"],
        ["Self assignment", "        public void Hit() { {|#0:_n = _n + 1|}; }"],
        ["Other value type", "        private volatile bool _b;\n        public void Hit() { {|#0:_b = !_b|}; }"],
        ["Delegate", "        private volatile System.EventHandler _h;\n        public void Hit(System.EventHandler h) { {|#0:_h += h|}; }"],
        ["Increment of a reference type", "        public class Level { public static Level operator ++(Level l) => l; }\n        private volatile Level _l;\n        public void Hit() { {|#0:_l++|}; }"],
    ];

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(DSA046Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA046Tests))]
    public async Task The_operation_becomes_an_interlocked_call(string title, string members, string fixedMembers)
    {
        var test = new CSharpCodeFixVerifier<DSA046Analyzer, DSA046CodeFixProvider>.Test();
        test.TestCode = Wrap(members);
        test.FixedCode = Wrap(fixedMembers);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA046CodeFixProvider.UseInterlockedEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", DSA046Tests.ToEditorConfig(string.Empty)));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA046Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    [DynamicData(nameof(GetNoFixes), DynamicDataDisplayName = nameof(DSA046Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA046Tests))]
    public async Task No_interlocked_fix_is_offered_when_it_would_change_the_meaning(string title, string members)
    {
        // the fixed code is the code itself: the verifier checks that the Interlocked action is not offered
        var test = new CSharpCodeFixVerifier<DSA046Analyzer, DSA046CodeFixProvider>.Test();
        test.TestCode = Wrap(members);
        test.FixedCode = Wrap(members);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA046CodeFixProvider.UseInterlockedEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", DSA046Tests.ToEditorConfig(string.Empty)));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA046Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
        test.FixedState.ExpectedDiagnostics.AddRange(test.ExpectedDiagnostics);

        await test.RunAsync().ConfigureAwait(false);
    }

    // title, members: the framework (.NET Standard 2.0, as .NET Framework) has no Interlocked overload for uint, and has the ones for int
    [TestMethod]
    [DataRow("Unsigned increment", "        public void Hit() { {|#0:_u++|}; }", false)]
    [DataRow("Unsigned add", "        public void Hit(uint v) { {|#0:_u += v|}; }", false)]
    [DataRow("Signed increment", "        public void Hit() { {|#0:_n++|}; }", true)]
    public async Task The_unsigned_overloads_are_used_only_where_the_framework_has_them(string title, string members, bool offered)
    {
        var test = new CSharpCodeFixVerifier<DSA046Analyzer, DSA046CodeFixProvider>.Test();
        test.TestCode = Wrap(members);
        test.FixedCode = offered ? Wrap(members.Replace("{|#0:_n++|}", "System.Threading.Interlocked.Increment(ref _n)")) : Wrap(members);
        test.ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20;
        test.CodeActionEquivalenceKey = DSA046CodeFixProvider.UseInterlockedEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", DSA046Tests.ToEditorConfig(string.Empty)));
        test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA046Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
        if (!offered)
            test.FixedState.ExpectedDiagnostics.AddRange(test.ExpectedDiagnostics);

        await test.RunAsync().ConfigureAwait(false);
    }
}
