using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA053Tests
{
    private const string Sync = "public bool Has(IEnumerable<Item> items, int id) { ";
    private const string Query = "public bool Has(IQueryable<Item> q, int id) { ";
    private const string Async = "public async Task<bool> HasAsync(IQueryable<Item> q, int id, CancellationToken ct) { ";

    // title, members ([[...]] marks the expected call, in order), settings (key=value;...), expected ("Source|Shape|Method|Alternative|Variable;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the number kept in a variable ---------------------------------------------------------------------------------------
        ["Variable, in memory, greater than zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, without a predicate", Sync + "var n = [[items.Count()]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, equal to zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n == 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, not equal to zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n != 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, at least one", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n >= 1; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, less than one", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n < 1; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, at most zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n <= 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, zero on the left", Sync + "var n = [[items.Count(x => x.Id == id)]]; return 0 < n; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, one on the left", Sync + "var n = [[items.Count(x => x.Id == id)]]; return 1 <= n; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, zero equal on the left", Sync + "var n = [[items.Count(x => x.Id == id)]]; return 0 == n; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, is greater than zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, is zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, is not zero", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is not 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, is at least one", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is >= 1; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, is a pair of patterns", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is > 0 and < 100000; }", "", ""],
        ["Variable, is zero or less", Sync + "var n = [[items.Count(x => x.Id == id)]]; return n is (<= 0); }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, a constant", "private const int None = 0; public bool Has(IEnumerable<Item> items, int id) { var n = [[items.Count(x => x.Id == id)]]; return n == None; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, several tests", Sync + "var n = [[items.Count(x => x.Id == id)]]; if (n == 0) { return false; } if (n > 0 && id > 0) { return true; } return n != 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, with its type", Sync + "int n = [[items.Count(x => x.Id == id)]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Variable, LongCount", Sync + "long n = [[items.LongCount(x => x.Id == id)]]; return n > 0; }", "", E("Enumerable", "Local", "LongCount", "Any", "n")],
        ["Variable, query", Query + "var n = [[q.Count(x => x.Id == id)]]; return n > 0; }", "", E("Queryable", "Local", "Count", "Any", "n")],
        ["Variable, async query", Async + "var n = await [[q.CountAsync(x => x.Id == id, ct)]]; return n > 0; }", "", E("Queryable", "Local", "CountAsync", "AnyAsync", "n")],
        ["Variable, async query without a predicate", Async + "var n = await [[q.CountAsync(ct)]]; return n == 0; }", "", E("Queryable", "Local", "CountAsync", "AnyAsync", "n")],
        ["Variable, async LongCount with ConfigureAwait", Async + "var n = await [[q.LongCountAsync(ct)]].ConfigureAwait(false); return n != 0; }", "", E("Queryable", "Local", "LongCountAsync", "AnyAsync", "n")],
        ["Variable, as a static call", Sync + "var n = [[Enumerable.Count(items)]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],

        // ---- the call compared right away ---------------------------------------------------------------------------------------------
        ["Async query with a predicate, greater than zero", Async + "return await [[q.CountAsync(x => x.Id == id, ct)]] > 0; }", "", E("Queryable", "Direct", "CountAsync", "AnyAsync")],
        ["Async query with a predicate, equal to zero", Async + "return (await [[q.CountAsync(x => x.Id == id, ct)]]) == 0; }", "", E("Queryable", "Direct", "CountAsync", "AnyAsync")],
        ["Async LongCount with a predicate", Async + "return await [[q.LongCountAsync(x => x.Id == id, ct)]].ConfigureAwait(false) != 0; }", "", E("Queryable", "Direct", "LongCountAsync", "AnyAsync")],
        ["Async query without a predicate is CA1828's", Async + "return await q.CountAsync(ct) > 0; }", "", ""],
        ["Sync Count is CA1827's", Sync + "return items.Count(x => x.Id == id) > 0; }", "", ""],
        ["Sync Count without a predicate is CA1827's", Sync + "return items.Count() == 0; }", "", ""],
        ["Query Count is CA1827's", Query + "return q.Count(x => x.Id == id) != 0; }", "", ""],
        ["Sync Count, direct comparisons asked", Sync + "return [[items.Count(x => x.Id == id)]] > 0; }", "report_direct_comparisons=true", E("Enumerable", "Direct", "Count", "Any")],
        ["Sync Count, is a pattern", Sync + "return [[items.Count(x => x.Id == id)]] is > 0; }", "report_direct_comparisons=true", E("Enumerable", "Direct", "Count", "Any")],
        ["Query Count, direct comparisons asked", Query + "return [[q.LongCount()]] == 0; }", "report_direct_comparisons=true", E("Queryable", "Direct", "LongCount", "Any")],
        ["Async without a predicate, direct comparisons asked", Async + "return await [[q.CountAsync(ct)]] > 0; }", "report_direct_comparisons=true", E("Queryable", "Direct", "CountAsync", "AnyAsync")],
        ["Async with a predicate, refused", Async + "return await q.CountAsync(x => x.Id == id, ct) > 0; }", "report_async_predicate_comparisons=false", ""],

        // ---- the number is used: nothing to report ----------------------------------------------------------------------------------
        ["Returned", "public int Total(IEnumerable<Item> items) { var n = items.Count(); if (n == 0) { return 0; } return n; }", "", ""],
        ["Returned right away", "public int Total(IEnumerable<Item> items) { return items.Count(); }", "", ""],
        ["Assigned to a variable", Sync + "var n = items.Count(x => x.Id == id); var copy = n; return n > 0 && copy > 0; }", "", ""],
        ["Assigned to a field", "private int _last; public bool Has(IEnumerable<Item> items, int id) { var n = items.Count(x => x.Id == id); _last = n; return n > 0; }", "", ""],
        ["Assigned to a property", "public int Last { get; set; } public bool Has(IEnumerable<Item> items, int id) { var n = items.Count(x => x.Id == id); Last = n; return n > 0; }", "", ""],
        ["Assigned through a tuple", Sync + "var n = items.Count(x => x.Id == id); var pair = (n, id); return n > 0 && pair.id > 0; }", "", ""],
        ["Used in an expression", Sync + "var n = items.Count(x => x.Id == id); return n > 0 && n * 2 < 100; }", "", ""],
        ["Passed as an argument", "public bool Has(IEnumerable<Item> items, int id) { var n = items.Count(x => x.Id == id); Log(n); return n > 0; } private static void Log(int n) { }", "", ""],
        ["Used in an interpolated string", Sync + "var n = items.Count(x => x.Id == id); return n > 0 && $\"{n}\".Length > 0; }", "", ""],
        ["Captured by a lambda", Sync + "var n = items.Count(x => x.Id == id); Func<bool> test = () => n > 0; return test(); }", "", ""],
        ["Compared with another number", Sync + "var n = items.Count(x => x.Id == id); return n > id; }", "", ""],
        ["Compared with one", Sync + "var n = items.Count(x => x.Id == id); return n > 1; }", "", ""],
        ["Compared with two", Sync + "var n = items.Count(x => x.Id == id); return n >= 2; }", "", ""],
        ["Equal to one", Sync + "var n = items.Count(x => x.Id == id); return n == 1; }", "", ""],
        ["Greater than minus one", Sync + "var n = items.Count(x => x.Id == id); return n > -1; }", "", ""],
        ["Tested against zero and another threshold", Sync + "var n = items.Count(x => x.Id == id); return n > 0 && n < 10; }", "", ""],
        ["Reassigned", Sync + "var n = items.Count(x => x.Id == id); n = 3; return n > 0; }", "", ""],
        ["Incremented", Sync + "var n = items.Count(x => x.Id == id); n++; return n > 0; }", "", ""],
        ["Never used", Sync + "var n = items.Count(x => x.Id == id); return true; }", "", ""],
        ["Used as an index", "public Item Get(IEnumerable<Item> items, int id) { var n = items.Count(x => x.Id == id); return n > 0 ? items.ElementAt(n - 1) : null; }", "", ""],
        ["Compared with a variable", Sync + "var n = items.Count(x => x.Id == id); var zero = 0; return n > zero; }", "", ""],
        ["The count of a count", Sync + "return items.Count(x => x.Id == id) > items.Count(); }", "report_direct_comparisons=true", ""],
        ["Not awaited", "public bool Has(IQueryable<Item> q, int id) { var task = q.CountAsync(x => x.Id == id); return task != null; }", "", ""],
        ["A method with the same name that is not an extension", "public sealed class Bag { public int Count() => 0; } public bool Has(Bag bag) { var n = bag.Count(); return n > 0; }", "", ""],
        ["A projection", Sync + "var n = [[items.Select(x => x.Id).Count()]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],

        // ---- the sequences whose size is known without enumerating them: Length and Count are right -------------------------------------
        ["An array", "public bool Has(Item[] items) { var n = items.Count(); return n > 0; }", "", ""],
        ["An array Length", "public bool Has(Item[] items) { var n = items.Length; return n > 0; }", "", ""],
        ["A list", "public bool Has(List<Item> items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A list Count", "public bool Has(List<Item> items) { var n = items.Count; return n > 0; }", "", ""],
        ["A collection", "public bool Has(ICollection<Item> items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A read-only collection", "public bool Has(IReadOnlyCollection<Item> items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A read-only list", "public bool Has(IReadOnlyList<Item> items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A set", "public bool Has(HashSet<Item> items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A dictionary", "public bool Has(Dictionary<int, Item> items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A string", "public bool Has(string text) { var n = text.Count(); return n > 0; }", "", ""],
        ["A non generic collection", "public bool Has(System.Collections.ICollection items) { var n = [[items.Cast<object>().Count()]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["A list, asked", "public bool Has(List<Item> items) { var n = [[items.Count()]]; return n > 0; }", "report_types_with_cheap_count=true", E("Enumerable", "Local", "Count", "Any", "n")],
        ["A sequence that is a list at run time", Sync + "var n = [[items.Count()]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],
        ["A sequence made by a query on a list", "public bool Has(List<Item> items) { var n = [[items.Where(x => x.Id > 1).Count()]]; return n > 0; }", "", E("Enumerable", "Local", "Count", "Any", "n")],

        // ---- what is not understood ----------------------------------------------------------------------------------------------------
        ["A dynamic receiver", "public bool Has(dynamic items) { var n = items.Count(); return n > 0; }", "", ""],
        ["A constant of another type", "public bool Has(IEnumerable<Item> items) { long n = items.LongCount(); return n > 0u; }", "", ""],
        ["A pattern with a constant of another type", "public bool Has(IEnumerable<Item> items) { long n = items.LongCount(); return n is > 0u; }", "", ""],
        ["A pair of patterns, one of them not understood", "public bool Has(IEnumerable<Item> items) { long n = items.LongCount(); return n is > 0 and > 0u; }", "", ""],
        ["An invocation of an invocation", "public bool Has(Func<Func<int>> f) { var n = f()(); return n > 0; }", "", ""],
        ["A name that is the member of something else", "public bool Has(IEnumerable<Item> items) { var Id = [[items.Count()]]; return Id > 0 && items.First().Id > 0; }", "", E("Enumerable", "Local", "Count", "Any", "Id")],
        ["A local in a switch section", "public bool Has(IEnumerable<Item> items, int id) { switch (id) { case 1: var n = [[items.Count()]]; return n > 0; default: return false; } }", "", E("Enumerable", "Local", "Count", "Any", "n")],

        // ---- the options ---------------------------------------------------------------------------------------------------------------
        ["Variables refused", Sync + "var n = items.Count(x => x.Id == id); return n > 0; }", "report_locals=false", ""],
        ["Variables refused, direct comparisons still reported", Async + "return await [[q.CountAsync(x => x.Id == id, ct)]] > 0; }", "report_locals=false", E("Queryable", "Direct", "CountAsync", "AnyAsync")],
        ["Queries refused", Query + "var n = q.Count(); return n > 0; }", "report_queryable_sources=false", ""],
        ["Memory refused", Sync + "var n = items.Count(); return n > 0; }", "report_enumerable_sources=false", ""],
        ["Memory refused, queries still reported", Query + "var n = [[q.Count()]]; return n > 0; }", "report_enumerable_sources=false", E("Queryable", "Local", "Count", "Any", "n")],
        ["Only Count", Sync + "var n = [[items.Count()]]; var m = items.LongCount(); return n > 0 && m > 0; }", "included_methods=Count", E("Enumerable", "Local", "Count", "Any", "n")],

        // ---- the exclusions ------------------------------------------------------------------------------------------------------------
        ["Excluded type name", Sync + "var n = items.Count(); return n > 0; }", "excluded_type_name_patterns=Repo*", ""],
        ["Type name not excluded", Sync + "var n = [[items.Count()]]; return n > 0; }", "excluded_type_name_patterns=Other*", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Excluded namespace", Sync + "var n = items.Count(); return n > 0; }", "excluded_namespace_patterns=Test*", ""],
        ["Excluded member name", Sync + "var n = items.Count(); return n > 0; }", "excluded_member_name_patterns=Has", ""],
        ["Member name not excluded", Sync + "var n = [[items.Count()]]; return n > 0; }", "excluded_member_name_patterns=Other*", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Excluded constructor", "public Repository(IEnumerable<Item> items) { var n = items.Count(); Ok = n > 0; } public bool Ok { get; }", "excluded_member_name_patterns=Repository", ""],
        ["Constructor not excluded", "public Repository(IEnumerable<Item> items) { var n = [[items.Count()]]; Ok = n > 0; } public bool Ok { get; }", "excluded_member_name_patterns=Other", E("Enumerable", "Local", "Count", "Any", "n")],
        ["Excluded property", "public bool Ok(IEnumerable<Item> items) => false; public bool Flag { get { var n = new List<Item>().Select(x => x).Count(); return n > 0; } }", "excluded_member_name_patterns=Flag", ""],
        ["A field initializer has no member name", "private readonly bool _flag = [[new List<Item>().Select(x => x).Count()]] > 0;", "report_direct_comparisons=true;excluded_member_name_patterns=Other", E("Enumerable", "Direct", "Count", "Any")],
        ["Excluded file", Sync + "var n = items.Count(); return n > 0; }", "excluded_file_patterns=Test0.cs", ""],
        ["File not excluded", Sync + "var n = [[items.Count()]]; return n > 0; }", "excluded_file_patterns=Other.cs", E("Enumerable", "Local", "Count", "Any", "n")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string members, string settings, string expected)
    {
        await RunAsync(members, settings, expected).ConfigureAwait(false);
    }
}
