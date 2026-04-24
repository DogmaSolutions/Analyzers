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
/// Code fix provider for DSA003: replaces string.IsNullOrEmpty with string.IsNullOrWhiteSpace.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA003CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA003CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA003Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with string.IsNullOrWhiteSpace",
                createChangedDocument: ct => ReplaceWithIsNullOrWhiteSpaceAsync(context.Document, node, ct),
                equivalenceKey: DSA003Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> ReplaceWithIsNullOrWhiteSpaceAsync(
        Document document,
        SyntaxNode node,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        SyntaxNode newNode;

        if (node is InvocationExpressionSyntax invocation)
        {
            // string.IsNullOrEmpty(s) or String.IsNullOrEmpty(s)
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var newName = SyntaxFactory.IdentifierName("IsNullOrWhiteSpace");
                var newMemberAccess = memberAccess.WithName(newName);
                newNode = invocation.WithExpression(newMemberAccess);
            }
            // using static System.String; IsNullOrEmpty(s)
            else if (invocation.Expression is IdentifierNameSyntax)
            {
                var newName = SyntaxFactory.IdentifierName("IsNullOrWhiteSpace")
                    .WithLeadingTrivia(invocation.Expression.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.Expression.GetTrailingTrivia());
                newNode = invocation.WithExpression(newName);
            }
            else
            {
                return document;
            }
        }
        else
        {
            return document;
        }

        var newRoot = root.ReplaceNode(node, newNode);
        return document.WithSyntaxRoot(newRoot);
    }
}
