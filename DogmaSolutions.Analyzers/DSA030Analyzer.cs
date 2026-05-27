using System;
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
            if (typeInfo.Type != null && IsDbSetType(typeInfo.Type))
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
            if (typeInfo.Type != null && IsDbSetType(typeInfo.Type))
                return true;

            var symbolInfo = semanticModel.GetSymbolInfo(current);
            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            {
                if (typeInfo.Type != null && !ImplementsIQueryable(typeInfo.Type))
                    return false;
                return LocalInitializerInvolvesEf(localSymbol, semanticModel);
            }
        }

        return false;
    }

    private static bool LocalInitializerInvolvesEf(ILocalSymbol localSymbol, SemanticModel semanticModel)
    {
        var declaringRefs = localSymbol.DeclaringSyntaxReferences;
        if (declaringRefs.Length == 0)
            return false;

        var declarationNode = declaringRefs[0].GetSyntax();
        if (!(declarationNode is VariableDeclaratorSyntax declarator) || declarator.Initializer == null)
            return false;

        var initValue = declarator.Initializer.Value;

        foreach (var node in initValue.DescendantNodesAndSelf())
        {
            var typeInfo = semanticModel.GetTypeInfo(node);
            if (typeInfo.Type != null && IsDbSetType(typeInfo.Type))
                return true;
        }

        foreach (var inv in initValue.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            var ms = semanticModel.GetSymbolInfo(inv).Symbol as IMethodSymbol;
            if (ms != null && IsFromEntityFramework(ms))
                return true;
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

    private static bool LocalHasTrackingInInitializer(ISymbol localSymbol)
    {
        var declaringRefs = localSymbol.DeclaringSyntaxReferences;
        if (declaringRefs.Length == 0)
            return false;

        var declarationNode = declaringRefs[0].GetSyntax();
        if (!(declarationNode is VariableDeclaratorSyntax declarator) || declarator.Initializer == null)
            return false;

        return ExpressionContainsTrackingChoice(declarator.Initializer.Value);
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
        if (maxDepth <= 0)
            return false;

        if (!(parameter.ContainingSymbol is IMethodSymbol containingMethod))
            return false;

        var foundAnySite = false;

        foreach (var tree in compilation.SyntaxTrees)
        {
            var model = compilation.GetSemanticModel(tree);
            var root = tree.GetRoot();

            foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
            {
                var invokedSymbol = model.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
                if (invokedSymbol == null)
                    continue;

                if (!IsCallToMethod(invokedSymbol, containingMethod))
                    continue;

                foundAnySite = true;

                var argExpr = GetArgumentForParameter(invocation, parameter, invokedSymbol);
                if (argExpr == null)
                    return false;

                if (!ArgumentExpressionHasTrackingChoice(argExpr, model, compilation, maxDepth - 1))
                    return false;
            }
        }

        return foundAnySite;
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
        var paramOrdinal = parameter.Ordinal;
        var isReducedExtensionCall = invokedSymbol.ReducedFrom != null;

        if (isReducedExtensionCall && paramOrdinal == 0)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                return memberAccess.Expression;
            return null;
        }

        var argIndex = isReducedExtensionCall ? paramOrdinal - 1 : paramOrdinal;

        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            if (arg.NameColon != null &&
                arg.NameColon.Name.Identifier.ValueText == parameter.Name)
                return arg.Expression;
        }

        if (argIndex >= 0 && argIndex < invocation.ArgumentList.Arguments.Count)
        {
            var arg = invocation.ArgumentList.Arguments[argIndex];
            if (arg.NameColon == null)
                return arg.Expression;
        }

        return null;
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

        return IsFromEntityFramework(method);
    }

    private static bool IsFromEntityFramework(IMethodSymbol method)
    {
        var ns = method.ContainingType?.ContainingNamespace?.ToDisplayString();
        return ns != null && ns.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal);
    }

    private static bool IsDbSetType(ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            if (current.Name == "DbSet" &&
                current.ContainingNamespace?.ToDisplayString() == "Microsoft.EntityFrameworkCore")
                return true;
            current = current.BaseType;
        }

        return false;
    }

    private static ExpressionSyntax GetReceiver(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            return memberAccess.Expression;
        return null;
    }
}
