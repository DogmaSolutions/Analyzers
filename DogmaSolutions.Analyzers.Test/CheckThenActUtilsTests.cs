using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Unit tests for <see cref="CheckThenActUtils"/>, the shared check-then-act pattern detection used by DSA012, DSA017 and DSA018.
/// This file holds the shared helpers; the tests are split per topic in the sibling partial files.
/// </summary>
[TestClass]
public partial class CheckThenActUtilsTests
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> RuntimeReferences = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
        .Split(System.IO.Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToList());

    private static ExpressionSyntax ParseCondition(string expression) => SyntaxFactory.ParseExpression(expression);

    /// <summary>
    /// Parses the statements into the body of a method, and returns the first <c>if</c> statement found (in source order).
    /// </summary>
    private static IfStatementSyntax ParseIf(string statements)
    {
        var tree = CSharpSyntaxTree.ParseText("class C { void M() { " + statements + " } }");
        return tree.GetRoot().DescendantNodes().OfType<IfStatementSyntax>().First();
    }

    private static CSharpCompilation CreateCompilation(string source = "")
    {
        return CSharpCompilation.Create(
            "CheckThenActUtilsTests",
            new[] { CSharpSyntaxTree.ParseText(source) },
            RuntimeReferences.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    private static INamedTypeSymbol GetType(CSharpCompilation compilation, string metadataName)
    {
        var type = compilation.GetTypeByMetadataName(metadataName);
        Assert.IsNotNull(type, "Type not found: " + metadataName);
        return type;
    }
}
