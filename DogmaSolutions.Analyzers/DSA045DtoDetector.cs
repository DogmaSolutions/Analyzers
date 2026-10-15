using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What <see cref="DSA045DtoDetector"/> found out about a type.</summary>
    internal readonly struct DtoVerdict
    {
        internal DtoVerdict(bool isPure, int dataMembers, bool hasBaseTypes)
        {
            IsPure = isPure;
            DataMembers = dataMembers;
            HasBaseTypes = hasBaseTypes;
        }

        /// <summary>The type, and its base types when they are inspected, are nothing but data.</summary>
        internal bool IsPure { get; }

        /// <summary>Properties and fields of the type and of its base types.</summary>
        internal int DataMembers { get; }

        /// <summary>The type derives from a type other than <c>object</c> that was inspected too.</summary>
        internal bool HasBaseTypes { get; }
    }

    /// <summary>
    /// Decides whether a type is a pure data carrier: properties (and, optionally, fields) with no logic, no explicit constructor, no method,
    /// event, operator or nested type. The base types are looked at too: from source with the same rules; from metadata, where the bodies
    /// are not visible, by their shape (anything but properties, fields and a parameterless constructor makes them impure).
    /// </summary>
    internal static class DSA045DtoDetector
    {
        internal static DtoVerdict Evaluate(INamedTypeSymbol type, DSA045Settings settings, CancellationToken cancellationToken)
        {
            var count = CountDataMembers(type, settings, cancellationToken);
            if (count < 0)
                return default;

            var hasBaseTypes = false;
            if (settings.InspectBaseTypes)
            {
                for (var baseType = type.BaseType; baseType != null && baseType.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType); baseType = baseType.BaseType)
                {
                    var baseCount = IsInspectable(baseType) ? CountBaseDataMembers(baseType, settings, cancellationToken) : -1;
                    if (baseCount < 0)
                        return default;

                    count += baseCount;
                    hasBaseTypes = true;
                }
            }

            return new DtoVerdict(count >= settings.MinDataMembers, count, hasBaseTypes);
        }

        /// <summary>A base type that cannot be resolved is never assumed to be a data carrier.</summary>
        private static bool IsInspectable(INamedTypeSymbol baseType) => baseType.TypeKind != TypeKind.Error;

        /// <summary>The number of data members of a base type, remembered: the types that derive from the same base ask for it again and again.</summary>
        private sealed class BaseCount
        {
            internal BaseCount(DSA045Settings settings, int count)
            {
                Settings = settings;
                Count = count;
            }

            internal DSA045Settings Settings { get; }

            internal int Count { get; }
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<INamedTypeSymbol, BaseCount> _baseCounts = new();

        private static int CountBaseDataMembers(INamedTypeSymbol baseType, DSA045Settings settings, CancellationToken cancellationToken)
        {
            var cached = _baseCounts.GetValue(baseType, key => new BaseCount(settings, CountDataMembers(key, settings, cancellationToken)));
            return ReferenceEquals(cached.Settings, settings) ? cached.Count : CountDataMembers(baseType, settings, cancellationToken);
        }

        /// <summary>The number of properties and fields of the type, or -1 when a member of the type is something else than data.</summary>
        internal static int CountDataMembers(INamedTypeSymbol type, DSA045Settings settings, CancellationToken cancellationToken)
        {
            var count = 0;
            var members = type.GetMembers();

            // the old-style properties are looked for only if a property or a field needs it: most data carriers have none
            DSA045BackingFields backingFields = null;
            foreach (var member in members.Where(member => !member.IsImplicitlyDeclared))
            {
                var weight = GetDataWeight(member, type, () => backingFields ??= FindBackingFields(members, settings, cancellationToken), settings, cancellationToken);
                if (weight < 0)
                    return -1;

                count += weight;
            }

            return count;
        }

        private static DSA045BackingFields FindBackingFields(ImmutableArray<ISymbol> members, DSA045Settings settings, CancellationToken cancellationToken) =>
            settings.AllowBackingFieldProperties ? DSA045BackingFields.Find(members.Where(member => !member.IsImplicitlyDeclared), cancellationToken) : DSA045BackingFields.None;

        /// <summary>1 for a property or a field, 0 for a member that is neither data nor logic, -1 for logic.</summary>
        private static int GetDataWeight(ISymbol member, INamedTypeSymbol type, Func<DSA045BackingFields> backingFields, DSA045Settings settings, CancellationToken cancellationToken)
        {
            switch (member)
            {
                case IFieldSymbol field:
                    return IsBackingFieldCandidate(field) && backingFields().IsField(field) ? GetBackingFieldWeight(field, settings, cancellationToken) : GetFieldWeight(field, settings, cancellationToken);
                case IPropertySymbol property:
                    return IsDataProperty(property, settings, cancellationToken) || backingFields().IsProperty(property) ? 1 : -1;
                case IMethodSymbol method:
                    return GetMethodWeight(method, type, settings, cancellationToken);
                case INamedTypeSymbol:
                    return settings.AllowNestedTypes ? 0 : -1;
                default:
                    return -1;
            }
        }

        /// <summary>Only a private instance field can back an old-style property.</summary>
        private static bool IsBackingFieldCandidate(IFieldSymbol field) => field.DeclaredAccessibility == Accessibility.Private && !field.IsStatic && !field.IsConst;

        /// <summary>The field of an old-style property is not data of its own, but its initializer is still looked at.</summary>
        private static int GetBackingFieldWeight(IFieldSymbol field, DSA045Settings settings, CancellationToken cancellationToken) =>
            HasAllowedInitializer(field, settings, cancellationToken) ? 0 : -1;

        private static bool HasAllowedInitializer(IFieldSymbol field, DSA045Settings settings, CancellationToken cancellationToken)
        {
            var initializer = (field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax)?.Initializer;
            return initializer == null || IsAllowedInitializer(initializer.Value, settings);
        }

        private static int GetFieldWeight(IFieldSymbol field, DSA045Settings settings, CancellationToken cancellationToken)
        {
            // the backing field of an auto-property of a type read from metadata has a name the compiler chose; the other private fields of such
            // a type back the properties that cannot be seen to be old-style ones
            if (field.IsConst || field.Name.EndsWith(">k__BackingField", System.StringComparison.Ordinal) || (field.DeclaredAccessibility == Accessibility.Private && field.DeclaringSyntaxReferences.IsEmpty))
                return 0;

            if (field.IsStatic || !settings.AllowFields)
                return -1;

            return HasAllowedInitializer(field, settings, cancellationToken) ? 1 : -1;
        }

        private static int GetMethodWeight(IMethodSymbol method, INamedTypeSymbol type, DSA045Settings settings, CancellationToken cancellationToken)
        {
            switch (method.MethodKind)
            {
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    return 0;
                case MethodKind.Constructor:
                    return IsDataConstructor(method, type, settings, cancellationToken) ? 0 : -1;
                default:
                    return -1;
            }
        }

        // ---- properties -----------------------------------------------------------------------------------------------

        private static bool IsDataProperty(IPropertySymbol property, DSA045Settings settings, CancellationToken cancellationToken)
        {
            if (property.IsIndexer || property.IsStatic)
                return false;

            // a property read from metadata: its accessors cannot be looked at; the one of a positional record is data by definition
            var syntax = property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken);
            return syntax is null or ParameterSyntax ||
                   (syntax is PropertyDeclarationSyntax declaration && IsAutoProperty(declaration, settings) && (declaration.Initializer == null || IsAllowedInitializer(declaration.Initializer.Value, settings)));
        }

        private static bool IsAutoProperty(PropertyDeclarationSyntax declaration, DSA045Settings settings) =>
            settings.AllowComputedProperties || (declaration.ExpressionBody == null && (declaration.AccessorList?.Accessors.All(accessor => accessor.Body == null && accessor.ExpressionBody == null) ?? false));

        // ---- constructors ---------------------------------------------------------------------------------------------

        private static bool IsDataConstructor(IMethodSymbol constructor, INamedTypeSymbol type, DSA045Settings settings, CancellationToken cancellationToken) =>
            !constructor.IsStatic && IsDataConstructorSyntax(constructor, constructor.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken), type, settings);

        private static bool IsDataConstructorSyntax(IMethodSymbol constructor, SyntaxNode syntax, INamedTypeSymbol type, DSA045Settings settings)
        {
            switch (syntax)
            {
                case null:
                    // from metadata: the default constructor the compiler emits
                    return constructor.Parameters.IsEmpty;
                case RecordDeclarationSyntax:
                    // the primary constructor of a record declares its properties
                    return type.IsRecord;
                case ConstructorDeclarationSyntax declaration:
                    return settings.AllowParameterlessConstructor && declaration.ParameterList.Parameters.Count == 0 && declaration.Initializer == null && declaration.Body is { Statements.Count: 0 };
                default:
                    return false;
            }
        }

        // ---- initializers ---------------------------------------------------------------------------------------------

        private static bool IsAllowedInitializer(ExpressionSyntax initializer, DSA045Settings settings) => settings.AllowPropertyInitializers && IsSimple(initializer);

        /// <summary>Literals, <c>default</c>, constants, <c>typeof</c>, object, array and collection creations of simple values: a value, not a behavior.</summary>
        private static bool IsSimple(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax:
                case DefaultExpressionSyntax:
                case TypeOfExpressionSyntax:
                case IdentifierNameSyntax:
                case MemberAccessExpressionSyntax:
                    return true;
                case ParenthesizedExpressionSyntax parenthesized:
                    return IsSimple(parenthesized.Expression);
                case PrefixUnaryExpressionSyntax prefix when prefix.Kind() is SyntaxKind.UnaryMinusExpression or SyntaxKind.UnaryPlusExpression or SyntaxKind.LogicalNotExpression or SyntaxKind.BitwiseNotExpression:
                    return IsSimple(prefix.Operand);
                default:
                    return IsSimpleCreation(expression);
            }
        }

        private static bool IsSimpleCreation(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case BaseObjectCreationExpressionSyntax creation:
                    return (creation.ArgumentList?.Arguments.All(argument => IsSimple(argument.Expression)) ?? true) && AreSimple(creation.Initializer);
                case ArrayCreationExpressionSyntax array:
                    return AreSimple(array.Initializer);
                case ImplicitArrayCreationExpressionSyntax implicitArray:
                    return AreSimple(implicitArray.Initializer);
                case CollectionExpressionSyntax collection:
                    return collection.Elements.All(element => element is ExpressionElementSyntax expressionElement && IsSimple(expressionElement.Expression));
                default:
                    return false;
            }
        }

        private static bool AreSimple(InitializerExpressionSyntax initializer) =>
            initializer == null || initializer.Expressions.All(element => element is AssignmentExpressionSyntax assignment ? IsSimple(assignment.Right) : IsSimple(element));
    }
}
