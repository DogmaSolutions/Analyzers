#nullable disable
using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What DSA052 and DSA053 do in the same way: find the call, and check that its result is only tested for existence.</summary>
    internal static class ExistenceAnalyzerCore
    {
        /// <summary>
        /// The existence test the invocation is part of, if the options say it is to be reported. The questions are asked from the cheapest to the most
        /// expensive: nearly all the invocations stop at the name, most of the others at the shape of their use, before the semantic model is asked anything.
        /// </summary>
        internal static ExistenceUse Find(SyntaxNodeAnalysisContext context, ExistenceSettings settings, ExistenceKind kind)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var name = GetInvokedName(invocation);
            if (name == null || !settings.IncludedMethods.Contains(name))
                return null;

            var shape = ExistenceAnalysis.GetShape(invocation, kind);
            if (shape == null || !settings.IsShapeReported(shape.Shape) || settings.IsExcludedBySyntax(invocation))
                return null;

            var call = ExistenceAnalysis.ResolveCall(invocation, context.SemanticModel, context.CancellationToken);
            if (call == null || !settings.IsSourceReported(call) || !settings.IsCallReported(shape.Shape, call))
                return null;

            var use = ExistenceAnalysis.Complete(shape, call, kind, context.SemanticModel, context.CancellationToken);
            return use == null || settings.IsExcludedBySymbol(invocation, context.SemanticModel) ? null : use;
        }

        /// <summary>The name of the method as it is written, before anything is asked to the semantic model.</summary>
        internal static string GetInvokedName(InvocationExpressionSyntax invocation) =>
            invocation.Expression switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name.Identifier.ValueText,
                SimpleNameSyntax simpleName => simpleName.Identifier.ValueText,
                MemberBindingExpressionSyntax memberBinding => memberBinding.Name.Identifier.ValueText,
                _ => null,
            };

        /// <summary>The message of the scenario: the source of the call, how the result reaches the test, and what the method is called.</summary>
        internal static string Describe(string rule, ExistenceUse use, string singleNote = null)
        {
            var resource = Resources.ResourceManager.GetString("DSA" + rule + "Message" + (use.Call.IsQueryable ? "Queryable" : "Enumerable") + use.Shape, Resources.Culture);
            var message = string.Format(Resources.Culture, resource, use.Call.Method.Name, use.Call.Alternative.Name, use.LocalName);
            return singleNote != null && use.Call.Method.Name.StartsWith("Single", System.StringComparison.Ordinal) ? message + string.Format(Resources.Culture, singleNote, use.Call.Method.Name, use.Call.Alternative.Name) : message;
        }

        /// <summary>The type knows its size without enumerating: an array, a string, a collection.</summary>
        internal static bool HasCheapCount(ITypeSymbol type) =>
            type != null &&
            (type is IArrayTypeSymbol || type.SpecialType == SpecialType.System_String || IsCollection(type) || type.AllInterfaces.Any(IsCollection));

        private static bool IsCollection(ITypeSymbol type) =>
            type.OriginalDefinition.SpecialType is SpecialType.System_Collections_Generic_ICollection_T or SpecialType.System_Collections_Generic_IReadOnlyCollection_T ||
            (type.Name == "ICollection" && type.ContainingNamespace.ToDisplayString() == "System.Collections");
    }
}
