using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// What the semantic phase of the existence analysis answers for a use that the syntactic phase would have rejected already: nothing, whatever it is asked.
/// The two phases are separate, so that each one does not rely on the other to be right.
/// </summary>
[TestClass]
public class ExistenceAnalysisTests
{
    private static async Task<(InvocationExpressionSyntax Invocation, SemanticModel Model, ExistenceCall Call)> PrepareAsync(string expression)
    {
        var source = "using System.Linq;\nusing System.Collections.Generic;\npublic class Repository\n{\n    public object Run(IEnumerable<Repository> items) { return " + expression + "; }\n}";
        var references = await ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, default).ConfigureAwait(false);
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("analysis", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var model = compilation.GetSemanticModel(tree);
        var invocation = (await tree.GetRootAsync().ConfigureAwait(false)).DescendantNodes().OfType<InvocationExpressionSyntax>().First(candidate => candidate.ToString().StartsWith("items.", System.StringComparison.Ordinal));
        return (invocation, model, ExistenceAnalysis.ResolveCall(invocation, model, CancellationToken.None));
    }

    [TestMethod]
    [DataRow("items.Count() + 1", false)]
    [DataRow("items.Count() is var number", false)]
    [DataRow("items.Count()", false)]
    [DataRow("items.Count() as object", false)]
    [DataRow("items.FirstOrDefault() == (object)1", true)]
    [DataRow("items.FirstOrDefault() is Repository found", true)]
    public async Task A_use_that_is_not_a_comparison_is_not_an_existence_test(string expression, bool item)
    {
        var (invocation, model, call) = await PrepareAsync(expression).ConfigureAwait(false);
        var shape = new ExistenceShapeInfo(invocation, invocation, false, ExistenceShape.Direct, null, null, null);

        Assert.IsNotNull(call);
        Assert.IsNull(ExistenceAnalysis.Complete(shape, call, item ? ExistenceKind.Item : ExistenceKind.Count, model, CancellationToken.None));
    }
}
