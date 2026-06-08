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
/// Code fix provider for DSA031: inserts .AsNoTracking() before the terminal materialization method.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA031CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA031CodeFixProvider : CodeFixProvider
{
    public const string EquivalenceKey = DSA031Analyzer.DiagnosticId + ".AsNoTracking";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA031Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(i => i.Span == diagnosticSpan);

        if (invocation == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add .AsNoTracking()",
                createChangedDocument: ct => InsertAsNoTrackingAsync(context.Document, invocation, ct),
                equivalenceKey: EquivalenceKey),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, invocation, DSA031Analyzer.DiagnosticId, nameof(Resources.DSA031ReviewComment));
    }

    private static async Task<Document> InsertAsNoTrackingAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        if (invocation.Expression is not MemberAccessExpressionSyntax terminalAccess)
            return document;

        var receiver = terminalAccess.Expression;

        var trackingCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                receiver,
                SyntaxFactory.IdentifierName("AsNoTracking")));

        var newTerminalAccess = terminalAccess.WithExpression(trackingCall);
        var newInvocation = invocation.WithExpression(newTerminalAccess);

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
