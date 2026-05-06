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
public sealed class DSA024Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA024";

    internal const string ExactNamesOptionKey = "dotnet_diagnostic.DSA024.exact_parameter_names";
    internal const string PrefixSuffixNamesOptionKey = "dotnet_diagnostic.DSA024.prefix_suffix_parameter_names";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA024AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA024AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA024AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.BestPractice;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA024");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] DefaultExactNames = { "path" };

    private static readonly string[] DefaultPrefixSuffixNames =
    {
        "filePath", "fileName", "directoryPath", "directoryName",
        "folderPath", "folderName", "fileFullPath", "directoryFullPath",
        "fileFullName", "directoryFullName", "xmlFile", "xmlFilePath",
        "xmlFileName", "jsonFile", "jsonFileName", "jsonFilePath",
    };

    private static readonly string[] DSA023StaticTypeNames = { "File", "Directory", "Path" };
    private static readonly string[] DSA023InstanceTypeNames = { "FileInfo", "DirectoryInfo", "StreamReader", "StreamWriter", "FileStream" };

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

        if (IsDSA023TargetType(methodSymbol.ContainingType))
            return;

        CheckArguments(context, invocation.ArgumentList, methodSymbol);
    }

    private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
    {
        var creation = (ObjectCreationExpressionSyntax)context.Node;
        var ctorSymbol = context.SemanticModel.GetSymbolInfo(creation).Symbol as IMethodSymbol;
        if (ctorSymbol == null)
            return;

        if (IsDSA023TargetType(ctorSymbol.ContainingType))
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

        if (IsDSA023TargetType(ctorSymbol.ContainingType))
            return;

        CheckArguments(context, creation.ArgumentList, ctorSymbol);
    }

    private static bool IsDSA023TargetType(INamedTypeSymbol type)
    {
        if (type == null)
            return false;

        var ns = type.ContainingNamespace?.ToDisplayString();
        if (ns != "System.IO")
            return false;

        return Array.IndexOf(DSA023StaticTypeNames, type.Name) >= 0 ||
               Array.IndexOf(DSA023InstanceTypeNames, type.Name) >= 0;
    }

    private static void CheckArguments(
        SyntaxNodeAnalysisContext context,
        ArgumentListSyntax argumentList,
        IMethodSymbol method)
    {
        if (argumentList == null)
            return;

        var exactNames = GetExactNames(context);
        var prefixSuffixNames = GetPrefixSuffixNames(context);

        for (var i = 0; i < argumentList.Arguments.Count; i++)
        {
            var arg = argumentList.Arguments[i];
            var param = GetParameterForArgument(method, arg, i);
            if (param == null)
                continue;

            if (!IsMatchingParameter(param.Name, exactNames, prefixSuffixNames))
                continue;

            var expr = UnwrapParentheses(arg.Expression);
            if (!(expr is BinaryExpressionSyntax binary) || !binary.IsKind(SyntaxKind.AddExpression))
                continue;

            var typeInfo = context.SemanticModel.GetTypeInfo(binary);
            if (typeInfo.Type?.SpecialType != SpecialType.System_String)
                continue;

            if (ContainsProtocolPrefix(binary))
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

    internal static bool IsMatchingParameter(string parameterName, string[] exactNames, string[] prefixSuffixNames)
    {
        foreach (var exact in exactNames)
        {
            if (string.Equals(parameterName, exact, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        foreach (var ps in prefixSuffixNames)
        {
            if (parameterName.StartsWith(ps, StringComparison.OrdinalIgnoreCase) ||
                parameterName.EndsWith(ps, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
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

    private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expr)
    {
        while (expr is ParenthesizedExpressionSyntax paren)
            expr = paren.Expression;
        return expr;
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

    internal static string[] GetExactNames(SyntaxNodeAnalysisContext context)
    {
        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (config.TryGetValue(ExactNamesOptionKey, out var configValue) &&
            !string.IsNullOrWhiteSpace(configValue))
        {
            return configValue.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }

        return DefaultExactNames;
    }

    internal static string[] GetPrefixSuffixNames(SyntaxNodeAnalysisContext context)
    {
        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (config.TryGetValue(PrefixSuffixNamesOptionKey, out var configValue) &&
            !string.IsNullOrWhiteSpace(configValue))
        {
            return configValue.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }

        return DefaultPrefixSuffixNames;
    }
}
