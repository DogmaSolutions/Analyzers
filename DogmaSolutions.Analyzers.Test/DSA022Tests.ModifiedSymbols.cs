using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Unit tests for <c>DSA022Analyzer.CollectModifiedSymbols</c>: the symbols a loop declares or modifies, which are the
/// ones that make an expression using them not invariant.
/// </summary>
public partial class DSA022Tests
{
    private static string CollectModifiedSymbolNames(string loopStatement)
    {
        var source = @"
using System.Collections.Generic;
class C
{
    void Foo(ref int r) { }
    void Bar(out int o) { o = 0; }
    void M(List<int> items, List<(int, int)> pairs, bool c, int[] arr)
    {
        int x = 0, y = 0, z = 0, w = 0;
        " + loopStatement + @"
    }
}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(System.IO.Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "CollectModifiedSymbols", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);

        var loop = tree.GetRoot().DescendantNodes()
            .First(n => n is ForStatementSyntax or ForEachStatementSyntax or ForEachVariableStatementSyntax or WhileStatementSyntax or DoStatementSyntax);
        var body = loop switch
        {
            ForStatementSyntax f => f.Statement,
            ForEachStatementSyntax f => f.Statement,
            ForEachVariableStatementSyntax f => f.Statement,
            WhileStatementSyntax w => w.Statement,
            DoStatementSyntax d => d.Statement,
            _ => throw new InvalidOperationException(),
        };

        return string.Join(",", DSA022Analyzer.CollectModifiedSymbols(body, loop, model).Select(s => s.Name).OrderBy(n => n, StringComparer.Ordinal));
    }

    [TestMethod]
    public void CollectModifiedSymbols_for_loop_variables_and_incrementors()
    {
        Assert.AreEqual("i,inner,j", CollectModifiedSymbolNames("for (int i = 0, j = 5; i < 10; i++, j += 2) { var inner = 1; }"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_for_loop_without_declaration_still_collects_the_incrementors()
    {
        Assert.AreEqual("x", CollectModifiedSymbolNames("for (; x < 10; ++x) { }"));
        Assert.AreEqual("y", CollectModifiedSymbolNames("for (; y < 10; y = y + 1) { }"));
        Assert.AreEqual("z", CollectModifiedSymbolNames("for (; z > 0; z--) { }"));
        Assert.AreEqual(string.Empty, CollectModifiedSymbolNames("for (;;) { }"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_foreach_variable()
    {
        Assert.AreEqual("item", CollectModifiedSymbolNames("foreach (var item in items) { }"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_foreach_deconstruction_variables()
    {
        Assert.AreEqual("a,b", CollectModifiedSymbolNames("foreach (var (a, b) in pairs) { }"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_stacked_foreach_without_braces_includes_the_nested_variable()
    {
        Assert.AreEqual("a,b", CollectModifiedSymbolNames("foreach (var a in items) foreach (var b in items) { }"));
        Assert.AreEqual("a,b,w", CollectModifiedSymbolNames("foreach (var a in items) foreach (var b in items) Foo(ref w);"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_body_assignments_increments_and_ref_out_arguments()
    {
        Assert.AreEqual(
            "inner,n,q,v,w,x,y,z",
            CollectModifiedSymbolNames(@"while (c)
        {
            var inner = 1;
            x = 1;
            y++;
            --z;
            Foo(ref w);
            Bar(out var v);
            if (arr is int[] { Length: var n }) { }
            foreach (var q in items) { }
        }"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_do_loop_body_declarations()
    {
        Assert.AreEqual("d,e", CollectModifiedSymbolNames("do { var d = 1; if (items is var e) { } } while (c);"));
    }

    [TestMethod]
    public void CollectModifiedSymbols_ignores_reads_and_unresolved_targets()
    {
        Assert.AreEqual("read", CollectModifiedSymbolNames("while (c) { var read = x + y; }"));
        Assert.AreEqual(string.Empty, CollectModifiedSymbolNames("while (c) { unknown = 1; unknown2++; }"));
    }
}
