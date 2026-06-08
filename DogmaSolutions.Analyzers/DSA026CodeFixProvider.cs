using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA026CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA026CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA026Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];

        if (!diagnostic.Properties.TryGetValue(DSA026Analyzer.NearestNameProperty, out var nearestName) ||
            string.IsNullOrEmpty(nearestName))
            return;

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Use '{nearestName}' from nearest scope",
                createChangedDocument: ct => ReplaceTokenAsync(context.Document, diagnostic, nearestName, ct),
                equivalenceKey: DSA026Analyzer.DiagnosticId),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA026Analyzer.DiagnosticId, nameof(Resources.DSA026ReviewComment));
    }

    private static async Task<Document> ReplaceTokenAsync(
        Document document,
        Diagnostic diagnostic,
        string nearestName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        if (node is not IdentifierNameSyntax && node is not MemberAccessExpressionSyntax)
        {
            node = root.FindToken(diagnosticSpan.Start).Parent;
            if (node is not IdentifierNameSyntax && node is not MemberAccessExpressionSyntax)
                return document;
        }

        var newNode = SyntaxFactory.IdentifierName(nearestName)
            .WithLeadingTrivia(node.GetLeadingTrivia())
            .WithTrailingTrivia(node.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}
