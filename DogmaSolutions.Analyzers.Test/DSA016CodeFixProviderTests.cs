using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The helpers of the code fix of DSA016.</summary>
[TestClass]
public class DSA016CodeFixProviderTests
{
    private const string Source = "class C { object M(System.Collections.Generic.List<string> items) { return items?.Where(x => x != null).ToList(); } }";

    [TestMethod]
    public void The_invocation_of_a_diagnostic_on_its_whole_span_is_found()
    {
        var root = CSharpSyntaxTree.ParseText(Source).GetRoot();
        var invocation = root.DescendantNodes().OfType<InvocationExpressionSyntax>().First(candidate => candidate.Expression is MemberAccessExpressionSyntax);

        Assert.AreSame(invocation, DSA016CodeFixProvider.FindInvocation(root, invocation.Span));
    }

    [TestMethod]
    public void The_invocation_of_a_conditional_access_is_found_from_its_member_binding()
    {
        var root = CSharpSyntaxTree.ParseText(Source).GetRoot();
        var binding = root.DescendantNodes().OfType<MemberBindingExpressionSyntax>().First();

        var invocation = DSA016CodeFixProvider.FindInvocation(root, binding.Span);

        Assert.IsNotNull(invocation);
        Assert.IsInstanceOfType(invocation.Expression, typeof(MemberBindingExpressionSyntax));
    }

    [TestMethod]
    public void Nothing_is_found_where_there_is_no_invocation()
    {
        var root = CSharpSyntaxTree.ParseText("class C { int F = 1; }").GetRoot();

        Assert.IsNull(DSA016CodeFixProvider.FindInvocation(root, root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First().Span));
    }

    [TestMethod]
    [DataRow("short", "short")]
    [DataRow("123456789012345678901234567890123456789012345678901234567890", "123456789012345678901234567890123456789012345678901234567890")]
    [DataRow("1234567890123456789012345678901234567890123456789012345678901", "123456789012345678901234567890123456789012345678901234567...")]
    public void A_long_text_is_abbreviated(string text, string expected)
    {
        Assert.AreEqual(expected, DSA016CodeFixProvider.Abbreviate(text));
    }
}
