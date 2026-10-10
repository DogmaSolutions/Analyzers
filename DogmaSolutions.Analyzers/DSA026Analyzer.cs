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
public sealed class DSA026Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA026";
    public const string NearestNameProperty = "NearestName";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA026AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA026AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA026AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Bug;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA026.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeScope,
            SyntaxKind.ParenthesizedLambdaExpression,
            SyntaxKind.SimpleLambdaExpression,
            SyntaxKind.AnonymousMethodExpression,
            SyntaxKind.LocalFunctionStatement);
    }

    private static void AnalyzeScope(SyntaxNodeAnalysisContext context)
    {
        var ctParameter = FindCancellationTokenParameter(context.Node, context.SemanticModel);
        if (ctParameter == null)
            return;

        var body = GetBody(context.Node);
        if (body == null)
            return;

        var properties = ImmutableDictionary.CreateBuilder<string, string>();
        properties.Add(NearestNameProperty, ctParameter.Name);
        var immutableProperties = properties.ToImmutable();

        // A CancellationToken of an outer scope used instead of the one of this scope: ct2 ...
        foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (TryGetCapturedTokenSymbol(identifier, ctParameter, context, out var symbol))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _rule,
                    identifier.GetLocation(),
                    immutableProperties,
                    ctParameter.Name,
                    symbol.Name));
            }
        }

        // ... or the Token of a CancellationTokenSource of an outer scope: cts.Token
        foreach (var memberAccess in body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (IsCapturedTokenSourceToken(memberAccess, context))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    _rule,
                    memberAccess.GetLocation(),
                    immutableProperties,
                    ctParameter.Name,
                    memberAccess.ToString()));
            }
        }
    }

    /// <summary>
    /// An identifier naming a CancellationToken (parameter or local) declared outside the analyzed scope, other than
    /// the token parameter of the scope, and not used to create a linked token source.
    /// </summary>
    private static bool TryGetCapturedTokenSymbol(
        IdentifierNameSyntax identifier,
        IParameterSymbol ctParameter,
        SyntaxNodeAnalysisContext context,
        out ISymbol symbol)
    {
        symbol = null;

        if (identifier.Parent is NameColonSyntax)
            return false;

        if (IsInsideNestedScopeWithCancellationToken(identifier, context.Node, context.SemanticModel))
            return false;

        symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;

        ITypeSymbol type = symbol switch
        {
            IParameterSymbol p => p.Type,
            ILocalSymbol l => l.Type,
            _ => null
        };

        return type != null &&
               IsCancellationTokenType(type) &&
               !SymbolEqualityComparer.Default.Equals(symbol, ctParameter) &&
               IsDeclaredOutsideScope(symbol, context.Node) &&
               !IsInsideCreateLinkedTokenSource(identifier, context.SemanticModel);
    }

    /// <summary>
    /// <c>source.Token</c> where source is a CancellationTokenSource (local or parameter) declared outside the analyzed scope,
    /// not used to create a linked token source.
    /// </summary>
    private static bool IsCapturedTokenSourceToken(MemberAccessExpressionSyntax memberAccess, SyntaxNodeAnalysisContext context)
    {
        if (memberAccess.Name.Identifier.ValueText != "Token")
            return false;

        if (context.SemanticModel.GetSymbolInfo(memberAccess).Symbol is not IPropertySymbol propSymbol ||
            !IsCancellationTokenType(propSymbol.Type) ||
            !IsCancellationTokenSourceType(propSymbol.ContainingType))
            return false;

        var receiverSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
        return (receiverSymbol is ILocalSymbol || receiverSymbol is IParameterSymbol) &&
               IsDeclaredOutsideScope(receiverSymbol, context.Node) &&
               !IsInsideNestedScopeWithCancellationToken(memberAccess, context.Node, context.SemanticModel) &&
               !IsInsideCreateLinkedTokenSource(memberAccess, context.SemanticModel);
    }

    private static IParameterSymbol FindCancellationTokenParameter(SyntaxNode scope, SemanticModel model)
    {
        return scope switch
        {
            ParenthesizedLambdaExpressionSyntax lambda => FindCtParam(lambda.ParameterList.Parameters, model),
            SimpleLambdaExpressionSyntax simple => CheckSingleParam(simple.Parameter, model),
            AnonymousMethodExpressionSyntax anon when anon.ParameterList != null => FindCtParam(anon.ParameterList.Parameters, model),
            LocalFunctionStatementSyntax local => FindCtParam(local.ParameterList.Parameters, model),
            _ => null
        };
    }

    private static IParameterSymbol FindCtParam(SeparatedSyntaxList<ParameterSyntax> parameters, SemanticModel model)
    {
        foreach (var param in parameters)
        {
            if (model.GetDeclaredSymbol(param) is IParameterSymbol symbol && IsCancellationTokenType(symbol.Type))
                return symbol;
        }

        return null;
    }

    private static IParameterSymbol CheckSingleParam(ParameterSyntax parameter, SemanticModel model)
    {
        if (model.GetDeclaredSymbol(parameter) is IParameterSymbol symbol && IsCancellationTokenType(symbol.Type))
            return symbol;

        return null;
    }

    private static SyntaxNode GetBody(SyntaxNode scope)
    {
        return scope switch
        {
            ParenthesizedLambdaExpressionSyntax lambda => lambda.Body,
            SimpleLambdaExpressionSyntax simple => simple.Body,
            AnonymousMethodExpressionSyntax anon => anon.Body,
            LocalFunctionStatementSyntax local => (SyntaxNode)local.Body ?? local.ExpressionBody,
            _ => null
        };
    }

    internal static bool IsCancellationTokenType(ITypeSymbol type)
    {
        return type is
        {
            Name: "CancellationToken",
            ContainingNamespace:
            {
                Name: "Threading",
                ContainingNamespace:
                {
                    Name: "System",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };
    }

    private static bool IsCancellationTokenSourceType(ITypeSymbol type)
    {
        return type is
        {
            Name: "CancellationTokenSource",
            ContainingNamespace:
            {
                Name: "Threading",
                ContainingNamespace:
                {
                    Name: "System",
                    ContainingNamespace.IsGlobalNamespace: true
                }
            }
        };
    }

    private static bool IsDeclaredOutsideScope(ISymbol symbol, SyntaxNode scope)
    {
        foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
        {
            var declNode = syntaxRef.GetSyntax();
            if (scope.Span.Contains(declNode.Span))
                return false;
        }

        return true;
    }

    private static bool IsInsideNestedScopeWithCancellationToken(SyntaxNode node, SyntaxNode outerScope, SemanticModel model)
    {
        var current = node.Parent;
        while (current != null && current != outerScope)
        {
            if (SyntaxUtils.IsFunctionBoundary(current))
            {
                if (FindCancellationTokenParameter(current, model) != null)
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool IsInsideCreateLinkedTokenSource(SyntaxNode node, SemanticModel model)
    {
        foreach (var current in node.Ancestors())
        {
            if (current is InvocationExpressionSyntax invocation && IsCreateLinkedTokenSource(model.GetSymbolInfo(invocation).Symbol))
                return true;

            if (current is StatementSyntax or MemberDeclarationSyntax or
                LambdaExpressionSyntax or LocalFunctionStatementSyntax)
                break;
        }

        return false;
    }

    private static bool IsCreateLinkedTokenSource(ISymbol symbol)
    {
        return symbol is IMethodSymbol { Name: "CreateLinkedTokenSource", ContainingType: { Name: "CancellationTokenSource" } type } &&
               type.ContainingNamespace?.ToDisplayString() == "System.Threading";
    }
}
