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
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA026");

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

        foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (identifier.Parent is NameColonSyntax)
                continue;

            if (IsInsideNestedScopeWithCancellationToken(identifier, context.Node, context.SemanticModel))
                continue;

            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;
            if (symbol == null)
                continue;

            ITypeSymbol type = symbol switch
            {
                IParameterSymbol p => p.Type,
                ILocalSymbol l => l.Type,
                _ => null
            };

            if (type == null || !IsCancellationTokenType(type))
                continue;

            if (SymbolEqualityComparer.Default.Equals(symbol, ctParameter))
                continue;

            if (!IsDeclaredOutsideScope(symbol, context.Node))
                continue;

            if (IsInsideCreateLinkedTokenSource(identifier, context.SemanticModel))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                _rule,
                identifier.GetLocation(),
                immutableProperties,
                ctParameter.Name,
                symbol.Name));
        }

        foreach (var memberAccess in body.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
        {
            if (memberAccess.Name.Identifier.ValueText != "Token")
                continue;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberAccess).Symbol;
            if (memberSymbol is not IPropertySymbol propSymbol || !IsCancellationTokenType(propSymbol.Type))
                continue;

            if (!IsCancellationTokenSourceType(propSymbol.ContainingType))
                continue;

            var receiverSymbol = context.SemanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            if (receiverSymbol is not ILocalSymbol && receiverSymbol is not IParameterSymbol)
                continue;

            if (!IsDeclaredOutsideScope(receiverSymbol, context.Node))
                continue;

            if (IsInsideNestedScopeWithCancellationToken(memberAccess, context.Node, context.SemanticModel))
                continue;

            if (IsInsideCreateLinkedTokenSource(memberAccess, context.SemanticModel))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(
                _rule,
                memberAccess.GetLocation(),
                immutableProperties,
                ctParameter.Name,
                memberAccess.ToString()));
        }
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
            if (current is ParenthesizedLambdaExpressionSyntax or
                SimpleLambdaExpressionSyntax or
                AnonymousMethodExpressionSyntax or
                LocalFunctionStatementSyntax)
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
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (current is InvocationExpressionSyntax invocation)
            {
                var symbol = model.GetSymbolInfo(invocation).Symbol;
                if (symbol is IMethodSymbol
                    {
                        Name: "CreateLinkedTokenSource",
                        ContainingType:
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
                        }
                    })
                {
                    return true;
                }
            }

            if (current is StatementSyntax or MemberDeclarationSyntax or
                LambdaExpressionSyntax or LocalFunctionStatementSyntax)
                break;
        }

        return false;
    }
}
