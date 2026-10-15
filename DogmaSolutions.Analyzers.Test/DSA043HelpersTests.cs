using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DataRow

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The helpers of DSA043: eligibility of a type, kind names, logger namespaces.</summary>
[TestClass]
public class DSA043HelpersTests
{
    [TestMethod]
    [DataRow("class C { }", "class")]
    [DataRow("record C;", "record")]
    [DataRow("struct C { }", "struct")]
    [DataRow("record struct C;", "record struct")]
    [DataRow("interface C { }", null)]
    [DataRow("enum C { A }", null)]
    [DataRow("delegate void C();", null)]
    public void GetKindName_names_the_kinds_of_type_that_are_looked_at(string declaration, string expected)
    {
        var type = DSA043TestSupport.GetType(declaration);

        Assert.AreEqual(expected, DSA043Eligibility.GetKindName(type));
    }

    [TestMethod]
    public void IsSourceType_accepts_a_type_written_in_the_source()
    {
        Assert.IsTrue(DSA043Eligibility.IsSourceType(DSA043TestSupport.GetType("class C { }")));
    }

    [TestMethod]
    public void IsSourceType_rejects_metadata_implicit_and_anonymous_types()
    {
        var tree = CSharpSyntaxTree.ParseText("class C { object O = new { A = 1 }; }");
        var compilation = DSA043TestSupport.CreateCompilation(tree);
        var model = compilation.GetSemanticModel(tree);
        var anonymous = (INamedTypeSymbol)model.GetTypeInfo(tree.GetRoot().DescendantNodes().OfType<AnonymousObjectCreationExpressionSyntax>().Single()).Type;

        Assert.IsFalse(DSA043Eligibility.IsSourceType(compilation.GetSpecialType(SpecialType.System_String)), "metadata type");
        Assert.IsFalse(DSA043Eligibility.IsSourceType(anonymous), "anonymous type (implicitly declared)");
        Assert.IsFalse(DSA043Eligibility.IsSourceType(compilation.GetTypeByMetadataName("C").GetMembers().OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.Constructor).ContainingType.BaseType), "System.Object");
    }

    [TestMethod]
    [DataRow("A.B", "A.B", true)]
    [DataRow("A.B.C", "A.B", true)]
    [DataRow("A.BC", "A.B", false)]
    [DataRow("A", "A.B", false)]
    [DataRow("", "A", false)]
    public void IsInNamespace_matches_the_namespace_and_its_children(string typeNamespace, string namespaceName, bool expected)
    {
        Assert.AreEqual(expected, LoggerDetector.IsInNamespace(typeNamespace, namespaceName));
    }

    [TestMethod]
    public void Logging_is_available_when_a_logger_namespace_or_type_exists_in_the_compilation()
    {
        var tree = CSharpSyntaxTree.ParseText(DSA043TestSupport.FakeLoggingSource);
        var withLogging = DSA043TestSupport.CreateCompilation(tree);
        var without = DSA043TestSupport.CreateCompilation();

        DSA043Settings Settings(string namespaces, string types) =>
            new(new FakeAnalyzerConfigOptions((DSA043Settings.LoggerNamespacesKey, namespaces), (DSA043Settings.LoggerTypesKey, types)));

        Assert.IsTrue(LoggerDetector.IsLoggingAvailable(withLogging, Settings("Microsoft.Extensions.Logging", string.Empty)));
        Assert.IsTrue(LoggerDetector.IsLoggingAvailable(withLogging, Settings("Nothing.Here", "MyCompany.Diagnostics.IAppLogger")));
        Assert.IsFalse(LoggerDetector.IsLoggingAvailable(withLogging, Settings("Nothing.Here", "Nothing.Here.IType")));
        Assert.IsFalse(LoggerDetector.IsLoggingAvailable(withLogging, Settings("Microsoft.Extensions.Missing", string.Empty)));
        Assert.IsFalse(LoggerDetector.IsLoggingAvailable(without, Settings("Microsoft.Extensions.Logging", string.Empty)));
    }

    [TestMethod]
    public void IsLoggingType_is_false_for_what_is_not_a_named_type()
    {
        var compilation = DSA043TestSupport.CreateCompilation();
        var settings = new DSA043Settings(new FakeAnalyzerConfigOptions());

        Assert.IsFalse(LoggerDetector.IsLoggingType(compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_String)), settings));
        Assert.IsFalse(LoggerDetector.IsLoggingType(compilation.GetSpecialType(SpecialType.System_String), settings));
    }

    [TestMethod]
    public void The_analyzer_ignores_a_symbol_that_is_not_written_in_the_source()
    {
        var compilation = DSA043TestSupport.CreateCompilation();
        var reported = new System.Collections.Generic.List<Diagnostic>();
        var context = new Microsoft.CodeAnalysis.Diagnostics.SymbolAnalysisContext(
            compilation.GetSpecialType(SpecialType.System_String),
            compilation,
            new Microsoft.CodeAnalysis.Diagnostics.AnalyzerOptions(System.Collections.Immutable.ImmutableArray<AdditionalText>.Empty),
            reported.Add,
            _ => true,
            System.Threading.CancellationToken.None);

        DSA043Analyzer.AnalyzeNamedType(context);

        Assert.AreEqual(0, reported.Count);
    }
}
