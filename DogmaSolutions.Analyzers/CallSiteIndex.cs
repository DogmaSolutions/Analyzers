using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The invocations of a compilation, indexed by the simple name of the called method. The rules that follow a parameter back to the
    /// arguments passed at every call site (DSA021, DSA030, endpoint authorization) used to bind every invocation of every file for
    /// each parameter they met; with the index only the invocations that name the method are bound. The index is syntactic and built once per
    /// compilation.
    /// </summary>
    internal static class CallSiteIndex
    {
        private static readonly ConditionalWeakTable<Compilation, Index> _indexes = new();

        /// <summary>
        /// The invocations that can be a call of a method called <paramref name="methodName"/> — those that name it, plus those whose
        /// callee is not a plain name — each with the semantic model of its tree.
        /// </summary>
        internal static IEnumerable<(SemanticModel Model, InvocationExpressionSyntax Invocation)> FindCandidates(Compilation compilation, string methodName)
        {
            if (compilation == null) throw new ArgumentNullException(nameof(compilation));

            var index = _indexes.GetValue(compilation, Build);
            var models = new Dictionary<SyntaxTree, SemanticModel>();

            foreach (var invocation in index.Candidates(methodName))
            {
                var tree = invocation.SyntaxTree;
                if (!models.TryGetValue(tree, out var model))
                    models[tree] = model = compilation.GetSemanticModel(tree);

                yield return (model, invocation);
            }
        }

        private static Index Build(Compilation compilation)
        {
            var named = new Dictionary<string, List<InvocationExpressionSyntax>>(StringComparer.Ordinal);
            var unnamed = new List<InvocationExpressionSyntax>();

            foreach (var tree in compilation.SyntaxTrees)
            {
                foreach (var node in tree.GetRoot().DescendantNodes())
                {
                    if (node is not InvocationExpressionSyntax invocation)
                        continue;

                    var name = SyntaxUtils.GetInvokedName(invocation);
                    if (name == null)
                    {
                        unnamed.Add(invocation);
                        continue;
                    }

                    if (!named.TryGetValue(name, out var list))
                        named[name] = list = new List<InvocationExpressionSyntax>();

                    list.Add(invocation);
                }
            }

            return new Index(named, unnamed);
        }

        private sealed class Index
        {
            private readonly Dictionary<string, List<InvocationExpressionSyntax>> _named;
            private readonly List<InvocationExpressionSyntax> _unnamed;

            internal Index(Dictionary<string, List<InvocationExpressionSyntax>> named, List<InvocationExpressionSyntax> unnamed)
            {
                _named = named;
                _unnamed = unnamed;
            }

            internal IEnumerable<InvocationExpressionSyntax> Candidates(string methodName)
            {
                if (_named.TryGetValue(methodName, out var list))
                {
                    foreach (var invocation in list)
                        yield return invocation;
                }

                foreach (var invocation in _unnamed)
                    yield return invocation;
            }
        }
    }
}
