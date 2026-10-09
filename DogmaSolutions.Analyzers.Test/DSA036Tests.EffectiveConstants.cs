using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Unit tests for <c>DSA036Analyzer.IsEffectivelyConstant</c>: can an expression be considered a constant, so that
/// a regex built from it can be hoisted into a static field.
/// </summary>
public partial class DSA036Tests
{
    private static bool IsEffectivelyConstantExpression(string expression)
    {
        var source = @"
using System;
class C
{
    enum E { A }
    const int K = 3;
    const string S = ""s"";
    static readonly int SR = 4;
    readonly int IR = 5;
    int F;
    static int SF;
    static int SProp { get { return 1; } }
    int Prop { get { return 1; } }
    int M(int p)
    {
        const int cl = 1;
        var el = 2;
        var ml = 3;
        ml++;
        var pl = p;
        var test = " + expression + @";
        return 0;
    }
}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(System.IO.Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));
        var compilation = CSharpCompilation.Create(
            "EffectiveConstants", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);

        var declarator = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>().Single(d => d.Identifier.ValueText == "test");
        var scope = declarator.Ancestors().OfType<BlockSyntax>().First();
        return DSA036Analyzer.IsEffectivelyConstant(declarator.Initializer.Value, model, scope);
    }

    [TestMethod]
    [DataRow("5")]
    [DataRow("\"text\"")]
    [DataRow("default(int)")]
    [DataRow("default(string)")]
    [DataRow("(5)")]
    [DataRow("(long)K")]
    [DataRow("K + 1")]
    [DataRow("K * 2 - 1")]
    [DataRow("-K")]
    [DataRow("~K")]
    [DataRow("typeof(string)")]
    [DataRow("typeof(C)")]
    [DataRow("K")]
    [DataRow("S")]
    [DataRow("C.K")]
    [DataRow("SR")]
    [DataRow("C.SR")]
    [DataRow("E.A")]
    [DataRow("int.MaxValue")]
    [DataRow("string.Empty")]
    [DataRow("(int)SR + K * 2")]
    [DataRow("$\"abc{K}\"")]
    [DataRow("$\"abc{SR}def\"")]
    [DataRow("$\"abc\"")]
    [DataRow("cl")]
    [DataRow("el")]
    [DataRow("el + K")]
    [DataRow("$\"x{el}\"")]
    public void IsEffectivelyConstant_true(string expression)
    {
        Assert.IsTrue(IsEffectivelyConstantExpression(expression));
    }

    [TestMethod]
    [DataRow("p")]
    [DataRow("F")]
    [DataRow("IR")]
    [DataRow("SF")]
    [DataRow("Prop")]
    [DataRow("SProp")]
    [DataRow("this.F")]
    [DataRow("p + 1")]
    [DataRow("1 + p")]
    [DataRow("K + p")]
    [DataRow("-p")]
    [DataRow("(int)p")]
    [DataRow("(p)")]
    [DataRow("$\"abc{p}\"")]
    [DataRow("$\"abc{F}def{K}\"")]
    [DataRow("M(1)")]
    [DataRow("new object().GetHashCode()")]
    [DataRow("ml")]
    [DataRow("pl")]
    [DataRow("ml + K")]
    [DataRow("p.ToString()")]
    [DataRow("Unknown")]
    [DataRow("Unknown.Member")]
    public void IsEffectivelyConstant_false(string expression)
    {
        Assert.IsFalse(IsEffectivelyConstantExpression(expression));
    }

    [TestMethod]
    public void IsEffectivelyConstant_false_for_a_null_expression()
    {
        Assert.IsFalse(DSA036Analyzer.IsEffectivelyConstant(null, null, null));
    }
}
