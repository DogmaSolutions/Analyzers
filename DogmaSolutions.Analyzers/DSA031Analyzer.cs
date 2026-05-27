using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Use AsNoTracking for Entity Framework queries that provably do not require change tracking.
/// Fires when the query projects to a non-entity type, or when the containing method body
/// does not modify or persist the materialized entities.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA031Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA031";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA031AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA031AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA031AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA031.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] TrackingMethods =
    {
        "AsNoTracking", "AsTracking", "AsNoTrackingWithIdentityResolution"
    };

    private static readonly string[] EntityTerminalMethods =
    {
        "ToListAsync", "ToArrayAsync",
        "FirstAsync", "FirstOrDefaultAsync",
        "SingleAsync", "SingleOrDefaultAsync",
        "LastAsync", "LastOrDefaultAsync",
        "ToDictionaryAsync",
        "ToList", "ToArray",
        "First", "FirstOrDefault",
        "Single", "SingleOrDefault",
        "Last", "LastOrDefault",
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

        if (HasTrackingChoiceInChain(invocation, context.SemanticModel))
            return;

        if (IsNonEntityProjection(invocation, context.SemanticModel))
        {
            Report(context, invocation, methodName);
            return;
        }

        if (IsInReadOnlyMethodBody(invocation, context.SemanticModel))
        {
            Report(context, invocation, methodName);
        }
    }

    private static void Report(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, string methodName)
    {
        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: invocation.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null,
            methodName);
        context.ReportDiagnostic(diagnostic);
    }

    // ── Terminal method detection ──────────────────────────────────────

    private static bool TryGetTerminalMethodName(InvocationExpressionSyntax invocation, out string methodName)
    {
        methodName = null;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var name = memberAccess.Name.Identifier.ValueText;

            if (Array.IndexOf(BulkOperationMethods, name) >= 0)
                return false;

            if (Array.IndexOf(EntityTerminalMethods, name) >= 0)
            {
                methodName = name;
                return true;
            }
        }

        return false;
    }

    // ── Projection detection ──────────────────────────────────────────

    private static bool IsNonEntityProjection(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null || methodSymbol.TypeArguments.Length == 0)
            return false;

        var resultElementType = methodSymbol.TypeArguments[0];
        return IsProvablyNonEntityType(resultElementType);
    }

    private static bool IsProvablyNonEntityType(ITypeSymbol type)
    {
        if (type.IsAnonymousType)
            return true;

        if (type is INamedTypeSymbol { IsTupleType: true })
            return true;

        if (type.SpecialType != SpecialType.None)
            return true;

        return false;
    }

    // ── Read-only method body detection ───────────────────────────────

    private static bool IsInReadOnlyMethodBody(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var methodDecl = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (methodDecl == null)
            return false;

        var entityType = GetEntityTypeFromTerminal(invocation, semanticModel);
        if (entityType == null)
            return false;

        if (ReturnTypeInvolvesEntityType(methodDecl, entityType, semanticModel))
            return false;

        if (MethodHasEntityRefOrOutParameter(methodDecl, entityType, semanticModel))
            return false;

        var methodBody = (SyntaxNode)methodDecl.Body ?? methodDecl.ExpressionBody;
        if (methodBody == null)
            return false;

        if (MethodBodyReturnsEntityType(methodBody, entityType, semanticModel))
            return false;

        if (MethodBodyContainsMutationCalls(methodBody, semanticModel))
            return false;

        if (MethodBodyContainsEntityPropertyAssignment(methodBody, entityType, semanticModel))
            return false;

        if (MethodBodyAssignsEntityToFieldOrProperty(methodBody, entityType, semanticModel))
            return false;

        if (MethodBodyPassesEntityToMethods(methodBody, entityType, semanticModel))
            return false;

        return true;
    }

    private static ITypeSymbol GetEntityTypeFromTerminal(InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol != null && methodSymbol.TypeArguments.Length > 0)
            return methodSymbol.TypeArguments[0];

        return null;
    }

    private static bool ReturnTypeInvolvesEntityType(
        MethodDeclarationSyntax methodDecl,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
        if (methodSymbol == null)
            return false;

        return TypeInvolvesEntityType(methodSymbol.ReturnType, entityType);
    }

    private static bool TypeInvolvesEntityType(ITypeSymbol type, ITypeSymbol entityType)
    {
        if (SymbolEqualityComparer.Default.Equals(type, entityType))
            return true;

        if (type is IArrayTypeSymbol arrayType)
            return TypeInvolvesEntityType(arrayType.ElementType, entityType);

        if (type is INamedTypeSymbol namedType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                if (TypeInvolvesEntityType(typeArg, entityType))
                    return true;
            }
        }

        return false;
    }

    private static bool MethodHasEntityRefOrOutParameter(
        MethodDeclarationSyntax methodDecl,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDecl);
        if (methodSymbol == null)
            return false;

        foreach (var parameter in methodSymbol.Parameters)
        {
            if (parameter.RefKind is RefKind.Out or RefKind.Ref)
            {
                if (TypeInvolvesEntityType(parameter.Type, entityType))
                    return true;
            }
        }

        return false;
    }

    private static bool MethodBodyReturnsEntityType(
        SyntaxNode methodBody,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        foreach (var returnStatement in methodBody.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            if (returnStatement.Expression == null)
                continue;

            var returnType = semanticModel.GetTypeInfo(returnStatement.Expression).Type;
            if (returnType != null && TypeInvolvesEntityType(returnType, entityType))
                return true;
        }

        foreach (var yieldStatement in methodBody.DescendantNodes().OfType<YieldStatementSyntax>())
        {
            if (yieldStatement.Expression == null)
                continue;

            var yieldType = semanticModel.GetTypeInfo(yieldStatement.Expression).Type;
            if (yieldType != null && TypeInvolvesEntityType(yieldType, entityType))
                return true;
        }

        if (methodBody is ArrowExpressionClauseSyntax arrowExpr)
        {
            var exprType = semanticModel.GetTypeInfo(arrowExpr.Expression).Type;
            if (exprType != null && TypeInvolvesEntityType(exprType, entityType))
                return true;
        }

        return false;
    }

    private static bool MethodBodyContainsMutationCalls(SyntaxNode methodBody, SemanticModel semanticModel)
    {
        foreach (var invocation in methodBody.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (methodSymbol == null)
                continue;

            var name = methodSymbol.Name;

            if (name is "SaveChanges" or "SaveChangesAsync")
                return true;

            if (name is "Add" or "AddAsync" or "AddRange" or "AddRangeAsync" or
                "Update" or "UpdateRange" or
                "Remove" or "RemoveRange" or
                "Attach" or "AttachRange" or
                "Entry")
            {
                if (IsDbContextOrDbSetType(methodSymbol.ContainingType))
                    return true;
            }
        }

        return false;
    }

    private static bool MethodBodyContainsEntityPropertyAssignment(
        SyntaxNode methodBody,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        foreach (var assignment in methodBody.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is MemberAccessExpressionSyntax memberAccess)
            {
                if (MemberAccessChainInvolvesEntityType(memberAccess.Expression, entityType, semanticModel))
                    return true;
            }

            if (assignment.Left is ElementAccessExpressionSyntax elementAccess)
            {
                if (MemberAccessChainInvolvesEntityType(elementAccess.Expression, entityType, semanticModel))
                    return true;
            }
        }

        foreach (var postfix in methodBody.DescendantNodes().OfType<PostfixUnaryExpressionSyntax>())
        {
            if (postfix.Operand is MemberAccessExpressionSyntax memberAccess)
            {
                if (MemberAccessChainInvolvesEntityType(memberAccess.Expression, entityType, semanticModel))
                    return true;
            }
        }

        foreach (var prefix in methodBody.DescendantNodes().OfType<PrefixUnaryExpressionSyntax>())
        {
            if (prefix.Operand is MemberAccessExpressionSyntax memberAccess)
            {
                if (MemberAccessChainInvolvesEntityType(memberAccess.Expression, entityType, semanticModel))
                    return true;
            }
        }

        return false;
    }

    private static bool MemberAccessChainInvolvesEntityType(
        ExpressionSyntax expression,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        var current = expression;
        while (current != null)
        {
            var type = semanticModel.GetTypeInfo(current).Type;
            if (type != null && SymbolEqualityComparer.Default.Equals(type, entityType))
                return true;

            if (current is MemberAccessExpressionSyntax memberAccess)
                current = memberAccess.Expression;
            else if (current is ElementAccessExpressionSyntax elementAccess)
                current = elementAccess.Expression;
            else
                break;
        }

        return false;
    }

    private static bool MethodBodyAssignsEntityToFieldOrProperty(
        SyntaxNode methodBody,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        foreach (var assignment in methodBody.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left).Symbol;
            if (leftSymbol is IFieldSymbol || leftSymbol is IPropertySymbol)
            {
                var rightType = semanticModel.GetTypeInfo(assignment.Right).Type;
                if (rightType != null && TypeInvolvesEntityType(rightType, entityType))
                    return true;
            }

            if (assignment.Left is TupleExpressionSyntax tupleExpr)
            {
                var rightType = semanticModel.GetTypeInfo(assignment.Right).Type;
                if (rightType != null && TypeInvolvesEntityType(rightType, entityType))
                {
                    foreach (var element in tupleExpr.Arguments)
                    {
                        var elementSymbol = semanticModel.GetSymbolInfo(element.Expression).Symbol;
                        if (elementSymbol is IFieldSymbol || elementSymbol is IPropertySymbol)
                            return true;
                    }
                }
            }

            if (assignment.Left is ElementAccessExpressionSyntax elementAccess)
            {
                var containerSymbol = semanticModel.GetSymbolInfo(elementAccess.Expression).Symbol;
                if (containerSymbol is IFieldSymbol || containerSymbol is IPropertySymbol)
                {
                    var rightType = semanticModel.GetTypeInfo(assignment.Right).Type;
                    if (rightType != null && TypeInvolvesEntityType(rightType, entityType))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool MethodBodyPassesEntityToMethods(
        SyntaxNode methodBody,
        ITypeSymbol entityType,
        SemanticModel semanticModel)
    {
        foreach (var argument in methodBody.DescendantNodes().OfType<ArgumentSyntax>())
        {
            if (argument.Expression is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
                continue;

            var argType = semanticModel.GetTypeInfo(argument.Expression).Type;
            if (argType == null)
                continue;

            if (TypeInvolvesEntityType(argType, entityType))
                return true;
        }

        return false;
    }

    // ── EF chain detection (shared with DSA030) ───────────────────────

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
                current = GetReceiver(invocation);
            else
                break;
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

    // ── Tracking choice detection ─────────────────────────────────────

    private static bool HasTrackingChoiceInChain(InvocationExpressionSyntax terminalInvocation, SemanticModel semanticModel)
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

    // ── Subquery detection ────────────────────────────────────────────

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

    // ── Shared helpers ────────────────────────────────────────────────

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

    private static bool IsDbContextOrDbSetType(ITypeSymbol type)
    {
        var current = type;
        while (current != null)
        {
            if (current.ContainingNamespace?.ToDisplayString() == "Microsoft.EntityFrameworkCore" &&
                (current.Name == "DbContext" || current.Name == "DbSet"))
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
