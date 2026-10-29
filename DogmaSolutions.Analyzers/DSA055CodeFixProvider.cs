#nullable disable
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA055CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA055CodeFixProvider : CodeFixProvider
{
    internal const string ReplaceEquivalenceKey = DSA055Analyzer.DiagnosticId + ".Replace";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA055Analyzer.DiagnosticId];

    // each fix changes one comparison: the batch fixer merges them without conflicts
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // the comparison is the one the developer wrote, with the other operator: valid keys are expected to be positive
        if (diagnostic.Properties.TryGetValue(DSA055Analyzer.ReplacementProperty, out var replacement) && node is BinaryExpressionSyntax or IsPatternExpressionSyntax)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: $"[DSA055] Compare with '{replacement} 0'",
                    createChangedDocument: ct => ReplaceAsync(context.Document, node, replacement, ct),
                    equivalenceKey: ReplaceEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA055Analyzer.DiagnosticId, nameof(Resources.DSA055ReviewComment));
    }

    /// <summary>The compared value of <c>x != 0</c>, <c>0 == x</c>, <c>x is 0</c> and <c>x is not 0</c>.</summary>
    private static ExpressionSyntax GetOperand(SyntaxNode node) => node switch
    {
        BinaryExpressionSyntax binary => DSA055Analyzer.IsZero(binary.Right) ? binary.Left : binary.Right,
        _ => ((IsPatternExpressionSyntax)node).Expression,
    };

    private static async Task<Document> ReplaceAsync(Document document, SyntaxNode node, string replacement, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var isGreater = replacement == ">";
        var comparison = SyntaxFactory.BinaryExpression(
            isGreater ? SyntaxKind.GreaterThanExpression : SyntaxKind.LessThanOrEqualExpression,
            GetOperand(node).WithoutTrivia(),
            SyntaxFactory.Token(isGreater ? SyntaxKind.GreaterThanToken : SyntaxKind.LessThanEqualsToken).WithLeadingTrivia(SyntaxFactory.Space).WithTrailingTrivia(SyntaxFactory.Space),
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(0))).WithTriviaFrom(node);

        return document.WithSyntaxRoot(root.ReplaceNode(node, comparison));
    }
}
