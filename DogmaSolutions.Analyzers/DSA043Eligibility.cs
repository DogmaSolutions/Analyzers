using System.Linq;
using Microsoft.CodeAnalysis;

namespace DogmaSolutions.Analyzers
{
    /// <summary>Which types are looked at by DSA043 at all: the cheap, symbol-only filters that run before any measure.</summary>
    internal static class DSA043Eligibility
    {
        internal static bool IsCandidate(INamedTypeSymbol type, DSA043Settings settings)
        {
            return IsIncludedKind(type, settings) &&
                   IsIncludedShape(type, settings) &&
                   !IsExcludedByName(type, settings) &&
                   !IsExcludedByLocation(type, settings) &&
                   !AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes.ToList());
        }

        /// <summary>A named type written in the source: not implicit, not anonymous, with at least one declaration.</summary>
        internal static bool IsSourceType(INamedTypeSymbol type) =>
            !type.IsImplicitlyDeclared && !type.IsAnonymousType && type.DeclaringSyntaxReferences.Length > 0 && type.Locations[0].IsInSource;

        private static bool IsIncludedKind(INamedTypeSymbol type, DSA043Settings settings)
        {
            var kind = GetKindName(type);
            return kind != null && settings.IncludedTypeKinds.Any(included => string.Equals(included, kind, System.StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>The name used for the type in <c>included_type_kinds</c>; null for the types that are never looked at (interfaces, enums, delegates).</summary>
        internal static string GetKindName(INamedTypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Class)
                return type.IsRecord ? "record" : "class";

            if (type.TypeKind == TypeKind.Struct)
                return type.IsRecord ? "record struct" : "struct";

            return null;
        }

        private static bool IsIncludedShape(INamedTypeSymbol type, DSA043Settings settings)
        {
            return (settings.IncludeStaticClasses || !type.IsStatic) &&
                   (settings.IncludeAbstractClasses || !type.IsAbstract) &&
                   (settings.IncludeNestedTypes || type.ContainingType == null);
        }

        private static bool IsExcludedByName(INamedTypeSymbol type, DSA043Settings settings)
        {
            var typeNamespace = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
            return settings.ExcludedTypeNamePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(type.Name, pattern)) ||
                   settings.ExcludedNamespacePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(typeNamespace, pattern));
        }

        private static bool IsExcludedByLocation(INamedTypeSymbol type, DSA043Settings settings) =>
            AnalyzersUtils.IsFileExcluded(type.Locations[0].SourceTree?.FilePath, settings.ExcludedFilePatterns);
    }
}
