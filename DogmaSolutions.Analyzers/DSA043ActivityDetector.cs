using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Tells whether a class does something worth logging about: a catch block, an await or a throw (found by the measure of the class,
    /// without any semantic query), or a call into one of the configured namespaces (I/O, database, HTTP, messaging, ...).
    /// </summary>
    internal static class DSA043ActivityDetector
    {
        internal static bool HasActivity(INamedTypeSymbol type, TypeMetrics metrics, DSA043Settings settings, Compilation compilation, CancellationToken cancellationToken)
        {
            if (!settings.RequireActivity)
                return true;

            return (metrics.SyntacticActivity & settings.ActivityKinds) != ActivityKinds.None ||
                   (settings.ActivityKinds.HasFlag(ActivityKinds.Call) && CallsActivityNamespace(type, settings, compilation, cancellationToken));
        }

        private static bool CallsActivityNamespace(INamedTypeSymbol type, DSA043Settings settings, Compilation compilation, CancellationToken cancellationToken)
        {
            return DSA043TypeMetrics.GetDeclarations(type, cancellationToken).Any(declaration =>
            {
                var model = compilation.GetSemanticModel(declaration.SyntaxTree);
                return declaration.DescendantNodes().Where(IsCallOrCreation).Any(node => IsInActivityNamespace(model.GetSymbolInfo(node, cancellationToken).Symbol, settings));
            });
        }

        private static bool IsCallOrCreation(SyntaxNode node) => node is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax;

        private static bool IsInActivityNamespace(ISymbol symbol, DSA043Settings settings)
        {
            var typeNamespace = symbol?.ContainingType?.ContainingNamespace?.ToDisplayString();
            return typeNamespace != null && settings.ActivityNamespaces.Any(name => LoggerDetector.IsInNamespace(typeNamespace, name));
        }
    }
}
