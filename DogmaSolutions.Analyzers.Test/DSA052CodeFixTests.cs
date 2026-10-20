using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA052: the call becomes the one that answers the existence question.</summary>
[TestClass]
public class DSA052CodeFixTests
{
    private const string Sync = "public bool Has(IEnumerable<Item> items, int id, bool flag) { ";
    private const string Async = "public async Task<bool> HasAsync(IQueryable<Item> q, int id, bool flag, CancellationToken ct) { ";

    private static CSharpCodeFixVerifier<DSA052Analyzer, DSA052CodeFixProvider>.Test CreateTest(string source, string fixedSource, string settings, int diagnostics)
    {
        var test = new CSharpCodeFixVerifier<DSA052Analyzer, DSA052CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA052CodeFixProvider.UseAnyEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ExistenceTestSupport.ToEditorConfig("DSA052", settings)));
        for (var i = 0; i < diagnostics; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA052Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        return test;
    }

    // title, members of the source ([[...]] marks the expected calls), members of the fixed source, settings
    private static IEnumerable<object[]> GetFixes =>
    [
        ["Not null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", Sync + "return items.Any(x => x.Id == id); }", ""],
        ["Null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] == null; }", Sync + "return !items.Any(x => x.Id == id); }", ""],
        ["Is null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is null; }", Sync + "return !items.Any(x => x.Id == id); }", ""],
        ["Is not null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is not null; }", Sync + "return items.Any(x => x.Id == id); }", ""],
        ["Is an empty property pattern", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is { }; }", Sync + "return items.Any(x => x.Id == id); }", ""],
        ["Null on the left", Sync + "return null != [[items.FirstOrDefault(x => x.Id == id)]]; }", Sync + "return items.Any(x => x.Id == id); }", ""],
        ["Without a predicate", Sync + "return [[items.FirstOrDefault()]] != null; }", Sync + "return items.Any(); }", ""],
        ["In parentheses", Sync + "return ([[items.FirstOrDefault(x => x.Id == id)]]) != null; }", Sync + "return items.Any(x => x.Id == id); }", ""],
        ["In a condition", Sync + "if ([[items.FirstOrDefault(x => x.Id == id)]] == null) { return false; } return true; }", Sync + "if (!items.Any(x => x.Id == id)) { return false; } return true; }", ""],
        ["In a logical expression", Sync + "return flag && [[items.FirstOrDefault(x => x.Id == id)]] == null; }", Sync + "return flag && !items.Any(x => x.Id == id); }", ""],
        ["As an operand of a comparison", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] == null == flag; }", Sync + "return (!items.Any(x => x.Id == id)) == flag; }", ""],
        ["As the value of a variable", Sync + "var missing = [[items.FirstOrDefault(x => x.Id == id)]] == null; return missing; }", Sync + "var missing = !items.Any(x => x.Id == id); return missing; }", ""],
        ["As an argument", Sync + "return string.Equals(flag.ToString(), ([[items.FirstOrDefault(x => x.Id == id)]] != null).ToString()); }", Sync + "return string.Equals(flag.ToString(), (items.Any(x => x.Id == id)).ToString()); }", ""],
        ["The call as a static method", Sync + "return [[Enumerable.FirstOrDefault(items, x => x.Id == id)]] != null; }", Sync + "return Enumerable.Any(items, x => x.Id == id); }", ""],
        ["With an explicit type argument", Sync + "return [[items.FirstOrDefault<Item>(x => x.Id == id)]] != null; }", Sync + "return items.Any<Item>(x => x.Id == id); }", ""],
        ["SingleOrDefault", Sync + "return [[items.SingleOrDefault(x => x.Id == id)]] != null; }", Sync + "return items.Any(x => x.Id == id); }", ""],
        ["On a query", "public bool Has(IQueryable<Item> q, int id) { return [[q.FirstOrDefault(x => x.Id == id)]] == null; }", "public bool Has(IQueryable<Item> q, int id) { return !q.Any(x => x.Id == id); }", ""],
        ["Async, not null", Async + "return await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]] != null; }", Async + "return await q.AnyAsync(x => x.Id == id, ct); }", ""],
        ["Async, null in parentheses", Async + "return (await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]]) == null; }", Async + "return !await q.AnyAsync(x => x.Id == id, ct); }", ""],
        ["Async, ConfigureAwait", Async + "return await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]].ConfigureAwait(false) is null; }", Async + "return !await q.AnyAsync(x => x.Id == id, ct).ConfigureAwait(false); }", ""],
        ["Async, without a predicate", Async + "return await [[q.FirstOrDefaultAsync(ct)]] is not null; }", Async + "return await q.AnyAsync(ct); }", ""],
        ["Async, SingleOrDefaultAsync", Async + "return await [[q.SingleOrDefaultAsync(x => x.Id == id, ct)]] != null; }", Async + "return await q.AnyAsync(x => x.Id == id, ct); }", ""],
        ["Async, in a logical expression", Async + "return flag && await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]] == null; }", Async + "return flag && !await q.AnyAsync(x => x.Id == id, ct); }", ""],
        ["Async, as the operand of a comparison", Async + "return await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]] == null == flag; }", Async + "return (!await q.AnyAsync(x => x.Id == id, ct)) == flag; }", ""],
        ["In an expression-bodied member", "public bool Has(IEnumerable<Item> items, int id) => [[items.FirstOrDefault(x => x.Id == id)]] != null;", "public bool Has(IEnumerable<Item> items, int id) => items.Any(x => x.Id == id);", ""],
        ["The trivia stay", Sync + "return /* any? */ [[items.FirstOrDefault(x => x.Id == id)]] != null; /* done */ }", Sync + "return /* any? */ items.Any(x => x.Id == id); /* done */ }", ""],
        ["Variable, tested for null", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; if (item == null) { return false; } return true; }", Sync + "var itemExists = items.Any(x => x.Id == id); if (!itemExists) { return false; } return true; }", ""],
        ["Variable, with its type", Sync + "Item item = [[items.FirstOrDefault(x => x.Id == id)]]; return item != null; }", Sync + "bool itemExists = items.Any(x => x.Id == id); return itemExists; }", ""],
        ["Variable, several tests", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; if (item is null) { return false; } if (item != null && flag) { return true; } return item is not null; }", Sync + "var itemExists = items.Any(x => x.Id == id); if (!itemExists) { return false; } if (itemExists && flag) { return true; } return itemExists; }", ""],
        ["Variable, tested in a comparison", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; return item == null == flag; }", Sync + "var itemExists = items.Any(x => x.Id == id); return (!itemExists) == flag; }", ""],
        ["Variable, in parentheses", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; return (item) != null; }", Sync + "var itemExists = items.Any(x => x.Id == id); return itemExists; }", ""],
        ["Variable, the name is taken", Sync + "var itemExists = true; var item = [[items.FirstOrDefault(x => x.Id == id)]]; return item != null && itemExists; }", Sync + "var itemExists = true; var itemExists2 = items.Any(x => x.Id == id); return itemExists2 && itemExists; }", ""],
        ["Variable, the name is taken in the member", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; Func<bool> f = () => { var itemExists = true; return itemExists; }; return item != null && f(); }", Sync + "var itemExists2 = items.Any(x => x.Id == id); Func<bool> f = () => { var itemExists = true; return itemExists; }; return itemExists2 && f(); }", ""],
        ["Variable, async", Async + "var item = await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]]; return item == null; }", Async + "var itemExists = await q.AnyAsync(x => x.Id == id, ct); return !itemExists; }", ""],
        ["Variable, async with ConfigureAwait", Async + "var item = await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]].ConfigureAwait(false); return item != null; }", Async + "var itemExists = await q.AnyAsync(x => x.Id == id, ct).ConfigureAwait(false); return itemExists; }", ""],
        ["Variable, SingleOrDefault", Sync + "var found = [[items.SingleOrDefault(x => x.Id == id)]]; return found != null; }", Sync + "var foundExists = items.Any(x => x.Id == id); return foundExists; }", ""],
        ["Two tests", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null && [[items.FirstOrDefault(x => x.Id != id)]] == null; }", Sync + "return items.Any(x => x.Id == id) && !items.Any(x => x.Id != id); }", ""],
        ["Two variables", Sync + "var a = [[items.FirstOrDefault(x => x.Id == id)]]; var b = [[items.FirstOrDefault(x => x.Id != id)]]; return a != null && b == null; }", Sync + "var aExists = items.Any(x => x.Id == id); var bExists = items.Any(x => x.Id != id); return aExists && !bExists; }", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(DSA052Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA052Tests))]
    public async Task The_call_becomes_the_existence_question(string title, string members, string fixedMembers, string settings)
    {
        var source = ExistenceTestSupport.BuildSource(members, expectDiagnostics: true);
        var diagnostics = System.Text.RegularExpressions.Regex.Matches(members, @"\[\[").Count;

        await CreateTest(source, ExistenceTestSupport.BuildSource(fixedMembers, expectDiagnostics: false), settings, diagnostics).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task A_test_inside_the_predicate_of_another_is_rewritten_with_it()
    {
        var source = ExistenceTestSupport.BuildSource(Sync + "return {|#0:items.FirstOrDefault(x => {|#1:new[] { x }.FirstOrDefault()|} != null)|} != null; }", expectDiagnostics: false).Replace("{|#", "{|#");
        var fixedSource = ExistenceTestSupport.BuildSource(Sync + "return items.Any(x => new[] { x }.Any()); }", expectDiagnostics: false);

        await CreateTest(source, fixedSource, string.Empty, 2).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task The_call_with_the_name_of_the_method_only_is_renamed()
    {
        // inside the class that declares the extension methods, they can be called by their name alone
        const string Template = "using System.Linq;\nusing System.Threading.Tasks;\nnamespace TestApp\n{\n    public class Item { }\n    public static class Provider\n    {\n        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source) => null;\n        public static Task<bool> AnyAsync<T>(this IQueryable<T> source) => null;\n        public static async Task<bool> HasAsync(IQueryable<Item> q) { return await TEST != null; }\n    }\n}";
        var source = Template.Replace("TEST != null", "{|#0:FirstOrDefaultAsync(q)|} != null");
        var fixedSource = Template.Replace("await TEST != null", "await AnyAsync(q)");

        await CreateTest(source, fixedSource, string.Empty, 1).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Nothing_is_offered_when_the_provider_has_no_existence_method()
    {
        // FirstOrDefaultAsync without an AnyAsync next to it: there is no question to ask instead
        var source = "using System.Linq;\nusing System.Threading.Tasks;\nnamespace TestApp\n{\n    public class Item { }\n    public static class Provider { public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source) => null; }\n    public class Repository\n    {\n        public async Task<bool> HasAsync(IQueryable<Item> q) { return await q.FirstOrDefaultAsync() != null; }\n    }\n}";

        await CSharpAnalyzerVerifier<DSA052Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Entity_Framework_Core_has_the_methods()
    {
        var source = "using System.Linq;\nusing System.Threading;\nusing System.Threading.Tasks;\nusing Microsoft.EntityFrameworkCore;\nnamespace TestApp\n{\n    public class Item { public int Id { get; set; } }\n    public class Repository\n    {\n        public async Task<bool> HasAsync(IQueryable<Item> q, int id, CancellationToken ct) { return await {|#0:q.FirstOrDefaultAsync(x => x.Id == id, ct)|} != null; }\n    }\n}";
        var fixedSource = source.Replace("{|#0:q.FirstOrDefaultAsync(x => x.Id == id, ct)|} != null", "q.AnyAsync(x => x.Id == id, ct)");

        var test = CreateTest(source, fixedSource, string.Empty, 1);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(System.Collections.Immutable.ImmutableArray.Create(new PackageIdentity("Microsoft.EntityFrameworkCore", "8.0.10")));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task The_review_comment_is_always_offered_and_the_fix_is_not_when_the_test_cannot_be_rewritten()
    {
        // the element is used: nothing is reported, so the verifier has nothing to fix
        var source = ExistenceTestSupport.BuildSource(Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item != null && item.Id > 0; }", expectDiagnostics: false);

        await CSharpAnalyzerVerifier<DSA052Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }
}
