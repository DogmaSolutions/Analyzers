using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The kinds of member whose body can be the target of DSA010.</summary>
    [Flags]
    internal enum DSA010MemberKinds
    {
        None = 0,
        Accessor = 1,
        Method = 2,
        LocalFunction = 4,
        Lambda = 8,
        Constructor = 16,
        Other = 32,
    }

    /// <summary>
    /// The editorconfig options of DSA010, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA010Settings
    {
        internal const string ReportReadsKey = "dotnet_diagnostic.DSA010.report_reads";
        internal const string ReportWritesKey = "dotnet_diagnostic.DSA010.report_writes";
        internal const string ReportSharedLocksKey = "dotnet_diagnostic.DSA010.report_shared_locks";
        internal const string ReportLockOnThisKey = "dotnet_diagnostic.DSA010.report_lock_on_this";
        internal const string ReportTearableTypesKey = "dotnet_diagnostic.DSA010.report_tearable_types";
        internal const string ReportVolatileFieldsKey = "dotnet_diagnostic.DSA010.report_volatile_fields";
        internal const string ReportReadonlyFieldsKey = "dotnet_diagnostic.DSA010.report_readonly_fields";
        internal const string SkipSignaledLocksKey = "dotnet_diagnostic.DSA010.skip_signaled_locks";
        internal const string BodyShapeKey = "dotnet_diagnostic.DSA010.body_shape";
        internal const string MemberKindsKey = "dotnet_diagnostic.DSA010.member_kinds";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA010.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA010.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA010.excluded_base_types";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA010.excluded_member_name_patterns";
        internal const string ExcludedFieldNamePatternsKey = "dotnet_diagnostic.DSA010.excluded_field_name_patterns";
        internal const string ExcludedLockNamePatternsKey = "dotnet_diagnostic.DSA010.excluded_lock_name_patterns";
        internal const string ExcludedFieldTypesKey = "dotnet_diagnostic.DSA010.excluded_field_types";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA010.excluded_file_patterns";

        internal const string SoleStatementShape = "sole_statement";
        internal const string AnyStatementShape = "any_statement";

        internal static readonly AnalyzerOptionsCache<DSA010Settings> Cache = new(static options => new DSA010Settings(options));

        internal DSA010Settings(AnalyzerConfigOptions options)
        {
            ReportReads = AnalyzerOptionsReader.ReadBool(options, ReportReadsKey, true);
            ReportWrites = AnalyzerOptionsReader.ReadBool(options, ReportWritesKey, true);
            ReportSharedLocks = AnalyzerOptionsReader.ReadBool(options, ReportSharedLocksKey, true);
            ReportLockOnThis = AnalyzerOptionsReader.ReadBool(options, ReportLockOnThisKey, false);
            ReportTearableTypes = AnalyzerOptionsReader.ReadBool(options, ReportTearableTypesKey, true);
            ReportVolatileFields = AnalyzerOptionsReader.ReadBool(options, ReportVolatileFieldsKey, true);
            ReportReadonlyFields = AnalyzerOptionsReader.ReadBool(options, ReportReadonlyFieldsKey, true);
            SkipSignaledLocks = AnalyzerOptionsReader.ReadBool(options, SkipSignaledLocksKey, true);
            SoleStatementOnly = !string.Equals(AnalyzerOptionsReader.ReadList(options, BodyShapeKey, Array.Empty<string>()).FirstOrDefault(), AnyStatementShape, StringComparison.OrdinalIgnoreCase);
            MemberKinds = ParseMemberKinds(AnalyzerOptionsReader.ReadList(options, MemberKindsKey, Array.Empty<string>()));

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, Array.Empty<string>());
            ExcludedFieldNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedFieldNamePatternsKey, Array.Empty<string>());
            ExcludedLockNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedLockNamePatternsKey, Array.Empty<string>());
            ExcludedFieldTypes = AnalyzerOptionsReader.ReadList(options, ExcludedFieldTypesKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report the critical sections that read a field.</summary>
        internal bool ReportReads { get; }

        /// <summary>Report the critical sections that assign a field.</summary>
        internal bool ReportWrites { get; }

        /// <summary>Report the locks that other, longer critical sections take too.</summary>
        internal bool ReportSharedLocks { get; }

        /// <summary>Report <c>lock (this)</c>.</summary>
        internal bool ReportLockOnThis { get; }

        /// <summary>Report the fields a lock may keep from tearing (<c>long</c>, <c>double</c>, <c>decimal</c>, structs...).</summary>
        internal bool ReportTearableTypes { get; }

        internal bool ReportVolatileFields { get; }

        internal bool ReportReadonlyFields { get; }

        /// <summary>Do not report a lock that is used with <c>Monitor.Pulse</c> / <c>PulseAll</c>: it is part of a signaling protocol.</summary>
        internal bool SkipSignaledLocks { get; }

        /// <summary>The critical section must be the only statement of the member (otherwise any critical section is looked at).</summary>
        internal bool SoleStatementOnly { get; }

        internal DSA010MemberKinds MemberKinds { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal string[] ExcludedFieldNamePatterns { get; }

        internal string[] ExcludedLockNamePatterns { get; }

        internal string[] ExcludedFieldTypes { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }

        // ---- parsing ----------------------------------------------------------------------------------------------

        private static DSA010MemberKinds ParseMemberKinds(string[] names)
        {
            var kinds = DSA010MemberKinds.None;
            foreach (var name in names)
            {
                if (Enum.TryParse(name.Replace("_", string.Empty), ignoreCase: true, out DSA010MemberKinds kind))
                    kinds |= kind;
            }

            return kinds == DSA010MemberKinds.None ? DSA010MemberKinds.Accessor | DSA010MemberKinds.Method : kinds;
        }
    }
}
