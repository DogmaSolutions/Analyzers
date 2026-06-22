using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(DSR004RefactoringProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSR004RefactoringProvider : CodeRefactoringProvider
{
    internal const string VisibilityEquivalenceKey = "DSR004.SplitByVisibility";
    internal const string TopicEquivalenceKey = "DSR004.SplitByTopic";

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count != 1 || topLevelTypes[0] is not TypeDeclarationSyntax typeDecl)
            return;

        if (typeDecl.Members.Count < 2)
            return;

        context.RegisterRefactoring(
            CodeAction.Create(
                title: "Split into partial files by member visibility",
                createChangedSolution: ct => DSA034CodeFixProvider.SplitByVisibilityAsync(context.Document, ct),
                equivalenceKey: VisibilityEquivalenceKey));

        context.RegisterRefactoring(
            CodeAction.Create(
                title: "Split into partial files by topic",
                createChangedSolution: ct => DSA034CodeFixProvider.SplitByTopicAsync(context.Document, ct),
                equivalenceKey: TopicEquivalenceKey));
    }
}
