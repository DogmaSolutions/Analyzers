using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Code fix provider for DSA004: replaces DateTime.Now with DateTime.UtcNow.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA004CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA004CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA004Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan, getInnermostNodeForTie: true);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with DateTime.UtcNow",
                createChangedDocument: ct => ReplaceWithUtcNowAsync(context.Document, node, ct),
                equivalenceKey: DSA004Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithUtcNowAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        SyntaxNode newNode;

        // DateTime.Now or System.DateTime.Now
        if (node is MemberAccessExpressionSyntax memberAccess)
        {
            var newName = SyntaxFactory.IdentifierName("UtcNow")
                .WithLeadingTrivia(memberAccess.Name.GetLeadingTrivia())
                .WithTrailingTrivia(memberAccess.Name.GetTrailingTrivia());
            newNode = memberAccess.WithName(newName);
        }
        // using static System.DateTime; Now (identifier only)
        else if (node is IdentifierNameSyntax)
        {
            newNode = SyntaxFactory.IdentifierName("UtcNow")
                .WithLeadingTrivia(node.GetLeadingTrivia())
                .WithTrailingTrivia(node.GetTrailingTrivia());
        }
        else
        {
            return document;
        }

        var newRoot = root.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}
