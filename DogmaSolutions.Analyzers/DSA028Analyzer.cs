using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA028Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA028";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA028AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA028AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA028AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA028.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] ImmutableReturnTypes =
    [
        "IEnumerable",
        "IReadOnlyCollection",
        "IReadOnlyList",
    ];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeLocalFunction, SyntaxKind.LocalFunctionStatement);
        context.RegisterSyntaxNodeAction(AnalyzePropertyGetter, SyntaxKind.GetAccessorDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeExpressionBodiedProperty, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var method = (MethodDeclarationSyntax)context.Node;
        var returnType = method.ReturnType;
        var semanticModel = context.SemanticModel;

        if (!IsImmutableCollectionReturnType(returnType, semanticModel))
            return;

        if (method.ExpressionBody != null)
        {
            CheckExpression(context, method.ExpressionBody.Expression);
            return;
        }

        if (method.Body == null)
            return;

        foreach (var returnStatement in method.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            if (returnStatement.Expression == null)
                continue;

            if (IsInsideNestedFunction(returnStatement, method))
                continue;

            CheckReturnedExpression(context, returnStatement.Expression, method.Body, semanticModel);
        }
    }

    private static void AnalyzeLocalFunction(SyntaxNodeAnalysisContext context)
    {
        var localFunc = (LocalFunctionStatementSyntax)context.Node;
        var returnType = localFunc.ReturnType;
        var semanticModel = context.SemanticModel;

        if (!IsImmutableCollectionReturnType(returnType, semanticModel))
            return;

        if (localFunc.ExpressionBody != null)
        {
            CheckExpression(context, localFunc.ExpressionBody.Expression);
            return;
        }

        if (localFunc.Body == null)
            return;

        foreach (var returnStatement in localFunc.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            if (returnStatement.Expression == null)
                continue;

            if (IsInsideNestedFunction(returnStatement, localFunc))
                continue;

            CheckReturnedExpression(context, returnStatement.Expression, localFunc.Body, semanticModel);
        }
    }

    private static void AnalyzePropertyGetter(SyntaxNodeAnalysisContext context)
    {
        var accessor = (AccessorDeclarationSyntax)context.Node;
        if (!accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
            return;

        TypeSyntax returnType;
        var grandparent = accessor.Parent?.Parent;
        if (grandparent is PropertyDeclarationSyntax property)
            returnType = property.Type;
        else if (grandparent is IndexerDeclarationSyntax indexer)
            returnType = indexer.Type;
        else
            return;

        var semanticModel = context.SemanticModel;
        if (!IsImmutableCollectionReturnType(returnType, semanticModel))
            return;

        if (accessor.ExpressionBody != null)
        {
            CheckExpression(context, accessor.ExpressionBody.Expression);
            return;
        }

        if (accessor.Body == null)
            return;

        foreach (var returnStatement in accessor.Body.DescendantNodes().OfType<ReturnStatementSyntax>())
        {
            if (returnStatement.Expression == null)
                continue;

            CheckReturnedExpression(context, returnStatement.Expression, accessor.Body, semanticModel);
        }
    }

    private static void AnalyzeExpressionBodiedProperty(SyntaxNodeAnalysisContext context)
    {
        var property = (PropertyDeclarationSyntax)context.Node;
        if (property.ExpressionBody == null)
            return;

        var semanticModel = context.SemanticModel;
        if (!IsImmutableCollectionReturnType(property.Type, semanticModel))
            return;

        CheckExpression(context, property.ExpressionBody.Expression);
    }

    private static bool IsImmutableCollectionReturnType(TypeSyntax typeSyntax, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
        var typeSymbol = typeInfo.Type;
        if (typeSymbol == null)
            return false;

        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType && namedType.TypeArguments.Length == 1)
        {
            var outerDef = namedType.OriginalDefinition;
            var outerNs = outerDef.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            if (outerNs == "System.Threading.Tasks" && outerDef.Name is "Task" or "ValueTask")
                typeSymbol = namedType.TypeArguments[0];
        }

        var originalDef = typeSymbol.OriginalDefinition;
        var name = originalDef.Name;
        var ns = originalDef.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        if (ns != "System.Collections.Generic" && ns != "System.Collections")
            return false;

        foreach (var allowed in ImmutableReturnTypes)
        {
            if (name == allowed)
                return true;
        }

        if (name == "IEnumerable" && ns == "System.Collections")
            return true;

        return false;
    }

    private static void CheckExpression(SyntaxNodeAnalysisContext context, ExpressionSyntax expression)
    {
        switch (expression)
        {
            case InvocationExpressionSyntax when IsToListInvocation(expression):
                ReportDiagnostic(context, expression);
                break;
            case ConditionalExpressionSyntax ternary:
                CheckExpression(context, ternary.WhenTrue);
                CheckExpression(context, ternary.WhenFalse);
                break;
            case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression):
                CheckExpression(context, coalesce.Left);
                CheckExpression(context, coalesce.Right);
                break;
            case ParenthesizedExpressionSyntax paren:
                CheckExpression(context, paren.Expression);
                break;
            case CastExpressionSyntax cast:
                CheckExpression(context, cast.Expression);
                break;
            case SwitchExpressionSyntax switchExpr:
                foreach (var arm in switchExpr.Arms)
                    CheckExpression(context, arm.Expression);
                break;
        }
    }

    private static void CheckReturnedExpression(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax expression,
        SyntaxNode methodBody,
        SemanticModel semanticModel)
    {
        switch (expression)
        {
            case ConditionalExpressionSyntax ternary:
                CheckReturnedExpression(context, ternary.WhenTrue, methodBody, semanticModel);
                CheckReturnedExpression(context, ternary.WhenFalse, methodBody, semanticModel);
                return;
            case BinaryExpressionSyntax coalesce when coalesce.IsKind(SyntaxKind.CoalesceExpression):
                CheckReturnedExpression(context, coalesce.Left, methodBody, semanticModel);
                CheckReturnedExpression(context, coalesce.Right, methodBody, semanticModel);
                return;
            case ParenthesizedExpressionSyntax paren:
                CheckReturnedExpression(context, paren.Expression, methodBody, semanticModel);
                return;
            case CastExpressionSyntax cast:
                CheckReturnedExpression(context, cast.Expression, methodBody, semanticModel);
                return;
            case SwitchExpressionSyntax switchExpr:
                foreach (var arm in switchExpr.Arms)
                    CheckReturnedExpression(context, arm.Expression, methodBody, semanticModel);
                return;
        }

        if (IsToListInvocation(expression))
        {
            ReportDiagnostic(context, expression);
            return;
        }

        if (expression is not IdentifierNameSyntax identifier)
            return;

        var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
        if (symbol is not ILocalSymbol localSymbol)
            return;

        var variableName = identifier.Identifier.ValueText;
        var assignment = FindToListAssignment(methodBody, variableName);
        if (assignment == null)
            return;

        if (IsVariableMutatedAfterAssignment(methodBody, variableName, assignment))
            return;

        if (WouldToArrayBreakCode(methodBody, variableName, assignment, localSymbol))
            return;

        ReportDiagnostic(context, assignment);
    }

    private static bool IsToListInvocation(ExpressionSyntax expression)
    {
        if (expression is not InvocationExpressionSyntax invocation)
            return false;

        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return false;

        return memberAccess.Name.Identifier.ValueText == "ToList" &&
               invocation.ArgumentList.Arguments.Count == 0;
    }

    private static InvocationExpressionSyntax FindToListAssignment(SyntaxNode body, string variableName)
    {
        foreach (var declarator in body.DescendantNodes().OfType<VariableDeclaratorSyntax>())
        {
            if (declarator.Identifier.ValueText != variableName)
                continue;

            if (declarator.Initializer?.Value is InvocationExpressionSyntax invocation &&
                IsToListInvocation(invocation))
                return invocation;
        }

        foreach (var assignment in body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is not IdentifierNameSyntax assignTarget)
                continue;

            if (assignTarget.Identifier.ValueText != variableName)
                continue;

            if (assignment.Right is InvocationExpressionSyntax invocation &&
                IsToListInvocation(invocation))
                return invocation;
        }

        return null;
    }

    private static bool IsVariableMutatedAfterAssignment(
        SyntaxNode body,
        string variableName,
        InvocationExpressionSyntax toListInvocation)
    {
        var toListSpan = toListInvocation.Span;

        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.SpanStart <= toListSpan.End)
                continue;

            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
                continue;

            if (memberAccess.Expression is not IdentifierNameSyntax receiver)
                continue;

            if (receiver.Identifier.ValueText != variableName)
                continue;

            var methodName = memberAccess.Name.Identifier.ValueText;
            if (IsMutatingMethod(methodName))
                return true;
        }

        foreach (var elementAccess in body.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
        {
            if (elementAccess.SpanStart <= toListSpan.End)
                continue;

            if (elementAccess.Expression is not IdentifierNameSyntax receiver)
                continue;

            if (receiver.Identifier.ValueText != variableName)
                continue;

            if (elementAccess.Parent is AssignmentExpressionSyntax assignment &&
                assignment.Left == elementAccess)
                return true;
        }

        foreach (var assignment in body.DescendantNodes().OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.SpanStart <= toListSpan.End)
                continue;

            if (assignment.Left is IdentifierNameSyntax target &&
                target.Identifier.ValueText == variableName)
                return true;
        }

        return false;
    }

    private static bool WouldToArrayBreakCode(
        SyntaxNode body,
        string variableName,
        InvocationExpressionSyntax toListInvocation,
        ILocalSymbol localSymbol)
    {
        var declarator = toListInvocation.Parent is EqualsValueClauseSyntax eq
            ? eq.Parent as VariableDeclaratorSyntax
            : null;

        var isVarDeclaration = false;
        if (declarator != null)
        {
            var declaration = declarator.Parent as VariableDeclarationSyntax;
            isVarDeclaration = declaration?.Type.IsVar ?? false;
        }

        if (!isVarDeclaration)
        {
            var typeOriginalDef = localSymbol.Type.OriginalDefinition;
            if (typeOriginalDef.Name == "List" &&
                (typeOriginalDef.ContainingNamespace?.ToDisplayString() ?? string.Empty) == "System.Collections.Generic")
                return true;

            return false;
        }

        return UsesListSpecificMembers(body, variableName);
    }

    private static bool UsesListSpecificMembers(SyntaxNode body, string variableName)
    {
        foreach (var memberAccess in body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (memberAccess.Expression is not IdentifierNameSyntax receiver)
                continue;

            if (receiver.Identifier.ValueText != variableName)
                continue;

            var memberName = memberAccess.Name.Identifier.ValueText;

            if (memberAccess.Parent is InvocationExpressionSyntax)
            {
                if (IsListSpecificMethod(memberName))
                    return true;
            }
            else
            {
                if (memberName is "Count" or "Capacity")
                    return true;
            }
        }

        return false;
    }

    private static bool IsListSpecificMethod(string methodName)
    {
        return methodName is "Find" or "FindAll" or "FindIndex" or "FindLast" or "FindLastIndex"
            or "Exists" or "TrueForAll" or "ConvertAll" or "GetRange"
            or "BinarySearch" or "ForEach"
            or "IndexOf" or "LastIndexOf" or "AsReadOnly" or "EnsureCapacity";
    }

    private static bool IsMutatingMethod(string methodName)
    {
        return methodName is "Add" or "AddRange" or "Insert" or "InsertRange"
            or "Remove" or "RemoveAt" or "RemoveAll" or "RemoveRange"
            or "Clear" or "Sort" or "Reverse" or "Set"
            or "TrimExcess" or "CopyTo";
    }

    private static bool IsInsideNestedFunction(SyntaxNode node, SyntaxNode boundary)
    {
        var current = node.Parent;
        while (current != null && current != boundary)
        {
            if (current is LambdaExpressionSyntax ||
                current is AnonymousMethodExpressionSyntax ||
                current is LocalFunctionStatementSyntax)
                return true;
            current = current.Parent;
        }

        return false;
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, ExpressionSyntax toListExpression)
    {
        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: toListExpression.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null);

        context.ReportDiagnostic(diagnostic);
    }
}
