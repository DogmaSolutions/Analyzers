using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA046, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA046Settings
    {
        internal const string ReportIncrementsKey = "dotnet_diagnostic.DSA046.report_increments";
        internal const string ReportCompoundAssignmentsKey = "dotnet_diagnostic.DSA046.report_compound_assignments";
        internal const string ReportSelfAssignmentsKey = "dotnet_diagnostic.DSA046.report_self_assignments";
        internal const string ReportInterlockedTypesKey = "dotnet_diagnostic.DSA046.report_interlocked_types";
        internal const string ReportOtherValueTypesKey = "dotnet_diagnostic.DSA046.report_other_value_types";
        internal const string ReportReferenceTypesKey = "dotnet_diagnostic.DSA046.report_reference_types";
        internal const string SkipInsideLockKey = "dotnet_diagnostic.DSA046.skip_inside_lock";
        internal const string SkipConstructorsKey = "dotnet_diagnostic.DSA046.skip_constructors";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA046.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA046.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA046.excluded_base_types";
        internal const string ExcludedFieldNamePatternsKey = "dotnet_diagnostic.DSA046.excluded_field_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA046.excluded_file_patterns";

        internal static readonly AnalyzerOptionsCache<DSA046Settings> Cache = new(static options => new DSA046Settings(options));

        internal DSA046Settings(AnalyzerConfigOptions options)
        {
            ReportIncrements = AnalyzerOptionsReader.ReadBool(options, ReportIncrementsKey, true);
            ReportCompoundAssignments = AnalyzerOptionsReader.ReadBool(options, ReportCompoundAssignmentsKey, true);
            ReportSelfAssignments = AnalyzerOptionsReader.ReadBool(options, ReportSelfAssignmentsKey, true);
            ReportInterlockedTypes = AnalyzerOptionsReader.ReadBool(options, ReportInterlockedTypesKey, true);
            ReportOtherValueTypes = AnalyzerOptionsReader.ReadBool(options, ReportOtherValueTypesKey, true);
            ReportReferenceTypes = AnalyzerOptionsReader.ReadBool(options, ReportReferenceTypesKey, true);
            SkipInsideLock = AnalyzerOptionsReader.ReadBool(options, SkipInsideLockKey, true);
            SkipConstructors = AnalyzerOptionsReader.ReadBool(options, SkipConstructorsKey, true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedFieldNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedFieldNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report <c>x++</c>, <c>++x</c>, <c>x--</c>, <c>--x</c>.</summary>
        internal bool ReportIncrements { get; }

        /// <summary>Report <c>x += v</c> and the other compound assignments.</summary>
        internal bool ReportCompoundAssignments { get; }

        /// <summary>Report <c>x = f(x)</c>: an assignment whose value is computed from the field itself.</summary>
        internal bool ReportSelfAssignments { get; }

        /// <summary>Report the fields <c>Interlocked</c> can work on (<c>int</c>, <c>uint</c>).</summary>
        internal bool ReportInterlockedTypes { get; }

        /// <summary>Report the other value types (<c>bool</c>, <c>byte</c>, <c>short</c>, <c>char</c>, <c>float</c>, enums...).</summary>
        internal bool ReportOtherValueTypes { get; }

        /// <summary>Report the volatile fields that hold a reference (delegates, strings...).</summary>
        internal bool ReportReferenceTypes { get; }

        /// <summary>Do not report the operations inside a <c>lock</c> statement: they may be atomic with respect to the other locked accesses.</summary>
        internal bool SkipInsideLock { get; }

        /// <summary>Do not report the operations in a constructor: nobody else can see the instance yet.</summary>
        internal bool SkipConstructors { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedFieldNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
