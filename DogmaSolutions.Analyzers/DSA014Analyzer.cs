using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Minimal API endpoints mapped on a RouteGroupBuilder should have an explicit
/// authorization configuration, either directly on the endpoint or inherited from
/// the route group. Supports local group analysis and cross-method tracing when
/// the group is received as a parameter.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA014Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA014";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA014AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA014AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA014AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Security;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA014");

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

        // Only handle RouteGroupBuilder receivers
        var receiverType = EndpointAuthorizationUtils.GetReceiverType(invocation, context.SemanticModel);
        if (!EndpointAuthorizationUtils.IsRouteGroupBuilder(receiverType))
            return;

        // Check the endpoint's own fluent chain for auth
        if (EndpointAuthorizationUtils.HasAuthInFluentChain(invocation))
            return;

        // Check group-level auth (local: declaration chain, separate calls, nested groups)
        var receiverSymbol = EndpointAuthorizationUtils.GetReceiverSymbol(invocation, context.SemanticModel);
        if (receiverSymbol != null)
        {
            // Local analysis works for both local variables and parameters
            // (for parameters, it checks separate calls like group.RequireAuthorization() in the same method)
            if (EndpointAuthorizationUtils.HasAuthOnSymbolLocally(
                    receiverSymbol, context.SemanticModel, new HashSet<ISymbol>(SymbolEqualityComparer.Default)))
                return;

            // Cross-method: if the receiver is a parameter, trace back to call sites
            if (receiverSymbol is IParameterSymbol parameter)
            {
                if (EndpointAuthorizationUtils.ParameterHasAuthAtAllCallSites(
                        parameter, context.SemanticModel.Compilation))
                    return;
            }
        }

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
