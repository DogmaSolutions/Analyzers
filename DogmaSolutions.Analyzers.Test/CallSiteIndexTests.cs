using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class CallSiteIndexTests
{
    private const string Source = @"
class C
{
    void Target(int x) { }
    void Other(int x) { }
    System.Action Handler;
    void M(System.Action[] actions, C c)
    {
        Target(1);
        c.Target(2);
        this.Target(3);
        c?.Target(4);
        Target<int>(5);
        Other(6);
        c.Other(7);
        Handler();
        actions[0]();
        ((System.Action)Handler)();
    }
    void Target<T>(T x) { }
}";

    [TestMethod]
    public void Candidates_are_the_calls_by_that_name_and_the_calls_whose_callee_is_not_a_plain_name()
    {
        var compilation = CSharpCompilation.Create("t", new[] { CSharpSyntaxTree.ParseText(Source) });

        var candidates = CallSiteIndex.FindCandidates(compilation, "Target").Select(c => c.Invocation.ToString()).ToList();

        CollectionAssert.AreEquivalent(
            new[] { "Target(1)", "c.Target(2)", "this.Target(3)", ".Target(4)", "Target<int>(5)", "actions[0]()", "((System.Action)Handler)()" },
            candidates);
    }

    [TestMethod]
    public void Candidates_come_with_the_semantic_model_of_their_tree()
    {
        var first = CSharpSyntaxTree.ParseText("class A { void Target() { } void M() { Target(); } }");
        var second = CSharpSyntaxTree.ParseText("class B { void M(A a) { a.Target(); } }");
        var compilation = CSharpCompilation.Create("t", new[] { first, second });

        var candidates = CallSiteIndex.FindCandidates(compilation, "Target").ToList();

        Assert.AreEqual(2, candidates.Count);
        foreach (var (model, invocation) in candidates)
            Assert.AreSame(invocation.SyntaxTree, model.SyntaxTree);
    }

    [TestMethod]
    public void The_index_is_built_once_per_compilation_and_a_method_with_no_call_has_no_candidates()
    {
        var compilation = CSharpCompilation.Create("t", new[] { CSharpSyntaxTree.ParseText("class A { void M() { N(); } void N() { } }") });

        Assert.AreEqual(1, CallSiteIndex.FindCandidates(compilation, "N").Count());
        Assert.AreEqual(0, CallSiteIndex.FindCandidates(compilation, "Missing").Count());
    }
}
