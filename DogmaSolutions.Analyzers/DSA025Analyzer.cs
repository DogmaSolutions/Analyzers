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
public sealed class DSA025Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA025";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA025AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA025AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA025AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA025");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly string[] LogMethodNames =
    {
        "LogTrace",
        "LogDebug",
        "LogInformation",
        "LogWarning",
        "LogError",
        "LogCritical",
        "Log",
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

        string methodName;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            methodName = memberAccess.Name.Identifier.ValueText;
        else if (invocation.Expression is IdentifierNameSyntax identifier)
            methodName = identifier.Identifier.ValueText;
        else
            return;

        if (Array.IndexOf(LogMethodNames, methodName) < 0)
            return;

        var methodSymbol = context.SemanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
        if (methodSymbol == null)
            return;

        if (!IsLoggerMethod(methodSymbol))
            return;

        var messageArg = FindMessageArgument(invocation, methodSymbol);
        if (messageArg == null)
            return;

        if (!(messageArg.Expression is InterpolatedStringExpressionSyntax interpolated))
            return;

        if (interpolated.Contents.All(c => c is InterpolatedStringTextSyntax))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: interpolated.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null,
            methodName);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsLoggerMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType == null)
            return false;

        if (IsILoggerType(containingType))
            return true;

        if (method.IsExtensionMethod && method.ReducedFrom != null)
            return IsLoggerExtensionMethod(method.ReducedFrom);

        return IsLoggerExtensionMethod(method);
    }

    private static bool IsLoggerExtensionMethod(IMethodSymbol method)
    {
        var containingType = method.ContainingType;
        if (containingType == null)
            return false;

        var ns = containingType.ContainingNamespace?.ToDisplayString();
        if (ns != "Microsoft.Extensions.Logging")
            return false;

        if (containingType.Name == "LoggerExtensions")
            return true;

        if (method.Parameters.Length > 0 && IsILoggerType(method.Parameters[0].Type))
            return true;

        return false;
    }

    private static bool IsILoggerType(ITypeSymbol type)
    {
        if (type.Name == "ILogger" &&
            type.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Logging")
            return true;

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "ILogger" &&
                iface.ContainingNamespace?.ToDisplayString() == "Microsoft.Extensions.Logging")
                return true;
        }

        return false;
    }

    internal static ArgumentSyntax FindMessageArgument(
        InvocationExpressionSyntax invocation,
        IMethodSymbol method)
    {
        var args = invocation.ArgumentList.Arguments;

        foreach (var arg in args)
        {
            if (arg.NameColon != null &&
                arg.NameColon.Name.Identifier.ValueText == "message")
                return arg;
        }

        for (var i = 0; i < args.Count && i < method.Parameters.Length; i++)
        {
            var param = method.Parameters[i];
            if (param.Name == "message" && param.Type.SpecialType == SpecialType.System_String)
                return args[i];
        }

        return null;
    }
}
