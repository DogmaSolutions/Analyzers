using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What the ToString that DSA051 generates is made of, from the most to the least specific.</summary>
    internal enum DSA051Strategy
    {
        /// <summary>The type has nothing to show: no ToString is generated.</summary>
        None,

        /// <summary>One member that identifies an instance (Id, Name, Email...), the first one of the configured list that the type has.</summary>
        WellKnown,

        /// <summary>All the public properties: <c>A=`1`, B=`2`</c>.</summary>
        PublicProperties,

        /// <summary>All the protected properties: <c>A=1, B=2</c>.</summary>
        ProtectedProperties,

        /// <summary>All the fields: <c>a=1, b=2</c>.</summary>
        Fields,

        /// <summary>All the other properties (internal, private): <c>A=1, B=2</c>.</summary>
        OtherProperties,
    }

    /// <summary>The members a generated ToString shows, and how.</summary>
    internal sealed class DSA051ToStringPlan
    {
        internal DSA051ToStringPlan(DSA051Strategy strategy, IReadOnlyList<ISymbol> members, int dataMembers)
        {
            Strategy = strategy;
            Members = members;
            DataMembers = dataMembers;
        }

        internal DSA051Strategy Strategy { get; }

        /// <summary>The members shown in the generated text, in declaration order.</summary>
        internal IReadOnlyList<ISymbol> Members { get; }

        /// <summary>The number of instance properties and fields of the type, whatever is shown.</summary>
        internal int DataMembers { get; }

        /// <summary>The interpolated string that the generated ToString returns (empty when there is nothing to generate).</summary>
        internal string BuildInterpolatedString(DSA051Settings settings)
        {
            if (Strategy == DSA051Strategy.None)
                return string.Empty;

            var quote = Strategy == DSA051Strategy.PublicProperties ? "`" : string.Empty;
            var text = new StringBuilder("$\"");
            for (var i = 0; i < Members.Count; i++)
            {
                var member = Members[i];
                if (i > 0)
                    text.Append(", ");

                if (Strategy != DSA051Strategy.WellKnown || settings.WellKnownIncludeKey)
                    text.Append(member.Name).Append('=');

                text.Append(quote).Append('{').Append(ToIdentifier(member.Name)).Append('}').Append(quote);
            }

            return text.Append('"').ToString();
        }

        private static string ToIdentifier(string name) => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;
    }

    /// <summary>Chooses the members a generated ToString shows: the heuristic of DSA051.</summary>
    internal static class DSA051Planner
    {
        internal static DSA051ToStringPlan Create(INamedTypeSymbol type, DSA051Settings settings)
        {
            var members = type.GetMembers();
            var properties = members.OfType<IPropertySymbol>().Where(IsShownProperty).ToList();
            var fields = members.OfType<IFieldSymbol>().Where(IsShownField).ToList();
            var dataMembers = properties.Count + fields.Count;

            // a property comes before the field that backs it, which has the same name once the "_" is dropped
            var wellKnown = FindWellKnown(properties, fields, settings.NormalizedWellKnownNames);
            if (wellKnown != null)
                return new DSA051ToStringPlan(DSA051Strategy.WellKnown, new[] { wellKnown }, dataMembers);

            return Plan(DSA051Strategy.PublicProperties, properties.Where(property => property.DeclaredAccessibility == Accessibility.Public), dataMembers, settings) ??
                   Plan(DSA051Strategy.ProtectedProperties, properties.Where(property => IsProtected(property.DeclaredAccessibility)), dataMembers, settings) ??
                   Plan(DSA051Strategy.Fields, fields, dataMembers, settings) ??
                   Plan(DSA051Strategy.OtherProperties, properties.Where(property => property.DeclaredAccessibility != Accessibility.Public && !IsProtected(property.DeclaredAccessibility)), dataMembers, settings) ??
                   new DSA051ToStringPlan(DSA051Strategy.None, Array.Empty<ISymbol>(), dataMembers);
        }

        /// <summary>A readable instance property that the text can show.</summary>
        private static bool IsShownProperty(IPropertySymbol property) =>
            IsShowable(property.Type) && !property.IsStatic && !property.IsIndexer && property.GetMethod != null && !property.IsImplicitlyDeclared && property.ExplicitInterfaceImplementations.IsEmpty;

        /// <summary>An instance field that the text can show (the field of an automatic property is not one).</summary>
        private static bool IsShownField(IFieldSymbol field) => IsShowable(field.Type) && !field.IsStatic && !field.IsConst && !field.IsImplicitlyDeclared;

        /// <summary>The plan that shows the candidates, without the sensitive ones and not more than the configured number (null when none is left).</summary>
        private static DSA051ToStringPlan? Plan(DSA051Strategy strategy, IEnumerable<ISymbol> candidates, int dataMembers, DSA051Settings settings)
        {
            var members = candidates.Where(member => !IsSensitive(member.Name, settings.NormalizedSensitiveNamePatterns)).ToList();
            if (settings.MaxMembers > 0 && members.Count > settings.MaxMembers)
                members = members.GetRange(0, settings.MaxMembers);

            return members.Count == 0 ? null : new DSA051ToStringPlan(strategy, members, dataMembers);
        }

        private static ISymbol? FindWellKnown(List<IPropertySymbol> properties, List<IFieldSymbol> fields, string[] normalizedNames)
        {
            if (normalizedNames.Length == 0)
                return null;

            // the names of the members are normalized once, not once per wanted name
            var propertyNames = properties.ConvertAll(static property => Normalize(property.Name));
            var fieldNames = fields.ConvertAll(static field => Normalize(field.Name));
            foreach (var wanted in normalizedNames)
            {
                var property = propertyNames.IndexOf(wanted);
                if (property >= 0)
                    return properties[property];

                var field = fieldNames.IndexOf(wanted);
                if (field >= 0)
                    return fields[field];
            }

            return null;
        }

        /// <summary>Names are compared ignoring "_" and casing.</summary>
        internal static string Normalize(string name) => name.Replace("_", string.Empty).ToUpperInvariant();

        private static bool IsSensitive(string name, string[] normalizedPatterns)
        {
            var normalized = Normalize(name);
            return normalizedPatterns.Any(pattern => normalized.Contains(pattern));
        }

        private static bool IsProtected(Accessibility accessibility) =>
            accessibility is Accessibility.Protected or Accessibility.ProtectedOrInternal or Accessibility.ProtectedAndInternal;

        /// <summary>A pointer, a function pointer or a ref struct cannot be written in an interpolated string.</summary>
        private static bool IsShowable(ITypeSymbol type) => type.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer) && !type.IsRefLikeType;
    }
}
