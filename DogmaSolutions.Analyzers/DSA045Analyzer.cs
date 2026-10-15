using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a pure data carrier (properties and no logic, no explicit constructor, base types included) that is not excluded from the code
    /// coverage: there is nothing to cover in it, so measuring it only inflates (or distorts) the figures. The code fix adds
    /// <c>[ExcludeFromCodeCoverage]</c>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA045Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA045";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA045AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA045AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA045AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.BestPractice;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA045.md");

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
            if (location == null || type.IsStatic || GetKind(type) == DSA045TypeKinds.None)
                return;

            var settings = DSA045Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(location.SourceTree));
            if (!IsIncluded(type, settings) || DSA045Exclusions.IsExcluded(type, location.SourceTree, settings, context.Compilation, referencedAssemblies))
                return;

            var verdict = DSA045DtoDetector.Evaluate(type, settings, context.CancellationToken);
            if (verdict.IsPure)
                context.ReportDiagnostic(Diagnostic.Create(_rule, location, GetKindName(type), type.Name, DescribeMembers(verdict.DataMembers), verdict.HasBaseTypes ? ", base types included" : string.Empty));
        }

        // ---- the kind of type -----------------------------------------------------------------------------------------------

        internal static DSA045TypeKinds GetKind(INamedTypeSymbol type)
        {
            switch (type.TypeKind)
            {
                case TypeKind.Class:
                    return type.IsRecord ? DSA045TypeKinds.Record : DSA045TypeKinds.Class;
                case TypeKind.Struct:
                    return type.IsRecord ? DSA045TypeKinds.RecordStruct : DSA045TypeKinds.Struct;
                default:
                    return DSA045TypeKinds.None;
            }
        }

        private static string GetKindName(INamedTypeSymbol type)
        {
            switch (GetKind(type))
            {
                case DSA045TypeKinds.Record:
                    return "record";
                case DSA045TypeKinds.RecordStruct:
                    return "record struct";
                case DSA045TypeKinds.Struct:
                    return "struct";
                default:
                    return "class";
            }
        }

        private static bool IsIncluded(INamedTypeSymbol type, DSA045Settings settings) =>
            (settings.IncludedTypeKinds & GetKind(type)) != 0 && (settings.IncludeAbstractClasses || !type.IsAbstract) && (settings.IncludeNestedTypes || type.ContainingType == null);

        private static string DescribeMembers(int count) => count == 1 ? "1 data member" : count + " data members";
    }
}
