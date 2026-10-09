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
/// Unit tests for the default <c>IsMatched</c> implementation of <see cref="ProhibitInvocationExpressionDiagnosticAnalyzer{T}"/>,
/// exercised through a minimal analyzer that prohibits <c>System.Guid.NewGuid()</c>.
/// </summary>
[TestClass]
public class ProhibitInvocationExpressionDiagnosticAnalyzerTests
{
    private static readonly DiagnosticDescriptor Rule = new("TEST001", "title", "message", "category", DiagnosticSeverity.Warning, true);

    private sealed class ProhibitNewGuidAnalyzer : ProhibitInvocationExpressionDiagnosticAnalyzer<Guid>
    {
        protected override string MemberName => nameof(Guid.NewGuid);
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
        public override void Initialize(AnalysisContext context) { }

        public bool Matches(InvocationExpressionSyntax invocation) => IsMatched(invocation, Rule);
    }

    private static bool Matches(string code)
    {
        var invocation = CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First();
        return new ProhibitNewGuidAnalyzer().Matches(invocation);
    }

    private static string InMethod(string statement, string usings = "") =>
        usings + "class C { void M() { " + statement + " } }";

    // ---- "Type.Member()" forms --------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("var g = Guid.NewGuid();")]
    [DataRow("var g = System.Guid.NewGuid();")]
    [DataRow("var g = (Guid.NewGuid());")]
    public void IsMatched_true_for_qualified_calls(string statement)
    {
        Assert.IsTrue(Matches(InMethod(statement)));
    }

    [TestMethod]
    [DataRow("var g = Guid.Parse(s);")] // member name differs
    [DataRow("var g = Other.NewGuid();")] // type name differs
    [DataRow("var g = a.b.NewGuid();")] // qualified, but not the full type name
    [DataRow("var g = Other.Guid.NewGuid();")]
    [DataRow("var g = GetGuid().NewGuid();")] // receiver is an invocation
    [DataRow("var g = x?.NewGuid();")] // conditional access, not a member access
    [DataRow("var g = this.NewGuid();")] // receiver is neither an identifier nor a qualified name
    public void IsMatched_false_for_other_member_access_calls(string statement)
    {
        Assert.IsFalse(Matches(InMethod(statement)));
    }

    // ---- "Member()" with "using static" -----------------------------------------------------------------------

    [TestMethod]
    [DataRow("using static System.Guid;")]
    [DataRow("using static global::System.Guid;")]
    [DataRow("using System; using static Guid;")]
    public void IsMatched_true_for_unqualified_call_when_the_type_is_imported_with_using_static(string usings)
    {
        Assert.IsTrue(Matches(InMethod("var g = NewGuid();", usings)));
    }

    [TestMethod]
    [DataRow("")] // no usings at all
    [DataRow("using System;")] // not a static using
    [DataRow("using static System.Math;")] // static using of another type
    [DataRow("using Guid = System.Guid;")] // alias, not a static using
    public void IsMatched_false_for_unqualified_call_without_a_matching_using_static(string usings)
    {
        Assert.IsFalse(Matches(InMethod("var g = NewGuid();", usings)));
    }

    [TestMethod]
    public void IsMatched_false_for_unqualified_call_with_another_name_even_if_the_type_is_imported()
    {
        Assert.IsFalse(Matches(InMethod("var g = Other();", "using static System.Guid;")));
    }

    [TestMethod]
    public void IsMatched_false_for_calls_that_are_neither_member_access_nor_identifier()
    {
        Assert.IsFalse(Matches(InMethod("var g = GetF()();", "using static System.Guid;")));
    }

    [TestMethod]
    public void IsMatched_false_when_the_invocation_has_no_compilation_unit_ancestor()
    {
        // A detached expression has no root to search the "using static" directives in.
        var invocation = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression("NewGuid()");
        Assert.IsFalse(new ProhibitNewGuidAnalyzer().Matches(invocation));
    }

    [TestMethod]
    public void IsMatched_throws_on_null_invocation()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => new ProhibitNewGuidAnalyzer().Matches(null));
    }
}
