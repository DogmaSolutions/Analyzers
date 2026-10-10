using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Recognition of Entity Framework Core symbols, shared by the rules that reason about queries (DSA021, DSA030, DSA031).
    /// </summary>
    internal static class EntityFrameworkSymbols
    {
        private const string EntityFrameworkCoreNamespace = "Microsoft.EntityFrameworkCore";
        private const string DbSetTypeName = "DbSet";

        /// <summary>True for the method of a type that lives in (a sub-namespace of) Microsoft.EntityFrameworkCore.</summary>
        internal static bool IsFromEntityFramework(IMethodSymbol method)
        {
            var ns = method.ContainingType?.ContainingNamespace?.ToDisplayString();
            return ns != null && ns.StartsWith(EntityFrameworkCoreNamespace, StringComparison.Ordinal);
        }

        /// <summary>True for DbSet&lt;T&gt; and for any type deriving from it.</summary>
        internal static bool IsDbSetType(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                if (current.Name == DbSetTypeName &&
                    current.ContainingNamespace?.ToDisplayString() == EntityFrameworkCoreNamespace)
                    return true;
                current = current.BaseType;
            }

            return false;
        }

        /// <summary>
        /// True when the initializer of the local (<c>var x = value;</c>) contains a DbSet-typed node or an invocation of an
        /// Entity Framework method. False when the local has no initializer, or is not declared by a variable declarator
        /// (foreach variables, pattern variables, out variables, ...).
        /// </summary>
        internal static bool LocalInitializerInvolvesEf(ILocalSymbol localSymbol, SemanticModel semanticModel)
        {
            var initValue = GetLocalInitializerValue(localSymbol);
            return initValue != null &&
                   (ContainsDbSetTypedNode(initValue, semanticModel) || ContainsEntityFrameworkInvocation(initValue, semanticModel));
        }

        /// <summary>
        /// Returns the initializer of a local declared as <c>var x = value;</c>, or null when the local has no initializer
        /// or is not declared by a variable declarator (foreach variables, pattern variables, out variables, ...).
        /// </summary>
        internal static ExpressionSyntax GetLocalInitializerValue(ILocalSymbol localSymbol)
        {
            var declarator = localSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            return declarator?.Initializer?.Value;
        }

        private static bool ContainsDbSetTypedNode(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression.DescendantNodesAndSelf().Any(node => IsDbSetType(semanticModel.GetTypeInfo(node).Type));
        }

        private static bool ContainsEntityFrameworkInvocation(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            return expression.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => semanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol method && IsFromEntityFramework(method));
        }
    }
}
