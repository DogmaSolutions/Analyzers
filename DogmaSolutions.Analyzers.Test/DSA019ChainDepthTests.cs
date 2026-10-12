using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// The syntactic and the effective (semantic) depth of a member chain: which nodes add a level and which are looked through.
/// </summary>
[TestClass]
public class DSA019ChainDepthTests
{
    private const string Declarations = @"
using System;
using System.Threading.Tasks;
namespace App
{
    public class Leaf { public int Value; public const int Limit = 3; public Leaf Next; public Leaf[] Items; public Leaf Self() => this; }
    public class Holder { public Leaf Leaf; public Task<Leaf> LeafAsync() => null; public class Nested { public const int Size = 1; public static Leaf Shared; } public enum Kind { A } }
    public static class Ext { public static Leaf AsNoTracking(this Leaf l) => l; }
    public class C
    {
        public Holder holder; public Leaf leaf; public object boxed;
        public async Task M(Leaf[] arr)
        {
            var __expression = ";

    private static (ExpressionSyntax Expression, SemanticModel Model) Parse(string expression)
    {
        var tree = CSharpSyntaxTree.ParseText(Declarations + expression + ";\n        }\n    }\n}");
        var compilation = CSharpCompilation.Create(
            "t",
            new[] { tree },
            new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Task).Assembly.Location), MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location) });
        var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().First(v => v.Identifier.ValueText == "__expression");
        return (declarator.Initializer.Value, compilation.GetSemanticModel(tree));
    }

    [TestMethod]
    [DataRow("holder.Leaf.Next.Value", 3)]
    [DataRow("arr[0].Next.Value", 3)]
    [DataRow("arr[0][1]", 2)]
    [DataRow("holder.Leaf.Self().Next", 3)]
    [DataRow("(holder.Leaf).Next.Value", 3)]
    [DataRow("((Leaf)boxed).Next.Value", 2)]
    [DataRow("(await holder.LeafAsync()).Next.Value", 3)]
    [DataRow("leaf", 0)]
    public void ComputeChainDepth_counts_member_and_element_accesses_and_looks_through_calls_parentheses_await_and_casts(string expression, int expected)
    {
        var (syntax, _) = Parse(expression);

        Assert.AreEqual(expected, DSA019Analyzer.ComputeChainDepth(syntax));
    }

    [TestMethod]
    [DataRow("holder.Leaf.Next.Value", 3)]
    [DataRow("arr[0].Next.Value", 3)]
    [DataRow("holder.Leaf.Self().Next", 3)]
    [DataRow("(holder.Leaf).Next.Value", 3)]
    [DataRow("((Leaf)boxed).Next.Value", 2)]
    [DataRow("(await holder.LeafAsync()).Next.Value", 3)]
    [DataRow("App.Holder.Nested.Shared.Value", 2)]
    [DataRow("System.Math.PI", 0)]
    [DataRow("System.Math.Abs(1)", 1)]
    [DataRow("App.Leaf.Limit", 0)]
    [DataRow("App.Holder.Nested.Size", 0)]
    [DataRow("App.Holder.Kind.A", 0)]
    [DataRow("holder.Leaf.Next.Value.ToString()", 4)]
    public void ComputeEffectiveChainDepth_does_not_count_namespaces_nested_types_and_constants(string expression, int expected)
    {
        var (syntax, model) = Parse(expression);

        Assert.AreEqual(expected, DSA019Analyzer.ComputeEffectiveChainDepth(syntax, model));
    }

    [TestMethod]
    public void ComputeEffectiveChainDepth_does_not_count_the_ignored_intermediate_members()
    {
        var (syntax, model) = Parse("holder.Leaf.AsNoTracking().Next.Value");

        Assert.AreEqual(4, DSA019Analyzer.ComputeEffectiveChainDepth(syntax, model));
        Assert.AreEqual(4, DSA019Analyzer.ComputeEffectiveChainDepth(syntax, model, new HashSet<string>()));
        Assert.AreEqual(3, DSA019Analyzer.ComputeEffectiveChainDepth(syntax, model, new HashSet<string> { "AsNoTracking" }));
    }
}
