using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Unit tests for the default matching of <see cref="ProhibitSimpleMemberAccessExpressionDiagnosticAnalyzer{T}"/>,
/// exercised through a minimal analyzer that prohibits <c>System.Guid.Empty</c>.
/// </summary>
[TestClass]
public class ProhibitSimpleMemberAccessExpressionDiagnosticAnalyzerTests
{
    private static readonly DiagnosticDescriptor Rule = new("TEST002", "title", "message", "category", DiagnosticSeverity.Warning, true);

    private sealed class ProhibitGuidEmptyAnalyzer : ProhibitSimpleMemberAccessExpressionDiagnosticAnalyzer<Guid>
    {
        protected override string MemberName => nameof(Guid.Empty);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
        public override void Initialize(AnalysisContext context) { }

        public bool MatchesMemberAccess(MemberAccessExpressionSyntax node) => IsMemberAccessExpressionMatched(node, Rule);
        public bool MatchesIdentifier(IdentifierNameSyntax node) => IsIdentifierNameMatched(node, Rule);
    }

    private static string InMethod(string statement, string usings = "") =>
        usings + "class C { void M() { " + statement + " } }";

    private static bool MatchesMemberAccess(string code)
    {
        var node = CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().First();
        return new ProhibitGuidEmptyAnalyzer().MatchesMemberAccess(node);
    }

    private static bool MatchesIdentifier(string code)
    {
        var node = CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.ValueText == "Empty" || id.Identifier.ValueText == "Other");
        return new ProhibitGuidEmptyAnalyzer().MatchesIdentifier(node);
    }

    // ---- "Type.Member" ----------------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("var g = Guid.Empty;")]
    [DataRow("var g = System.Guid.Empty;")]
    public void IsMemberAccessExpressionMatched_true_for_the_type_and_the_member(string statement)
    {
        Assert.IsTrue(MatchesMemberAccess(InMethod(statement)));
    }

    [TestMethod]
    [DataRow("var g = Guid.NewGuid;", "")] // another member
    [DataRow("var g = Other.Empty;", "")] // another type
    [DataRow("var g = a.b.Empty;", "")]
    [DataRow("var g = Other.Empty;", "using System;")] // not a static using
    [DataRow("var g = Other.Empty;", "using static System.Math;")] // static using of another type
    [DataRow("var g = Other.Empty;", "using Guid = System.Guid;")]
    public void IsMemberAccessExpressionMatched_false_otherwise(string statement, string usings)
    {
        Assert.IsFalse(MatchesMemberAccess(InMethod(statement, usings)));
    }

    [TestMethod]
    public void IsMemberAccessExpressionMatched_false_for_a_detached_node_without_compilation_unit()
    {
        var node = (MemberAccessExpressionSyntax)SyntaxFactory.ParseExpression("Other.Empty");
        Assert.IsFalse(new ProhibitGuidEmptyAnalyzer().MatchesMemberAccess(node));
    }

    [TestMethod]
    public void IsMemberAccessExpressionMatched_throws_on_null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ProhibitGuidEmptyAnalyzer().MatchesMemberAccess(null));
    }

    // ---- "Member" imported with "using static" ----------------------------------------------------------------

    [TestMethod]
    [DataRow("using static System.Guid;")]
    [DataRow("using static global::System.Guid;")]
    public void IsIdentifierNameMatched_true_when_the_type_is_imported_with_using_static(string usings)
    {
        Assert.IsTrue(MatchesIdentifier(InMethod("var g = Empty;", usings)));
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("using System;")]
    [DataRow("using static System.Math;")]
    public void IsIdentifierNameMatched_false_without_a_matching_using_static(string usings)
    {
        Assert.IsFalse(MatchesIdentifier(InMethod("var g = Empty;", usings)));
    }

    [TestMethod]
    public void IsIdentifierNameMatched_false_for_another_name()
    {
        Assert.IsFalse(MatchesIdentifier(InMethod("var g = Other;", "using static System.Guid;")));
    }

    [TestMethod]
    public void IsIdentifierNameMatched_false_for_a_detached_node()
    {
        var node = (IdentifierNameSyntax)SyntaxFactory.ParseExpression("Empty");
        Assert.IsFalse(new ProhibitGuidEmptyAnalyzer().MatchesIdentifier(node));
    }

    [TestMethod]
    public void IsIdentifierNameMatched_throws_on_null()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ProhibitGuidEmptyAnalyzer().MatchesIdentifier(null));
    }
}
