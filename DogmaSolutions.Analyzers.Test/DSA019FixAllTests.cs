using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA019FixAllTests
{
    private sealed class Provider : FixAllContext.DiagnosticProvider
    {
        private readonly ImmutableArray<Diagnostic> _diags;
        public Provider(ImmutableArray<Diagnostic> d) { _diags = d; }
        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken ct) => Task.FromResult<IEnumerable<Diagnostic>>(_diags);
        public override Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken ct) => Task.FromResult<IEnumerable<Diagnostic>>(_diags);
        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken ct) => Task.FromResult<IEnumerable<Diagnostic>>(ImmutableArray<Diagnostic>.Empty);
    }

    private static string Header => "namespace S { public class A { public string Name; public A Self; } public class B { public A A = new(); public B Next; } public class C { public B B = new(); public B[] Items; public C Other; }\npublic class Svc {\n";

    private static IEnumerable<(string Name, string Source)> Cases(int n)
    {
        var sb = new StringBuilder(Header + "public void M(C c, C d) {\n");
        for (var i = 0; i < n; i++) sb.Append("var v" + i + " = c.B.A.Name; var w" + i + " = c.B.Next.A.Name + d.B.A.Name + c.Items[" + i + "].A.Name;\n");
        sb.Append("} }}");
        yield return ("stress", sb.ToString());

        sb = new StringBuilder(Header + "public void M(C c, C d) {\n");
        for (var i = 0; i < n; i++) sb.Append("var a" + i + " = c.B.A.Self.Name; var b" + i + " = c.B.A.Self.Self.Name; var e" + i + " = c.B.A.Name;\n");
        sb.Append("} }}");
        yield return ("nested-prefixes", sb.ToString());

        sb = new StringBuilder(Header + "public void M(C c) {\n");
        for (var i = 0; i < n; i++) sb.Append("var a" + i + " = c.B.A.Name;\n");
        sb.Append("foreach (var x in c.Items) { var p = x.A.Self.Name; var q = x.A.Self.Name; }\nforeach (var x in c.Items) { var r = x.A.Self.Name; }\n");
        sb.Append("System.Action act = () => { var s = c.B.A.Name; var t = c.B.A.Name; };\nSystem.Func<string> f = () => c.B.A.Name + c.B.A.Name;\n");
        sb.Append("} public void N(C c) { var z1 = c.B.A.Name; var z2 = c.B.A.Name;  var m1 = c.Other\n   .B.A.Name; var m2 = c.Other.B.A.Name; var k1 = c.Other.B /*x*/ .A.Name; var k2 = c.Other.B.A.Name; var q1 = c.Items[0].A.Name; var q2 = c.Items[1].A.Name; var i1 = c.Items[0].A.Self.Name(); }}}".Replace("Name()", "Name"));
        yield return ("mixed", sb.ToString());
    }

    /// <summary>
    /// The Fix All of DSA019 only skips the diagnostics whose fix is identical to an earlier one: whatever the shape of the code, its
    /// result must be exactly the one of the plain batch fixer.
    /// </summary>
    [TestMethod]
    public async Task FixAll_gives_the_same_result_as_the_batch_fixer()
    {
        foreach (var n in new[] { 1, 6 })
        foreach (var (name, source) in Cases(n))
        {
            using var ws = new AdhocWorkspace();
            var refs = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).Where(p => p.Contains("System.Private.CoreLib") || p.Contains("System.Runtime.dll") || p.Contains("System.Collections.dll")).Select(p => MetadataReference.CreateFromFile(p)).ToArray();
            var project = ws.AddProject("p", LanguageNames.CSharp).WithMetadataReferences(refs);
            var doc = project.AddDocument("a.cs", source);
            var comp = await doc.Project.GetCompilationAsync().ConfigureAwait(false);
            var cwa = comp.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer()));
            var diags = (await cwa.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false)).ToImmutableArray();
            var codeFix = new DSA019CodeFixProvider();
            foreach (var key in new[] { "DSA019", "DSA019_Long", "DSA019_Compact", "DSA019_root" })
            {
                var results = new List<string>();
                foreach (var fixAll in new[] { WellKnownFixAllProviders.BatchFixer, codeFix.GetFixAllProvider() })
                {
                    var ctx = new FixAllContext(doc, codeFix, FixAllScope.Document, key, new[] { "DSA019" }, new Provider(diags), CancellationToken.None);
                    var action = await fixAll.GetFixAsync(ctx).ConfigureAwait(false);
                    var text = "<no fix>";
                    if (action != null)
                    {
                        var ops = await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
                        var changes = ops.OfType<ApplyChangesOperation>().FirstOrDefault();
                        if (changes != null)
                            text = (await changes.ChangedSolution.GetDocument(doc.Id).GetTextAsync().ConfigureAwait(false)).ToString();
                    }

                    results.Add(text);
                }

                Assert.AreEqual(results[0], results[1], name + " / " + key + " / n=" + n);
                if (key == "DSA019")
                    Assert.AreNotEqual(source, results[1], name + ": the fix must change the code");
            }
        }
    }
}
