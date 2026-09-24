using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA035Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA035";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA035AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA035AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA035AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA035.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly HashSet<string> ReflectionMethodNames = new(StringComparer.Ordinal)
    {
        "GetType",
        "GetProperty",
        "GetProperties",
        "GetMethod",
        "GetMethods",
        "GetField",
        "GetFields",
        "GetMember",
        "GetMembers",
        "GetConstructor",
        "GetConstructors",
        "InvokeMember",
        "GetCustomAttribute",
        "GetCustomAttributes",
        "GetCustomAttributeData",
        "GetInterface",
        "GetInterfaces",
        "GetNestedType",
        "GetNestedTypes",
        "GetEvent",
        "GetEvents",
        "IsAssignableFrom",
        "IsInstanceOfType",
        "IsSubclassOf",
        "MakeGenericType",
        "GetGenericArguments",
    };

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeLoop,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.ForEachVariableStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement);
    }

    private static void AnalyzeLoop(SyntaxNodeAnalysisContext context)
    {
        var loopNode = context.Node;
        var body = GetLoopBody(loopNode);
        if (body == null)
            return;

        var modifiedSymbols = DSA022Analyzer.CollectModifiedSymbols(body, loopNode, context.SemanticModel);
        CollectLambdaParameters(body, context.SemanticModel, modifiedSymbols);

        foreach (var invocation in body.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (IsInsideNestedLoop(invocation, body))
                continue;

            if (!IsReflectionInvocation(invocation, context.SemanticModel))
                continue;

            if (!IsReceiverLoopInvariant(invocation, modifiedSymbols, context.SemanticModel))
                continue;

            if (!AreArgumentsLoopInvariant(invocation, modifiedSymbols, context.SemanticModel))
                continue;

            var diagnostic = Diagnostic.Create(
                descriptor: _rule,
                location: invocation.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                invocation.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static StatementSyntax GetLoopBody(SyntaxNode loopNode)
    {
        switch (loopNode)
        {
            case ForStatementSyntax forStmt: return forStmt.Statement;
            case ForEachStatementSyntax forEachStmt: return forEachStmt.Statement;
            case ForEachVariableStatementSyntax forEachVarStmt: return forEachVarStmt.Statement;
            case WhileStatementSyntax whileStmt: return whileStmt.Statement;
            case DoStatementSyntax doStmt: return doStmt.Statement;
            default: return null;
        }
    }

    private static bool IsInsideNestedLoop(SyntaxNode expr, StatementSyntax loopBody)
    {
        var current = expr.Parent;
        while (current != null && current != loopBody)
        {
            if (current is ForStatementSyntax || current is ForEachStatementSyntax ||
                current is ForEachVariableStatementSyntax ||
                current is WhileStatementSyntax || current is DoStatementSyntax)
                return true;
            current = current.Parent;
        }

        return false;
    }

    private static void CollectLambdaParameters(SyntaxNode body, SemanticModel model, HashSet<ISymbol> symbols)
    {
        foreach (var node in body.DescendantNodes())
        {
            switch (node)
            {
                case ParenthesizedLambdaExpressionSyntax lambda:
                    foreach (var param in lambda.ParameterList.Parameters)
                    {
                        var sym = model.GetDeclaredSymbol(param);
                        if (sym != null)
                            symbols.Add(sym);
                    }
                    break;

                case SimpleLambdaExpressionSyntax simple:
                    var simpleSym = model.GetDeclaredSymbol(simple.Parameter);
                    if (simpleSym != null)
                        symbols.Add(simpleSym);
                    break;

                case AnonymousMethodExpressionSyntax anon when anon.ParameterList != null:
                    foreach (var param in anon.ParameterList.Parameters)
                    {
                        var sym = model.GetDeclaredSymbol(param);
                        if (sym != null)
                            symbols.Add(sym);
                    }
                    break;

                case LocalFunctionStatementSyntax localFunc:
                    foreach (var param in localFunc.ParameterList.Parameters)
                    {
                        var sym = model.GetDeclaredSymbol(param);
                        if (sym != null)
                            symbols.Add(sym);
                    }
                    break;
            }
        }
    }

    private static bool IsReflectionInvocation(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        string methodName;
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess:
                methodName = memberAccess.Name.Identifier.ValueText;
                break;
            case MemberBindingExpressionSyntax memberBinding:
                methodName = memberBinding.Name.Identifier.ValueText;
                break;
            case IdentifierNameSyntax identifier:
                methodName = identifier.Identifier.ValueText;
                break;
            default:
                return false;
        }

        if (!ReflectionMethodNames.Contains(methodName))
            return false;

        var symbolInfo = model.GetSymbolInfo(invocation);
        var methodSymbol = symbolInfo.Symbol as IMethodSymbol ?? symbolInfo.CandidateSymbols.OfType<IMethodSymbol>().FirstOrDefault();
        if (methodSymbol == null)
            return false;

        var originalMethod = methodSymbol.OriginalDefinition;
        var declaringType = originalMethod.ContainingType;
        if (declaringType == null)
            return false;

        return IsReflectionDeclaringType(declaringType);
    }

    private static bool IsReflectionDeclaringType(INamedTypeSymbol type)
    {
        var ns = type.ContainingNamespace?.ToDisplayString();

        if (ns == "System" && type.Name == "Object")
            return true;

        if (ns == "System" && type.Name == "Type")
            return true;

        if (ns == "System" && type.Name == "Attribute")
            return true;

        if (ns == "System.Reflection")
            return true;

        if (ns == "System.Reflection.Emit")
            return true;

        return false;
    }

    private static bool IsReceiverLoopInvariant(
        InvocationExpressionSyntax invocation,
        HashSet<ISymbol> modifiedSymbols,
        SemanticModel model)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            return IsExpressionLoopInvariant(memberAccess.Expression, modifiedSymbols, model);

        if (invocation.Expression is MemberBindingExpressionSyntax &&
            invocation.Parent is ConditionalAccessExpressionSyntax conditionalAccess)
            return IsExpressionLoopInvariant(conditionalAccess.Expression, modifiedSymbols, model);

        return false;
    }

    private static bool AreArgumentsLoopInvariant(
        InvocationExpressionSyntax invocation,
        HashSet<ISymbol> modifiedSymbols,
        SemanticModel model)
    {
        foreach (var argument in invocation.ArgumentList.Arguments)
        {
            if (!IsExpressionLoopInvariant(argument.Expression, modifiedSymbols, model))
                return false;
        }

        return true;
    }

    private static bool IsExpressionLoopInvariant(
        ExpressionSyntax expression,
        HashSet<ISymbol> modifiedSymbols,
        SemanticModel model)
    {
        foreach (var node in expression.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case IdentifierNameSyntax identifier:
                    if (identifier.Parent is MemberAccessExpressionSyntax mas && mas.Name == identifier)
                        break;

                    var symbol = model.GetSymbolInfo(identifier).Symbol;
                    if (symbol == null)
                        return false;

                    if (modifiedSymbols.Contains(symbol))
                        return false;

                    switch (symbol)
                    {
                        case ILocalSymbol:
                        case IParameterSymbol:
                        case IFieldSymbol { IsConst: true }:
                        case IFieldSymbol { IsReadOnly: true }:
                        case IFieldSymbol { IsStatic: true }:
                        case IPropertySymbol:
                        case INamedTypeSymbol:
                        case ITypeParameterSymbol:
                        case INamespaceSymbol:
                            break;
                        default:
                            return false;
                    }
                    break;

                case InvocationExpressionSyntax nestedInvocation:
                    if (IsReflectionInvocation(nestedInvocation, model))
                    {
                        if (!IsReceiverLoopInvariant(nestedInvocation, modifiedSymbols, model) ||
                            !AreArgumentsLoopInvariant(nestedInvocation, modifiedSymbols, model))
                            return false;
                    }
                    else
                    {
                        return false;
                    }
                    break;

                case ObjectCreationExpressionSyntax:
                case AwaitExpressionSyntax:
                    return false;
            }
        }

        return true;
    }

}
