using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA052Tests
{
    private const string Sync = "public bool Has(IEnumerable<Item> items, int id) { ";
    private const string Query = "public bool Has(IQueryable<Item> q, int id) { ";
    private const string Async = "public async Task<bool> HasAsync(IQueryable<Item> q, int id, CancellationToken ct) { ";

    // title, members ([[...]] marks the expected call, in order), settings (key=value;...), expected ("Source|Shape|Method|Alternative|Variable;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the call compared right away ----------------------------------------------------------------------------------------
        ["In memory, not null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] == null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, is null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, is not null", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is not null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, is an empty property pattern", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is { }; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, null on the left", Sync + "return null != [[items.FirstOrDefault(x => x.Id == id)]]; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, without a predicate", Sync + "return [[items.FirstOrDefault()]] != null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, in parentheses", Sync + "return ([[items.FirstOrDefault(x => x.Id == id)]]) != null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, in a condition", Sync + "if ([[items.FirstOrDefault(x => x.Id == id)]] == null) { return false; } return true; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, a pattern with and", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] is not null and not null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, the call as a static method", Sync + "return [[Enumerable.FirstOrDefault(items, x => x.Id == id)]] != null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, a list", "public bool Has(List<Item> items, int id) { return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In memory, SingleOrDefault", Sync + "return [[items.SingleOrDefault(x => x.Id == id)]] != null; }", "", E("Enumerable", "Direct", "SingleOrDefault", "Any")],
        ["Query, not null", Query + "return [[q.FirstOrDefault(x => x.Id == id)]] != null; }", "", E("Queryable", "Direct", "FirstOrDefault", "Any")],
        ["Query, SingleOrDefault", Query + "return [[q.SingleOrDefault(x => x.Id == id)]] == null; }", "", E("Queryable", "Direct", "SingleOrDefault", "Any")],
        ["Async query, not null", Async + "return await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]] != null; }", "", E("Queryable", "Direct", "FirstOrDefaultAsync", "AnyAsync")],
        ["Async query, in parentheses", Async + "return (await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]]) == null; }", "", E("Queryable", "Direct", "FirstOrDefaultAsync", "AnyAsync")],
        ["Async query, ConfigureAwait", Async + "return await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]].ConfigureAwait(false) is null; }", "", E("Queryable", "Direct", "FirstOrDefaultAsync", "AnyAsync")],
        ["Async query, without a predicate", Async + "return await [[q.FirstOrDefaultAsync(ct)]] is not null; }", "", E("Queryable", "Direct", "FirstOrDefaultAsync", "AnyAsync")],
        ["Async query, SingleOrDefaultAsync", Async + "return await [[q.SingleOrDefaultAsync(x => x.Id == id, ct)]] != null; }", "", E("Queryable", "Direct", "SingleOrDefaultAsync", "AnyAsync")],
        ["In an expression-bodied member", "public bool Has(IEnumerable<Item> items, int id) => [[items.FirstOrDefault(x => x.Id == id)]] != null;", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["In a lambda", "public Func<bool> Has(IEnumerable<Item> items, int id) => () => [[items.FirstOrDefault(x => x.Id == id)]] != null;", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Two tests", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null && [[items.FirstOrDefault(x => x.Id != id)]] == null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any") + ";" + E("Enumerable", "Direct", "FirstOrDefault", "Any")],

        // ---- the result kept in a variable -----------------------------------------------------------------------------------------
        ["Variable, in memory", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; if (item == null) { return false; } return true; }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variable, with its type", Sync + "Item item = [[items.FirstOrDefault(x => x.Id == id)]]; return item != null; }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variable, is null", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; return item is null; }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variable, several tests", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; if (item == null) { return false; } if (item != null && id > 0) { return true; } return item is not null; }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variable, tested in parentheses", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; return (item) != null; }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variable, query", Query + "var item = [[q.FirstOrDefault(x => x.Id == id)]]; return item != null; }", "", E("Queryable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variable, async query", Async + "var item = await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]]; return item != null; }", "", E("Queryable", "Local", "FirstOrDefaultAsync", "AnyAsync", "item")],
        ["Variable, async query with ConfigureAwait", Async + "var item = await [[q.FirstOrDefaultAsync(x => x.Id == id, ct)]].ConfigureAwait(false); return item == null; }", "", E("Queryable", "Local", "FirstOrDefaultAsync", "AnyAsync", "item")],
        ["Variable, SingleOrDefault", Sync + "var item = [[items.SingleOrDefault(x => x.Id == id)]]; return item != null; }", "", E("Enumerable", "Local", "SingleOrDefault", "Any", "item")],
        ["Variable, in a switch section", "public bool Has(IEnumerable<Item> items, int id) { switch (id) { case 1: var item = [[items.FirstOrDefault(x => x.Id == id)]]; return item != null; default: return false; } }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],

        // ---- the element is used: nothing to report --------------------------------------------------------------------------------
        ["Returned", "public Item Find(IEnumerable<Item> items, int id) { var item = items.FirstOrDefault(x => x.Id == id); if (item == null) { return null; } return item; }", "", ""],
        ["Returned right away", "public Item Find(IEnumerable<Item> items, int id) { return items.FirstOrDefault(x => x.Id == id); }", "", ""],
        ["Assigned to a variable", Sync + "var item = items.FirstOrDefault(x => x.Id == id); var copy = item; return item != null && copy != null; }", "", ""],
        ["Assigned to a field", "private Item _last; public bool Has(IEnumerable<Item> items, int id) { var item = items.FirstOrDefault(x => x.Id == id); _last = item; return item != null; }", "", ""],
        ["Assigned to a property", "public Item Last { get; set; } public bool Has(IEnumerable<Item> items, int id) { var item = items.FirstOrDefault(x => x.Id == id); Last = item; return item != null; }", "", ""],
        ["Assigned through a tuple", Sync + "var item = items.FirstOrDefault(x => x.Id == id); var pair = (item, id); return item != null && pair.id > 0; }", "", ""],
        ["Assigned to an array", Sync + "var item = items.FirstOrDefault(x => x.Id == id); var all = new[] { item }; return item != null && all.Length > 0; }", "", ""],
        ["Deconstructed into a tuple", "public bool Has(IEnumerable<Item> items, int id) { Item a; var item = items.FirstOrDefault(x => x.Id == id); (a, _) = (item, 1); return item != null && a != null; }", "", ""],
        ["Accessed", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item != null && item.Name == \"a\"; }", "", ""],
        ["Accessed after the test", Sync + "var item = items.FirstOrDefault(x => x.Id == id); if (item == null) { return false; } return item.Id > 0; }", "", ""],
        ["Accessed with a null conditional", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item != null && item?.Name != null; }", "", ""],
        ["Passed as an argument", "public bool Has(IEnumerable<Item> items, int id) { var item = items.FirstOrDefault(x => x.Id == id); Use(item); return item != null; } private static void Use(Item item) { }", "", ""],
        ["Passed as a ref argument", "public bool Has(IEnumerable<Item> items, int id) { var item = items.FirstOrDefault(x => x.Id == id); Use(ref item); return item != null; } private static void Use(ref Item item) { }", "", ""],
        ["Used in an interpolated string", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item != null && $\"{item}\".Length > 0; }", "", ""],
        ["Captured by a lambda", Sync + "var item = items.FirstOrDefault(x => x.Id == id); Func<bool> test = () => item != null; return test(); }", "", ""],
        ["Compared with another variable", Sync + "var item = items.FirstOrDefault(x => x.Id == id); var other = items.LastOrDefault(); return item != other; }", "", ""],
        ["Compared with something else", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item == items.First(); }", "", ""],
        ["Reassigned", Sync + "var item = items.FirstOrDefault(x => x.Id == id); item = items.First(); return item != null; }", "", ""],
        ["Tested with a type pattern", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item is Item; }", "", ""],
        ["Tested with a property pattern", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item is { Id: 1 }; }", "", ""],
        ["Tested with a declaration pattern", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item is Item found && found.Id > 0; }", "", ""],
        ["Never used", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return true; }", "", ""],
        ["Declared with another variable", Sync + "Item a = items.FirstOrDefault(x => x.Id == id), b = null; return a != null; }", "", ""],
        ["Result of the null conditional", Sync + "return items?.FirstOrDefault(x => x.Id == id) != null; }", "", ""],
        ["Compared with a value", Sync + "return items.FirstOrDefault(x => x.Id == id) == new Item(); }", "", ""],
        ["The default value overload", Sync + "return items.FirstOrDefault(x => x.Id == id, new Item()) != null; }", "", ""],
        ["The task is not awaited", "public bool Has(IQueryable<Item> q, int id) { var task = q.FirstOrDefaultAsync(x => x.Id == id); return task != null; }", "", ""],
        ["The result of the task is read", "public bool Has(IQueryable<Item> q, int id) { return q.FirstOrDefaultAsync(x => x.Id == id).Result != null; }", "", ""],
        ["A method with the same name that is not an extension", "public sealed class Bag { public Item FirstOrDefault() => null; } public bool Has(Bag bag) { return bag.FirstOrDefault() != null; }", "", ""],
        ["Another method", Sync + "return items.First(x => x.Id == id) != null; }", "", ""],
        ["Tested in a foreach", "public int Count(IEnumerable<Item> items, int id) { var n = 0; foreach (var item in items.Where(x => x.Id == id)) { if (item != null) { n++; } } return n; }", "", ""],

        // ---- the elements that can be null, and the ones that cannot be -------------------------------------------------------------
        ["Value type elements", "public bool Has(IEnumerable<int> items) { return items.FirstOrDefault(x => x > 3) != 0; }", "", ""],
        ["Struct elements", "public bool Has(IEnumerable<Pair> items) { return items.FirstOrDefault(x => x.X > 3).Equals(default(Pair)); }", "", ""],
        ["Nullable value type elements", "public bool Has(IEnumerable<int?> items) { return items.FirstOrDefault(x => x > 3) != null; }", "", ""],
        ["String elements", "public bool Has(IEnumerable<string> items) { return items.FirstOrDefault(x => x.Length > 3) != null; }", "", ""],
        ["String elements, not skipped", "public bool Has(IEnumerable<string> items) { return [[items.FirstOrDefault(x => x.Length > 3)]] != null; }", "skip_possibly_null_elements=false", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Object elements", "public bool Has(IEnumerable<object> items) { return items.FirstOrDefault() != null; }", "", ""],
        ["Object elements, not skipped", "public bool Has(IEnumerable<object> items) { return [[items.FirstOrDefault()]] != null; }", "skip_possibly_null_elements=false", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Elements of an unconstrained type", "public bool Has<T>(IEnumerable<T> items) { return items.FirstOrDefault() != null; }", "", ""],
        ["Elements of a class type", "public bool Has<T>(IEnumerable<T> items) where T : class { return [[items.FirstOrDefault()]] != null; }", "", E("Enumerable", "Direct", "FirstOrDefault", "Any")],

        // ---- the options -------------------------------------------------------------------------------------------------------------
        ["Direct comparisons refused", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "report_direct_comparisons=false", ""],
        ["Direct comparisons refused, variables still reported", Sync + "var item = [[items.FirstOrDefault(x => x.Id == id)]]; return item != null; }", "report_direct_comparisons=false", E("Enumerable", "Local", "FirstOrDefault", "Any", "item")],
        ["Variables refused", Sync + "var item = items.FirstOrDefault(x => x.Id == id); return item != null; }", "report_locals=false", ""],
        ["Variables refused, direct comparisons still reported", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "report_locals=false", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Queries refused", Query + "return q.FirstOrDefault(x => x.Id == id) != null; }", "report_queryable_sources=false", ""],
        ["Queries refused, memory still reported", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "report_queryable_sources=false", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Memory refused", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "report_enumerable_sources=false", ""],
        ["Memory refused, queries still reported", Query + "return [[q.FirstOrDefault(x => x.Id == id)]] != null; }", "report_enumerable_sources=false", E("Queryable", "Direct", "FirstOrDefault", "Any")],
        ["Only FirstOrDefault", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null && items.SingleOrDefault(x => x.Id == id + 1) != null; }", "included_methods=FirstOrDefault", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Only the async methods", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "included_methods=FirstOrDefaultAsync", ""],
        ["LastOrDefault is not there by default", Sync + "return items.LastOrDefault(x => x.Id == id) != null; }", "", ""],
        ["LastOrDefault configured", Sync + "return [[items.LastOrDefault(x => x.Id == id)]] != null; }", "included_methods=LastOrDefault", E("Enumerable", "Direct", "LastOrDefault", "Any")],

        // ---- the exclusions ----------------------------------------------------------------------------------------------------------
        ["Excluded type name", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "excluded_type_name_patterns=Repo*", ""],
        ["Type name not excluded", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "excluded_type_name_patterns=Other*", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Excluded namespace", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "excluded_namespace_patterns=Test*", ""],
        ["Namespace not excluded", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "excluded_namespace_patterns=Other*", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Excluded member name", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "excluded_member_name_patterns=Has", ""],
        ["Member name not excluded", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "excluded_member_name_patterns=Other*", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["Excluded constructor", "public Repository(IEnumerable<Item> items) { Ok = items.FirstOrDefault() != null; } public bool Ok { get; }", "excluded_member_name_patterns=Repository", ""],
        ["Excluded property", "public bool Flag { get { return new List<Item>().FirstOrDefault() != null; } }", "excluded_member_name_patterns=Flag", ""],
        ["A field initializer has no member name", "private readonly bool _flag = [[new List<Item>().FirstOrDefault()]] != null;", "excluded_member_name_patterns=Other", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
        ["An invocation of an invocation", "public bool Has(Func<Func<Item>> f) { return f()() != null; }", "", ""],
        ["A dynamic receiver", "public bool Has(dynamic items) { return items.FirstOrDefault() != null; }", "", ""],
        ["A name that is the member of something else", "public bool Has(IEnumerable<Item> items) { var Id = [[items.FirstOrDefault()]]; return Id != null && items.First().Id > 0; }", "", E("Enumerable", "Local", "FirstOrDefault", "Any", "Id")],
        ["Excluded file", Sync + "return items.FirstOrDefault(x => x.Id == id) != null; }", "excluded_file_patterns=Test0.cs", ""],
        ["File not excluded", Sync + "return [[items.FirstOrDefault(x => x.Id == id)]] != null; }", "excluded_file_patterns=Other.cs", E("Enumerable", "Direct", "FirstOrDefault", "Any")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string members, string settings, string expected)
    {
        await RunAsync(members, settings, expected).ConfigureAwait(false);
    }
}
