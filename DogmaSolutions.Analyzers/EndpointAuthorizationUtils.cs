using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class EndpointAuthorizationUtils
{
    internal static readonly string[] MapMethods =
    {
        "MapGet", "MapPost", "MapPut", "MapDelete", "MapPatch", "MapMethods", "Map"
    };

    internal static readonly string[] AuthMethods = { "RequireAuthorization", "AllowAnonymous" };

    internal static bool TryGetMapMethodName(InvocationExpressionSyntax invocation, out string methodName)
    {
        methodName = null;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var name = memberAccess.Name.Identifier.ValueText;
            if (Array.IndexOf(MapMethods, name) >= 0)
            {
                methodName = name;
                return true;
            }
        }

        return false;
    }

    internal static bool IsAuthMethod(string methodName) => Array.IndexOf(AuthMethods, methodName) >= 0;

    internal static bool IsRouteGroupBuilder(ITypeSymbol type) =>
        type != null &&
        type.Name == "RouteGroupBuilder" &&
        type.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Routing";

    internal static ISymbol GetReceiverSymbol(InvocationExpressionSyntax mapInvocation, SemanticModel semanticModel)
    {
        if (mapInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(memberAccess.Expression);
            return symbolInfo.Symbol;
        }

        return null;
    }

    internal static ITypeSymbol GetReceiverType(InvocationExpressionSyntax mapInvocation, SemanticModel semanticModel)
    {
        if (mapInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var typeInfo = semanticModel.GetTypeInfo(memberAccess.Expression);
            return typeInfo.Type;
        }

        return null;
    }

    /// <summary>
    /// Walks the fluent invocation chain both upward (parent invocations calling methods
    /// on this expression's result) and downward (the receiver chain before this call)
    /// looking for RequireAuthorization or AllowAnonymous.
    /// </summary>
    internal static bool HasAuthInFluentChain(InvocationExpressionSyntax mapInvocation)
    {
        // Walk UP: the Map* call might be the receiver of a chain like
        // builder.MapGet(...).WithName(...).RequireAuthorization()
        var current = (SyntaxNode)mapInvocation;
        while (current.Parent is MemberAccessExpressionSyntax parentAccess &&
               parentAccess.Parent is InvocationExpressionSyntax parentInvocation)
        {
            if (IsAuthMethod(parentAccess.Name.Identifier.ValueText))
                return true;
            current = parentInvocation;
        }

        // Walk DOWN: handles cases where auth is called on the receiver
        // before the Map call in the same expression
        if (mapInvocation.Expression is MemberAccessExpressionSyntax access)
        {
            if (HasAuthInDescendantInvocations(access.Expression))
                return true;
        }

        return false;
    }

    internal static bool HasAuthInDescendantInvocations(SyntaxNode node)
    {
        foreach (var invocation in node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                IsAuthMethod(memberAccess.Name.Identifier.ValueText))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether a symbol (local variable, field, property, or parameter) has auth
    /// configured locally: in its declaration initializer, via separate method calls in the
    /// same scope, or inherited from a parent MapGroup receiver.
    /// </summary>
    internal static bool HasAuthOnSymbolLocally(ISymbol symbol, SemanticModel semanticModel, HashSet<ISymbol> visited)
    {
        if (!visited.Add(symbol))
            return false;

        var declaringSyntax = symbol.DeclaringSyntaxReferences;
        if (declaringSyntax.Length == 0)
            return false;

        var declarationNode = declaringSyntax[0].GetSyntax();
        var containingBlock = GetContainingBlock(declarationNode);
        if (containingBlock == null)
            return false;

        // Check the initializer of the variable declaration
        if (declarationNode is VariableDeclaratorSyntax declarator && declarator.Initializer != null)
        {
            var initValue = declarator.Initializer.Value;

            if (HasAuthInDescendantInvocations(initValue))
                return true;

            // Check if the initializer is a MapGroup chain, and if so,
            // recursively check the group's receiver
            if (TryGetReceiverOfMapGroup(initValue, out var groupReceiver))
            {
                var receiverSymbol = semanticModel.GetSymbolInfo(groupReceiver);
                if (receiverSymbol.Symbol != null && HasAuthOnSymbolLocally(receiverSymbol.Symbol, semanticModel, visited))
                    return true;
            }
        }

        // Check for separate statements like: group.RequireAuthorization();
        foreach (var invocation in containingBlock.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax access &&
                IsAuthMethod(access.Name.Identifier.ValueText))
            {
                var receiverSymbol = semanticModel.GetSymbolInfo(access.Expression);
                if (SymbolEqualityComparer.Default.Equals(receiverSymbol.Symbol, symbol))
                    return true;
            }
        }

        // Check assignments (not just the declaration initializer) for auth in the assigned value
        foreach (var assignment in containingBlock.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left);
            if (SymbolEqualityComparer.Default.Equals(leftSymbol.Symbol, symbol))
            {
                if (HasAuthInDescendantInvocations(assignment.Right))
                    return true;

                if (TryGetReceiverOfMapGroup(assignment.Right, out var groupReceiver))
                {
                    var receiverSymbol = semanticModel.GetSymbolInfo(groupReceiver);
                    if (receiverSymbol.Symbol != null && HasAuthOnSymbolLocally(receiverSymbol.Symbol, semanticModel, visited))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether authorization is configured at all call sites for the method
    /// containing the given parameter. Searches the entire compilation for invocations
    /// and verifies that each call site passes an argument with auth for this parameter.
    /// </summary>
    internal static bool ParameterHasAuthAtAllCallSites(
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
                    return false; // Cannot determine argument → assume no auth

                if (!ExpressionHasAuth(argExpr, model, compilation, maxDepth - 1))
                    return false; // This call site lacks auth
            }
        }

        return foundAnySite;
    }

    /// <summary>
    /// If the expression is (or contains) a MapGroup invocation, returns the receiver
    /// of that MapGroup call (the parent builder/group).
    /// </summary>
    internal static bool TryGetReceiverOfMapGroup(ExpressionSyntax expression, out ExpressionSyntax receiver)
    {
        receiver = null;

        foreach (var invocation in expression.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is MemberAccessExpressionSyntax access &&
                access.Name.Identifier.ValueText == "MapGroup")
            {
                receiver = access.Expression;
                return true;
            }
        }

        return false;
    }

    internal static SyntaxNode GetContainingBlock(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is BlockSyntax || current is GlobalStatementSyntax ||
                current is CompilationUnitSyntax || current is MethodDeclarationSyntax ||
                current is LocalFunctionStatementSyntax)
                return current;
            current = current.Parent;
        }

        return null;
    }

    private static bool IsCallToMethod(IMethodSymbol invokedSymbol, IMethodSymbol targetMethod)
    {
        var candidateOriginal = invokedSymbol.OriginalDefinition;
        var candidateReduced = invokedSymbol.ReducedFrom?.OriginalDefinition;
        var targetOriginal = targetMethod.OriginalDefinition;

        return SymbolEqualityComparer.Default.Equals(candidateOriginal, targetOriginal) ||
               (candidateReduced != null && SymbolEqualityComparer.Default.Equals(candidateReduced, targetOriginal));
    }

    private static ExpressionSyntax GetArgumentForParameter(
        InvocationExpressionSyntax invocation,
        IParameterSymbol parameter,
        IMethodSymbol invokedSymbol)
    {
        var paramOrdinal = parameter.Ordinal;
        var isReducedExtensionCall = invokedSymbol.ReducedFrom != null;

        // Extension method 'this' parameter: the receiver expression is the first argument
        if (isReducedExtensionCall && paramOrdinal == 0)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                return memberAccess.Expression;
            return null;
        }

        var argIndex = isReducedExtensionCall ? paramOrdinal - 1 : paramOrdinal;

        // Check for named arguments first
        foreach (var arg in invocation.ArgumentList.Arguments)
        {
            if (arg.NameColon != null && arg.NameColon.Name.Identifier.ValueText == parameter.Name)
                return arg.Expression;
        }

        // Fall back to positional
        if (argIndex >= 0 && argIndex < invocation.ArgumentList.Arguments.Count)
        {
            var arg = invocation.ArgumentList.Arguments[argIndex];
            if (arg.NameColon == null)
                return arg.Expression;
        }

        return null;
    }

    private static bool ExpressionHasAuth(
        ExpressionSyntax expression,
        SemanticModel model,
        Compilation compilation,
        int maxDepth)
    {
        // Check if the expression itself has auth in its invocation chain
        if (HasAuthInDescendantInvocations(expression))
            return true;

        var symbolInfo = model.GetSymbolInfo(expression);
        if (symbolInfo.Symbol == null)
            return false;

        // If the expression resolves to a parameter, recurse to its call sites
        if (symbolInfo.Symbol is IParameterSymbol nestedParam && maxDepth > 0)
            return ParameterHasAuthAtAllCallSites(nestedParam, compilation, maxDepth);

        // Otherwise check local auth on the resolved symbol
        return HasAuthOnSymbolLocally(symbolInfo.Symbol, model, new HashSet<ISymbol>(SymbolEqualityComparer.Default));
    }
}
