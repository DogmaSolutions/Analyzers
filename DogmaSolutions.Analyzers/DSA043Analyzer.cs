using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a complex class that neither injects nor uses a logger (<c>Microsoft.Extensions.Logging.ILogger</c> by default). A class
    /// that is large or branchy, and that catches, awaits, throws or calls into I/O, is where production incidents are diagnosed, and
    /// without any log its behaviour is invisible (CWE-778, insufficient logging). What "complex" means, which classes are looked at
    /// and what counts as a logger are all configurable, see the options of <see cref="DSA043Settings"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA043Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA043";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA043AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA043AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA043AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.BestPractice;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA043.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        }

        internal static void AnalyzeNamedType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (DSA043Eligibility.IsSourceType(type))
                AnalyzeSourceType(context, type);
        }

        private static void AnalyzeSourceType(SymbolAnalysisContext context, INamedTypeSymbol type)
        {
            var settings = DSA043Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(type.Locations[0].SourceTree));
            if (!DSA043Eligibility.IsCandidate(type, settings) || !LoggerDetector.IsLoggingAvailable(context.Compilation, settings))
                return;

            var metrics = DSA043TypeMetrics.Compute(type, settings.CountBlankLines, context.CancellationToken);
            if (!settings.IsComplexEnough(metrics) || !DSA043ActivityDetector.HasActivity(type, metrics, settings, context.Compilation, context.CancellationToken))
                return;

            if (LoggerDetector.DeclaresLogger(type, settings) || LoggerDetector.UsesLogger(type, context.Compilation, settings, context.CancellationToken))
                return;

            context.ReportDiagnostic(
                Diagnostic.Create(_rule, type.Locations[0], properties: null, type.Name, metrics.CyclomaticComplexity, metrics.Lines));
        }
    }
}
