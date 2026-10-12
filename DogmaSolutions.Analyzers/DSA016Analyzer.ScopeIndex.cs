using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

// The index of the invocations of a scope, grouped by method and arguments: it makes the search of repeated calls linear in the size
// of the scope instead of comparing every invocation with all the others.
public sealed partial class DSA016Analyzer
{
    private static readonly ConditionalWeakTable<SyntaxNode, InvocationScopeIndex> _scopeIndexes = new();

    private static InvocationScopeIndex GetScopeIndex(SyntaxNode scope) => _scopeIndexes.GetValue(scope, BuildScopeIndex);

    private static InvocationScopeIndex BuildScopeIndex(SyntaxNode scope)
    {
        var groups = new Dictionary<(string Method, string Arguments), InvocationGroup>();
        var hasBranching = false;
        foreach (var node in scope.DescendantNodesAndSelf(n => !SyntaxUtils.IsNestedScope(n)))
        {
            hasBranching |= node is SwitchStatementSyntax or IfStatementSyntax or SwitchExpressionSyntax or ConditionalExpressionSyntax;

            // like the descendants of the scope: the scope itself (an expression body) is not one of its own members
            if (node is InvocationExpressionSyntax invocation &&
                !ReferenceEquals(node, scope) &&
                TryGetInvocationParts(invocation, out var receiverText, out var methodName, out var argumentsText))
            {
                var groupKey = (methodName, SyntaxUtils.NormalizeWhitespace(argumentsText));
                if (!groups.TryGetValue(groupKey, out var group))
                    groups[groupKey] = group = new InvocationGroup();

                group.Add(invocation, receiverText, argumentsText);
            }
        }

        return new InvocationScopeIndex(groups, hasBranching);
    }

    private sealed class InvocationScopeIndex
    {
        private readonly Dictionary<(string Method, string Arguments), InvocationGroup> _groups;

        internal InvocationScopeIndex(Dictionary<(string Method, string Arguments), InvocationGroup> groups, bool hasBranching)
        {
            _groups = groups;
            foreach (var group in groups.Values)
                group.HasBranching = hasBranching;
        }

        internal InvocationGroup FindGroup(string methodName, string normalizedArguments) =>
            _groups.TryGetValue((methodName, normalizedArguments), out var group) ? group : null;
    }

    /// <summary>The invocations of a scope that call the same method with the same arguments.</summary>
    private sealed class InvocationGroup
    {
        private readonly List<(InvocationExpressionSyntax Invocation, string ReceiverText, string ArgumentsText)> _members = new();
        private readonly object _gate = new();
        private Dictionary<string, List<InvocationExpressionSyntax>> _membersByKey;

        internal bool HasBranching { get; set; }

        internal int Count => _members.Count;

        internal void Add(InvocationExpressionSyntax invocation, string receiverText, string argumentsText) =>
            _members.Add((invocation, receiverText, argumentsText));

        /// <summary>
        /// 1 (the invocation itself) plus the number of other instance-method calls of the group with the same receiver key that are
        /// not in mutually exclusive branches. The key of every member is worked out once.
        /// </summary>
        internal int CountSameCalls(InvocationExpressionSyntax invocation, string key, SyntaxNode scope, SemanticModel semanticModel)
        {
            var membersByKey = _membersByKey;
            if (membersByKey == null)
            {
                lock (_gate)
                {
                    membersByKey = _membersByKey ??= GroupByKey(semanticModel);
                }
            }

            if (!membersByKey.TryGetValue(key, out var sameKey))
                return 1;

            var count = 1;
            foreach (var sibling in sameKey)
            {
                if (ReferenceEquals(sibling, invocation))
                    continue;

                if (!HasBranching || !AreInMutuallyExclusiveBranches(invocation, sibling, scope))
                    count++;
            }

            return count;
        }

        private Dictionary<string, List<InvocationExpressionSyntax>> GroupByKey(SemanticModel semanticModel)
        {
            var byKey = new Dictionary<string, List<InvocationExpressionSyntax>>(StringComparer.Ordinal);
            foreach (var (invocation, receiverText, argumentsText) in _members)
            {
                if (IsStaticMethodCall(invocation, semanticModel))
                    continue;

                var memberKey = BuildKey(receiverText, GetNameKey(MethodName(invocation)), argumentsText, GetReceiverSymbolKey(invocation, semanticModel));
                if (!byKey.TryGetValue(memberKey, out var list))
                    byKey[memberKey] = list = new List<InvocationExpressionSyntax>();

                list.Add(invocation);
            }

            return byKey;
        }

        private static SimpleNameSyntax MethodName(InvocationExpressionSyntax invocation) =>
            invocation.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Name : ((MemberBindingExpressionSyntax)invocation.Expression).Name;
    }
}
