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
/// Unit tests for <see cref="EndpointAuthorizationUtils.HasAuthOnSymbolLocally"/>: is authorization configured on a
/// builder / route group symbol through its initializer, through separate calls or through assignments.
/// </summary>
[TestClass]
public class EndpointAuthorizationUtilsTests
{
    private static readonly Lazy<IReadOnlyList<MetadataReference>> RuntimeReferences = new(() =>
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
        .Split(System.IO.Path.PathSeparator)
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToList());

    /// <summary>A minimal route builder, and a method whose body is the given statements.</summary>
    private static string Source(string statements, string members = "") => @"
namespace TestApp
{
    public class Builder
    {
        public Builder MapGroup(string pattern) => this;
        public Builder MapGet(string pattern) => this;
        public Builder RequireAuthorization() => this;
        public Builder AllowAnonymous() => this;
    }
    public class Endpoints
    {
" + members + @"
        public void Configure(Builder app, Builder otherApp)
        {
" + statements + @"
        }
    }
}";

    private static (ISymbol Symbol, SemanticModel Model) GetSymbol(string source, string name)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "EndpointAuthorizationUtilsTests",
            new[] { tree },
            RuntimeReferences.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var root = tree.GetRoot();

        ISymbol symbol = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().Where(d => d.Identifier.ValueText == name)
            .Select(d => model.GetDeclaredSymbol(d)).FirstOrDefault()
            ?? root.DescendantNodes().OfType<ParameterSyntax>().Where(p => p.Identifier.ValueText == name)
                .Select(p => model.GetDeclaredSymbol(p)).FirstOrDefault();

        Assert.IsNotNull(symbol, "Symbol not found: " + name);
        return (symbol, model);
    }

    private static bool HasAuth(string statements, string symbolName = "group", string members = "")
    {
        var (symbol, model) = GetSymbol(Source(statements, members), symbolName);
        return EndpointAuthorizationUtils.HasAuthOnSymbolLocally(symbol, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
    }

    // ---- Declaration initializer ------------------------------------------------------------------------------

    [TestMethod]
    [DataRow(@"var group = app.MapGroup(""/x"").RequireAuthorization();")]
    [DataRow(@"var group = app.MapGroup(""/x"").AllowAnonymous();")]
    [DataRow(@"var group = app.MapGroup(""/x"");" + "\n" + @"group.RequireAuthorization();")]
    [DataRow(@"var group = app.MapGroup(""/x"");" + "\n" + @"group.AllowAnonymous();")]
    public void HasAuthOnSymbolLocally_true_for_auth_in_the_initializer_or_in_a_separate_call(string statements)
    {
        Assert.IsTrue(HasAuth(statements));
    }

    [TestMethod]
    [DataRow(@"var group = app.MapGroup(""/x"");")]
    [DataRow(@"var group = app.MapGroup(""/x"").MapGet(""/y"");")]
    [DataRow(@"Builder group;")]
    [DataRow(@"var group = app.MapGroup(""/x"");" + "\n" + @"group.MapGet(""/y"");")]
    [DataRow(@"var group = app.MapGroup(""/x"");" + "\n" + @"otherApp.RequireAuthorization();")]
    [DataRow(@"var group = app.MapGroup(""/x"");" + "\n" + @"Helper();" + "\n" + @"void Helper() { }")]
    public void HasAuthOnSymbolLocally_false_without_auth(string statements)
    {
        Assert.IsFalse(HasAuth(statements));
    }

    // ---- Groups inheriting the auth of their parent -----------------------------------------------------------

    [TestMethod]
    public void HasAuthOnSymbolLocally_true_when_the_parent_group_has_auth()
    {
        Assert.IsTrue(HasAuth(@"
            var parent = app.MapGroup(""/p"").RequireAuthorization();
            var group = parent.MapGroup(""/x"");"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_true_when_the_grandparent_group_has_auth()
    {
        Assert.IsTrue(HasAuth(@"
            var root = app.MapGroup(""/r"");
            root.RequireAuthorization();
            var parent = root.MapGroup(""/p"");
            var group = parent.MapGroup(""/x"");"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_when_the_parent_group_has_no_auth()
    {
        Assert.IsFalse(HasAuth(@"
            var parent = app.MapGroup(""/p"");
            var group = parent.MapGroup(""/x"");"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_when_the_group_receiver_does_not_resolve()
    {
        Assert.IsFalse(HasAuth(@"var group = Unknown.MapGroup(""/x"");"));
    }

    // ---- Assignments ------------------------------------------------------------------------------------------

    [TestMethod]
    public void HasAuthOnSymbolLocally_true_when_an_authorized_value_is_assigned()
    {
        Assert.IsTrue(HasAuth(@"
            Builder group = null;
            group = app.MapGroup(""/x"").RequireAuthorization();"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_true_when_a_group_of_an_authorized_parent_is_assigned()
    {
        Assert.IsTrue(HasAuth(@"
            var parent = app.MapGroup(""/p"").RequireAuthorization();
            Builder group = null;
            group = parent.MapGroup(""/x"");"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_when_a_group_of_an_unauthorized_parent_is_assigned()
    {
        Assert.IsFalse(HasAuth(@"
            var parent = app.MapGroup(""/p"");
            Builder group = null;
            group = parent.MapGroup(""/x"");"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_when_a_group_with_an_unresolved_receiver_is_assigned()
    {
        Assert.IsFalse(HasAuth(@"
            Builder group = null;
            group = Unknown.MapGroup(""/x"");"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_when_a_plain_value_is_assigned()
    {
        Assert.IsFalse(HasAuth(@"
            Builder group = null;
            group = app;"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_ignores_assignments_to_other_symbols()
    {
        Assert.IsFalse(HasAuth(@"
            var group = app.MapGroup(""/x"");
            Builder other = null;
            other = app.MapGroup(""/y"").RequireAuthorization();"));
    }

    // ---- Other kinds of symbols -------------------------------------------------------------------------------

    [TestMethod]
    public void HasAuthOnSymbolLocally_true_for_a_parameter_on_which_auth_is_called()
    {
        var source = @"
namespace TestApp
{
    public class Builder { public Builder RequireAuthorization() => this; }
    public class Endpoints { public void Configure(Builder group) { group.RequireAuthorization(); } }
}";
        var (symbol, model) = GetSymbol(source, "group");
        Assert.IsTrue(EndpointAuthorizationUtils.HasAuthOnSymbolLocally(symbol, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default)));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_for_a_parameter_without_auth()
    {
        var source = @"
namespace TestApp
{
    public class Builder { public Builder MapGet(string p) => this; }
    public class Endpoints { public void Configure(Builder group) { group.MapGet(""/x""); } }
}";
        var (symbol, model) = GetSymbol(source, "group");
        Assert.IsFalse(EndpointAuthorizationUtils.HasAuthOnSymbolLocally(symbol, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default)));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_true_for_a_field_with_auth_in_its_initializer()
    {
        Assert.IsTrue(HasAuth(string.Empty, "Group", members: @"
        private static readonly Builder Group = new Builder().MapGroup(""/x"").RequireAuthorization();"));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_for_a_symbol_without_source()
    {
        var compilation = CSharpCompilation.Create("NoSource", new[] { CSharpSyntaxTree.ParseText("class C { }") }, RuntimeReferences.Value);
        var symbol = compilation.GetSpecialType(SpecialType.System_String);
        var model = compilation.GetSemanticModel(compilation.SyntaxTrees.Single());

        Assert.IsFalse(EndpointAuthorizationUtils.HasAuthOnSymbolLocally(symbol, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default)));
    }

    [TestMethod]
    public void HasAuthOnSymbolLocally_false_the_second_time_for_the_same_symbol_and_visited_set()
    {
        var (symbol, model) = GetSymbol(Source(@"var group = app.MapGroup(""/x"").RequireAuthorization();"), "group");
        var visited = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        Assert.IsTrue(EndpointAuthorizationUtils.HasAuthOnSymbolLocally(symbol, model, visited));
        Assert.IsFalse(EndpointAuthorizationUtils.HasAuthOnSymbolLocally(symbol, model, visited));
    }
}
