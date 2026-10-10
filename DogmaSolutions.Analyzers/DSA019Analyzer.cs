using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
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
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA019.md");

    private static readonly AnalyzerOptionsCache<ParsedConfig> _configCache = new(static o => new ParsedConfig(o));

    private sealed class ParsedConfig
    {
        public readonly int Threshold;
        public readonly string[] ExcludedPrefixes;
        public readonly HashSet<string> IgnoredIntermediateMembers;

        public ParsedConfig(AnalyzerConfigOptions config)
        {
            Threshold = AnalyzerOptionsReader.ReadInt(config, MaxDepthOptionKey, DefaultMaxDepth, minInclusive: 1);
            ExcludedPrefixes = AnalyzerOptionsReader.ReadList(config, ExcludedPrefixesOptionKey, DefaultExcludedPrefixes);

            var ignoredMembers = AnalyzerOptionsReader.ReadList(config, IgnoredIntermediateMembersOptionKey, defaultValue: null);
            IgnoredIntermediateMembers = ignoredMembers == null
                ? DefaultIgnoredIntermediateMembers
                : new HashSet<string>(ignoredMembers, StringComparer.Ordinal);
        }
    }

    private static ParsedConfig GetParsedConfig(SyntaxNodeAnalysisContext context)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
        return _configCache.Get(options);
    }

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

        var key = SyntaxUtils.NormalizeWhitespace(memberAccess.ToString());

        // Check if the chain starts with an excluded prefix
        if (MatchesExcludedPrefix(key, context))
            return;

        var scope = SyntaxUtils.GetContainingScope(memberAccess);
        if (scope == null)
            return;

        // Nothing to report unless the same text appears more than once in the scope: decide it before any semantic work
        if (!HasTextualDuplicateInScope<MemberAccessExpressionSyntax>(key, scope, threshold))
            return;

        // Semantic effective depth: excludes namespace qualifications from the count
        var effectiveDepth = ComputeEffectiveChainDepth(memberAccess, context.SemanticModel, ignoredMembers);
        if (effectiveDepth < threshold)
            return;

        if (IsInsideExpressionTreeLambda(memberAccess, context.SemanticModel))
            return;

        var count = 1 + CountSameAccessesInScope(memberAccess, key, scope, threshold, ignoredMembers, context.SemanticModel); // 1 = self
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

        if (ComputeChainDepth(elementAccess) < threshold)
            return;

        var key = SyntaxUtils.NormalizeWhitespace(elementAccess.ToString());

        // Check if the chain starts with an excluded prefix
        if (MatchesExcludedPrefix(key, context))
            return;

        var scope = SyntaxUtils.GetContainingScope(elementAccess);
        if (scope == null)
            return;

        // Nothing to report unless the same text appears more than once in the scope: decide it before any semantic work
        if (!HasTextualDuplicateInScope<ElementAccessExpressionSyntax>(key, scope, threshold))
            return;

        if (ComputeEffectiveChainDepth(elementAccess, context.SemanticModel, ignoredMembers) < threshold)
            return;

        if (IsInsideExpressionTreeLambda(elementAccess, context.SemanticModel))
            return;

        var count = 1 + CountSameAccessesInScope(elementAccess, key, scope, threshold, ignoredMembers, context.SemanticModel); // 1 = self
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

    /// <summary>
    /// How many other deep-enough accesses of the scope are the same chain as <paramref name="access"/>. The accesses of a scope
    /// are indexed once by their normalized text, so that each access only meets the ones that can possibly be equal to it.
    /// </summary>
    private static int CountSameAccessesInScope<T>(
        T access,
        string key,
        SyntaxNode scope,
        int threshold,
        HashSet<string> ignoredMembers,
        SemanticModel semanticModel)
        where T : ExpressionSyntax
    {
        if (!GetScopeIndex<T>(scope, threshold).ByKey.TryGetValue(key, out var candidates))
            return 0;

        var count = 0;
        foreach (var sibling in candidates)
        {
            if (!ReferenceEquals(sibling, access) &&
                ComputeEffectiveChainDepth(sibling, semanticModel, ignoredMembers) >= threshold &&
                AreSemanticallySame(access, sibling, semanticModel))
                count++;
        }

        return count;
    }

    private static bool HasTextualDuplicateInScope<T>(string key, SyntaxNode scope, int threshold)
        where T : ExpressionSyntax
    {
        return GetScopeIndex<T>(scope, threshold).ByKey.TryGetValue(key, out var candidates) && candidates.Count > 1;
    }

    /// <summary>
    /// The accesses of a scope (outside nested scopes) whose syntactic chain is deep enough and that are not inside a nameof,
    /// grouped by normalized text. Built once per scope and threshold.
    /// </summary>
    private sealed class ScopeIndex<T>
        where T : ExpressionSyntax
    {
        internal ScopeIndex(int threshold, Dictionary<string, List<T>> byKey)
        {
            Threshold = threshold;
            ByKey = byKey;
        }

        internal int Threshold { get; }

        internal Dictionary<string, List<T>> ByKey { get; }
    }

    private static readonly ConditionalWeakTable<SyntaxNode, object> _memberAccessIndexes = new();
    private static readonly ConditionalWeakTable<SyntaxNode, object> _elementAccessIndexes = new();

    private static ScopeIndex<T> GetScopeIndex<T>(SyntaxNode scope, int threshold)
        where T : ExpressionSyntax
    {
        var cache = typeof(T) == typeof(MemberAccessExpressionSyntax) ? _memberAccessIndexes : _elementAccessIndexes;
        var index = (ScopeIndex<T>)cache.GetValue(scope, s => BuildScopeIndex<T>(s, threshold));
        return index.Threshold == threshold ? index : BuildScopeIndex<T>(scope, threshold);
    }

    private static ScopeIndex<T> BuildScopeIndex<T>(SyntaxNode scope, int threshold)
        where T : ExpressionSyntax
    {
        var byKey = new Dictionary<string, List<T>>(StringComparer.Ordinal);
        foreach (var node in scope.DescendantNodes(n => !SyntaxUtils.IsNestedScope(n)).OfType<T>())
        {
            if (IsInsideNameof(node) || ComputeChainDepth(node) < threshold)
                continue;

            var key = SyntaxUtils.NormalizeWhitespace(node.ToString());
            if (!byKey.TryGetValue(key, out var list))
                byKey[key] = list = new List<T>();

            list.Add(node);
        }

        return new ScopeIndex<T>(threshold, byKey);
    }

    private static int GetThreshold(SyntaxNodeAnalysisContext context)
    {
        return GetParsedConfig(context).Threshold;
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
        return GetParsedConfig(context).ExcludedPrefixes;
    }

    private static readonly string[] DefaultExcludedPrefixes =
    {
        "Is",
        "Has",
        "Does",
        "Contains",
        "Throws",
        "Assert.That",
    };

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
        return GetParsedConfig(context).IgnoredIntermediateMembers;
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

    internal static IEnumerable<ElementAccessExpressionSyntax> GetElementAccessesInScope(SyntaxNode scope)
    {
        return scope.DescendantNodes(n => !SyntaxUtils.IsNestedScope(n))
            .OfType<ElementAccessExpressionSyntax>();
    }
}
