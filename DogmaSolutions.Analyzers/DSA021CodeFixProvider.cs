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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA021CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA021CodeFixProvider : CodeFixProvider
{
    public const string TagWithCallSiteEquivalenceKey = DSA021Analyzer.DiagnosticId + "_TagWithCallSite";
    public const string TagWithEquivalenceKey = DSA021Analyzer.DiagnosticId + "_TagWith";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA021Analyzer.DiagnosticId];

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
                title: "Add .TagWithCallSite()",
                createChangedDocument: ct => AddTagToQueryAsync(context.Document, invocation, useCallSite: true, ct),
                equivalenceKey: TagWithCallSiteEquivalenceKey),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add .TagWith(\"...\")",
                createChangedDocument: ct => AddTagToQueryAsync(context.Document, invocation, useCallSite: false, ct),
                equivalenceKey: TagWithEquivalenceKey),
            diagnostic);
    }

    private static async Task<Document> AddTagToQueryAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        bool useCallSite,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        if (invocation.Expression is not MemberAccessExpressionSyntax terminalAccess)
            return document;

        var receiver = terminalAccess.Expression;

        ExpressionSyntax tagExpression;
        if (useCallSite)
        {
            tagExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver,
                    SyntaxFactory.IdentifierName("TagWithCallSite")));
        }
        else
        {
            tagExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    receiver,
                    SyntaxFactory.IdentifierName("TagWith")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(
                                SyntaxKind.StringLiteralExpression,
                                SyntaxFactory.Literal("TODO: describe this query"))))));
        }

        var newTerminalAccess = terminalAccess.WithExpression(tagExpression);
        var newInvocation = invocation.WithExpression(newTerminalAccess);

        var newRoot = root.ReplaceNode(invocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }
}
