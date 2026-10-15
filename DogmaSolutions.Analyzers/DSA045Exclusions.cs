using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace DogmaSolutions.Analyzers
{
    /// <summary>Where a data carrier is not reported: already excluded from the coverage, generated, in a test project, or excluded by the options.</summary>
    internal static class DSA045Exclusions
    {
        private const string AttributeSuffix = "Attribute";

        /// <summary>Whether the attributes of the assembly exclude it all, answered once per compilation and not once per type.</summary>
        private sealed class AssemblyExclusion
        {
            internal AssemblyExclusion(DSA045Settings settings, bool isExcluded)
            {
                Settings = settings;
                IsExcluded = isExcluded;
            }

            internal DSA045Settings Settings { get; }

            internal bool IsExcluded { get; }
        }

        private static readonly ConditionalWeakTable<Compilation, AssemblyExclusion> _assemblies = new();

        internal static bool IsExcluded(INamedTypeSymbol type, SyntaxTree tree, DSA045Settings settings, Compilation compilation, ISet<string> referencedAssemblies)
        {
            return IsExcludedByOptions(type, tree, settings) ||
                   (settings.SkipTestAssemblies && settings.TestAssemblyNames.Any(referencedAssemblies.Contains)) ||
                   IsAssemblyExcluded(compilation, settings) ||
                   HasExcludingAttribute(type, settings);
        }

        private static bool IsAssemblyExcluded(Compilation compilation, DSA045Settings settings)
        {
            var cached = _assemblies.GetValue(compilation, key => new AssemblyExclusion(settings, HasAnyAttribute(key.Assembly.GetAttributes(), settings.ExclusionAttributes)));
            return ReferenceEquals(cached.Settings, settings) ? cached.IsExcluded : HasAnyAttribute(compilation.Assembly.GetAttributes(), settings.ExclusionAttributes);
        }

        private static bool IsExcludedByOptions(INamedTypeSymbol type, SyntaxTree tree, DSA045Settings settings)
        {
            return settings.ExcludedTypeNamePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(type.Name, pattern)) ||
                   IsNamespaceExcluded(type, settings) ||
                   AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns) ||
                   (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));
        }

        /// <summary>The name of the namespace is built only if there is a pattern to match it with.</summary>
        private static bool IsNamespaceExcluded(INamedTypeSymbol type, DSA045Settings settings)
        {
            if (settings.ExcludedNamespacePatterns.Length == 0)
                return false;

            var typeNamespace = type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace);
            return settings.ExcludedNamespacePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(typeNamespace, pattern));
        }

        /// <summary>The type, or a type that contains it (an attribute on the outer type applies to the nested ones), is excluded already or is generated.</summary>
        private static bool HasExcludingAttribute(INamedTypeSymbol type, DSA045Settings settings)
        {
            for (var current = type; current != null; current = current.ContainingType)
            {
                var attributes = current.GetAttributes();
                if (!attributes.IsEmpty && (HasAnyAttribute(attributes, settings.ExclusionAttributes) || HasAnyAttribute(attributes, settings.IgnoredAttributes)))
                    return true;
            }

            return false;
        }

        private static bool HasAnyAttribute(IEnumerable<AttributeData> attributes, string[] names) =>
            names.Length > 0 && attributes.Any(attribute => attribute.AttributeClass != null && IsNamed(SymbolNames.GetFullName(attribute.AttributeClass), names));

        private static bool IsNamed(string attributeName, string[] names)
        {
            foreach (var name in names)
            {
                if (string.Equals(attributeName, name, StringComparison.Ordinal) || IsNameWithSuffix(attributeName, name))
                    return true;
            }

            return false;
        }

        /// <summary><c>name + "Attribute"</c> without building the string.</summary>
        private static bool IsNameWithSuffix(string attributeName, string name) =>
            attributeName.Length == name.Length + AttributeSuffix.Length &&
            attributeName.StartsWith(name, StringComparison.Ordinal) &&
            attributeName.EndsWith(AttributeSuffix, StringComparison.Ordinal);
    }
}
