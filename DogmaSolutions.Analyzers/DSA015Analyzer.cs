using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Minimal API endpoints mapped on an IEndpointRouteBuilder received as a method
/// parameter should have an explicit authorization configuration. Traces authorization
/// across method boundaries to verify that all call sites provide an authorized builder.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA015Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA015";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA015AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA015AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA015AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Security;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA015");

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

        // Only handle IEndpointRouteBuilder parameters (not RouteGroupBuilder — DSA014 handles those)
        var receiverSymbol = EndpointAuthorizationUtils.GetReceiverSymbol(invocation, context.SemanticModel);
        if (!(receiverSymbol is IParameterSymbol parameter))
            return;

        var receiverType = EndpointAuthorizationUtils.GetReceiverType(invocation, context.SemanticModel);
        if (EndpointAuthorizationUtils.IsRouteGroupBuilder(receiverType))
            return;

        // Check the endpoint's own fluent chain for auth
        if (EndpointAuthorizationUtils.HasAuthInFluentChain(invocation))
            return;

        // Check separate auth calls on the parameter in the same method body
        // (e.g., builder.RequireAuthorization(); builder.MapGet(...))
        if (EndpointAuthorizationUtils.HasAuthOnSymbolLocally(
                parameter, context.SemanticModel, new HashSet<ISymbol>(SymbolEqualityComparer.Default)))
            return;

        // Cross-method: trace back to call sites and verify all pass an authorized builder
        if (EndpointAuthorizationUtils.ParameterHasAuthAtAllCallSites(
                parameter, context.SemanticModel.Compilation))
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
