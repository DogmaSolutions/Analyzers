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
/// Code fix provider for DSA030: inserts .AsNoTracking() or .AsTracking() before the terminal materialization method.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA030CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA030CodeFixProvider : CodeFixProvider
{
    public const string AsNoTrackingEquivalenceKey = DSA030Analyzer.DiagnosticId + ".AsNoTracking";
    public const string AsTrackingEquivalenceKey = DSA030Analyzer.DiagnosticId + ".AsTracking";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA030Analyzer.DiagnosticId];

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
                createChangedDocument: ct => InsertTrackingMethodAsync(context.Document, invocation, "AsNoTracking", ct),
                equivalenceKey: AsNoTrackingEquivalenceKey),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add .AsTracking()",
                createChangedDocument: ct => InsertTrackingMethodAsync(context.Document, invocation, "AsTracking", ct),
                equivalenceKey: AsTrackingEquivalenceKey),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, invocation, DSA030Analyzer.DiagnosticId, nameof(Resources.DSA030ReviewComment));
    }

    private static async Task<Document> InsertTrackingMethodAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        string trackingMethodName,
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
                SyntaxFactory.IdentifierName(trackingMethodName)));

        var newTerminalAccess = terminalAccess.WithExpression(trackingCall);
        var newInvocation = invocation.WithExpression(newTerminalAccess);

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
