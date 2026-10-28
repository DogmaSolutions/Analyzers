using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The answer "is this class a Web API controller" is kept per compilation: an edit of another file must change it.</summary>
[TestClass]
public class AnalyzersUtilsControllerTests
{
    private const string Framework = "namespace Microsoft.AspNetCore.Mvc { public class ControllerBase { } }";
    private const string Child = "namespace App { public class Child : Parent { } }";

    private static CSharpCompilation Create(SyntaxTree child, string parent) =>
        CSharpCompilation.Create(
            "test",
            [CSharpSyntaxTree.ParseText(Framework), CSharpSyntaxTree.ParseText(parent), child],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static bool Ask(CSharpCompilation compilation, SyntaxTree tree) =>
        AnalyzersUtils.IsWebApiControllerClass(tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().Single(), compilation.GetSemanticModel(tree));

    [TestMethod]
    public void The_answer_follows_an_edit_of_the_file_of_the_base_class()
    {
        // the file of the class is the same tree object in both compilations, as it is in an IDE where only the other file was edited
        var child = CSharpSyntaxTree.ParseText(Child);

        var before = Create(child, "namespace App { public class Parent { } }");
        var after = Create(child, "namespace App { public class Parent : Microsoft.AspNetCore.Mvc.ControllerBase { } }");

        Assert.IsFalse(Ask(before, child));
        Assert.IsTrue(Ask(after, child));
        Assert.IsFalse(Ask(before, child));
    }

    [TestMethod]
    public void A_class_without_base_types_is_not_a_controller()
    {
        var tree = CSharpSyntaxTree.ParseText("namespace App { public class Alone { } }");
        var compilation = Create(tree, "namespace App { public class Parent { } }");

        Assert.IsFalse(Ask(compilation, tree));
    }
}
