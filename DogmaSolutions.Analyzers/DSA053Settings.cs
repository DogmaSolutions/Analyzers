#nullable disable
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The editorconfig options of DSA053, read once per options instance. An invalid value falls back to the documented default.</summary>
    internal sealed class DSA053Settings : ExistenceSettings
    {
        internal const string IncludedMethodsKey = "dotnet_diagnostic.DSA053.included_methods";
        internal const string ReportDirectComparisonsKey = "dotnet_diagnostic.DSA053.report_direct_comparisons";
        internal const string ReportAsyncPredicateComparisonsKey = "dotnet_diagnostic.DSA053.report_async_predicate_comparisons";
        internal const string ReportLocalsKey = "dotnet_diagnostic.DSA053.report_locals";
        internal const string ReportQueryableSourcesKey = "dotnet_diagnostic.DSA053.report_queryable_sources";
        internal const string ReportEnumerableSourcesKey = "dotnet_diagnostic.DSA053.report_enumerable_sources";
        internal const string ReportTypesWithCheapCountKey = "dotnet_diagnostic.DSA053.report_types_with_cheap_count";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA053.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA053.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA053.excluded_base_types";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA053.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA053.excluded_file_patterns";

        internal static readonly string[] DefaultMethods = { "Count", "LongCount", "CountAsync", "LongCountAsync" };

        internal static readonly AnalyzerOptionsCache<DSA053Settings> Cache = new(static options => new DSA053Settings(options));

        internal DSA053Settings(AnalyzerConfigOptions options)
            : base(options, DSA053Analyzer.DiagnosticId, DefaultMethods)
        {
            ReportDirectComparisons = AnalyzerOptionsReader.ReadBool(options, ReportDirectComparisonsKey, false);
            ReportAsyncPredicateComparisons = AnalyzerOptionsReader.ReadBool(options, ReportAsyncPredicateComparisonsKey, true);
            ReportTypesWithCheapCount = AnalyzerOptionsReader.ReadBool(options, ReportTypesWithCheapCountKey, false);
        }

        internal override bool IsCallReported(ExistenceShape shape, ExistenceCall call) =>
            (shape == ExistenceShape.Local || IsDirectComparisonReported(call)) && (ReportTypesWithCheapCount || !ExistenceAnalyzerCore.HasCheapCount(call.ReceiverType));

        // CA1827 reports the comparisons of Count() and LongCount(), CA1828 the ones of CountAsync() and LongCountAsync() without a predicate
        private bool IsDirectComparisonReported(ExistenceCall call) => ReportDirectComparisons || (call.IsAsync && call.HasPredicate && ReportAsyncPredicateComparisons);

        internal override bool IsShapeReported(ExistenceShape shape) => shape == ExistenceShape.Local ? ReportLocals : ReportDirectComparisons || ReportAsyncPredicateComparisons;

        /// <summary>
        /// Report every <c>q.Count() &gt; 0</c> compared right away, including the ones that CA1827 and CA1828 report already (default: only the ones
        /// they miss: <c>CountAsync(predicate)</c>, see <see cref="ReportAsyncPredicateComparisons"/>).
        /// </summary>
        internal bool ReportDirectComparisons { get; }

        /// <summary><c>await q.CountAsync(predicate) &gt; 0</c> compared right away: CA1828 does not report it.</summary>
        internal bool ReportAsyncPredicateComparisons { get; }

        /// <summary>
        /// Report the sequences that know their size without enumerating them (arrays, <c>ICollection</c>, <c>IReadOnlyCollection</c>): there the count
        /// is cheap and the <c>Count</c> / <c>Length</c> property is the right tool (CA1829, CA1860).
        /// </summary>
        internal bool ReportTypesWithCheapCount { get; }
    }
}
