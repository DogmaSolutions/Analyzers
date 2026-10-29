using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA055, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA055Settings
    {
        internal const string ReportEqualityKey = "dotnet_diagnostic.DSA055.report_equality";
        internal const string ReportMembersKey = "dotnet_diagnostic.DSA055.report_members";
        internal const string ReportUnsignedTypesKey = "dotnet_diagnostic.DSA055.report_unsigned_types";
        internal const string KeyNameSuffixesKey = "dotnet_diagnostic.DSA055.key_name_suffixes";
        internal const string KeyExactNamesKey = "dotnet_diagnostic.DSA055.key_exact_names";
        internal const string ExcludedNamePatternsKey = "dotnet_diagnostic.DSA055.excluded_name_patterns";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA055.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA055.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA055.excluded_base_types";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA055.excluded_member_name_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA055.excluded_file_patterns";

        private static readonly string[] DefaultSuffixes = { "Id", "Key" };
        private static readonly string[] DefaultExactNames = { "id", "key", "Id", "Key", "Pk", "pk", "primaryKey" };

        internal static readonly AnalyzerOptionsCache<DSA055Settings> Cache = new(static options => new DSA055Settings(options));

        internal DSA055Settings(AnalyzerConfigOptions options)
        {
            ReportEquality = AnalyzerOptionsReader.ReadBool(options, ReportEqualityKey, true);
            ReportMembers = AnalyzerOptionsReader.ReadBool(options, ReportMembersKey, true);
            ReportUnsignedTypes = AnalyzerOptionsReader.ReadBool(options, ReportUnsignedTypesKey, true);
            KeyNameSuffixes = AnalyzerOptionsReader.ReadList(options, KeyNameSuffixesKey, DefaultSuffixes);
            KeyExactNames = AnalyzerOptionsReader.ReadList(options, KeyExactNamesKey, DefaultExactNames);

            ExcludedNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamePatternsKey, Array.Empty<string>());
            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Report <c>== 0</c> and <c>is 0</c> too (a valid key is then expected to be <c>&gt; 0</c>, an invalid one <c>&lt;= 0</c>).</summary>
        internal bool ReportEquality { get; }

        /// <summary>Report the fields and the properties (<c>order.CustomerId != 0</c>), not only the locals and the parameters.</summary>
        internal bool ReportMembers { get; }

        /// <summary>Report the unsigned types too: there <c>!= 0</c> and <c>&gt; 0</c> are the same, but only the second one says what the code means.</summary>
        internal bool ReportUnsignedTypes { get; }

        /// <summary>The names that end with one of these are keys (case sensitive).</summary>
        internal string[] KeyNameSuffixes { get; }

        /// <summary>The names that are keys (case sensitive).</summary>
        internal string[] KeyExactNames { get; }

        internal string[] ExcludedNamePatterns { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }

        /// <summary>Whether a name is the name of a key: one of the exact names, or one that ends with one of the suffixes.</summary>
        internal bool IsKeyName(string name)
        {
            foreach (var exact in KeyExactNames)
            {
                if (string.Equals(name, exact, StringComparison.Ordinal))
                    return true;
            }

            foreach (var suffix in KeyNameSuffixes)
            {
                if (name.EndsWith(suffix, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }
}
