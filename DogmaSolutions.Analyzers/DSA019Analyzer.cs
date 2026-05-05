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
    internal const string ExcludedPrefixesOptionKey = "dotnet_diagnostic.DSA019.excluded_prefixes";
    internal const string IgnoredIntermediateMembersOptionKey = "dotnet_diagnostic.DSA019.ignored_intermediate_members";

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
        context.RegisterSyntaxNodeAction(AnalyzeElementAccess, SyntaxKind.ElementAccessExpression);
    }

    private static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext context)
    {
        var memberAccess = (MemberAccessExpressionSyntax)context.Node;

        if (IsInsideNameof(memberAccess))
            return;

        var threshold = GetThreshold(context);
        var ignoredMembers = GetIgnoredIntermediateMembers(context);

        // Quick syntactic pre-filter (cheaper than semantic analysis)
        var syntacticDepth = ComputeChainDepth(memberAccess);
        if (syntacticDepth < threshold)
            return;

        // Semantic effective depth: excludes namespace qualifications from the count
        var effectiveDepth = ComputeEffectiveChainDepth(memberAccess, context.SemanticModel, ignoredMembers);
        if (effectiveDepth < threshold)
            return;

        if (IsInsideExpressionTreeLambda(memberAccess, context.SemanticModel))
            return;

        var key = NormalizeWhitespace(memberAccess.ToString());

        // Check if the chain starts with an excluded prefix
        if (MatchesExcludedPrefix(key, context))
            return;

        var scope = GetContainingScope(memberAccess);
        if (scope == null)
            return;

        var count = 1; // count self
        foreach (var sibling in GetMemberAccessesInScope(scope))
        {
            if (ReferenceEquals(sibling, memberAccess))
                continue;

            if (IsInsideNameof(sibling))
                continue;

            var sibSyntacticDepth = ComputeChainDepth(sibling);
            if (sibSyntacticDepth < threshold)
                continue;

            var sibEffectiveDepth = ComputeEffectiveChainDepth(sibling, context.SemanticModel, ignoredMembers);
            if (sibEffectiveDepth < threshold)
                continue;

            var sibKey = NormalizeWhitespace(sibling.ToString());
            if (sibKey == key && AreSemanticallySame(memberAccess, sibling, context.SemanticModel))
                count++;
        }

        if (count > 1)
        {
            var diagnostic = Diagnostic.Create(
                descriptor: _rule,
                location: memberAccess.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                memberAccess.ToString(),
                count);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeElementAccess(SyntaxNodeAnalysisContext context)
    {
        var elementAccess = (ElementAccessExpressionSyntax)context.Node;

        if (IsInsideNameof(elementAccess))
            return;

        var threshold = GetThreshold(context);
        var ignoredMembers = GetIgnoredIntermediateMembers(context);

        var syntacticDepth = ComputeChainDepth(elementAccess);
        if (syntacticDepth < threshold)
            return;

        var effectiveDepth = ComputeEffectiveChainDepth(elementAccess, context.SemanticModel, ignoredMembers);
        if (effectiveDepth < threshold)
            return;

        if (IsInsideExpressionTreeLambda(elementAccess, context.SemanticModel))
            return;

        var key = NormalizeWhitespace(elementAccess.ToString());

        // Check if the chain starts with an excluded prefix
        if (MatchesExcludedPrefix(key, context))
            return;

        var scope = GetContainingScope(elementAccess);
        if (scope == null)
            return;

        var count = 1; // count self
        foreach (var sibling in GetElementAccessesInScope(scope))
        {
            if (ReferenceEquals(sibling, elementAccess))
                continue;

            if (IsInsideNameof(sibling))
                continue;

            var sibSyntacticDepth = ComputeChainDepth(sibling);
            if (sibSyntacticDepth < threshold)
                continue;

            var sibEffectiveDepth = ComputeEffectiveChainDepth(sibling, context.SemanticModel, ignoredMembers);
            if (sibEffectiveDepth < threshold)
                continue;

            var sibKey = NormalizeWhitespace(sibling.ToString());
            if (sibKey == key && AreSemanticallySame(elementAccess, sibling, context.SemanticModel))
                count++;
        }

        if (count > 1)
        {
            var diagnostic = Diagnostic.Create(
                descriptor: _rule,
                location: elementAccess.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                elementAccess.ToString(),
                count);
            context.ReportDiagnostic(diagnostic);
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

    /// <summary>
    /// Computes the effective chain depth using the semantic model to exclude compile-time
    /// qualifications. Namespace navigations, nested type accesses, and constant field
    /// accesses do not count as runtime dereferences. Static and instance member accesses
    /// (fields, properties, methods) do count.
    /// </summary>
    internal static int ComputeEffectiveChainDepth(ExpressionSyntax expression, SemanticModel semanticModel, HashSet<string> ignoredIntermediateMembers = null)
    {
        var depth = 0;
        ExpressionSyntax current = expression;
        while (true)
        {
            if (current is MemberAccessExpressionSyntax ma)
            {
                // If the receiver is a namespace, everything below is namespace navigation — stop
                var receiverSymbol = semanticModel.GetSymbolInfo(ma.Expression).Symbol;
                if (receiverSymbol is INamespaceSymbol)
                    break;

                // Check what THIS access resolves to
                var accessSymbol = semanticModel.GetSymbolInfo(ma).Symbol;

                // Nested type resolution (e.g., Outer.Inner.Nested) — compile-time, not a dereference
                if (accessSymbol is INamespaceSymbol || accessSymbol is INamedTypeSymbol)
                {
                    current = ma.Expression;
                    continue;
                }

                // Constant fields and enum members — compile-time inlined, not a dereference
                if (accessSymbol is IFieldSymbol field && field.IsConst)
                {
                    current = ma.Expression;
                    continue;
                }

                if (ignoredIntermediateMembers != null &&
                    ignoredIntermediateMembers.Contains(ma.Name.Identifier.ValueText))
                {
                    current = ma.Expression;
                    continue;
                }

                depth++;
                current = ma.Expression;
            }
            else if (current is ElementAccessExpressionSyntax ea)
            {
                depth++;
                current = ea.Expression;
            }
            else if (current is InvocationExpressionSyntax inv)
            {
                current = inv.Expression;
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

    private static bool MatchesExcludedPrefix(string normalizedKey, SyntaxNodeAnalysisContext context)
    {
        var prefixes = GetExcludedPrefixes(context);
        if (prefixes.Length == 0)
            return false;

        foreach (var prefix in prefixes)
        {
            if (normalizedKey.StartsWith(prefix, System.StringComparison.Ordinal) &&
                normalizedKey.Length > prefix.Length &&
                (normalizedKey[prefix.Length] == '.' || normalizedKey[prefix.Length] == '['))
                return true;
        }

        return false;
    }

    private static string[] GetExcludedPrefixes(SyntaxNodeAnalysisContext context)
    {
        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (config.TryGetValue(ExcludedPrefixesOptionKey, out var configValue) &&
            !string.IsNullOrWhiteSpace(configValue))
        {
            return configValue.Split(',')
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .ToArray();
        }

        return System.Array.Empty<string>();
    }

    private static readonly HashSet<string> DefaultIgnoredIntermediateMembers = new HashSet<string>(StringComparer.Ordinal)
    {
        "TagWithCallSite",
        "TagWith",
        "AsNoTracking",
        "AsNoTrackingWithIdentityResolution",
        "AsTracking",
        "AsSplitQuery",
        "AsSingleQuery",
        "IgnoreAutoIncludes",
        "IgnoreQueryFilters",
        "ConfigureAwait",
    };

    private static HashSet<string> GetIgnoredIntermediateMembers(SyntaxNodeAnalysisContext context)
    {
        var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        if (config.TryGetValue(IgnoredIntermediateMembersOptionKey, out var configValue) &&
            !string.IsNullOrWhiteSpace(configValue))
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in configValue.Split(','))
            {
                var trimmed = name.Trim();
                if (trimmed.Length > 0)
                    set.Add(trimmed);
            }

            return set;
        }

        return DefaultIgnoredIntermediateMembers;
    }

    /// <summary>
    /// Verifies that two expressions with identical text are semantically equivalent by
    /// checking that all identifiers resolve to the same symbols. This prevents false
    /// positives when same-named variables from different scopes (e.g., foreach loop
    /// variables) make expressions look identical textually but reference different data.
    /// </summary>
    internal static bool AreSemanticallySame(ExpressionSyntax expr1, ExpressionSyntax expr2, SemanticModel semanticModel)
    {
        var ids1 = expr1.DescendantNodes().OfType<IdentifierNameSyntax>().ToArray();
        var ids2 = expr2.DescendantNodes().OfType<IdentifierNameSyntax>().ToArray();

        if (ids1.Length != ids2.Length)
            return false;

        for (var i = 0; i < ids1.Length; i++)
        {
            var sym1 = semanticModel.GetSymbolInfo(ids1[i]).Symbol;
            var sym2 = semanticModel.GetSymbolInfo(ids2[i]).Symbol;

            // If either can't be resolved, skip (e.g., lambda parameters)
            if (sym1 == null || sym2 == null)
                continue;

            if (!SymbolEqualityComparer.Default.Equals(sym1, sym2))
                return false;
        }

        return true;
    }

    private static bool IsInsideExpressionTreeLambda(SyntaxNode node, SemanticModel semanticModel)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is LambdaExpressionSyntax &&
                current.Parent is ArgumentSyntax &&
                current.Parent.Parent is ArgumentListSyntax &&
                current.Parent.Parent.Parent is InvocationExpressionSyntax outerInvocation &&
                outerInvocation.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var receiverType = semanticModel.GetTypeInfo(memberAccess.Expression).Type;
                if (receiverType != null && ImplementsIQueryable(receiverType))
                    return true;
            }

            current = current.Parent;
        }

        return false;
    }

    private static bool ImplementsIQueryable(ITypeSymbol type)
    {
        if (type.Name == "IQueryable" && type.ContainingNamespace?.ToDisplayString() == "System.Linq")
            return true;

        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "IQueryable" && iface.ContainingNamespace?.ToDisplayString() == "System.Linq")
                return true;
        }

        return false;
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

    internal static IEnumerable<ElementAccessExpressionSyntax> GetElementAccessesInScope(SyntaxNode scope)
    {
        return scope.DescendantNodes(n => !IsNestedScope(n))
            .OfType<ElementAccessExpressionSyntax>();
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
