using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Unit tests for the shared "is the caret on the type declaration header?" helper used to gate the type-level
/// refactorings to the declaration line.
/// </summary>
[TestClass]
public class AnalyzersUtilsTypeHeaderTests
{
    private static TypeDeclarationSyntax ParseType(string code) =>
        CSharpSyntaxTree.ParseText(code).GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();

    [TestMethod]
    public void HeaderSpan_runs_from_declaration_start_to_the_open_brace()
    {
        var type = ParseType("public sealed class Foo : Bar\n{\n    void M() { }\n}");
        var header = AnalyzersUtils.GetTypeDeclarationHeaderSpan(type);
        Assert.AreEqual(type.SpanStart, header.Start);
        Assert.AreEqual(type.OpenBraceToken.SpanStart, header.End);
    }

    [TestMethod]
    public void IsSpanOnTypeDeclarationHeader_true_when_span_is_on_the_declaration_line()
    {
        var type = ParseType("public class Foo\n{\n    void M() { }\n}");
        // The identifier is inside the header.
        Assert.IsTrue(AnalyzersUtils.IsSpanOnTypeDeclarationHeader(type, type.Identifier.Span));
        // The 'class' keyword is inside the header.
        Assert.IsTrue(AnalyzersUtils.IsSpanOnTypeDeclarationHeader(type, type.Keyword.Span));
    }

    [TestMethod]
    public void IsSpanOnTypeDeclarationHeader_false_when_span_is_in_the_body()
    {
        var type = ParseType("public class Foo\n{\n    void M() { }\n}");
        var methodBody = type.DescendantNodes().OfType<MethodDeclarationSyntax>().First().Body!.Span;
        Assert.IsFalse(AnalyzersUtils.IsSpanOnTypeDeclarationHeader(type, methodBody));
    }

    [TestMethod]
    public void HeaderSpan_handles_a_declaration_with_no_body_braces()
    {
        // Malformed/partial: no body braces (the parser supplies a zero-width missing open brace). The helper must
        // not throw, must keep the identifier inside the header, and must not run past the declaration.
        var type = ParseType("public class Foo");
        var header = AnalyzersUtils.GetTypeDeclarationHeaderSpan(type);
        Assert.IsTrue(header.End >= type.Identifier.Span.End);
        Assert.IsTrue(AnalyzersUtils.IsSpanOnTypeDeclarationHeader(type, type.Identifier.Span));
    }
}
