using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The follow of a field through one member, on inputs the analyzer cannot produce by itself.</summary>
[TestClass]
public class DSA054FlowTests
{
    private const string Source = "class C { int _n; int One() { _n = 1; return _n; } int Two() => _n; }";

    private static (MethodDeclarationSyntax One, List<IdentifierNameSyntax> Uses) Parse()
    {
        var root = CSharpSyntaxTree.ParseText(Source).GetRoot();
        var uses = root.DescendantNodes().OfType<IdentifierNameSyntax>().Where(static name => name.Identifier.ValueText == "_n").ToList();
        return (root.DescendantNodes().OfType<MethodDeclarationSyntax>().First(), uses);
    }

    [TestMethod]
    public void A_use_that_the_member_does_not_contain_is_state()
    {
        var (one, uses) = Parse();

        // _n of "Two" is after the body of "One": the walk cannot account for it
        Assert.AreEqual(DSA054Scenario.None, DSA054Flow.Analyze(one, uses, false).Scenario);
    }

    [TestMethod]
    public void The_uses_of_the_member_alone_are_a_finding()
    {
        var (one, uses) = Parse();

        var result = DSA054Flow.Analyze(one, uses.Take(2).ToList(), false);

        Assert.AreEqual(DSA054Scenario.Memoization, result.Scenario);
    }

    [TestMethod]
    public void A_member_without_uses_is_state()
    {
        var (one, _) = Parse();

        Assert.AreEqual(DSA054Scenario.None, DSA054Flow.Analyze(one, new List<IdentifierNameSyntax>(), false).Scenario);
    }
}
