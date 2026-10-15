using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Tells whether a class injects, inherits or uses a logger (DSA043, DSA044). A "logging type" is a type that lives in one of the logger namespaces
    /// (default <c>Microsoft.Extensions.Logging</c>, so <c>ILogger</c>, <c>ILogger&lt;T&gt;</c>, <c>ILoggerFactory</c>,
    /// <c>LoggerExtensions</c>, <c>LoggerMessage</c>, ...), is one of the configured logger types, or implements or derives from one of them.
    /// The checks go from the cheapest (declarations only) to the dearest (binding the bodies), and stop at the first positive answer.
    /// </summary>
    internal static class LoggerDetector
    {
        /// <summary>Whether the compilation references any logging type at all: without one a class has no logger to inject.</summary>
        internal static bool IsLoggingAvailable(Compilation compilation, ILoggerRecognitionSettings settings)
        {
            return settings.LoggerNamespaces.Any(name => NamespaceExists(compilation, name)) ||
                   settings.LoggerTypes.Any(name => compilation.GetTypeByMetadataName(name) != null);
        }

        private static bool NamespaceExists(Compilation compilation, string qualifiedName)
        {
            var current = compilation.GlobalNamespace;
            foreach (var part in qualifiedName.Split('.'))
            {
                current = current.GetNamespaceMembers().FirstOrDefault(member => member.Name == part);
                if (current == null)
                    return false;
            }

            return true;
        }

        // ---- declarations -----------------------------------------------------------------------------------------

        /// <summary>The class has a field, a property or a constructor parameter that is a logger — its own, or inherited from a base class.</summary>
        internal static bool DeclaresLogger(INamedTypeSymbol type, ILoggerRecognitionSettings settings)
        {
            return type.GetMembers().Any(member => IsLoggerMember(member, includeConstructorParameters: true, settings)) ||
                   (settings.AcceptInheritedLogger && GetBaseTypes(type).Any(baseType => baseType.GetMembers().Any(member => IsInheritedLogger(member, settings))));
        }

        private static bool IsInheritedLogger(ISymbol member, ILoggerRecognitionSettings settings) =>
            member.DeclaredAccessibility != Accessibility.Private && IsLoggerMember(member, includeConstructorParameters: false, settings);

        private static bool IsLoggerMember(ISymbol member, bool includeConstructorParameters, ILoggerRecognitionSettings settings)
        {
            switch (member)
            {
                case IFieldSymbol field:
                    return IsLoggingType(field.Type, settings);
                case IPropertySymbol property:
                    return IsLoggingType(property.Type, settings);
                case IMethodSymbol { MethodKind: MethodKind.Constructor } constructor:
                    return includeConstructorParameters && constructor.Parameters.Any(parameter => IsLoggingType(parameter.Type, settings));
                default:
                    return false;
            }
        }

        // ---- usage ------------------------------------------------------------------------------------------------

        /// <summary>
        /// The bodies of the class declare a logger (a local, or a parameter when accepted), call or read something static of a logging type, or create
        /// a logging object or apply a logging attribute (e.g. <c>[LoggerMessage]</c>).
        /// </summary>
        internal static bool UsesLogger(INamedTypeSymbol type, Compilation compilation, ILoggerRecognitionSettings settings, CancellationToken cancellationToken)
        {
            return DSA043TypeMetrics.GetDeclarations(type, cancellationToken).Any(declaration =>
            {
                var model = compilation.GetSemanticModel(declaration.SyntaxTree);
                return declaration.DescendantNodes().Any(node => IsLoggerUsage(node, model, settings, cancellationToken));
            });
        }

        private static bool IsLoggerUsage(SyntaxNode node, SemanticModel model, ILoggerRecognitionSettings settings, CancellationToken cancellationToken)
        {
            switch (node)
            {
                case InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax or AttributeSyntax:
                    return IsLoggingSymbol(model.GetSymbolInfo(node, cancellationToken).Symbol, settings);
                case MemberAccessExpressionSyntax access:
                    return IsLoggingSymbol(model.GetSymbolInfo(access, cancellationToken).Symbol, settings);
                case VariableDeclaratorSyntax declarator:
                    return IsLoggingSymbol(model.GetDeclaredSymbol(declarator, cancellationToken), settings);
                case ParameterSyntax parameter:
                    return settings.AcceptLoggerParameters && IsLoggingSymbol(model.GetDeclaredSymbol(parameter, cancellationToken), settings);
                default:
                    return false;
            }
        }

        private static bool IsLoggingSymbol(ISymbol symbol, ILoggerRecognitionSettings settings)
        {
            switch (symbol)
            {
                case IMethodSymbol method:
                    return IsLoggingType(method.ContainingType, settings) || IsLoggingType(method.ReturnType, settings) || method.TypeArguments.Any(argument => IsLoggingType(argument, settings));
                case IFieldSymbol { IsStatic: true } field:
                    return IsLoggingType(field.ContainingType, settings);
                case IPropertySymbol { IsStatic: true } property:
                    return IsLoggingType(property.ContainingType, settings);
                case ILocalSymbol local:
                    return IsLoggingType(local.Type, settings);
                case IParameterSymbol parameter:
                    return IsLoggingType(parameter.Type, settings);
                default:
                    return false;
            }
        }

        // ---- types ------------------------------------------------------------------------------------------------

        /// <summary>A logging type, or a type that implements or derives from one.</summary>
        internal static bool IsLoggingType(ITypeSymbol type, ILoggerRecognitionSettings settings)
        {
            if (type is not INamedTypeSymbol named)
                return false;

            return IsLoggingTypeName(named, settings) ||
                   named.AllInterfaces.Any(@interface => IsLoggingTypeName(@interface, settings)) ||
                   GetBaseTypes(named).Any(baseType => IsLoggingTypeName(baseType, settings));
        }

        private static bool IsLoggingTypeName(INamedTypeSymbol type, ILoggerRecognitionSettings settings)
        {
            var original = type.OriginalDefinition;
            var typeNamespace = original.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(original.ContainingNamespace);

            return settings.LoggerNamespaces.Any(name => IsInNamespace(typeNamespace, name)) ||
                   settings.LoggerTypes.Contains(typeNamespace + "." + original.Name);
        }

        /// <summary>The namespace is the given one or one of its children.</summary>
        internal static bool IsInNamespace(string typeNamespace, string namespaceName) =>
            typeNamespace == namespaceName || typeNamespace.StartsWith(namespaceName + ".", StringComparison.Ordinal);

        private static IEnumerable<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
                yield return current;
        }
    }
}
