#nullable disable
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The editorconfig options of DSA052, read once per options instance. An invalid value falls back to the documented default.</summary>
    internal sealed class DSA052Settings : ExistenceSettings
    {
        internal const string IncludedMethodsKey = "dotnet_diagnostic.DSA052.included_methods";
        internal const string ReportDirectComparisonsKey = "dotnet_diagnostic.DSA052.report_direct_comparisons";
        internal const string ReportLocalsKey = "dotnet_diagnostic.DSA052.report_locals";
        internal const string ReportQueryableSourcesKey = "dotnet_diagnostic.DSA052.report_queryable_sources";
        internal const string ReportEnumerableSourcesKey = "dotnet_diagnostic.DSA052.report_enumerable_sources";
        internal const string SkipPossiblyNullElementsKey = "dotnet_diagnostic.DSA052.skip_possibly_null_elements";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA052.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA052.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA052.excluded_base_types";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA052.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA052.excluded_file_patterns";

        internal static readonly string[] DefaultMethods = { "FirstOrDefault", "FirstOrDefaultAsync", "SingleOrDefault", "SingleOrDefaultAsync" };

        internal static readonly AnalyzerOptionsCache<DSA052Settings> Cache = new(static options => new DSA052Settings(options));

        internal DSA052Settings(AnalyzerConfigOptions options)
            : base(options, DSA052Analyzer.DiagnosticId, DefaultMethods)
        {
            ReportDirectComparisons = AnalyzerOptionsReader.ReadBool(options, ReportDirectComparisonsKey, true);
            SkipPossiblyNullElements = AnalyzerOptionsReader.ReadBool(options, SkipPossiblyNullElementsKey, true);
        }

        /// <summary>Report <c>q.FirstOrDefault(p) != null</c>, compared right away.</summary>
        internal bool ReportDirectComparisons { get; }

        /// <summary>
        /// Do not report a sequence whose elements can be null (<c>string</c>, <c>object</c>, <c>T?</c>): there <c>FirstOrDefault() != null</c> and
        /// <c>Any()</c> are not the same question.
        /// </summary>
        internal bool SkipPossiblyNullElements { get; }
    }
}
