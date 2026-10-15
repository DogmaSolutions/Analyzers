using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The old-style properties of a type that an auto-property could replace: <c>get { return _a; } set { _a = value; }</c>, with a private
    /// field of the same type that nothing else backs. Such a property has no logic, so it is data as an auto-property is.
    /// </summary>
    internal sealed class DSA045BackingFields
    {
        internal static readonly DSA045BackingFields None = new(ImmutableDictionary<IPropertySymbol, IFieldSymbol>.Empty);

        private readonly ImmutableDictionary<IPropertySymbol, IFieldSymbol> _fieldsByProperty;
        private readonly HashSet<IFieldSymbol> _fields;

        private DSA045BackingFields(ImmutableDictionary<IPropertySymbol, IFieldSymbol> fieldsByProperty)
        {
            _fieldsByProperty = fieldsByProperty;
            _fields = new HashSet<IFieldSymbol>(fieldsByProperty.Values, SymbolEqualityComparer.Default);
        }

        internal bool IsProperty(IPropertySymbol property) => _fieldsByProperty.ContainsKey(property);

        internal bool IsField(IFieldSymbol field) => _fields.Contains(field);

        /// <summary>The properties among <paramref name="members"/> that a private field of their own backs with nothing but a getter and a setter that copy it.</summary>
        internal static DSA045BackingFields Find(IEnumerable<ISymbol> members, CancellationToken cancellationToken)
        {
            var list = members.ToList();
            var fieldsByName = new Dictionary<string, IFieldSymbol>();
            foreach (var candidate in list.OfType<IFieldSymbol>())
            {
                if (!fieldsByName.ContainsKey(candidate.Name))
                    fieldsByName.Add(candidate.Name, candidate);
            }

            var propertiesByField = new Dictionary<IFieldSymbol, List<IPropertySymbol>>(SymbolEqualityComparer.Default);
            foreach (var property in list.OfType<IPropertySymbol>())
            {
                var field = FindField(property, fieldsByName, cancellationToken);
                if (field != null)
                    propertiesByField[field] = propertiesByField.TryGetValue(field, out var others) ? others.Append(property).ToList() : new List<IPropertySymbol> { property };
            }

            // a field that backs two properties cannot be replaced by an auto-property
            var builder = ImmutableDictionary.CreateBuilder<IPropertySymbol, IFieldSymbol>(SymbolEqualityComparer.Default);
            foreach (var pair in propertiesByField.Where(pair => pair.Value.Count == 1))
                builder[pair.Value[0]] = pair.Key;

            return new DSA045BackingFields(builder.ToImmutable());
        }

        private static IFieldSymbol FindField(IPropertySymbol property, Dictionary<string, IFieldSymbol> fieldsByName, CancellationToken cancellationToken)
        {
            if (property.IsIndexer || property.IsStatic || property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) is not PropertyDeclarationSyntax declaration)
                return null;

            var name = GetCopiedFieldName(declaration);
            var field = name != null && fieldsByName.TryGetValue(name, out var found) ? found : null;
            return field != null && CanBeBackingField(field, property) ? field : null;
        }

        /// <summary>A private, instance, non-volatile field of the type of the property (a read-only one only if there is no setter).</summary>
        private static bool CanBeBackingField(IFieldSymbol field, IPropertySymbol property) =>
            field.DeclaredAccessibility == Accessibility.Private && !field.IsStatic && !field.IsConst && !field.IsVolatile &&
            SymbolEqualityComparer.Default.Equals(field.Type, property.Type) && (!field.IsReadOnly || property.SetMethod == null);

        // ---- the shape of the accessors -----------------------------------------------------------------------------------

        /// <summary>The name of the field that the getter returns and the setter (if any) assigns, or null when the accessors do anything else.</summary>
        private static string GetCopiedFieldName(PropertyDeclarationSyntax declaration)
        {
            if (declaration.ExpressionBody != null)
                return GetFieldName(declaration.ExpressionBody.Expression);

            var accessors = declaration.AccessorList?.Accessors ?? default;
            var getter = accessors.FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.GetAccessorDeclaration));
            var setter = accessors.FirstOrDefault(accessor => accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration));
            if (getter == null || accessors.Count != (setter == null ? 1 : 2))
                return null;

            var name = GetFieldName(GetSingleExpression(getter, returned: true));
            return setter == null || name == GetAssignedFieldName(setter) ? name : null;
        }

        /// <summary><c>_a = value</c>: the name of the field.</summary>
        private static string GetAssignedFieldName(AccessorDeclarationSyntax setter) =>
            GetSingleExpression(setter, returned: false) is AssignmentExpressionSyntax { RawKind: (int)SyntaxKind.SimpleAssignmentExpression, Right: IdentifierNameSyntax { Identifier.ValueText: "value" } } assignment
                ? GetFieldName(assignment.Left)
                : null;

        /// <summary>The expression of an accessor that is nothing else: the one it returns (or assigns, for a setter), either as an expression body or as the only statement.</summary>
        private static ExpressionSyntax GetSingleExpression(AccessorDeclarationSyntax accessor, bool returned)
        {
            if (accessor.ExpressionBody != null)
                return accessor.ExpressionBody.Expression;

            var statement = accessor.Body?.Statements.Count == 1 ? accessor.Body.Statements[0] : null;
            return returned ? (statement as ReturnStatementSyntax)?.Expression : (statement as ExpressionStatementSyntax)?.Expression;
        }

        /// <summary><c>_a</c> or <c>this._a</c>, possibly parenthesized.</summary>
        private static string GetFieldName(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    return GetFieldName(parenthesized.Expression);
                case IdentifierNameSyntax identifier:
                    return identifier.Identifier.ValueText;
                case MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax name }:
                    return name.Identifier.ValueText;
                default:
                    return null;
            }
        }
    }
}
