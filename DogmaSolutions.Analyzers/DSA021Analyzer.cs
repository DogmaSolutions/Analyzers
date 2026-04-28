using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA021Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA021";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA021AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA021AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA021AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.BestPractice;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA021");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] TagMethods = { "TagWith", "TagWithCallSite" };

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
        "ExecuteUpdateAsync", "ExecuteDeleteAsync",
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

        if (HasTagInChain(invocation, context.SemanticModel, context.SemanticModel.Compilation))
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
        if (terminalSymbol != null && IsEntityFrameworkExtensionMethod(terminalSymbol))
            return true;

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
                return LocalInitializerInvolvesEf(localSymbol, semanticModel);
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

    internal static bool HasTagInChain(
        InvocationExpressionSyntax terminalInvocation,
        SemanticModel semanticModel,
        Compilation compilation)
    {
        ExpressionSyntax current = terminalInvocation;
        while (current is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                if (IsTagMethod(memberAccess.Name.Identifier.ValueText))
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
                return ParameterHasTagAtAllCallSites(parameter, compilation);

            if (symbolInfo.Symbol is ILocalSymbol localSymbol)
                return LocalHasTagInInitializer(localSymbol);
        }

        return false;
    }

    private static bool LocalHasTagInInitializer(ISymbol localSymbol)
    {
        var declaringRefs = localSymbol.DeclaringSyntaxReferences;
        if (declaringRefs.Length == 0)
            return false;

        var declarationNode = declaringRefs[0].GetSyntax();
        if (!(declarationNode is VariableDeclaratorSyntax declarator) || declarator.Initializer == null)
            return false;

        return ExpressionContainsTag(declarator.Initializer.Value);
    }

    private static bool ExpressionContainsTag(ExpressionSyntax expression)
    {
        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                IsTagMethod(memberAccess.Name.Identifier.ValueText))
                return true;
        }

        return false;
    }

    internal static bool ParameterHasTagAtAllCallSites(
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

                if (!ArgumentExpressionHasTag(argExpr, model, compilation, maxDepth - 1))
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

    private static bool ArgumentExpressionHasTag(
        ExpressionSyntax expression,
        SemanticModel model,
        Compilation compilation,
        int maxDepth)
    {
        if (ExpressionContainsTag(expression))
            return true;

        var symbolInfo = model.GetSymbolInfo(expression);
        if (symbolInfo.Symbol == null)
            return false;

        if (symbolInfo.Symbol is IParameterSymbol nestedParam && maxDepth > 0)
            return ParameterHasTagAtAllCallSites(nestedParam, compilation, maxDepth);

        if (symbolInfo.Symbol is ILocalSymbol localSymbol)
            return LocalHasTagInInitializer(localSymbol);

        return false;
    }

    private static bool IsTagMethod(string methodName) => Array.IndexOf(TagMethods, methodName) >= 0;

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