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
public sealed class DSA023Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA023";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA023AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA023AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA023AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.BestPractice;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA023");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] TargetStaticTypeNames = { "File", "Directory", "Path" };

    private static readonly string[] TargetInstanceTypeNames =
    {
        "FileInfo", "DirectoryInfo",
        "StreamReader", "StreamWriter", "FileStream",
    };

    private static readonly string[] ExcludedPathMethods = { "Combine", "Join", "ChangeExtension" };

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeObjectCreation, SyntaxKind.ObjectCreationExpression);
        context.RegisterSyntaxNodeAction(AnalyzeImplicitObjectCreation, SyntaxKind.ImplicitObjectCreationExpression);
    }

    private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
    {
        var invocation = (InvocationExpressionSyntax)context.Node;
        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null)
            return;

        if (!IsTargetType(methodSymbol.ContainingType))
            return;

        if (methodSymbol.ContainingType.Name == "Path" &&
            Array.IndexOf(ExcludedPathMethods, methodSymbol.Name) >= 0)
            return;

        CheckArguments(context, invocation.ArgumentList, methodSymbol);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var ctorSymbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        if (ctorSymbol == null)
            return;

        if (!IsTargetConstructorType(ctorSymbol.ContainingType))
            return;

        if (creation.ArgumentList == null)
            return;

        CheckArguments(context, creation.ArgumentList, ctorSymbol);
    }

    private static void AnalyzeImplicitObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ImplicitObjectCreationExpressionSyntax)context.Node;
        var ctorSymbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        if (ctorSymbol == null)
            return;

        if (!IsTargetConstructorType(ctorSymbol.ContainingType))
            return;

        CheckArguments(context, creation.ArgumentList, ctorSymbol);
    }

    private static bool IsTargetType(INamedTypeSymbol type)
    {
        if (type == null)
            return false;

        var ns = type.ContainingNamespace?.ToDisplayString();
        if (ns != "System.IO")
            return false;

        return Array.IndexOf(TargetStaticTypeNames, type.Name) >= 0 ||
               Array.IndexOf(TargetInstanceTypeNames, type.Name) >= 0;
    }

    private static bool IsTargetConstructorType(INamedTypeSymbol type)
    {
        if (type == null)
            return false;

        var ns = type.ContainingNamespace?.ToDisplayString();
        if (ns != "System.IO")
            return false;

        return Array.IndexOf(TargetInstanceTypeNames, type.Name) >= 0;
    }

    private static void CheckArguments(
        SyntaxNodeAnalysisContext context,
        ArgumentListSyntax argumentList,
        IMethodSymbol method)
    {
        if (argumentList == null)
            return;

        for (var i = 0; i < argumentList.Arguments.Count; i++)
        {
            var arg = argumentList.Arguments[i];
            var param = GetParameterForArgument(method, arg, i);
            if (param == null)
                continue;

            if (!IsPathParameter(param))
                continue;

            var expr = UnwrapParentheses(arg.Expression);
            if (!(expr is BinaryExpressionSyntax binary) || !binary.IsKind(SyntaxKind.AddExpression))
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(binary);
            if (typeInfo.Type?.SpecialType != SpecialType.System_String)
                continue;

            if (ContainsProtocolPrefix(binary))
                continue;

            if (IsExtensionAppending(binary))
                continue;

            var methodDisplay = method.MethodKind == MethodKind.Constructor
                ? "new " + method.ContainingType.Name
                : method.ContainingType.Name + "." + method.Name;

            var diagnostic = Diagnostic.Create(
                descriptor: _rule,
                location: binary.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                methodDisplay);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static IParameterSymbol GetParameterForArgument(
        IMethodSymbol method,
        ArgumentSyntax argument,
        int ordinalIndex)
    {
        if (argument.NameColon != null)
        {
            var name = argument.NameColon.Name.Identifier.ValueText;
            return method.Parameters.FirstOrDefault(p => p.Name == name);
        }

        if (ordinalIndex >= 0 && ordinalIndex < method.Parameters.Length)
            return method.Parameters[ordinalIndex];

        return null;
    }

    private static bool IsPathParameter(IParameterSymbol parameter)
    {
        var name = parameter.Name;
        return name.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("file", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("dir", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("folder", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expr)
    {
        while (expr is ParenthesizedExpressionSyntax paren)
            expr = paren.Expression;
        return expr;
    }

    private static bool IsExtensionAppending(BinaryExpressionSyntax binary)
    {
        var right = binary.Right;
        while (right is ParenthesizedExpressionSyntax paren)
            right = paren.Expression;

        if (!(right is LiteralExpressionSyntax rightLiteral) ||
            !rightLiteral.IsKind(SyntaxKind.StringLiteralExpression))
            return false;

        var rightValue = rightLiteral.Token.ValueText;
        if (!rightValue.StartsWith(".", StringComparison.Ordinal) ||
            rightValue.IndexOf('\\') >= 0 ||
            rightValue.IndexOf('/') >= 0)
            return false;

        foreach (var node in binary.Left.DescendantNodesAndSelf())
        {
            if (node is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                var value = literal.Token.ValueText;
                if (value.IndexOf('\\') >= 0 || value.IndexOf('/') >= 0)
                    return false;
            }
        }

        return true;
    }

    private static bool ContainsProtocolPrefix(BinaryExpressionSyntax binary)
    {
        foreach (var node in binary.DescendantNodesAndSelf())
        {
            if (node is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression) &&
                literal.Token.ValueText.Contains("://"))
            {
                return true;
            }
        }

        return false;
    }
}
