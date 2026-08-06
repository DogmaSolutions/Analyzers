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
public sealed class DSA036Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA036";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA036AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA036AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA036AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA036.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly HashSet<string> StaticRegexMethods = new(StringComparer.Ordinal)
    {
        "IsMatch",
        "Match",
        "Matches",
        "Replace",
        "Split",
        "Count",
        "EnumerateMatches",
    };

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeNode,
            SyntaxKind.ObjectCreationExpression,
            SyntaxKind.ImplicitObjectCreationExpression,
            SyntaxKind.InvocationExpression);
    }

    private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
    {
        switch (context.Node)
        {
            case ObjectCreationExpressionSyntax creation:
                AnalyzeObjectCreation(context, creation, creation.ArgumentList);
                break;
            case ImplicitObjectCreationExpressionSyntax implicitCreation:
                AnalyzeImplicitObjectCreation(context, implicitCreation);
                break;
            case InvocationExpressionSyntax invocation:
                AnalyzeStaticInvocation(context, invocation);
                break;
        }
    }

    private static void AnalyzeObjectCreation(
        SyntaxNodeAnalysisContext context,
        ObjectCreationExpressionSyntax creation,
        ArgumentListSyntax argumentList)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        if (!IsRegexType(typeInfo.Type))
            return;

        AnalyzeRegexCreation(context, creation, argumentList);
    }

    private static void AnalyzeImplicitObjectCreation(
        SyntaxNodeAnalysisContext context,
        ImplicitObjectCreationExpressionSyntax creation)
    {
        var typeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken);
        if (!IsRegexType(typeInfo.ConvertedType))
            return;

        AnalyzeRegexCreation(context, creation, creation.ArgumentList);
    }

    private static void AnalyzeRegexCreation(
        SyntaxNodeAnalysisContext context,
        ExpressionSyntax creation,
        ArgumentListSyntax argumentList)
    {
        if (argumentList == null || argumentList.Arguments.Count == 0)
            return;

        if (IsInStaticReadOnlyFieldInitializer(creation))
            return;

        var enclosingMethod = GetEnclosingMethodBody(creation);
        if (enclosingMethod == null)
            return;

        foreach (var arg in argumentList.Arguments)
        {
            if (!IsEffectivelyConstant(arg.Expression, context.SemanticModel, enclosingMethod))
                return;
        }

        var props = ImmutableDictionary.CreateBuilder<string, string>();
        props.Add("Kind", "Constructor");

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: _rule,
            location: creation.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: props.ToImmutable(),
            creation.ToString()));
    }

    private static void AnalyzeStaticInvocation(
        SyntaxNodeAnalysisContext context,
        InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess)
            return;

        var methodName = memberAccess.Name.Identifier.ValueText;
        if (!StaticRegexMethods.Contains(methodName))
            return;

        var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return;

        if (!methodSymbol.IsStatic)
            return;

        if (!IsRegexType(methodSymbol.ContainingType))
            return;

        if (IsInStaticReadOnlyFieldInitializer(invocation))
            return;

        var enclosingMethod = GetEnclosingMethodBody(invocation);
        if (enclosingMethod == null)
            return;

        foreach (var param in methodSymbol.Parameters)
        {
            if (IsInputParameter(param))
                continue;

            var argIndex = GetArgumentIndex(invocation.ArgumentList, param);
            if (argIndex < 0 || argIndex >= invocation.ArgumentList.Arguments.Count)
                continue;

            var argExpression = invocation.ArgumentList.Arguments[argIndex].Expression;
            if (!IsEffectivelyConstant(argExpression, context.SemanticModel, enclosingMethod))
                return;
        }

        var props = ImmutableDictionary.CreateBuilder<string, string>();
        props.Add("Kind", "StaticMethod");
        props.Add("MethodName", methodName);

        context.ReportDiagnostic(Diagnostic.Create(
            descriptor: _rule,
            location: invocation.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: props.ToImmutable(),
            invocation.ToString()));
    }

    private static bool IsRegexType(ITypeSymbol type)
    {
        if (type == null)
            return false;
        return type.ToDisplayString() == "System.Text.RegularExpressions.Regex";
    }

    private static bool IsInputParameter(IParameterSymbol param)
    {
        return param.Name is "input" or "replacement" or "evaluator";
    }

    private static int GetArgumentIndex(ArgumentListSyntax argumentList, IParameterSymbol param)
    {
        for (var i = 0; i < argumentList.Arguments.Count; i++)
        {
            var arg = argumentList.Arguments[i];
            if (arg.NameColon != null)
            {
                if (arg.NameColon.Name.Identifier.ValueText == param.Name)
                    return i;
            }
            else if (i == param.Ordinal)
            {
                return i;
            }
        }

        return -1;
    }

    internal static bool IsEffectivelyConstant(ExpressionSyntax expression, SemanticModel model, SyntaxNode enclosingScope)
    {
        if (expression == null)
            return false;

        var constantValue = model.GetConstantValue(expression);
        if (constantValue.HasValue)
            return true;

        switch (expression)
        {
            case LiteralExpressionSyntax:
            case DefaultExpressionSyntax:
                return true;

            case ParenthesizedExpressionSyntax paren:
                return IsEffectivelyConstant(paren.Expression, model, enclosingScope);

            case CastExpressionSyntax cast:
                return IsEffectivelyConstant(cast.Expression, model, enclosingScope);

            case BinaryExpressionSyntax binary:
                return IsEffectivelyConstant(binary.Left, model, enclosingScope) &&
                       IsEffectivelyConstant(binary.Right, model, enclosingScope);

            case PrefixUnaryExpressionSyntax prefix:
                return IsEffectivelyConstant(prefix.Operand, model, enclosingScope);

            case MemberAccessExpressionSyntax memberAccess:
                return IsEffectivelyConstantSymbol(model.GetSymbolInfo(memberAccess).Symbol);

            case IdentifierNameSyntax identifier:
            {
                var symbol = model.GetSymbolInfo(identifier).Symbol;
                if (IsEffectivelyConstantSymbol(symbol))
                    return true;

                if (symbol is ILocalSymbol local)
                    return IsLocalEffectivelyConstant(local, identifier, model, enclosingScope);

                return false;
            }

            case TypeOfExpressionSyntax:
                return true;

            case InterpolatedStringExpressionSyntax interpolated:
                return interpolated.Contents.All(c =>
                    c is InterpolatedStringTextSyntax ||
                    (c is InterpolationSyntax interp && IsEffectivelyConstant(interp.Expression, model, enclosingScope)));

            default:
                return false;
        }
    }

    private static bool IsEffectivelyConstantSymbol(ISymbol symbol)
    {
        if (symbol == null)
            return false;

        if (symbol is IFieldSymbol field)
        {
            if (field.IsConst)
                return true;
            if (field.IsStatic && field.IsReadOnly)
                return true;
        }

        if (symbol is ILocalSymbol { IsConst: true })
            return true;

        return false;
    }

    private static bool IsLocalEffectivelyConstant(
        ILocalSymbol local,
        IdentifierNameSyntax usage,
        SemanticModel model,
        SyntaxNode enclosingScope)
    {
        if (local.IsConst)
            return true;

        var declaringSyntaxRef = local.DeclaringSyntaxReferences.FirstOrDefault();
        if (declaringSyntaxRef == null)
            return false;

        var declaratorNode = declaringSyntaxRef.GetSyntax();
        if (declaratorNode is not VariableDeclaratorSyntax declarator)
            return false;

        if (declarator.Initializer == null)
            return false;

        if (!IsEffectivelyConstant(declarator.Initializer.Value, model, enclosingScope))
            return false;

        return IsLocalNeverReassigned(local, enclosingScope, model);
    }

    private static bool IsLocalNeverReassigned(ILocalSymbol local, SyntaxNode scope, SemanticModel model)
    {
        foreach (var node in scope.DescendantNodes())
        {
            switch (node)
            {
                case AssignmentExpressionSyntax assignment:
                {
                    if (assignment.Parent is EqualsValueClauseSyntax)
                        continue;

                    var targetSymbol = GetAssignmentTargetSymbol(assignment.Left, model);
                    if (SymbolEqualityComparer.Default.Equals(targetSymbol, local))
                        return false;
                    break;
                }

                case ArgumentSyntax { RefKindKeyword.RawKind: not 0 } arg:
                {
                    if (arg.Expression is IdentifierNameSyntax id)
                    {
                        var sym = model.GetSymbolInfo(id).Symbol;
                        if (SymbolEqualityComparer.Default.Equals(sym, local))
                            return false;
                    }
                    break;
                }

                case PostfixUnaryExpressionSyntax postfix:
                {
                    if (postfix.Operand is IdentifierNameSyntax id)
                    {
                        var sym = model.GetSymbolInfo(id).Symbol;
                        if (SymbolEqualityComparer.Default.Equals(sym, local))
                            return false;
                    }
                    break;
                }

                case PrefixUnaryExpressionSyntax prefix
                    when prefix.IsKind(SyntaxKind.PreIncrementExpression) ||
                         prefix.IsKind(SyntaxKind.PreDecrementExpression):
                {
                    if (prefix.Operand is IdentifierNameSyntax id)
                    {
                        var sym = model.GetSymbolInfo(id).Symbol;
                        if (SymbolEqualityComparer.Default.Equals(sym, local))
                            return false;
                    }
                    break;
                }
            }
        }

        return true;
    }

    private static ISymbol GetAssignmentTargetSymbol(ExpressionSyntax left, SemanticModel model)
    {
        switch (left)
        {
            case IdentifierNameSyntax id:
                return model.GetSymbolInfo(id).Symbol;
            case TupleExpressionSyntax tuple:
                foreach (var arg in tuple.Arguments)
                {
                    var sym = GetAssignmentTargetSymbol(arg.Expression, model);
                    if (sym != null)
                        return sym;
                }
                return null;
            default:
                return model.GetSymbolInfo(left).Symbol;
        }
    }

    private static bool IsInStaticReadOnlyFieldInitializer(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is FieldDeclarationSyntax fieldDecl)
            {
                var hasStatic = fieldDecl.Modifiers.Any(SyntaxKind.StaticKeyword);
                var hasReadonly = fieldDecl.Modifiers.Any(SyntaxKind.ReadOnlyKeyword);
                return hasStatic && hasReadonly;
            }

            if (current is PropertyDeclarationSyntax)
                return false;

            if (current is BaseMethodDeclarationSyntax)
                return false;

            if (current is LocalFunctionStatementSyntax)
                return false;

            if (current is AnonymousFunctionExpressionSyntax)
                return false;

            current = current.Parent;
        }

        return false;
    }

    private static SyntaxNode GetEnclosingMethodBody(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            switch (current)
            {
                case MethodDeclarationSyntax method:
                    return (SyntaxNode)method.Body ?? method.ExpressionBody;
                case ConstructorDeclarationSyntax ctor:
                    return (SyntaxNode)ctor.Body ?? ctor.ExpressionBody;
                case AccessorDeclarationSyntax accessor:
                    return (SyntaxNode)accessor.Body ?? accessor.ExpressionBody;
                case LocalFunctionStatementSyntax localFunc:
                    return (SyntaxNode)localFunc.Body ?? localFunc.ExpressionBody;
                case SimpleLambdaExpressionSyntax lambda:
                    return lambda.Body;
                case ParenthesizedLambdaExpressionSyntax lambda:
                    return lambda.Body;
                case AnonymousMethodExpressionSyntax anon:
                    return anon.Body;
                case FieldDeclarationSyntax:
                case PropertyDeclarationSyntax propDecl when propDecl.Initializer != null:
                    return current;
                case GlobalStatementSyntax:
                case CompilationUnitSyntax:
                    return current;
            }

            current = current.Parent;
        }

        return null;
    }
}
