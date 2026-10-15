using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>A logger that can be used at a given point of the code.</summary>
    internal sealed class AvailableLogger
    {
        internal AvailableLogger(string name, bool isMicrosoftLogger)
        {
            Name = name;
            IsMicrosoftLogger = isMicrosoftLogger;
        }

        /// <summary>The name to write to use it: a field, a property or a parameter, or the static logger class.</summary>
        internal string Name { get; }

        /// <summary>An <c>ILogger</c> / <c>ILogger&lt;T&gt;</c> of Microsoft.Extensions.Logging, the one the code fix knows how to call.</summary>
        internal bool IsMicrosoftLogger { get; }
    }

    /// <summary>
    /// Looks for a logger that the code at a given point can really use, from the nearest to the farthest: the parameters of the
    /// enclosing methods and lambdas, the fields and properties of the class (own or inherited), the primary constructor, and a static
    /// logger the class already calls.
    /// </summary>
    internal static class DSA044LoggerAvailability
    {
        private const string MicrosoftLoggerName = "Microsoft.Extensions.Logging.ILogger";

        /// <summary>The static logger a type calls, with the settings it was looked for with.</summary>
        private sealed class StaticLoggerName
        {
            internal StaticLoggerName(DSA044Settings settings, string name)
            {
                Settings = settings;
                Name = name;
            }

            internal DSA044Settings Settings { get; }

            internal string Name { get; }
        }

        private static readonly ConditionalWeakTable<INamedTypeSymbol, StaticLoggerName> _staticLoggerByType = new();

        /// <param name="enclosing">The symbol that encloses the code: the method, or the lambda or local function inside it.</param>
        internal static AvailableLogger Find(ISymbol enclosing, Compilation compilation, DSA044Settings settings, CancellationToken cancellationToken)
        {
            var member = GetEnclosingMember(enclosing);
            var isStaticContext = settings.RequireAccessibleLogger && member.IsStatic;
            var type = member.ContainingType;
            var loggers = GetTypeLoggers(type, settings);

            return FindInParameters(enclosing, settings)
                   ?? loggers.FindMember(isStaticContext)
                   ?? (isStaticContext ? null : loggers.PrimaryConstructor)
                   ?? FindStaticLoggerCall(type, compilation, settings, cancellationToken);
        }

        /// <summary>The method (or field, property...) that holds the code, looking through lambdas and local functions.</summary>
        internal static ISymbol GetEnclosingMember(ISymbol enclosing)
        {
            var current = enclosing;
            while (current is IMethodSymbol { MethodKind: MethodKind.AnonymousFunction or MethodKind.LocalFunction } function)
                current = function.ContainingSymbol;

            return current;
        }

        // ---- parameters -------------------------------------------------------------------------------------------

        private static AvailableLogger FindInParameters(ISymbol enclosing, DSA044Settings settings)
        {
            if (!settings.AcceptLoggerParameters)
                return null;

            for (var current = enclosing; current is IMethodSymbol method; current = method.ContainingSymbol)
            {
                var found = method.Parameters.FirstOrDefault(parameter => LoggerDetector.IsLoggingType(parameter.Type, settings));
                if (found != null)
                    return Describe(found.Name, found.Type);

                if (method.MethodKind is not (MethodKind.AnonymousFunction or MethodKind.LocalFunction))
                    break;
            }

            return null;
        }

        // ---- fields, properties and the primary constructor, of the type ---------------------------------------------

        /// <summary>A field or property that is a logger: what it is called, and whether the code of a static member can reach it.</summary>
        private readonly struct MemberLogger
        {
            internal MemberLogger(AvailableLogger logger, bool isStatic)
            {
                Logger = logger;
                IsStatic = isStatic;
            }

            internal AvailableLogger Logger { get; }

            internal bool IsStatic { get; }
        }

        /// <summary>
        /// The loggers a type offers to its code, found once per type (and not once per call of the type): its own members first, then the
        /// inherited ones, and the parameters of its primary constructor.
        /// </summary>
        private sealed class TypeLoggers
        {
            private readonly ImmutableArray<MemberLogger> _members;

            internal TypeLoggers(DSA044Settings settings, ImmutableArray<MemberLogger> members, AvailableLogger primaryConstructor)
            {
                Settings = settings;
                _members = members;
                PrimaryConstructor = primaryConstructor;
            }

            /// <summary>The settings the loggers were looked for with.</summary>
            internal DSA044Settings Settings { get; }

            /// <summary>A logger that the parameters of the primary constructor offer to the instance members.</summary>
            internal AvailableLogger PrimaryConstructor { get; }

            internal AvailableLogger FindMember(bool isStaticContext)
            {
                foreach (var member in _members)
                {
                    if (!isStaticContext || member.IsStatic)
                        return member.Logger;
                }

                return null;
            }
        }

        private static readonly ConditionalWeakTable<INamedTypeSymbol, TypeLoggers> _typeLoggers = new();

        private static TypeLoggers GetTypeLoggers(INamedTypeSymbol type, DSA044Settings settings)
        {
            var cached = _typeLoggers.GetValue(type, key => FindTypeLoggers(key, settings));

            // the other settings of another file of a partial type: not cached, but right
            return ReferenceEquals(cached.Settings, settings) ? cached : FindTypeLoggers(type, settings);
        }

        private static TypeLoggers FindTypeLoggers(INamedTypeSymbol type, DSA044Settings settings)
        {
            var members = type.GetMembers().Concat(GetInheritedMembers(type, settings))
                .Select(member => (Member: member, LoggerType: GetLoggerType(member, settings)))
                .Where(candidate => candidate.LoggerType != null)
                .Select(candidate => new MemberLogger(Describe(candidate.Member.Name, candidate.LoggerType), candidate.Member.IsStatic))
                .ToImmutableArray();

            return new TypeLoggers(settings, members, FindInPrimaryConstructor(type, settings));
        }

        private static IEnumerable<ISymbol> GetInheritedMembers(INamedTypeSymbol type, DSA044Settings settings)
        {
            if (!settings.AcceptInheritedLogger)
                return Enumerable.Empty<ISymbol>();

            return GetBaseTypes(type).SelectMany(baseType => baseType.GetMembers()).Where(member => member.DeclaredAccessibility != Accessibility.Private);
        }

        /// <summary>The type of a field or property when it is a logging type, else null.</summary>
        private static ITypeSymbol GetLoggerType(ISymbol member, DSA044Settings settings)
        {
            var type = GetMemberType(member);
            return type != null && LoggerDetector.IsLoggingType(type, settings) ? type : null;
        }

        private static ITypeSymbol GetMemberType(ISymbol member)
        {
            if (member is IFieldSymbol field)
                return field.Type;

            if (member is IPropertySymbol property)
                return property.Type;

            return null;
        }

        private static IEnumerable<INamedTypeSymbol> GetBaseTypes(INamedTypeSymbol type)
        {
            for (var current = type.BaseType; current != null; current = current.BaseType)
                yield return current;
        }

        /// <summary>The parameters of a primary constructor are captured by the instance members of the class.</summary>
        private static AvailableLogger FindInPrimaryConstructor(INamedTypeSymbol type, DSA044Settings settings)
        {
            var found = type.InstanceConstructors
                .Where(constructor => constructor.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax))
                .SelectMany(constructor => constructor.Parameters)
                .FirstOrDefault(parameter => LoggerDetector.IsLoggingType(parameter.Type, settings));

            return found == null ? null : Describe(found.Name, found.Type);
        }

        // ---- static logger ----------------------------------------------------------------------------------------

        /// <summary>A class that already calls a static logger (e.g. <c>Serilog.Log</c>) can call it anywhere.</summary>
        private static AvailableLogger FindStaticLoggerCall(INamedTypeSymbol type, Compilation compilation, DSA044Settings settings, CancellationToken cancellationToken)
        {
            if (!settings.AcceptStaticLoggerCalls || !ReferencesStaticLogger(compilation, settings))
                return null;

            var cached = _staticLoggerByType.GetValue(type, key => new StaticLoggerName(settings, FindStaticLoggerName(key, compilation, settings, cancellationToken)));
            var name = ReferenceEquals(cached.Settings, settings) ? cached.Name : FindStaticLoggerName(type, compilation, settings, cancellationToken);
            return name == null ? null : new AvailableLogger(name, isMicrosoftLogger: false);
        }

        private sealed class StaticLoggerReference
        {
            internal StaticLoggerReference(DSA044Settings settings, bool isReferenced)
            {
                Settings = settings;
                IsReferenced = isReferenced;
            }

            internal DSA044Settings Settings { get; }

            internal bool IsReferenced { get; }
        }

        private static readonly ConditionalWeakTable<Compilation, StaticLoggerReference> _staticLoggerReferences = new();

        /// <summary>Whether the compilation has any of the static loggers: a class cannot call one that does not exist, and looking for the calls means binding the class.</summary>
        private static bool ReferencesStaticLogger(Compilation compilation, DSA044Settings settings)
        {
            var cached = _staticLoggerReferences.GetValue(compilation, key => new StaticLoggerReference(settings, HasStaticLogger(key, settings)));
            return ReferenceEquals(cached.Settings, settings) ? cached.IsReferenced : HasStaticLogger(compilation, settings);
        }

        private static bool HasStaticLogger(Compilation compilation, DSA044Settings settings) => settings.StaticLoggerTypes.Any(name => compilation.GetTypeByMetadataName(name) != null);

        private static string FindStaticLoggerName(INamedTypeSymbol type, Compilation compilation, DSA044Settings settings, CancellationToken cancellationToken)
        {
            return DSA043TypeMetrics.GetDeclarations(type, cancellationToken)
                .SelectMany(declaration => declaration.DescendantNodes().OfType<InvocationExpressionSyntax>().Select(invocation => (declaration.SyntaxTree, invocation)))
                .Select(pair => compilation.GetSemanticModel(pair.SyntaxTree).GetSymbolInfo(pair.invocation, cancellationToken).Symbol?.ContainingType)
                .FirstOrDefault(containing => containing != null && settings.StaticLoggerTypes.Contains(SymbolNames.GetFullName(containing)))?.Name;
        }

        // ---- result -----------------------------------------------------------------------------------------------

        private static AvailableLogger Describe(string name, ITypeSymbol loggerType) => new(name, IsMicrosoftLogger(loggerType));

        private static bool IsMicrosoftLogger(ITypeSymbol type) =>
            type is INamedTypeSymbol named &&
            (IsMicrosoftLoggerInterface(named) || named.AllInterfaces.Any(IsMicrosoftLoggerInterface));

        private static bool IsMicrosoftLoggerInterface(INamedTypeSymbol type) =>
            (type.OriginalDefinition.ContainingNamespace == null ? null : SymbolNames.GetFullName(type.OriginalDefinition.ContainingNamespace)) + "." + type.OriginalDefinition.Name == MicrosoftLoggerName;
    }
}
