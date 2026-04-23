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
/// Code fix provider for DSA020: removes the redundant async/await on Task.FromResult
/// lambdas and replaces unused parameters with a discard.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA020CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA020CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA020Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var lambda = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<LambdaExpressionSyntax>()
            .FirstOrDefault(l => l.Span == diagnosticSpan);

        if (lambda == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Remove redundant async/await",
                createChangedDocument: ct => RemoveRedundantAsyncAwaitAsync(context.Document, lambda, ct),
                equivalenceKey: DSA020Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> RemoveRedundantAsyncAwaitAsync(
        Document document,
        LambdaExpressionSyntax lambda,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var awaitExpression = DSA020Analyzer.GetAwaitExpression(lambda);
        if (awaitExpression == null)
            return document;

        // The expression without the await (e.g., Task.FromResult(value))
        // Also strip .ConfigureAwait(...) if present — it returns ConfiguredTaskAwaitable,
        // not Task, and is only meaningful in an await context.
        var taskExpression = awaitExpression.Expression;
        if (taskExpression is InvocationExpressionSyntax configureInvocation &&
            configureInvocation.Expression is MemberAccessExpressionSyntax configureAccess &&
            configureAccess.Name.Identifier.ValueText == "ConfigureAwait")
        {
            taskExpression = configureAccess.Expression;
        }

        SyntaxNode newLambda;

        if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
        {
            // async ct => await Task.FromResult(x) → _ => Task.FromResult(x)
            var paramName = ShouldDiscardParameter(simpleLambda.Parameter, taskExpression)
                ? "_"
                : simpleLambda.Parameter.Identifier.ValueText;

            newLambda = SyntaxFactory.SimpleLambdaExpression(
                    SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName)),
                    taskExpression.WithoutLeadingTrivia())
                .WithLeadingTrivia(lambda.GetLeadingTrivia())
                .WithTrailingTrivia(lambda.GetTrailingTrivia());
        }
        else if (lambda is ParenthesizedLambdaExpressionSyntax parenLambda)
        {
            if (parenLambda.ParameterList.Parameters.Count == 0)
            {
                // async () => await Task.FromResult(x) → () => Task.FromResult(x)
                newLambda = SyntaxFactory.ParenthesizedLambdaExpression(
                        parenLambda.ParameterList,
                        taskExpression.WithoutLeadingTrivia())
                    .WithLeadingTrivia(lambda.GetLeadingTrivia())
                    .WithTrailingTrivia(lambda.GetTrailingTrivia());
            }
            else if (parenLambda.ParameterList.Parameters.Count == 1)
            {
                // async (ct) => await Task.FromResult(x) → _ => Task.FromResult(x)
                var param = parenLambda.ParameterList.Parameters[0];
                var paramName = ShouldDiscardParameter(param, taskExpression)
                    ? "_"
                    : param.Identifier.ValueText;

                newLambda = SyntaxFactory.SimpleLambdaExpression(
                        SyntaxFactory.Parameter(SyntaxFactory.Identifier(paramName)),
                        taskExpression.WithoutLeadingTrivia())
                    .WithLeadingTrivia(lambda.GetLeadingTrivia())
                    .WithTrailingTrivia(lambda.GetTrailingTrivia());
            }
            else
            {
                // Multiple parameters: keep parenthesized, just remove async/await
                newLambda = parenLambda
                    .WithModifiers(RemoveAsyncModifier(parenLambda.Modifiers))
                    .WithExpressionBody(taskExpression.WithoutLeadingTrivia())
                    .WithBody(null);
            }
        }
        else
        {
            return document;
        }

        var newRoot = root.ReplaceNode(lambda, newLambda);
        return document.WithSyntaxRoot(newRoot);
    }

    /// <summary>
    /// Returns true if the parameter is not referenced in the Task.FromResult arguments
    /// and can safely be replaced with a discard (_).
    /// </summary>
    private static bool ShouldDiscardParameter(ParameterSyntax parameter, ExpressionSyntax taskExpression)
    {
        var paramName = parameter.Identifier.ValueText;

        // Already a discard
        if (paramName == "_")
            return true;

        // Check if the parameter name appears anywhere in the task expression
        var identifiers = taskExpression.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>();

        return !identifiers.Any(id => id.Identifier.ValueText == paramName);
    }

    private static SyntaxTokenList RemoveAsyncModifier(SyntaxTokenList modifiers)
    {
        return SyntaxFactory.TokenList(modifiers.Where(m => !m.IsKind(SyntaxKind.AsyncKeyword)));
    }
}
