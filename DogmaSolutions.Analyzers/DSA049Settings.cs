using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA049, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA049Settings
    {
        internal const string ReportUnlockedWritesKey = "dotnet_diagnostic.DSA049.report_unlocked_writes";
        internal const string ReportUnlockedReadsKey = "dotnet_diagnostic.DSA049.report_unlocked_reads";
        internal const string ReportDifferentLocksKey = "dotnet_diagnostic.DSA049.report_different_locks";
        internal const string InferLocksOfPrivateHelpersKey = "dotnet_diagnostic.DSA049.infer_locks_of_private_helpers";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA049.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA049.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA049.excluded_base_types";
        internal const string ExcludedFieldNamePatternsKey = "dotnet_diagnostic.DSA049.excluded_field_name_patterns";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA049.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA049.excluded_file_patterns";

        internal static readonly AnalyzerOptionsCache<DSA049Settings> Cache = new(static options => new DSA049Settings(options));

        internal DSA049Settings(AnalyzerConfigOptions options)
        {
            ReportUnlockedWrites = AnalyzerOptionsReader.ReadBool(options, ReportUnlockedWritesKey, true);
            ReportUnlockedReads = AnalyzerOptionsReader.ReadBool(options, ReportUnlockedReadsKey, true);
            ReportDifferentLocks = AnalyzerOptionsReader.ReadBool(options, ReportDifferentLocksKey, true);
            InferLocksOfPrivateHelpers = AnalyzerOptionsReader.ReadBool(options, InferLocksOfPrivateHelpersKey, true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedFieldNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedFieldNamePatternsKey, Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report the writes of a field that is written under a lock elsewhere, and that hold no lock.</summary>
        internal bool ReportUnlockedWrites { get; }

        /// <summary>Report the reads of a field that is written under a lock, and that hold no lock.</summary>
        internal bool ReportUnlockedReads { get; }

        /// <summary>Report the writes of a field that is written under another lock elsewhere.</summary>
        internal bool ReportDifferentLocks { get; }

        /// <summary>A private method is considered to hold the locks held by all of its callers.</summary>
        internal bool InferLocksOfPrivateHelpers { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedFieldNamePatterns { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
