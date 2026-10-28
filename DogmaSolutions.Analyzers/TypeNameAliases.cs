using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The names a type can be written with in the source of a compilation: its own, and the using aliases (global ones included) whose target
    /// mentions it. A rule that is about one type can skip, on the syntax alone, whatever is written with another name.
    /// </summary>
    internal static class TypeNameAliases
    {
        // a compilation is immutable and every edit makes a new one: nothing here can outlive the code it was computed from
        private static readonly ConditionalWeakTable<Compilation, Dictionary<string, HashSet<string>>> _byCompilation = new();

        /// <summary>The simple name of the type and the aliases that target a name containing it.</summary>
        internal static HashSet<string> Get(Compilation compilation, string typeName)
        {
            var cache = _byCompilation.GetOrCreateValue(compilation);
            lock (cache)
            {
                if (!cache.TryGetValue(typeName, out var names))
                    cache[typeName] = names = Build(compilation, typeName);

                return names;
            }
        }

        /// <summary>The last name of a type or of a qualified name: <c>Regex</c> for <c>System.Text.RegularExpressions.Regex</c>; null for any other syntax.</summary>
        internal static string GetLastName(SyntaxNode node) => node switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            QualifiedNameSyntax qualified => GetLastName(qualified.Right),
            MemberAccessExpressionSyntax access => access.Name.Identifier.ValueText,
            _ => null,
        };

        /// <summary>Whether the type syntax is written with a name of the type: it is the only way the type can be written.</summary>
        internal static bool IsWrittenLike(SyntaxNode type, Compilation compilation, string typeName) =>
            GetLastName(type) is { } name && Get(compilation, typeName).Contains(name);

        private static HashSet<string> Build(Compilation compilation, string typeName)
        {
            var names = new HashSet<string>(StringComparer.Ordinal) { typeName };
            foreach (var tree in compilation.SyntaxTrees)
            {
                foreach (var directive in tree.GetRoot().DescendantNodes(static node => node is CompilationUnitSyntax or BaseNamespaceDeclarationSyntax).OfType<UsingDirectiveSyntax>())
                {
                    if (directive.Alias != null && directive.Name != null && directive.Name.ToString().Contains(typeName))
                        names.Add(directive.Alias.Name.Identifier.ValueText);
                }
            }

            return names;
        }
    }
}
