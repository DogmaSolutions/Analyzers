using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Detects repeated deeply nested member access chains in the same scope.
/// When the same chain of property accesses, indexers, or method calls is repeated
/// multiple times, it should be extracted into a local variable to improve readability
/// and avoid repeated dereferencing.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA019Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA019";

    internal const int DefaultMaxDepth = 3;
    internal const string MaxDepthOptionKey = "dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA019AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA019AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA019AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.CodeSmell;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA019");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (IsInsideNameof(memberAccess))
            return;

        var threshold = GetThreshold(context);
        var depth = ComputeChainDepth(memberAccess);
        if (depth < threshold)
            return;

        var key = NormalizeWhitespace(memberAccess.ToString());

        var scope = GetContainingScope(memberAccess);
        if (scope == null)
            return;

        foreach (var sibling in GetMemberAccessesInScope(scope))
        {
            if (ReferenceEquals(sibling, memberAccess))
                continue;

            if (IsInsideNameof(sibling))
                continue;

            var sibDepth = ComputeChainDepth(sibling);
            if (sibDepth < threshold)
                continue;

            var sibKey = NormalizeWhitespace(sibling.ToString());
            if (sibKey == key)
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: _rule,
                    location: memberAccess.GetLocation(),
                    effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                    additionalLocations: null,
                    properties: null,
                    memberAccess.ToString());
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }

    private static int GetThreshold(SyntaxNodeAnalysisContext context)
    {
        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (config.TryGetValue(MaxDepthOptionKey, out var configValue) &&
            int.TryParse(configValue, out var threshold) &&
            threshold > 0)
        {
            return threshold;
        }

        return DefaultMaxDepth;
    }

    /// <summary>
    /// Counts the chain depth by walking down through member accesses, element accesses,
    /// invocations, and parenthesized expressions. Each MemberAccess and ElementAccess
    /// adds one level; InvocationExpressions are traversed transparently.
    /// </summary>
    internal static int ComputeChainDepth(ExpressionSyntax expression)
    {
        var depth = 0;
        var current = expression;
        while (true)
        {
            if (current is MemberAccessExpressionSyntax memberAccess)
            {
                depth++;
                current = memberAccess.Expression;
            }
            else if (current is ElementAccessExpressionSyntax elementAccess)
            {
                depth++;
                current = elementAccess.Expression;
            }
            else if (current is InvocationExpressionSyntax invocation)
            {
                // Method calls are traversed without adding depth;
                // the dereference is in the inner MemberAccessExpression
                current = invocation.Expression;
            }
            else if (current is ParenthesizedExpressionSyntax paren)
            {
                current = paren.Expression;
            }
            else if (current is AwaitExpressionSyntax awaitExpr)
            {
                current = awaitExpr.Expression;
            }
            else if (current is CastExpressionSyntax castExpr)
            {
                current = castExpr.Expression;
            }
            else
            {
                break;
            }
        }

        return depth;
    }

    private static bool IsInsideNameof(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is InvocationExpressionSyntax invocation &&
                invocation.Expression is IdentifierNameSyntax identifier &&
                identifier.Identifier.ValueText == "nameof")
                return true;
            current = current.Parent;
        }

        return false;
    }

    private static SyntaxNode GetContainingScope(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is SimpleLambdaExpressionSyntax simpleLambda)
                return simpleLambda.Body;
            if (current is ParenthesizedLambdaExpressionSyntax parenLambda)
                return parenLambda.Body;
            if (current is AnonymousMethodExpressionSyntax anonMethod)
                return anonMethod.Body;
            if (current is LocalFunctionStatementSyntax localFunc)
                return (SyntaxNode)localFunc.Body ?? localFunc.ExpressionBody?.Expression;
            if (current is MethodDeclarationSyntax method)
                return (SyntaxNode)method.Body ?? method.ExpressionBody?.Expression;
            if (current is ConstructorDeclarationSyntax ctor)
                return (SyntaxNode)ctor.Body ?? ctor.ExpressionBody?.Expression;
            if (current is AccessorDeclarationSyntax accessor)
                return (SyntaxNode)accessor.Body ?? accessor.ExpressionBody?.Expression;
            if (current is CompilationUnitSyntax compilationUnit)
                return compilationUnit;

            current = current.Parent;
        }

        return null;
    }

    private static IEnumerable<MemberAccessExpressionSyntax> GetMemberAccessesInScope(SyntaxNode scope)
    {
        return scope.DescendantNodes(n => !IsNestedScope(n))
            .OfType<MemberAccessExpressionSyntax>();
    }

    private static bool IsNestedScope(SyntaxNode node)
    {
        return node is SimpleLambdaExpressionSyntax ||
               node is ParenthesizedLambdaExpressionSyntax ||
               node is AnonymousMethodExpressionSyntax ||
               node is LocalFunctionStatementSyntax;
    }

    private static string NormalizeWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
        }

        return sb.ToString().Trim();
    }
}
