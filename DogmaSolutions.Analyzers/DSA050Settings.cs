using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA050, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA050Settings
    {
        internal const string ReportSetOnceKey = "dotnet_diagnostic.DSA050.report_set_once";
        internal const string ReportConsumeKey = "dotnet_diagnostic.DSA050.report_consume";
        internal const string ReportTransitionsKey = "dotnet_diagnostic.DSA050.report_state_transitions";
        internal const string ReportVolatileFieldsKey = "dotnet_diagnostic.DSA050.report_volatile_fields";
        internal const string SkipInsideLockKey = "dotnet_diagnostic.DSA050.skip_inside_lock";
        internal const string SkipConstructorsKey = "dotnet_diagnostic.DSA050.skip_constructors";
        internal const string RequireSharedAccessKey = "dotnet_diagnostic.DSA050.require_shared_access";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA050.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA050.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA050.excluded_base_types";
        internal const string ExcludedFieldNamePatternsKey = "dotnet_diagnostic.DSA050.excluded_field_name_patterns";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA050.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA050.excluded_file_patterns";

        internal static readonly AnalyzerOptionsCache<DSA050Settings> Cache = new(static options => new DSA050Settings(options));

        internal DSA050Settings(AnalyzerConfigOptions options)
        {
            ReportSetOnce = AnalyzerOptionsReader.ReadBool(options, ReportSetOnceKey, true);
            ReportConsume = AnalyzerOptionsReader.ReadBool(options, ReportConsumeKey, true);
            ReportStateTransitions = AnalyzerOptionsReader.ReadBool(options, ReportTransitionsKey, true);
            ReportVolatileFields = AnalyzerOptionsReader.ReadBool(options, ReportVolatileFieldsKey, true);
            SkipInsideLock = AnalyzerOptionsReader.ReadBool(options, SkipInsideLockKey, true);
            SkipConstructors = AnalyzerOptionsReader.ReadBool(options, SkipConstructorsKey, true);
            RequireSharedAccess = AnalyzerOptionsReader.ReadBool(options, RequireSharedAccessKey, true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedFieldNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedFieldNamePatternsKey, Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report <c>if (!flag) { flag = true; ... }</c>: a flag that claims a "once only" step.</summary>
        internal bool ReportSetOnce { get; }

        /// <summary>Report <c>if (flag) { flag = false; ... }</c>: a flag that is consumed.</summary>
        internal bool ReportConsume { get; }

        /// <summary>Report <c>if (state == A) { state = B; }</c>: a state transition of an enum or an integer.</summary>
        internal bool ReportStateTransitions { get; }

        /// <summary>Report the <c>volatile</c> fields too (the keyword does not make the test and the change atomic).</summary>
        internal bool ReportVolatileFields { get; }

        /// <summary>Do not report the code inside a <c>lock</c> statement.</summary>
        internal bool SkipInsideLock { get; }

        /// <summary>Do not report the code of a constructor.</summary>
        internal bool SkipConstructors { get; }

        /// <summary>Report a private member only when the field is also used by another member, or is static or volatile (a public member can be called by any thread).</summary>
        internal bool RequireSharedAccess { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedFieldNamePatterns { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
