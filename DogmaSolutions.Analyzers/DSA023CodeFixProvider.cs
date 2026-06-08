using System;
using System.Collections.Generic;
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA023CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA023CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA023Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var binaryExpr = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .FirstOrDefault(b => b.Span == diagnosticSpan);

        if (binaryExpr == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Replace with Path.Combine",
                createChangedDocument: ct => ReplaceWithPathCombineAsync(context.Document, binaryExpr, ct),
                equivalenceKey: DSA023Analyzer.DiagnosticId),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, binaryExpr, DSA023Analyzer.DiagnosticId, nameof(Resources.DSA023ReviewComment));
    }

    private static async Task<Document> ReplaceWithPathCombineAsync(
        Document document,
        BinaryExpressionSyntax binaryExpr,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var flatSegments = new List<ExpressionSyntax>();
        FlattenConcatenation(binaryExpr, flatSegments);

        var processedSegments = ProcessSegments(flatSegments);

        if (processedSegments.Count == 0)
            return document;

        ExpressionSyntax replacement;
        if (processedSegments.Count == 1)
        {
            replacement = processedSegments[0];
        }
        else
        {
            replacement = SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.IdentifierName("Path"),
                        SyntaxFactory.IdentifierName("Combine")),
                    SyntaxFactory.ArgumentList(
                        SyntaxFactory.SeparatedList(
                            processedSegments.Select(s => SyntaxFactory.Argument(s.WithoutTrivia())))))
                .NormalizeWhitespace();
        }

        replacement = replacement
            .WithLeadingTrivia(binaryExpr.GetLeadingTrivia())
            .WithTrailingTrivia(binaryExpr.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(binaryExpr, replacement);
        return document.WithSyntaxRoot(newRoot);
    }

    private static void FlattenConcatenation(ExpressionSyntax expr, List<ExpressionSyntax> result)
    {
        var unwrapped = expr;
        while (unwrapped is ParenthesizedExpressionSyntax paren)
            unwrapped = paren.Expression;

        if (unwrapped is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
        {
            FlattenConcatenation(binary.Left, result);
            FlattenConcatenation(binary.Right, result);
        }
        else
        {
            result.Add(unwrapped);
        }
    }

    private static List<ExpressionSyntax> ProcessSegments(List<ExpressionSyntax> flatSegments)
    {
        var result = new List<ExpressionSyntax>();
        foreach (var segment in flatSegments)
        {
            if (segment is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = literal.Token.ValueText;
                var parts = value.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    result.Add(SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal(part)));
                }
            }
            else
            {
                result.Add(segment);
            }
        }

        return result;
    }
}
