using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Unit tests for the helpers of <see cref="SyntaxUtils"/>.
/// </summary>
[TestClass]
public class SyntaxUtilsTests
{
    private static SyntaxNode GetMarker(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<IdentifierNameSyntax>().First(id => id.Identifier.ValueText == "marker");

    // ---- GetContainingScope -----------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("class C { void M() { var a = marker; } }", typeof(BlockSyntax))]
    [DataRow("class C { int M() => marker; }", typeof(IdentifierNameSyntax))]
    [DataRow("class C { C() { var a = marker; } }", typeof(BlockSyntax))]
    [DataRow("class C { int _f; C() => _f = marker; }", typeof(AssignmentExpressionSyntax))]
    [DataRow("class C { int P { get { return marker; } } }", typeof(BlockSyntax))]
    [DataRow("class C { int P { get => marker; } }", typeof(IdentifierNameSyntax))]
    [DataRow("class C { void M() { void Local() { var a = marker; } } }", typeof(BlockSyntax))]
    [DataRow("class C { void M() { int Local() => marker; } }", typeof(IdentifierNameSyntax))]
    [DataRow("class C { void M() { System.Func<int, int> f = x => marker; } }", typeof(IdentifierNameSyntax))]
    [DataRow("class C { void M() { System.Func<int, int> f = x => { return marker; }; } }", typeof(BlockSyntax))]
    [DataRow("class C { void M() { System.Func<int, int, int> f = (x, y) => marker; } }", typeof(IdentifierNameSyntax))]
    [DataRow("class C { void M() { System.Func<int> f = delegate { return marker; }; } }", typeof(BlockSyntax))]
    [DataRow("var a = marker;", typeof(CompilationUnitSyntax))]
    public void GetContainingScope_returns_the_body_of_the_innermost_container(string code, System.Type expectedKind)
    {
        var scope = SyntaxUtils.GetContainingScope(GetMarker(code));

        Assert.IsNotNull(scope);
        Assert.IsInstanceOfType(scope, expectedKind);
    }

    [TestMethod]
    public void GetContainingScope_prefers_the_innermost_container()
    {
        var marker = GetMarker("class C { void M() { System.Func<int, int> f = x => { return marker; }; } }");
        var scope = SyntaxUtils.GetContainingScope(marker);

        Assert.IsInstanceOfType<BlockSyntax>(scope);
        Assert.IsInstanceOfType<SimpleLambdaExpressionSyntax>(scope.Parent);
    }

    [TestMethod]
    public void GetContainingScope_is_null_for_a_member_without_body()
    {
        // An abstract method has no body to be the scope of anything: the search stops there.
        var marker = GetMarker("abstract class C { abstract int M(int a = marker); }");
        Assert.IsNull(SyntaxUtils.GetContainingScope(marker));
    }

    [TestMethod]
    public void GetContainingScope_is_null_for_a_detached_node()
    {
        var node = SyntaxFactory.ParseExpression("marker");
        Assert.IsNull(SyntaxUtils.GetContainingScope(node));
    }

    // ---- loops and nested functions ---------------------------------------------------------------------------

    [TestMethod]
    [DataRow("for (;;) { marker; }", typeof(ForStatementSyntax))]
    [DataRow("foreach (var a in b) { marker; }", typeof(ForEachStatementSyntax))]
    [DataRow("foreach (var (a, c) in b) { marker; }", typeof(ForEachVariableStatementSyntax))]
    [DataRow("while (true) { marker; }", typeof(WhileStatementSyntax))]
    [DataRow("do { marker; } while (true);", typeof(DoStatementSyntax))]
    [DataRow("while (true) marker;", typeof(WhileStatementSyntax))]
    public void FindEnclosingLoop_and_GetLoopBody_cover_every_loop_form(string statement, System.Type loopKind)
    {
        var marker = GetMarker("class C { void M() { " + statement + " } }");

        var loop = SyntaxUtils.FindEnclosingLoop(marker);

        Assert.IsNotNull(loop);
        Assert.IsInstanceOfType(loop, loopKind);
        Assert.IsTrue(SyntaxUtils.IsLoopStatement(loop));
        Assert.IsTrue(SyntaxUtils.GetLoopBody(loop).Span.Contains(marker.Span));
    }

    [TestMethod]
    [DataRow("while (true) { System.Action a = () => { marker; }; }")]
    [DataRow("while (true) { System.Action a = x => marker; }")]
    [DataRow("while (true) { System.Action a = delegate { marker; }; }")]
    [DataRow("while (true) { void Local() { marker; } }")]
    public void FindEnclosingLoop_stops_at_a_function_boundary(string statement)
    {
        var marker = GetMarker("class C { void M() { " + statement + " } }");

        Assert.IsNull(SyntaxUtils.FindEnclosingLoop(marker));
    }

    [TestMethod]
    public void GetLoopBody_of_a_non_loop_is_null()
    {
        Assert.IsNull(SyntaxUtils.GetLoopBody(SyntaxFactory.ParseStatement("{ }")));
        Assert.IsFalse(SyntaxUtils.IsLoopStatement(SyntaxFactory.ParseStatement("{ }")));
    }

    [TestMethod]
    [DataRow("for (;;) { for (;;) { marker; } }", true)]
    [DataRow("for (;;) { foreach (var (a, c) in b) { marker; } }", true)]
    [DataRow("for (;;) { while (true) marker; }", true)]
    [DataRow("for (;;) { if (x) { marker; } }", false)]
    [DataRow("for (;;) { marker; }", false)]
    public void IsInsideNestedLoop_only_looks_below_the_analyzed_loop_body(string statement, bool expected)
    {
        var marker = GetMarker("class C { void M() { " + statement + " } }");
        var outer = marker.Ancestors().OfType<StatementSyntax>().Last(SyntaxUtils.IsLoopStatement);

        Assert.AreEqual(expected, SyntaxUtils.IsInsideNestedLoop(marker, SyntaxUtils.GetLoopBody(outer)));
    }

    [TestMethod]
    [DataRow("class C { void M() { System.Action a = () => { marker; }; } }", true)]
    [DataRow("class C { void M() { System.Action a = delegate { marker; }; } }", true)]
    [DataRow("class C { void M() { void Local() { marker; } } }", true)]
    [DataRow("class C { void M() { marker; } }", false)]
    public void IsInsideNestedFunction_detects_lambdas_anonymous_methods_and_local_functions(string code, bool expected)
    {
        var marker = GetMarker(code);
        var method = marker.Ancestors().OfType<MethodDeclarationSyntax>().First();

        Assert.AreEqual(expected, SyntaxUtils.IsInsideNestedFunction(marker, method));
    }

    // ---- syntactic pre-filters ---------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("M(a + b)", true)]
    [DataRow("M((a + b))", true)]
    [DataRow("M(x, ((a + b)))", true)]
    [DataRow("M(name: a + b)", true)]
    [DataRow("M(a)", false)]
    [DataRow("M(a - b)", false)]
    [DataRow("M(a, b)", false)]
    [DataRow("M(f(a + b))", false)]
    [DataRow("M()", false)]
    public void HasAddExpressionArgument_looks_through_parentheses_only(string invocation, bool expected)
    {
        var syntax = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression(invocation);

        Assert.AreEqual(expected, SyntaxUtils.HasAddExpressionArgument(syntax.ArgumentList));
    }

    [TestMethod]
    public void HasAddExpressionArgument_of_a_missing_list_is_false()
    {
        Assert.IsFalse(SyntaxUtils.HasAddExpressionArgument(null));
    }

    [TestMethod]
    [DataRow("Next()", "Next")]
    [DataRow("rnd.Next()", "Next")]
    [DataRow("a.b.NextBytes(x)", "NextBytes")]
    [DataRow("rnd.Next<int>()", "Next")]
    [DataRow("Next<int>()", "Next")]
    public void GetInvokedName_returns_the_simple_name_of_the_callee(string invocation, string expected)
    {
        var syntax = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression(invocation);
        Assert.AreEqual(expected, SyntaxUtils.GetInvokedName(syntax));
    }

    [TestMethod]
    public void GetInvokedName_of_a_conditional_access_call_is_found_through_the_member_binding()
    {
        var conditional = (ConditionalAccessExpressionSyntax)SyntaxFactory.ParseExpression("rnd?.Next()");

        Assert.AreEqual("Next", SyntaxUtils.GetInvokedName((InvocationExpressionSyntax)conditional.WhenNotNull));
    }

    [TestMethod]
    [DataRow("f()()")]
    [DataRow("d[0]()")]
    public void GetInvokedName_of_a_callee_that_is_not_a_plain_name_is_null(string invocation)
    {
        var syntax = (InvocationExpressionSyntax)SyntaxFactory.ParseExpression(invocation);

        Assert.IsNull(SyntaxUtils.GetInvokedName(syntax));
    }
}
