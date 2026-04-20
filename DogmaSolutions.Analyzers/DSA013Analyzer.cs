using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Minimal API endpoints mapped on a local (non-parameter) IEndpointRouteBuilder
/// (not a RouteGroupBuilder) should have an explicit authorization configuration
/// in their fluent chain.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA013Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA013";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA013AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA013AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA013AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Security;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA013");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;

        if (!EndpointAuthorizationUtils.TryGetMapMethodName(invocation, out var mapMethodName))
            return;

        // Skip if receiver is a parameter (DSA015 handles IEndpointRouteBuilder params,
        // DSA014 handles RouteGroupBuilder params)
        var receiverSymbol = EndpointAuthorizationUtils.GetReceiverSymbol(invocation, context.SemanticModel);
        if (receiverSymbol is IParameterSymbol)
            return;

        // Skip if receiver type is RouteGroupBuilder (DSA014 handles it)
        var receiverType = EndpointAuthorizationUtils.GetReceiverType(invocation, context.SemanticModel);
        if (EndpointAuthorizationUtils.IsRouteGroupBuilder(receiverType))
            return;

        // Check the endpoint's own fluent chain for auth
        if (EndpointAuthorizationUtils.HasAuthInFluentChain(invocation))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: invocation.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null,
            mapMethodName);
        context.ReportDiagnostic(diagnostic);
    }
}
