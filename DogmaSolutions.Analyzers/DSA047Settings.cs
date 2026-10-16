using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA047, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA047Settings
    {
        internal const string ReportSwappedLocksKey = "dotnet_diagnostic.DSA047.report_swapped_locks";
        internal const string ReportExposedLocksKey = "dotnet_diagnostic.DSA047.report_exposed_locks";
        internal const string ReportInternalLocksKey = "dotnet_diagnostic.DSA047.report_internal_locks";
        internal const string ReportFieldsKey = "dotnet_diagnostic.DSA047.report_fields";
        internal const string ReportPropertiesKey = "dotnet_diagnostic.DSA047.report_properties";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA047.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA047.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA047.excluded_base_types";
        internal const string ExcludedLockNamePatternsKey = "dotnet_diagnostic.DSA047.excluded_lock_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA047.excluded_file_patterns";

        internal static readonly AnalyzerOptionsCache<DSA047Settings> Cache = new(static options => new DSA047Settings(options));

        internal DSA047Settings(AnalyzerConfigOptions options)
        {
            ReportSwappedLocks = AnalyzerOptionsReader.ReadBool(options, ReportSwappedLocksKey, true);
            ReportExposedLocks = AnalyzerOptionsReader.ReadBool(options, ReportExposedLocksKey, true);
            ReportInternalLocks = AnalyzerOptionsReader.ReadBool(options, ReportInternalLocksKey, false);
            ReportFields = AnalyzerOptionsReader.ReadBool(options, ReportFieldsKey, true);
            ReportProperties = AnalyzerOptionsReader.ReadBool(options, ReportPropertiesKey, true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedLockNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedLockNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report the lock objects that are reassigned after construction.</summary>
        internal bool ReportSwappedLocks { get; }

        /// <summary>Report the lock objects that code outside of the class can reach (and lock on).</summary>
        internal bool ReportExposedLocks { get; }

        /// <summary>Consider <c>internal</c> as outside of the class too (by default only <c>public</c> and <c>protected</c> are).</summary>
        internal bool ReportInternalLocks { get; }

        /// <summary>Report the lock objects that are fields.</summary>
        internal bool ReportFields { get; }

        /// <summary>Report the lock objects that are auto-properties.</summary>
        internal bool ReportProperties { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedLockNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
