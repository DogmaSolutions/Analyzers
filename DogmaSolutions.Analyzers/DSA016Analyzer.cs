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
/// Detects repeated invocations of the same LINQ/enumeration method on the same receiver
/// with the same arguments within the same scope. Each redundant call re-enumerates the
/// source, which is both wasteful and potentially non-deterministic if the source is a
/// deferred IEnumerable.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA016Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA016";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA016AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA016AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA016AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.CodeSmell;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA016");

    private static readonly HashSet<string> TrackedMethods = new HashSet<string>(StringComparer.Ordinal)
    {
        // Element access
        "First", "FirstOrDefault", "Single", "SingleOrDefault",
        "Last", "LastOrDefault", "ElementAt", "ElementAtOrDefault", "Find",
        // Boolean
        "Any", "All", "Contains", "Exists",
        // Counting
        "Count", "LongCount",
        // Aggregation
        "Min", "Max", "Sum", "Average", "Aggregate",
        // Async variants
        "FirstAsync", "FirstOrDefaultAsync", "SingleAsync", "SingleOrDefaultAsync",
        "LastAsync", "LastOrDefaultAsync",
        "AnyAsync", "AllAsync", "ContainsAsync", "ExistsAsync",
        "CountAsync", "LongCountAsync",
        "MinAsync", "MaxAsync", "SumAsync", "AverageAsync",
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

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

        if (!TryGetInvocationParts(invocation, out var receiverText, out var methodName, out var argsText))
            return;

        if (!TrackedMethods.Contains(methodName))
            return;

        var key = BuildKey(receiverText, methodName, argsText);

        var scope = GetContainingScope(invocation);
        if (scope == null)
            return;

        // Check if any other invocation in the same scope has the same key
        foreach (var sibling in GetInvocationsInScope(scope))
        {
            if (ReferenceEquals(sibling, invocation))
                continue;

            if (!TryGetInvocationParts(sibling, out var sibReceiver, out var sibMethod, out var sibArgs))
                continue;

            if (!TrackedMethods.Contains(sibMethod))
                continue;

            if (BuildKey(sibReceiver, sibMethod, sibArgs) == key)
            {
                var diagnostic = Diagnostic.Create(
                    descriptor: _rule,
                    location: invocation.GetLocation(),
                    effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                    additionalLocations: null,
                    properties: null,
                    methodName);
                context.ReportDiagnostic(diagnostic);
                return;
            }
        }
    }

    private static bool TryGetInvocationParts(
        InvocationExpressionSyntax invocation,
        out string receiverText,
        out string methodName,
        out string argumentsText)
    {
        receiverText = null;
        methodName = null;
        argumentsText = null;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            receiverText = memberAccess.Expression.ToString();
            methodName = memberAccess.Name.Identifier.ValueText;
            argumentsText = invocation.ArgumentList.ToString();
            return true;
        }

        if (invocation.Expression is MemberBindingExpressionSyntax memberBinding)
        {
            methodName = memberBinding.Name.Identifier.ValueText;
            argumentsText = invocation.ArgumentList.ToString();

            // Walk up to the ConditionalAccessExpression to get the receiver
            var ancestor = invocation.Parent;
            while (ancestor != null)
            {
                if (ancestor is ConditionalAccessExpressionSyntax ca)
                {
                    receiverText = ca.Expression.ToString();
                    return true;
                }

                ancestor = ancestor.Parent;
            }

            return false;
        }

        return false;
    }

    private static string BuildKey(string receiverText, string methodName, string argsText)
    {
        return $"{NormalizeWhitespace(receiverText)}|{methodName}|{NormalizeWhitespace(argsText)}";
    }

    /// <summary>
    /// Finds the innermost enclosing scope (method body, lambda body, local function body,
    /// constructor body, accessor body, or compilation unit for top-level statements).
    /// </summary>
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

    /// <summary>
    /// Returns all invocation expressions within the given scope, but does NOT descend
    /// into nested lambdas or local functions (those are separate scopes).
    /// </summary>
    private static IEnumerable<InvocationExpressionSyntax> GetInvocationsInScope(SyntaxNode scope)
    {
        return scope.DescendantNodes(n => !IsNestedScope(n))
            .OfType<InvocationExpressionSyntax>();
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
