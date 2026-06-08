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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA028CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA028CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA028Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var toListInvocation = node as InvocationExpressionSyntax
            ?? node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(IsToListInvocation);

        if (toListInvocation == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use ToArray() instead of ToList()",
                createChangedDocument: ct => ReplaceToListWithToArrayAsync(context.Document, toListInvocation, ct),
                equivalenceKey: DSA028Analyzer.DiagnosticId),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, toListInvocation, DSA028Analyzer.DiagnosticId, nameof(Resources.DSA028ReviewComment));
    }

    private static async Task<Document> ReplaceToListWithToArrayAsync(
        Document document,
        InvocationExpressionSyntax toListInvocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        if (toListInvocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return document;

        var toArrayAccess = memberAccess.WithName(
            SyntaxFactory.IdentifierName("ToArray")
                .WithTriviaFrom(memberAccess.Name));

        var newInvocation = toListInvocation.WithExpression(toArrayAccess);

        var newRoot = root.ReplaceNode(toListInvocation, newInvocation);
        return document.WithSyntaxRoot(newRoot);
    }

    private static bool IsToListInvocation(InvocationExpressionSyntax invocation)
    {
        return invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
               memberAccess.Name.Identifier.ValueText == "ToList" &&
               invocation.ArgumentList.Arguments.Count == 0;
    }
}
