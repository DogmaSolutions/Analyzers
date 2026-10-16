using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA048, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA048Settings
    {
        internal const string ReportLoopsWithTimeoutKey = "dotnet_diagnostic.DSA048.report_loops_with_timeout";
        internal const string IncludeTaskDelayKey = "dotnet_diagnostic.DSA048.include_task_delay";
        internal const string IncludeDoLoopsKey = "dotnet_diagnostic.DSA048.include_do_loops";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA048.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA048.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA048.excluded_base_types";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA048.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA048.excluded_file_patterns";

        internal static readonly AnalyzerOptionsCache<DSA048Settings> Cache = new(static options => new DSA048Settings(options));

        internal DSA048Settings(AnalyzerConfigOptions options)
        {
            ReportLoopsWithTimeout = AnalyzerOptionsReader.ReadBool(options, ReportLoopsWithTimeoutKey, true);
            IncludeTaskDelay = AnalyzerOptionsReader.ReadBool(options, IncludeTaskDelayKey, true);
            IncludeDoLoops = AnalyzerOptionsReader.ReadBool(options, IncludeDoLoopsKey, true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report the polling loops that give up after a timeout or a cancellation too (they still poll).</summary>
        internal bool ReportLoopsWithTimeout { get; }

        /// <summary>Report the loops that wait with <c>Task.Delay</c> as well as the ones that wait with <c>Thread.Sleep</c>.</summary>
        internal bool IncludeTaskDelay { get; }

        /// <summary>Report <c>do ... while</c> loops as well as <c>while</c> loops.</summary>
        internal bool IncludeDoLoops { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
