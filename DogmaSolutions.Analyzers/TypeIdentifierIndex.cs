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
        
        private readonly INamedTypeSymbol _type;
        private readonly Compilation _compilation;
        private readonly object _gate = new();
        private volatile SyntaxNode[] _roots;
        private volatile int _hasLocks; // 0: not known yet, 1: yes, 2: no
        private readonly Dictionary<string, List<IdentifierNameSyntax>> _found = new(StringComparer.Ordinal);

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

            var found = GetRoots(cancellationToken).Any(root => HasLockKeyword(root, cancellationToken));
            lock (_gate)
            {
                _hasLocks = found ? 1 : 2;
            }

            return found;
        }

        /// <summary>
        /// The identifiers with the given name, in source order (the nested types are included). The text of the type is searched for the word and only
        /// the hits are turned into tokens, so that a name costs one vectorized search: walking every node of the type to index all of them costs far more.
        /// </summary>
        internal IReadOnlyList<IdentifierNameSyntax> Find(string name, CancellationToken cancellationToken)
        {
            lock (_gate)
            {
                if (!_found.TryGetValue(name, out var list))
                    _found[name] = list = Search(name, GetRoots(cancellationToken), cancellationToken);

                return list;
            }
        }

        /// <summary>The text is searched for the word first (a type without it costs one vectorized search); the hits are checked on the tokens.</summary>
        private static bool HasLockKeyword(SyntaxNode root, CancellationToken cancellationToken)
        {
            var text = root.SyntaxTree.GetText(cancellationToken).ToString(root.Span);
            for (var index = text.IndexOf("lock", StringComparison.Ordinal); index >= 0; index = text.IndexOf("lock", index + 4, StringComparison.Ordinal))
            {
                if (root.FindToken(root.SpanStart + index).IsKind(SyntaxKind.LockKeyword))
                    return true;
            }

            return false;
        }

        private static List<IdentifierNameSyntax> Search(string name, SyntaxNode[] roots, CancellationToken cancellationToken)
        {
            var result = new List<IdentifierNameSyntax>();
            foreach (var root in roots)
            {
                var text = root.SyntaxTree.GetText(cancellationToken).ToString(root.Span);

                // a unicode escape can spell the name: such a type is walked token by token
                if (text.IndexOf("\\u", StringComparison.Ordinal) >= 0 || text.IndexOf("\\U", StringComparison.Ordinal) >= 0)
                    SearchTokens(name, root, result);
                else
                    SearchText(name, root, text, result);
            }

            return result;
        }

        private static void SearchTokens(string name, SyntaxNode root, List<IdentifierNameSyntax> result)
        {
            foreach (var token in root.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.IdentifierToken) && token.Parent is IdentifierNameSyntax identifier && token.ValueText == name)
                    result.Add(identifier);
            }
        }

        private static void SearchText(string name, SyntaxNode root, string text, List<IdentifierNameSyntax> result)
        {
            for (var index = text.IndexOf(name, StringComparison.Ordinal); index >= 0; index = text.IndexOf(name, index + name.Length, StringComparison.Ordinal))
            {
                var end = index + name.Length;
                if ((index > 0 && DSA054UseFinder.IsPart(text[index - 1])) || (end < text.Length && DSA054UseFinder.IsPart(text[end])))
                    continue;

                var position = root.SpanStart + (index > 0 && text[index - 1] == '@' ? index - 1 : index);
                var token = root.FindToken(position);
                if (token.SpanStart == position && token.IsKind(SyntaxKind.IdentifierToken) && token.Parent is IdentifierNameSyntax identifier)
                    result.Add(identifier);
            }
        }

        /// <summary>The model of a tree of the compilation (the analysis driver caches them).</summary>
        internal SemanticModel GetModel(SyntaxTree tree) => _compilation.GetSemanticModel(tree);

        /// <summary>The type declaration the identifier belongs to, which is not the type itself when the identifier is in a nested type.</summary>
        internal static BaseTypeDeclarationSyntax GetOwner(SyntaxNode node) => node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();
    }
}
