using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Direct unit tests for the pure, extracted helpers of <see cref="DSA040Analyzer"/>. Decomposing the analyzer into
/// small internal predicates/transforms makes every guard branch reachable without a contrived full-analyzer scenario.
/// </summary>
[TestClass]
public class DSA040HelpersTests
{
    [TestMethod]
    [DataRow("Next", true)]
    [DataRow("NextBytes", true)]
    [DataRow("NextDouble", true)]
    [DataRow("Sample", false)]
    [DataRow("ToString", false)]
    [DataRow("", false)]
    public void IsNextMethodName_classifies_producer_names(string name, bool expected) =>
        Assert.AreEqual(expected, DSA040Analyzer.IsNextMethodName(name));

    [TestMethod]
    [DataRow("token", true)]
    [DataRow("apiKey", true)]
    [DataRow("sessionId", true)]
    [DataRow("resetCode", true)]
    [DataRow("password", true)]
    [DataRow("Secret", true)]
    [DataRow("diceRoll", false)]
    [DataRow("keyboard", false)]      // 'key' only as a substring, not a word boundary
    [DataRow("count", false)]
    public void IsSecuritySensitiveName_matches_only_whole_security_words(string name, bool expected) =>
        Assert.AreEqual(expected, DSA040Analyzer.IsSecuritySensitiveName(name));

    [TestMethod]
    public void IsSecuritySensitiveName_is_false_for_null_empty_and_wordless()
    {
        Assert.IsFalse(DSA040Analyzer.IsSecuritySensitiveName(null));
        Assert.IsFalse(DSA040Analyzer.IsSecuritySensitiveName(string.Empty));
        Assert.IsFalse(DSA040Analyzer.IsSecuritySensitiveName("_"));   // splits to zero words
        Assert.IsFalse(DSA040Analyzer.IsSecuritySensitiveName("___"));
    }

    [TestMethod]
    public void SplitIntoWords_splits_on_underscore_camelCase_and_digit_boundaries()
    {
        CollectionAssert.AreEqual(new[] { "api", "key" }, DSA040Analyzer.SplitIntoWords("apiKey"));
        CollectionAssert.AreEqual(new[] { "session", "id" }, DSA040Analyzer.SplitIntoWords("session_id"));
        CollectionAssert.AreEqual(new[] { "reset", "code" }, DSA040Analyzer.SplitIntoWords("ResetCode"));
        CollectionAssert.AreEqual(new[] { "a", "1", "b" }, DSA040Analyzer.SplitIntoWords("a1b"));
        Assert.AreEqual(0, DSA040Analyzer.SplitIntoWords("_").Count);
    }

    [TestMethod]
    public void NameOf_returns_identifier_member_name_or_null()
    {
        Assert.AreEqual("x", DSA040Analyzer.NameOf(SyntaxFactory.ParseExpression("x")));
        Assert.AreEqual("Secret", DSA040Analyzer.NameOf(SyntaxFactory.ParseExpression("obj.Secret")));
        Assert.IsNull(DSA040Analyzer.NameOf(SyntaxFactory.ParseExpression("1 + 2")));
    }

    private static ExpressionSyntax InnermostCall(string expression)
    {
        var root = CSharpSyntaxTree.ParseText(expression).GetRoot();
        // the deepest InvocationExpression is the produced value (e.g. rnd.Next())
        return root.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .OrderByDescending(i => i.SpanStart).First(i => i.Expression is MemberAccessExpressionSyntax);
    }

    [TestMethod]
    public void ClimbValuePreservingWrappers_climbs_parens_casts_members_and_invocations()
    {
        // rnd.Next() wrapped by parens -> cast -> .ToString() invocation: the climb returns the outermost consumer.
        var call = InnermostCall("class C { void M(System.Random rnd) { var s = (((int)(rnd.Next()))).ToString(); } }");
        var climbed = DSA040Analyzer.ClimbValuePreservingWrappers(call);
        Assert.IsTrue(climbed.Span.Length >= call.Span.Length);
        Assert.IsInstanceOfType(climbed.Parent, typeof(EqualsValueClauseSyntax));
    }

    [TestMethod]
    public void ClimbValuePreservingWrappers_returns_value_unchanged_when_not_wrapped()
    {
        var call = InnermostCall("class C { void M(System.Random rnd) { int token = rnd.Next(); } }");
        var climbed = DSA040Analyzer.ClimbValuePreservingWrappers(call);
        Assert.AreSame(call, climbed);
    }
}
