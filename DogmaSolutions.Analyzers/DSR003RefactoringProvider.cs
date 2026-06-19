using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Refactoring provider: splits a file with multiple top-level types into one file per type.
/// Available from the lightbulb menu without requiring DSA033 to fire.
/// </summary>
[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(DSR003RefactoringProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSR003RefactoringProvider : CodeRefactoringProvider
{
    internal const string EquivalenceKey = "DSR003.SplitFile";

    public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var topLevelTypes = DSA033CodeFixProvider.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count < 2)
            return;

        context.RegisterRefactoring(
            CodeAction.Create(
                title: $"Split file into {topLevelTypes.Count} files (one per type)",
                createChangedSolution: ct => DSA033CodeFixProvider.SplitFileAsync(context.Document, ct),
                equivalenceKey: EquivalenceKey));
    }
}
