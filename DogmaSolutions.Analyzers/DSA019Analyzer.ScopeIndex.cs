using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

// The index of the member and element accesses of a scope: it makes the search of repeated chains linear in the size of the scope.
public sealed partial class DSA019Analyzer
{
    /// <summary>
    /// How many other deep-enough accesses of the scope are the same chain as <paramref name="access"/>. The accesses of a scope
    /// are indexed once by their normalized text, and the semantic comparison of each group of equal texts is done once for the
    /// whole group, so that the cost grows linearly (not quadratically) with the number of repetitions.
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
        return GetScopeIndex<T>(scope, threshold).ByKey.TryGetValue(key, out var group)
            ? group.CountOthersSameAs(access, semanticModel, ignoredMembers, threshold)
            : 0;
    }

    private static bool HasTextualDuplicateInScope<T>(string key, SyntaxNode scope, int threshold)
        where T : ExpressionSyntax
    {
        return GetScopeIndex<T>(scope, threshold).ByKey.TryGetValue(key, out var group) && group.Candidates.Count > 1;
    }

    /// <summary>
    /// The accesses of a scope (outside nested scopes) whose syntactic chain is deep enough and that are not inside a nameof,
    /// grouped by normalized text. Built once per scope and threshold.
    /// </summary>
    private sealed class ScopeIndex<T>
        where T : ExpressionSyntax
    {
        internal ScopeIndex(int threshold, Dictionary<string, AccessGroup<T>> byKey)
        {
            Threshold = threshold;
            ByKey = byKey;
        }

        internal int Threshold { get; }

        internal Dictionary<string, AccessGroup<T>> ByKey { get; }
    }

    /// <summary>
    /// The accesses of a scope that have the same normalized text. Which of them are semantically the same chain (same symbols for
    /// the same identifiers, see <see cref="AreSemanticallySame"/>) is worked out once, on first request.
    /// </summary>
    private sealed class AccessGroup<T>
        where T : ExpressionSyntax
    {
        private readonly object _gate = new();
        private Dictionary<T, int> _sameCounts;

        internal List<T> Candidates { get; } = new();

        /// <summary>The number of accesses, other than <paramref name="access"/>, that are deep enough and the same chain as it.</summary>
        internal int CountOthersSameAs(T access, SemanticModel semanticModel, HashSet<string> ignoredMembers, int threshold)
        {
            var counts = _sameCounts;
            if (counts == null)
            {
                lock (_gate)
                {
                    counts = _sameCounts ??= ComputeSameCounts(semanticModel, ignoredMembers, threshold);
                }
            }

            return counts.TryGetValue(access, out var count) ? count : 0;
        }

        private Dictionary<T, int> ComputeSameCounts(SemanticModel semanticModel, HashSet<string> ignoredMembers, int threshold)
        {
            // only the accesses that are deep enough once the ignored members are not counted can be the same chain as another one
            var eligible = Candidates.Where(c => ComputeEffectiveChainDepth(c, semanticModel, ignoredMembers) >= threshold).ToArray();
            var symbols = eligible.Select(c => GetIdentifierSymbols(c, semanticModel)).ToArray();
            var counts = new Dictionary<T, int>(eligible.Length);

            if (symbols.All(array => Array.IndexOf(array, null) < 0))
            {
                // every identifier is resolved: the chains are the same exactly when the symbol lists are equal
                var classes = new Dictionary<ISymbol[], int>(SymbolListComparer.Instance);
                foreach (var array in symbols)
                    classes[array] = classes.TryGetValue(array, out var size) ? size + 1 : 1;

                for (var i = 0; i < eligible.Length; i++)
                    counts[eligible[i]] = classes[symbols[i]] - 1;
            }
            else
            {
                // an unresolved identifier matches anything: compare the lists one by one (still without any new semantic query)
                for (var i = 0; i < eligible.Length; i++)
                {
                    var same = 0;
                    for (var j = 0; j < eligible.Length; j++)
                    {
                        if (i != j && AreSameSymbols(symbols[i], symbols[j]))
                            same++;
                    }

                    counts[eligible[i]] = same;
                }
            }

            return counts;
        }
    }

    private static ISymbol[] GetIdentifierSymbols(ExpressionSyntax expression, SemanticModel semanticModel)
    {
        return expression.DescendantNodes().OfType<IdentifierNameSyntax>().Select(id => semanticModel.GetSymbolInfo(id).Symbol).ToArray();
    }

    /// <summary>Same symbols pairwise; an identifier that could not be resolved (e.g. a lambda parameter) is skipped.</summary>
    private static bool AreSameSymbols(ISymbol[] first, ISymbol[] second)
    {
        if (first.Length != second.Length)
            return false;

        for (var i = 0; i < first.Length; i++)
        {
            if (first[i] == null || second[i] == null)
                continue;

            if (!SymbolEqualityComparer.Default.Equals(first[i], second[i]))
                return false;
        }

        return true;
    }

    private sealed class SymbolListComparer : IEqualityComparer<ISymbol[]>
    {
        internal static readonly SymbolListComparer Instance = new();

        public bool Equals(ISymbol[] x, ISymbol[] y) => AreSameSymbols(x, y);

        public int GetHashCode(ISymbol[] obj)
        {
            var hash = obj.Length;
            foreach (var symbol in obj)
                hash = unchecked((hash * 31) + SymbolEqualityComparer.Default.GetHashCode(symbol));

            return hash;
        }
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
        var byKey = new Dictionary<string, AccessGroup<T>>(StringComparer.Ordinal);
        foreach (var node in scope.DescendantNodes(n => !SyntaxUtils.IsNestedScope(n)).OfType<T>())
        {
            if (IsInsideNameof(node) || ComputeChainDepth(node) < threshold)
                continue;

            var key = SyntaxUtils.NormalizeWhitespace(node.ToString());
            if (!byKey.TryGetValue(key, out var group))
                byKey[key] = group = new AccessGroup<T>();

            group.Candidates.Add(node);
        }

        return new ScopeIndex<T>(threshold, byKey);
    }
}
