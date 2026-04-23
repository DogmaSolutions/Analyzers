using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Detects async lambdas that only await Task.FromResult. The async/await is redundant
/// because Task.FromResult already returns a completed Task, so the async state machine
/// overhead can be eliminated by returning the Task directly.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA020Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA020";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA020AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA020AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA020AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.CodeSmell;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA020");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeLambda,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.ParenthesizedLambdaExpression);
    }

    private static void AnalyzeLambda(SyntaxNodeAnalysisContext context)
    {
        var lambda = (LambdaExpressionSyntax)context.Node;

        // Must have the async modifier
        if (!lambda.Modifiers.Any(SyntaxKind.AsyncKeyword))
            return;

        // Extract the awaited expression from the lambda body
        var awaitExpression = GetAwaitExpression(lambda);
        if (awaitExpression == null)
            return;

        // The awaited expression must be Task.FromResult(...)
        if (!IsTaskFromResult(awaitExpression.Expression, context.SemanticModel))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: lambda.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Extracts the AwaitExpressionSyntax from the lambda body, handling both
    /// expression-body and block-body (with a single return statement) lambdas.
    /// </summary>
    internal static AwaitExpressionSyntax GetAwaitExpression(LambdaExpressionSyntax lambda)
    {
        // Expression body: async ct => await Task.FromResult(x)
        if (lambda.ExpressionBody is AwaitExpressionSyntax exprAwait)
            return exprAwait;

        // Block body: async ct => { return await Task.FromResult(x); }
        if (lambda.Body is BlockSyntax block &&
            block.Statements.Count == 1 &&
            block.Statements[0] is ReturnStatementSyntax returnStmt &&
            returnStmt.Expression is AwaitExpressionSyntax blockAwait)
            return blockAwait;

        return null;
    }

    /// <summary>
    /// Checks whether the expression is a call to Task.FromResult or Task.FromResult{T}.
    /// Uses the semantic model to verify it's System.Threading.Tasks.Task.FromResult.
    /// </summary>
    private static bool IsTaskFromResult(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        // Handle ConfigureAwait: await Task.FromResult(x).ConfigureAwait(false)
        if (expression is InvocationExpressionSyntax configureInvocation &&
            configureInvocation.Expression is MemberAccessExpressionSyntax configureAccess &&
            configureAccess.Name.Identifier.ValueText == "ConfigureAwait")
        {
            expression = configureAccess.Expression;
        }

        if (expression is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        if (memberAccess.Name.Identifier.ValueText != "FromResult")
            return false;

        // Semantic verification
        var symbolInfo = semanticModel.GetSymbolInfo(invocation);
        if (symbolInfo.Symbol is IMethodSymbol method &&
            method.ContainingType?.ToDisplayString() == "System.Threading.Tasks.Task")
            return true;

        return false;
    }
}
