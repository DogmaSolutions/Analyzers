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
}
