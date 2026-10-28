using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a class (or struct) that does not override <c>ToString()</c>: in the debugger, in logs and in the message of a failed assertion an
    /// instance shows up as its type name. The code fix generates the override, from the member that identifies an instance (Id, Name...) or
    /// from the public properties, the protected properties or the fields.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA051Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA051";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA051AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA051AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA051AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.BestPractice;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA051.md");

        private const int MaxNamesInMessage = 4;

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                var referencedAssemblies = new HashSet<string>(start.Compilation.ReferencedAssemblyNames.Select(name => name.Name), StringComparer.OrdinalIgnoreCase);
                start.RegisterSymbolAction(symbolContext => AnalyzeType(symbolContext, referencedAssemblies), SymbolKind.NamedType);
            });
        }

        private static void AnalyzeType(SymbolAnalysisContext context, ISet<string> referencedAssemblies)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            var location = type.Locations.FirstOrDefault(candidate => candidate.IsInSource);
            if (location == null || type.IsStatic || type.IsRecord || type.IsImplicitlyDeclared || GetKind(type) == DSA051TypeKinds.None)
                return;

            var settings = DSA051Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(location.SourceTree!));
            if (!IsIncluded(type, settings) || HasToString(type, settings) || IsExcluded(type, location.SourceTree!, settings, referencedAssemblies))
                return;

            var plan = DSA051Planner.Create(type, settings);
            if (plan.DataMembers >= settings.MinDataMembers)
                context.ReportDiagnostic(Diagnostic.Create(_rule, location, DescribeScenario(type, plan)));
        }

        internal static DSA051TypeKinds GetKind(INamedTypeSymbol type) =>
            type.TypeKind switch
            {
                TypeKind.Class => DSA051TypeKinds.Class,
                TypeKind.Struct => DSA051TypeKinds.Struct,
                _ => DSA051TypeKinds.None,
            };

        private static bool IsIncluded(INamedTypeSymbol type, DSA051Settings settings) =>
            (settings.IncludedTypeKinds & GetKind(type)) != 0 && (settings.IncludeAbstractClasses || !type.IsAbstract) && (settings.IncludeNestedTypes || type.ContainingType == null);

        /// <summary>
        /// The type declares a ToString, or inherits one it cannot do without (a base type other than object declares it and
        /// <c>consider_inherited_overrides</c> is on, or the inherited one is sealed and cannot be overridden).
        /// </summary>
        internal static bool HasToString(INamedTypeSymbol type, DSA051Settings settings)
        {
            if (DeclaresToString(type) != null)
                return true;

            for (var baseType = type.BaseType; baseType != null && baseType.SpecialType is not (SpecialType.System_Object or SpecialType.System_ValueType); baseType = baseType.BaseType)
            {
                var inherited = DeclaresToString(baseType);
                if (inherited != null)
                    return settings.ConsiderInheritedOverrides || inherited.IsSealed;
            }

            return false;
        }

        private static IMethodSymbol DeclaresToString(INamedTypeSymbol type) =>
            type.GetMembers(nameof(ToString)).OfType<IMethodSymbol>().FirstOrDefault(method => method.MethodKind == MethodKind.Ordinary && !method.IsStatic && method.Parameters.IsEmpty);

        // ---- the exclusions ---------------------------------------------------------------------------------------------------

        private static bool IsExcluded(INamedTypeSymbol type, SyntaxTree tree, DSA051Settings settings, ISet<string> referencedAssemblies)
        {
            return (settings.SkipTestAssemblies && settings.TestAssemblyNames.Any(referencedAssemblies.Contains)) ||
                   settings.ExcludedTypeNamePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(type.Name, pattern)) ||
                   IsNamespaceExcluded(type, settings) ||
                   AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns) ||
                   (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes)) ||
                   HasIgnoredAttribute(type, settings);
        }

        /// <summary>The name of the namespace is built only if there is a pattern to match it with.</summary>
        private static bool IsNamespaceExcluded(INamedTypeSymbol type, DSA051Settings settings)
        {
            if (settings.ExcludedNamespacePatterns.Length == 0)
                return false;

            var typeNamespace = type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace);
            return settings.ExcludedNamespacePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(typeNamespace, pattern));
        }

        /// <summary>The type, or a type that contains it (an attribute on the outer type applies to the nested ones), is generated or already has a representation of its own.</summary>
        private static bool HasIgnoredAttribute(INamedTypeSymbol type, DSA051Settings settings)
        {
            for (var current = type; current != null; current = current.ContainingType)
            {
                // the declaration tells whether there is an attribute at all: the attributes are bound only for the types that have some
                if (!SyntaxUtils.HasAttributeLists(current))
                    continue;

                if (current.GetAttributes().Any(attribute => attribute.AttributeClass != null && IsIgnored(SymbolNames.GetFullName(attribute.AttributeClass), settings.IgnoredAttributes)))
                    return true;
            }

            return false;
        }

        private static bool IsIgnored(string attributeName, string[] names) =>
            names.Any(name => string.Equals(attributeName, name, StringComparison.Ordinal) || string.Equals(attributeName, name + "Attribute", StringComparison.Ordinal));

        // ---- the message ------------------------------------------------------------------------------------------------------

        private static string DescribeScenario(INamedTypeSymbol type, DSA051ToStringPlan plan)
        {
            var format = plan.Strategy switch
            {
                DSA051Strategy.WellKnown => Resources.DSA051MessageWellKnown,
                DSA051Strategy.PublicProperties => Resources.DSA051MessagePublicProperties,
                DSA051Strategy.ProtectedProperties => Resources.DSA051MessageProtectedProperties,
                DSA051Strategy.Fields => Resources.DSA051MessageFields,
                DSA051Strategy.OtherProperties => Resources.DSA051MessageOtherProperties,
                _ => Resources.DSA051MessageNoMembers,
            };

            return string.Format(Resources.Culture, format, type.Name, ListNames(plan));
        }

        private static string ListNames(DSA051ToStringPlan plan)
        {
            var names = plan.Members.Take(MaxNamesInMessage).Select(member => member.Name).ToList();
            var text = string.Join(", ", names);
            return plan.Members.Count > MaxNamesInMessage ? text + " and " + (plan.Members.Count - MaxNamesInMessage) + " more" : text;
        }
    }
}
