using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA053: the call becomes the one that answers the existence question.</summary>
[TestClass]
public class DSA053CodeFixTests
{
    private const string Sync = "public bool Has(IEnumerable<Item> items, int id, bool flag) { ";
    private const string Async = "public async Task<bool> HasAsync(IQueryable<Item> q, int id, bool flag, CancellationToken ct) { ";

    private static CSharpCodeFixVerifier<DSA053Analyzer, DSA053CodeFixProvider>.Test CreateTest(string source, string fixedSource, string settings, int diagnostics)
    {
        var test = new CSharpCodeFixVerifier<DSA053Analyzer, DSA053CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA053CodeFixProvider.UseAnyEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ExistenceTestSupport.ToEditorConfig("DSA053", settings)));
        for (var i = 0; i < diagnostics; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA053Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        return test;
    }

    // title, members of the source ([[...]] marks the expected calls), members of the fixed source, settings
    private static IEnumerable<object[]> GetFixes =>
    [
        ["Variable, greater than zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n > 0; }", Sync + "var hasItems = items.Any(x => x.Id == id); return hasItems; }", ""],
        ["Variable, without a predicate", Sync + "var n = [[items.Count()]]; return n > 0; }", Sync + "var hasItems = items.Any(); return hasItems; }", ""],
        ["Variable, equal to zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n == 0; }", Sync + "var hasItems = items.Any(x => x.Id == id); return !hasItems; }", ""],
        ["Variable, not equal to zero in a condition", Sync + "var n = [[items.Count(x => x.Id == id)]]; if (n != 0) { return true; } return false; }", Sync + "var hasItems = items.Any(x => x.Id == id); if (hasItems) { return true; } return false; }", ""],
        ["Variable, zero on the left", Sync + "var n = [[items.Count(x => x.Id == id)]]; return 0 < n; }", Sync + "var hasItems = items.Any(x => x.Id == id); return hasItems; }", ""],
        ["Variable, a pattern", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is > 0; }", Sync + "var hasItems = items.Any(x => x.Id == id); return hasItems; }", ""],
        ["Variable, a negative pattern", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is 0; }", Sync + "var hasItems = items.Any(x => x.Id == id); return !hasItems; }", ""],
        ["Variable, several tests", Sync + "var n = [[items.Count(x => x.Id == id)]]; if (n == 0) { return false; } if (n > 0 && flag) { return true; } return n >= 1; }", Sync + "var hasItems = items.Any(x => x.Id == id); if (!hasItems) { return false; } if (hasItems && flag) { return true; } return hasItems; }", ""],
        ["Variable, tested in a comparison", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n == 0 == flag; }", Sync + "var hasItems = items.Any(x => x.Id == id); return (!hasItems) == flag; }", ""],
        ["Variable, with its type", Sync + "int n = [[items.Count(x => x.Id == id)]]; return n > 0; }", Sync + "bool hasItems = items.Any(x => x.Id == id); return hasItems; }", ""],
        ["Variable, LongCount", Sync + "long n = [[items.LongCount(x => x.Id == id)]]; return n > 0; }", Sync + "bool hasItems = items.Any(x => x.Id == id); return hasItems; }", ""],
        ["Variable, the name is taken", Sync + "var hasItems = true; var n = [[items.Count(x => x.Id == id)]]; return n > 0 && hasItems; }", Sync + "var hasItems = true; var hasItems2 = items.Any(x => x.Id == id); return hasItems2 && hasItems; }", ""],
        ["Variable, query", "public bool Has(IQueryable<Item> q, int id) { var n = [[q.Count(x => x.Id == id)]]; return n > 0; }", "public bool Has(IQueryable<Item> q, int id) { var hasItems = q.Any(x => x.Id == id); return hasItems; }", ""],
        ["Variable, async", Async + "var n = await [[q.CountAsync(x => x.Id == id, ct)]]; return n > 0; }", Async + "var hasItems = await q.AnyAsync(x => x.Id == id, ct); return hasItems; }", ""],
        ["Variable, async with ConfigureAwait", Async + "var n = await [[q.LongCountAsync(ct)]].ConfigureAwait(false); return n == 0; }", Async + "var hasItems = await q.AnyAsync(ct).ConfigureAwait(false); return !hasItems; }", ""],
        ["Direct, async with a predicate", Async + "return await [[q.CountAsync(x => x.Id == id, ct)]] > 0; }", Async + "return await q.AnyAsync(x => x.Id == id, ct); }", ""],
        ["Direct, async equal to zero", Async + "return (await [[q.CountAsync(x => x.Id == id, ct)]]) == 0; }", Async + "return !await q.AnyAsync(x => x.Id == id, ct); }", ""],
        ["Direct, async in a logical expression", Async + "return flag && await [[q.LongCountAsync(x => x.Id == id, ct)]].ConfigureAwait(false) == 0; }", Async + "return flag && !await q.AnyAsync(x => x.Id == id, ct).ConfigureAwait(false); }", ""],
        ["Direct, sync, asked", Sync + "return [[items.Count(x => x.Id == id)]] > 0; }", Sync + "return items.Any(x => x.Id == id); }", "report_direct_comparisons=true"],
        ["Direct, sync, a pattern", Sync + "return [[items.Count(x => x.Id == id)]] is 0; }", Sync + "return !items.Any(x => x.Id == id); }", "report_direct_comparisons=true"],
        ["Direct, sync, as the operand of a comparison", Sync + "return [[items.Count(x => x.Id == id)]] == 0 == flag; }", Sync + "return (!items.Any(x => x.Id == id)) == flag; }", "report_direct_comparisons=true"],
        ["Two variables", Sync + "var a = [[items.Count(x => x.Id == id)]]; var b = [[items.Count(x => x.Id != id)]]; return a > 0 && b == 0; }", Sync + "var hasItems = items.Any(x => x.Id == id); var hasItems2 = items.Any(x => x.Id != id); return hasItems && !hasItems2; }", ""],
        ["Two tests", Async + "return await [[q.CountAsync(x => x.Id == id, ct)]] > 0 && await [[q.CountAsync(x => x.Id != id, ct)]] == 0; }", Async + "return await q.AnyAsync(x => x.Id == id, ct) && !await q.AnyAsync(x => x.Id != id, ct); }", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(DSA053Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA053Tests))]
    public async Task The_call_becomes_the_existence_question(string title, string members, string fixedMembers, string settings)
    {
        var source = ExistenceTestSupport.BuildSource(members, expectDiagnostics: true);
        var diagnostics = System.Text.RegularExpressions.Regex.Matches(members, @"\[\[").Count;

        await CreateTest(source, ExistenceTestSupport.BuildSource(fixedMembers, expectDiagnostics: false), settings, diagnostics).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Nothing_is_reported_when_the_provider_has_no_existence_method()
    {
        var source = "using System.Linq;\nusing System.Threading.Tasks;\nnamespace TestApp\n{\n    public class Item { }\n    public static class Provider { public static Task<int> CountAsync<T>(this IQueryable<T> source) => null; }\n    public class Repository\n    {\n        public async Task<bool> HasAsync(IQueryable<Item> q) { var n = await q.CountAsync(); return n > 0; }\n    }\n}";

        await CSharpAnalyzerVerifier<DSA053Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Entity_Framework_Core_has_the_methods()
    {
        var source = "using System.Linq;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing Microsoft.EntityFrameworkCore;\nnamespace TestApp\n{\n    public class Item { public int Id { get; set; } }\n    public class Repository\n    {\n        public async Task<bool> HasAsync(IQueryable<Item> q, int id, CancellationToken ct) { return await {|#0:q.CountAsync(x => x.Id == id, ct)|} > 0; }\n    }\n}";
        var fixedSource = source.Replace("{|#0:q.CountAsync(x => x.Id == id, ct)|} > 0", "q.AnyAsync(x => x.Id == id, ct)");

        var test = CreateTest(source, fixedSource, string.Empty, 1);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(System.Collections.Immutable.ImmutableArray.Create(new PackageIdentity("Microsoft.EntityFrameworkCore", "8.0.10")));
        await test.RunAsync().ConfigureAwait(false);
    }
}
