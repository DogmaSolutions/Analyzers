using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA054, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA054Settings
    {
        internal const string ReportNeverReadKey = "dotnet_diagnostic.DSA054.report_never_read";
        internal const string ReportStaticFieldsKey = "dotnet_diagnostic.DSA054.report_static_fields";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA054.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA054.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA054.excluded_base_types";
        internal const string ExcludedFieldNamePatternsKey = "dotnet_diagnostic.DSA054.excluded_field_name_patterns";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA054.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA054.excluded_file_patterns";

        internal static readonly AnalyzerOptionsCache<DSA054Settings> Cache = new(static options => new DSA054Settings(options));

        internal DSA054Settings(AnalyzerConfigOptions options)
        {
            ReportNeverRead = AnalyzerOptionsReader.ReadBool(options, ReportNeverReadKey, false);
            ReportStaticFields = AnalyzerOptionsReader.ReadBool(options, ReportStaticFieldsKey, true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedFieldNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedFieldNamePatternsKey, Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report the private fields that are written but never read at all (the compiler and the IDE already report most of them).</summary>
        internal bool ReportNeverRead { get; }

        /// <summary>Report the static fields too.</summary>
        internal bool ReportStaticFields { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedFieldNamePatterns { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
