using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Entity Framework queries should explicitly specify a change tracking strategy
/// (AsNoTracking, AsTracking, or AsNoTrackingWithIdentityResolution).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA030Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA030";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA030AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA030AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA030AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.BestPractice;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA030.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] TrackingMethods =
    {
        "AsNoTracking", "AsTracking", "AsNoTrackingWithIdentityResolution"
    };

    private static readonly string[] AsyncTerminalMethods =
    {
        "ToListAsync", "ToArrayAsync",
        "FirstAsync", "FirstOrDefaultAsync",
        "SingleAsync", "SingleOrDefaultAsync",
        "LastAsync", "LastOrDefaultAsync",
        "CountAsync", "LongCountAsync",
        "AnyAsync", "AllAsync",
        "SumAsync", "AverageAsync", "MinAsync", "MaxAsync",
        "ContainsAsync",
        "ToDictionaryAsync",
        "LoadAsync", "ForEachAsync",
    };

    private static readonly string[] SyncTerminalMethods =
    {
        "ToList", "ToArray",
        "First", "FirstOrDefault",
        "Single", "SingleOrDefault",
        "Last", "LastOrDefault",
        "Count", "LongCount",
        "Any", "All",
        "Min", "Max", "Sum", "Average",
    };

    private static readonly string[] BulkOperationMethods =
    {
        "ExecuteUpdate", "ExecuteUpdateAsync",
        "ExecuteDelete", "ExecuteDeleteAsync",
    };

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

        if (!TryGetTerminalMethodName(invocation, out var methodName))
            return;

        if (!IsEntityFrameworkChain(invocation, context.SemanticModel))
            return;

        if (IsInsideSubquery(invocation, context.SemanticModel))
            return;

        if (HasTrackingChoiceInChain(invocation, context.SemanticModel, context.SemanticModel.Compilation))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: invocation.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null,
            methodName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool TryGetTerminalMethodName(InvocationExpressionSyntax invocation, out string methodName)
    {
        methodName = null;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var name = memberAccess.Name.Identifier.ValueText;

            if (Array.IndexOf(BulkOperationMethods, name) >= 0)
                return false;

            if (Array.IndexOf(AsyncTerminalMethods, name) >= 0 ||
                Array.IndexOf(SyncTerminalMethods, name) >= 0)
            {
                methodName = name;
                return true;
            }
        }

        return false;
    }

    private static bool IsEntityFrameworkChain(InvocationExpressionSyntax terminalInvocation, SemanticModel semanticModel)
    {
        var terminalSymbol = semanticModel.GetSymbolInfo(terminalInvocation).Symbol as IMethodSymbol;
        if (terminalSymbol != null)
        {
            if (IsEntityFrameworkExtensionMethod(terminalSymbol))
                return true;

            if (!terminalSymbol.IsExtensionMethod && terminalSymbol.ReducedFrom == null)
                return false;
        }

        ExpressionSyntax current = GetReceiver(terminalInvocation);
        while (current != null)
        {
            var typeInfo = semanticModel.GetTypeInfo(current);
            if (typeInfo.Type != null && EntityFrameworkSymbols.IsDbSetType(typeInfo.Type))
                return true;

            if (current is InvocationExpressionSyntax invocation)
            {
                current = GetReceiver(invocation);
            }
            else
            {
                break;
            }
        }

        if (current != null)
        {
            var typeInfo = semanticModel.GetTypeInfo(current);
            if (typeInfo.Type != null && EntityFrameworkSymbols.IsDbSetType(typeInfo.Type))
                return true;

            var symbolInfo = semanticModel.GetSymbolInfo(current);
            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                if (typeInfo.Type != null && !ImplementsIQueryable(typeInfo.Type))
                    return false;
                return EntityFrameworkSymbols.LocalInitializerInvolvesEf(localSymbol, semanticModel);
            }
        }

        return false;
    }

    internal static bool HasTrackingChoiceInChain(
        InvocationExpressionSyntax terminalInvocation,
        SemanticModel semanticModel,
        Compilation compilation)
    {
        ExpressionSyntax current = terminalInvocation;
        while (current is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (IsTrackingMethod(memberAccess.Name.Identifier.ValueText))
                    return true;
                current = memberAccess.Expression;
            }
            else
            {
                break;
            }
        }

        if (current != null)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(current);

            if (symbolInfo.Symbol is IParameterSymbol parameter)
                return ParameterHasTrackingAtAllCallSites(parameter, compilation);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
                return LocalHasTrackingInInitializer(localSymbol);
        }

        return false;
    }

    private static bool LocalHasTrackingInInitializer(ILocalSymbol localSymbol)
    {
        var initValue = EntityFrameworkSymbols.GetLocalInitializerValue(localSymbol);
        return initValue != null && ExpressionContainsTrackingChoice(initValue);
    }

    private static bool ExpressionContainsTrackingChoice(ExpressionSyntax expression)
    {
        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                IsTrackingMethod(memberAccess.Name.Identifier.ValueText))
                return true;
        }

        return false;
    }

    internal static bool ParameterHasTrackingAtAllCallSites(
        IParameterSymbol parameter,
        Compilation compilation,
        int maxDepth = 3)
    {
        if (maxDepth <= 0 || !(parameter.ContainingSymbol is IMethodSymbol containingMethod))
            return false;

        var foundAnySite = false;

        // Lazy enumeration: the search stops at the first call site that does not provide a tracking choice.
        foreach (var (model, invocation, invokedSymbol) in FindCallSites(compilation, containingMethod))
        {
            foundAnySite = true;

            var argExpr = GetArgumentForParameter(invocation, parameter, invokedSymbol);
            if (argExpr == null || !ArgumentExpressionHasTrackingChoice(argExpr, model, compilation, maxDepth - 1))
                return false;
        }

        return foundAnySite;
    }

    private static IEnumerable<(SemanticModel Model, InvocationExpressionSyntax Invocation, IMethodSymbol InvokedSymbol)> FindCallSites(
        Compilation compilation,
        IMethodSymbol targetMethod)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);

            foreach (var invocation in tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                if (model.GetSymbolInfo(invocation).Symbol is IMethodSymbol invokedSymbol &&
                    IsCallToMethod(invokedSymbol, targetMethod))
                    yield return (model, invocation, invokedSymbol);
            }
        }
    }

    private static bool IsCallToMethod(IMethodSymbol invokedSymbol, IMethodSymbol targetMethod)
    {
        var candidateOriginal = invokedSymbol.OriginalDefinition;
        var candidateReduced = invokedSymbol.ReducedFrom?.OriginalDefinition;
        var targetOriginal = targetMethod.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(candidateOriginal, targetOriginal) ||
               (candidateReduced != null &&
                SymbolEqualityComparer.Default.Equals(candidateReduced, targetOriginal));
    }

    private static ExpressionSyntax GetArgumentForParameter(
        InvocationExpressionSyntax invocation,
        IParameterSymbol parameter,
        IMethodSymbol invokedSymbol)
    {
        var isReducedExtensionCall = invokedSymbol.ReducedFrom != null;

        // In a reduced extension call (x.Ext(...)) the 'this' parameter is not an argument: it is the member-access receiver.
        if (isReducedExtensionCall && parameter.Ordinal == 0)
            return (invocation.Expression as MemberAccessExpressionSyntax)?.Expression;

        // The receiver takes no slot in the argument list of a reduced call, so the remaining parameters shift by one.
        var argIndex = isReducedExtensionCall ? parameter.Ordinal - 1 : parameter.Ordinal;

        return FindNamedArgument(invocation, parameter.Name) ?? FindPositionalArgument(invocation, argIndex);
    }

    private static ExpressionSyntax FindNamedArgument(InvocationExpressionSyntax invocation, string parameterName)
    {
        return invocation.ArgumentList.Arguments
            .FirstOrDefault(arg => arg.NameColon?.Name.Identifier.ValueText == parameterName)
            ?.Expression;
    }

    private static ExpressionSyntax FindPositionalArgument(InvocationExpressionSyntax invocation, int argIndex)
    {
        // An omitted optional parameter leaves no argument at its position: the slot is either past the end of the list,
        // or taken by a named argument that belongs to another parameter.
        var arguments = invocation.ArgumentList.Arguments;
        return argIndex < arguments.Count && arguments[argIndex].NameColon == null
            ? arguments[argIndex].Expression
            : null;
    }

    private static bool ArgumentExpressionHasTrackingChoice(
        ExpressionSyntax expression,
        SemanticModel model,
        Compilation compilation,
        int maxDepth)
    {
        if (ExpressionContainsTrackingChoice(expression))
            return true;

        var symbolInfo = model.GetSymbolInfo(expression);
        if (symbolInfo.Symbol == null)
            return false;

        if (symbolInfo.Symbol is IParameterSymbol nestedParam && maxDepth > 0)
            return ParameterHasTrackingAtAllCallSites(nestedParam, compilation, maxDepth);

        if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            return LocalHasTrackingInInitializer(localSymbol);

        return false;
    }

    private static bool IsInsideSubquery(SyntaxNode node, SemanticModel semanticModel)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is LambdaExpressionSyntax &&
                current.Parent is ArgumentSyntax &&
                current.Parent.Parent is ArgumentListSyntax &&
                current.Parent.Parent.Parent is InvocationExpressionSyntax outerInvocation &&
                IsEntityFrameworkChain(outerInvocation, semanticModel))
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool ImplementsIQueryable(ITypeSymbol type)
    {
        if (type.Name == "IQueryable" && type.ContainingNamespace?.ToDisplayString() == "System.Linq")
            return true;

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "IQueryable" && iface.ContainingNamespace?.ToDisplayString() == "System.Linq")
                return true;
        }

        return false;
    }

    private static bool IsTrackingMethod(string methodName) => Array.IndexOf(TrackingMethods, methodName) >= 0;

    private static bool IsEntityFrameworkExtensionMethod(IMethodSymbol method)
    {
        if (!method.IsExtensionMethod && method.ReducedFrom == null)
            return false;

        return EntityFrameworkSymbols.IsFromEntityFramework(method);
    }

    private static ExpressionSyntax GetReceiver(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.Expression;
        return null;
    }
}
