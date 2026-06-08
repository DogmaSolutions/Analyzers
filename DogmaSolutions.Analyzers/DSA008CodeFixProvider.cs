using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Code fix provider for DSA008: removes the [Required] attribute from non-nullable DateTime properties.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA008CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA008CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA008Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var propertyDeclaration = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault();

        if (propertyDeclaration == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove [Required] attribute",
                createChangedDocument: ct => RemoveRequiredAttributeAsync(context.Document, propertyDeclaration, ct),
                equivalenceKey: DSA008Analyzer.DiagnosticId + ".Remove"),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, propertyDeclaration, DSA008Analyzer.DiagnosticId, nameof(Resources.DSA008ReviewComment));
    }

    private static async Task<Document> RemoveRequiredAttributeAsync(
        Document document,
        PropertyDeclarationSyntax propertyDeclaration,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var newProperty = RequiredAttributeRemover.RemoveRequiredAttribute(propertyDeclaration, semanticModel);
        if (newProperty == null)
            return document;

        var newRoot = root.ReplaceNode(propertyDeclaration, newProperty);
        return document.WithSyntaxRoot(newRoot);
    }
}
