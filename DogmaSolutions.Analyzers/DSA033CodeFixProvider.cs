using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Code fix provider for DSA033: splits a file with multiple top-level types into one file per type.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA033CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA033CodeFixProvider : CodeFixProvider
{
    internal const string EquivalenceKey = DSA033Analyzer.DiagnosticId + ".SplitFile";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA033Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count < 2)
            return;

        var diagnostic = context.Diagnostics[0];

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Split file into {topLevelTypes.Count} files (one per type)",
                createChangedSolution: ct => SplitFileAsync(context.Document, ct),
                equivalenceKey: EquivalenceKey),
            diagnostic);
    }

    internal static async Task<Solution> SplitFileAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document.Project.Solution;

        var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count < 2)
            return document.Project.Solution;

        var solution = document.Project.Solution;
        var project = document.Project;
        var existingFolder = GetDocumentFolders(document);
        var sourceDir = !string.IsNullOrEmpty(document.FilePath)
            ? Path.GetDirectoryName(document.FilePath)
            : null;

        var firstType = true;
        BaseTypeDeclarationSyntax keepType = null;

        foreach (var typeDecl in topLevelTypes)
        {
            if (firstType)
            {
                keepType = typeDecl;
                firstType = false;
                continue;
            }

            var newFileName = typeDecl.Identifier.ValueText + ".cs";
            var typesToRemove = topLevelTypes.Where(t => t != typeDecl).ToList();
            var newRoot = root.RemoveNodes(typesToRemove, SyntaxRemoveOptions.KeepNoTrivia);
            var newFilePath = !string.IsNullOrEmpty(sourceDir)
                ? Path.Combine(sourceDir, newFileName)
                : null;

            solution = solution.AddDocument(
                DocumentId.CreateNewId(project.Id),
                newFileName,
                newRoot,
                folders: existingFolder,
                filePath: newFilePath);
        }

        if (keepType != null)
        {
            var typesToRemove = topLevelTypes.Where(t => t != keepType).ToList();
            var newOriginalRoot = root.RemoveNodes(typesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            var originalFileName = keepType.Identifier.ValueText + ".cs";

            var originalDocId = document.Id;
            solution = solution.WithDocumentSyntaxRoot(originalDocId, newOriginalRoot);
            solution = solution.WithDocumentName(originalDocId, originalFileName);
        }

        return solution;
    }

    private static IEnumerable<string> GetDocumentFolders(Document document)
    {
        return document.Folders;
    }
}
