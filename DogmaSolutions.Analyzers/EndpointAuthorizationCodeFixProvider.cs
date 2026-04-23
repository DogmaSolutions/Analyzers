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
/// Code fix provider for DSA013, DSA014, and DSA015: adds .RequireAuthorization()
/// or .AllowAnonymous() to the Minimal API endpoint's fluent chain.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EndpointAuthorizationCodeFixProvider))]
[Shared]
public sealed class EndpointAuthorizationCodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds =>
        [DSA013Analyzer.DiagnosticId, DSA014Analyzer.DiagnosticId, DSA015Analyzer.DiagnosticId];

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
            .FirstOrDefault(inv => inv.Span == diagnosticSpan);

        if (invocation == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add .RequireAuthorization()",
                createChangedDocument: ct => AddAuthMethodAsync(context.Document, invocation, "RequireAuthorization", ct),
                equivalenceKey: "AddRequireAuthorization"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Add .AllowAnonymous()",
                createChangedDocument: ct => AddAuthMethodAsync(context.Document, invocation, "AllowAnonymous", ct),
                equivalenceKey: "AddAllowAnonymous"),
            diagnostic);
    }

    private static async Task<Document> AddAuthMethodAsync(
        Document document,
        InvocationExpressionSyntax mapInvocation,
        string methodName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        // Walk up to the outermost invocation in the fluent chain
        var outermost = mapInvocation;
        var current = (SyntaxNode)mapInvocation;
        while (current.Parent is MemberAccessExpressionSyntax parentAccess &&
               parentAccess.Parent is InvocationExpressionSyntax parentInvocation)
        {
            outermost = parentInvocation;
            current = parentInvocation;
        }

        // Build: outermost.MethodName()
        var authCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                outermost.WithoutTrailingTrivia(),
                SyntaxFactory.IdentifierName(methodName)))
            .WithTrailingTrivia(outermost.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(outermost, authCall);
        return document.WithSyntaxRoot(newRoot);
    }
}
