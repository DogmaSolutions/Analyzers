using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The identifiers of the code of a type, grouped by name, built once per type. The analyzers that follow what a type does with its own members
    /// (DSA047, DSA049, DSA050) look for a few names: walking the whole type once per name would make them quadratic.
    /// </summary>
    internal sealed class TypeIdentifierIndex
    {
        private static readonly ConditionalWeakTable<INamedTypeSymbol, TypeIdentifierIndex> _cache = new();
        private static readonly IReadOnlyList<IdentifierNameSyntax> _none = Array.Empty<IdentifierNameSyntax>();

        private readonly INamedTypeSymbol _type;
        private readonly Compilation _compilation;
        private readonly object _gate = new();
        private volatile SyntaxNode[] _roots;
        private volatile int _hasLocks; // 0: not known yet, 1: yes, 2: no
        private volatile Dictionary<string, List<IdentifierNameSyntax>> _byName;

        private TypeIdentifierIndex(INamedTypeSymbol type, Compilation compilation)
        {
            _type = type;
            _compilation = compilation;
        }

        internal static TypeIdentifierIndex Get(INamedTypeSymbol type, Compilation compilation) =>
            _cache.GetValue(type.OriginalDefinition, key => new TypeIdentifierIndex(key, compilation));

        /// <summary>The declarations of the type, one for each part of a partial type.</summary>
        internal SyntaxNode[] GetRoots(CancellationToken cancellationToken)
        {
            if (_roots != null)
                return _roots;

            lock (_gate)
            {
                return _roots ??= _type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken)).ToArray();
            }
        }

        /// <summary>Whether the code of the type has a <c>lock</c> statement (a cheap token scan, so that the types that have none cost almost nothing).</summary>
        internal bool HasLocks(CancellationToken cancellationToken)
        {
            if (_hasLocks != 0)
                return _hasLocks == 1;

            var found = GetRoots(cancellationToken).Any(static root => root.DescendantTokens().Any(static token => token.IsKind(SyntaxKind.LockKeyword)));
            lock (_gate)
            {
                _hasLocks = found ? 1 : 2;
            }

            return found;
        }

        /// <summary>The identifiers with the given name, in source order (the nested types are included).</summary>
        internal IReadOnlyList<IdentifierNameSyntax> Find(string name, CancellationToken cancellationToken)
        {
            var byName = _byName;
            if (byName == null)
            {
                lock (_gate)
                {
                    byName = _byName ??= Build(GetRoots(cancellationToken));
                }
            }

            return byName.TryGetValue(name, out var list) ? list : _none;
        }

        /// <summary>The model of a tree of the compilation (the analysis driver caches them).</summary>
        internal SemanticModel GetModel(SyntaxTree tree) => _compilation.GetSemanticModel(tree);

        /// <summary>The type declaration the identifier belongs to, which is not the type itself when the identifier is in a nested type.</summary>
        internal static BaseTypeDeclarationSyntax GetOwner(SyntaxNode node) => node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();

        private static Dictionary<string, List<IdentifierNameSyntax>> Build(SyntaxNode[] roots)
        {
            var result = new Dictionary<string, List<IdentifierNameSyntax>>(StringComparer.Ordinal);
            foreach (var root in roots)
            {
                foreach (var node in root.DescendantNodes())
                {
                    if (node is not IdentifierNameSyntax name)
                        continue;

                    var text = name.Identifier.ValueText;
                    if (!result.TryGetValue(text, out var list))
                        result[text] = list = new List<IdentifierNameSyntax>();

                    list.Add(name);
                }
            }

            return result;
        }
    }
}
