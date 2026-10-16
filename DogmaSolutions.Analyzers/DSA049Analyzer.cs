using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a field that is written under a lock in some members and accessed without that lock in others. A lock excludes only the code that takes
    /// it: an unlocked write can run at the same time as the locked ones, and an unlocked read can see a stale or half-updated value. Every scenario
    /// (unlocked write, unlocked read, write under a different lock) has its own message.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA049Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA049";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA049AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA049AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA049AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA049.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.DeclaringSyntaxReferences.IsEmpty)
                return;

            var tree = type.DeclaringSyntaxReferences[0].SyntaxTree;
            var settings = DSA049Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree));
            if (IsExcluded(type, settings) || AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns))
                return;

            var map = new DSA049LockMap(type, context.Compilation, settings, context.CancellationToken);
            map.Build();
            if (!map.HasLocks)
                return;

            foreach (var group in map.Accesses.GroupBy(static access => access.Field.Name))
                ReportField(group.ToList(), map, settings, context);
        }

        private static void ReportField(System.Collections.Generic.List<DSA049Access> accesses, DSA049LockMap map, DSA049Settings settings, SymbolAnalysisContext context)
        {
            var field = accesses[0].Field;
            var guardedWrites = accesses.Where(static access => access.IsWrite && access.HeldLocks.Count > 0).ToList();
            if (guardedWrites.Count == 0 || map.IsLockFree(field) || Matches(field.Name, settings.ExcludedFieldNamePatterns))
                return;

            // the lock most of the writes hold is the one that guards the field
            var guard = guardedWrites.SelectMany(static access => access.HeldLocks).GroupBy(static name => name).OrderByDescending(static g => g.Count()).ThenBy(static g => g.Key, StringComparer.Ordinal).First().Key;
            var example = guardedWrites.First(access => access.HeldLocks.Contains(guard)).MemberName;

            foreach (var access in accesses.Where(access => !access.HeldLocks.Contains(guard) && !Matches(access.MemberName, settings.ExcludedMemberNamePatterns)))
            {
                var resource = GetResourceName(access, field, settings);
                if (resource == null)
                    continue;

                var held = access.HeldLocks.OrderBy(static name => name, StringComparer.Ordinal).FirstOrDefault() ?? string.Empty;
                var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(resource, Resources.Culture), field.Name, guard, example, held);
                context.ReportDiagnostic(Diagnostic.Create(_rule, access.Name.GetLocation(), message));
            }
        }

        private static string GetResourceName(DSA049Access access, IFieldSymbol field, DSA049Settings settings)
        {
            if (access.IsWrite)
            {
                if (access.HeldLocks.Count == 0)
                    return settings.ReportUnlockedWrites ? nameof(Resources.DSA049MessageUnlockedWrite) : null;

                return settings.ReportDifferentLocks ? nameof(Resources.DSA049MessageDifferentLocks) : null;
            }

            // a read of a volatile field is visible; a read under another lock is not this rule's business
            return settings.ReportUnlockedReads && !field.IsVolatile && access.HeldLocks.Count == 0 ? nameof(Resources.DSA049MessageUnlockedRead) : null;
        }

        private static bool IsExcluded(INamedTypeSymbol type, DSA049Settings settings) =>
            Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
            (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
            (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
