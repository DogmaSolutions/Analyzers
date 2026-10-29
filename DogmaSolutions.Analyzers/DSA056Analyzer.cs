#nullable disable
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
    /// Flags a test fixture (a class with test attributes of NUnit, xUnit, MSTest or TUnit, on the class or on a method) that is not excluded from the
    /// code coverage: its own lines are counted, they inflate the percentage and hide the untested code of the product. The rule does not even
    /// start in a compilation that references no test framework, or whose assembly is excluded as a whole.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA056Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA056";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA056AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA056AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA056AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA056.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterCompilationStartAction(start =>
            {
                // no test framework, no test fixture; an assembly that is excluded as a whole excludes every fixture
                if (!ReferencesTestFramework(start.Compilation) || IsAssemblyExcluded(start.Compilation))
                    return;

                start.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
            });
        }

        private static bool ReferencesTestFramework(Compilation compilation) =>
            compilation.ReferencedAssemblyNames.Any(static name => Array.IndexOf(DSA056Settings.TestFrameworkAssemblies, name.Name) >= 0);

        // the option can differ from a file to another, the default attribute is what an assembly-level attribute is looked for with
        private static bool IsAssemblyExcluded(Compilation compilation) =>
            compilation.Assembly.GetAttributes().Any(static attribute => IsNamed(attribute.AttributeClass, DefaultExclusion));

        private static readonly string[] DefaultExclusion = { "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" };

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            var location = type.Locations.FirstOrDefault(static candidate => candidate.IsInSource);
            if (location == null || type.TypeKind != TypeKind.Class || type.IsStatic || type.IsImplicitlyDeclared)
                return;

            var tree = location.SourceTree;
            var settings = DSA056Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree));
            if (!IsFixture(type, settings) || IsExcludedFromCoverage(type, settings) || IsExcludedByOptions(type, tree, settings))
                return;

            var message = string.Format(Resources.Culture, Resources.DSA056Message, type.Name);
            context.ReportDiagnostic(Diagnostic.Create(_rule, GetIdentifierLocation(type, location), message));
        }

        /// <summary>The identifier of the first declaration, or the declaration when the identifier is missing in a broken code.</summary>
        private static Location GetIdentifierLocation(INamedTypeSymbol type, Location location) =>
            location.SourceTree.GetRoot().FindNode(location.SourceSpan) is BaseTypeDeclarationSyntax declaration && !declaration.Identifier.IsMissing ? declaration.Identifier.GetLocation() : location;

        // ---- the fixture ------------------------------------------------------------------------------------------------------

        /// <summary>The class, or one of its methods, has an attribute of a test framework.</summary>
        private static bool IsFixture(INamedTypeSymbol type, DSA056Settings settings) =>
            HasTestAttribute(type.GetAttributes(), settings) || type.GetMembers().OfType<IMethodSymbol>().Any(method => HasTestAttribute(method.GetAttributes(), settings));

        private static bool HasTestAttribute(ImmutableArray<AttributeData> attributes, DSA056Settings settings) =>
            attributes.Any(attribute => IsNamed(attribute.AttributeClass, settings.TestAttributes));

        // ---- the coverage -----------------------------------------------------------------------------------------------------

        /// <summary>The attribute of the type itself (any part of it): the attribute is not inherited, and it is not applied to the nested types of a type.</summary>
        private static bool IsExcludedFromCoverage(INamedTypeSymbol type, DSA056Settings settings) =>
            type.GetAttributes().Any(attribute => IsNamed(attribute.AttributeClass, settings.ExclusionAttributes));

        private const string AttributeSuffix = "Attribute";

        /// <summary>Whether the attribute class, or a class it derives from, is one of the names (with or without the <c>Attribute</c> suffix).</summary>
        private static bool IsNamed(INamedTypeSymbol attributeClass, string[] names)
        {
            for (var current = attributeClass; current != null; current = current.BaseType)
            {
                var fullName = SymbolNames.GetFullName(current);
                foreach (var name in names)
                {
                    if (string.Equals(fullName, name, StringComparison.Ordinal) || (fullName.Length == name.Length + AttributeSuffix.Length && fullName.StartsWith(name, StringComparison.Ordinal)))
                        return true;
                }
            }

            return false;
        }

        // ---- the exclusions ---------------------------------------------------------------------------------------------------

        private static bool IsExcludedByOptions(INamedTypeSymbol type, SyntaxTree tree, DSA056Settings settings) =>
            AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns) ||
            Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
            (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
            (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
