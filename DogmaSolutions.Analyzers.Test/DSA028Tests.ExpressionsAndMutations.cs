using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA028 on expression-bodied members (ternary, coalesce, parenthesized, cast and switch expressions are looked into),
/// on asynchronous return types, and on the analysis of what happens to a local after it is assigned <c>ToList()</c>.
/// </summary>
public partial class DSA028Tests
{
    private static string Service(string members) => @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class Holder { public List<int> Items = new List<int>(); public int Count; }
                public class MyService
                {
                    private readonly IEnumerable<int> _source = new int[0];
                    private void Touch() { }
" + members + @"
                }
            }";

    private static IEnumerable<object[]> GetExpressionAndMutationMatchedCases =>
    [
        ["Expression-bodied method with a ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => {|#0:s.ToList()|};")],
        ["Expression-bodied method with a ternary", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, bool f) => f ? {|#0:s.ToList()|} : {|#1:s.Where(x => x > 0).ToList()|};")],
        ["Expression-bodied method with a coalesce", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, List<int> cached) => cached ?? {|#0:s.ToList()|};")],
        ["Expression-bodied method with a parenthesized ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => ({|#0:s.ToList()|});")],
        ["Expression-bodied method with a cast ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => (IEnumerable<int>){|#0:s.ToList()|};")],
        ["Expression-bodied method with a switch expression", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, int k) => k switch { 1 => {|#0:s.ToList()|}, _ => {|#1:s.Where(x => x > 0).ToList()|} };")],
        ["Expression-bodied property", Service(@"
                    public IEnumerable<int> Items => {|#0:_source.ToList()|};")],
        ["Expression-bodied property getter", Service(@"
                    public IEnumerable<int> Items { get => {|#0:_source.ToList()|}; }")],
        ["Expression-bodied local function", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        IEnumerable<int> Local() => {|#0:s.ToList()|};
                        return Local();
                    }")],
        ["Async method returning Task<IEnumerable<T>>", Service(@"
                    public async Task<IEnumerable<int>> M(IEnumerable<int> s)
                    {
                        await Task.Yield();
                        return {|#0:s.ToList()|};
                    }")],
        ["Async method returning ValueTask<IReadOnlyList<T>>", Service(@"
                    public async ValueTask<IReadOnlyList<int>> M(IEnumerable<int> s)
                    {
                        await Task.Yield();
                        return {|#0:s.ToList()|};
                    }")],
        ["Non-generic IEnumerable return type", Service(@"
                    public System.Collections.IEnumerable M(IEnumerable<int> s) => {|#0:s.ToList()|};")],

        // Things done after the ToList that are not a mutation of the returned variable.
        ["Another collection mutated after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, List<int> other)
                    {
                        var r = {|#0:s.ToList()|};
                        other.Add(1);
                        return r;
                    }")],
        ["Member of another object mutated after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, Holder holder)
                    {
                        var r = {|#0:s.ToList()|};
                        holder.Items.Add(1);
                        return r;
                    }")],
        ["Unqualified call after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        var r = {|#0:s.ToList()|};
                        Touch();
                        return r;
                    }")],
        ["Mutation before the ToList assignment", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        IList<int> r = new List<int>();
                        r.Add(1);
                        r = {|#0:s.ToList()|};
                        return r;
                    }")],
        ["Element of the variable read after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        var r = {|#0:s.ToList()|};
                        var first = r[0];
                        return r;
                    }")],
        ["Element of another collection written after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, List<int> other, Holder holder)
                    {
                        var r = {|#0:s.ToList()|};
                        other[0] = 1;
                        holder.Items[0] = 2;
                        return r;
                    }")],
        ["Element of the variable written before the ToList assignment", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        IList<int> r = new List<int> { 1 };
                        r[0] = 5;
                        r = {|#0:s.ToList()|};
                        return r;
                    }")],
        ["Other variable and member assigned after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, Holder holder)
                    {
                        var count = 0;
                        var r = {|#0:s.ToList()|};
                        count = 1;
                        holder.Count = 2;
                        return r;
                    }")],
        ["Variable reassigned before the ToList assignment", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        IList<int> r = new List<int>();
                        r = new List<int>();
                        r = {|#0:s.ToList()|};
                        return r;
                    }")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetExpressionAndMutationMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task ExpressionAndMutation_Matched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA028Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

#pragma warning disable CA1062
        for (var i = 0; sourceCode.Contains("{|#" + i + ":"); i++)
#pragma warning restore CA1062
        {
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<DSA028Analyzer>.Diagnostic(DSA028Analyzer.DiagnosticId).WithLocation(i));
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    private static IEnumerable<object[]> GetExpressionAndMutationNotMatchedCases =>
    [
        ["Expression-bodied method with ToArray", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => s.ToArray();")],
        ["Expression-bodied method with a filter only", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => s.Where(x => x > 0);")],
        ["Expression-bodied method with a call that is not a member access", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => Build(s);
                    private static IEnumerable<int> Build(IEnumerable<int> s) => s;")],
        ["Expression-bodied method with ToList that has arguments", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, Holder h) => h.ToList(1);")],
        ["Expression-bodied method with a coalesce without ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s, List<int> cached) => cached ?? s;")],
        ["Expression-bodied method returning a string-based coalesce on null literal", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s) => s ?? null;")],
        ["Return type is a type named IEnumerable in another namespace", @"
            using System.Collections.Generic;
            using System.Linq;
            namespace Other { public interface IEnumerable<T> { } }
            namespace TestApp
            {
                public class MyService
                {
                    public Other.IEnumerable<int> M(IEnumerable<int> s) => (Other.IEnumerable<int>)(object)s.ToList();
                }
            }"],
        ["Return type is a collection that is not in the immutable list", Service(@"
                    public Queue<int> M(IEnumerable<int> s) => new Queue<int>(s.ToList());")],
        ["Return type is a generic type that is not a task", Service(@"
                    public Lazy<IEnumerable<int>> M(IEnumerable<int> s) => new Lazy<IEnumerable<int>>(() => s.ToList());")],
        ["Return type is Task of a non-collection", Service(@"
                    public async Task<int> M(IEnumerable<int> s)
                    {
                        await Task.Yield();
                        return s.ToList().Count;
                    }")],
        ["Return type that does not resolve", Service(@"
                    public Missing M(IEnumerable<int> s) => null;")],
        ["Variable reassigned after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        var r = s.ToList();
                        r = new List<int>();
                        return r;
                    }")],
        ["Variable element written after the ToList", Service(@"
                    public IEnumerable<int> M(IEnumerable<int> s)
                    {
                        var r = s.ToList();
                        r[0] = 1;
                        return r;
                    }")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetExpressionAndMutationNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task ExpressionAndMutation_NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA028Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CompilerDiagnostics = Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None;

        await test.RunAsync().ConfigureAwait(false);
    }
}
