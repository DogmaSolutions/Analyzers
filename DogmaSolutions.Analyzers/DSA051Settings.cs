using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The kinds of type DSA051 can look at (records have a generated ToString, and are never looked at).</summary>
    [Flags]
    internal enum DSA051TypeKinds
    {
        None = 0,
        Class = 1,
        Struct = 2,
        All = Class | Struct,
    }

    /// <summary>
    /// The editorconfig options of DSA051, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA051Settings
    {
        internal const string IncludedTypeKindsKey = "dotnet_diagnostic.DSA051.included_type_kinds";
        internal const string IncludeAbstractClassesKey = "dotnet_diagnostic.DSA051.include_abstract_classes";
        internal const string IncludeNestedTypesKey = "dotnet_diagnostic.DSA051.include_nested_types";
        internal const string MinDataMembersKey = "dotnet_diagnostic.DSA051.min_data_members";
        internal const string ConsiderInheritedOverridesKey = "dotnet_diagnostic.DSA051.consider_inherited_overrides";
        internal const string WellKnownNamesKey = "dotnet_diagnostic.DSA051.well_known_names";
        internal const string WellKnownIncludeKeyKey = "dotnet_diagnostic.DSA051.well_known_include_key";
        internal const string SensitiveNamePatternsKey = "dotnet_diagnostic.DSA051.sensitive_name_patterns";
        internal const string MaxMembersKey = "dotnet_diagnostic.DSA051.max_members";
        internal const string UseExpressionBodyKey = "dotnet_diagnostic.DSA051.use_expression_body";
        internal const string IgnoredAttributesKey = "dotnet_diagnostic.DSA051.ignored_attributes";
        internal const string SkipTestAssembliesKey = "dotnet_diagnostic.DSA051.skip_test_assemblies";
        internal const string TestAssemblyNamesKey = "dotnet_diagnostic.DSA051.test_assembly_names";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA051.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA051.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA051.excluded_base_types";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA051.excluded_file_patterns";

        private static readonly string[] DefaultIncludedTypeKinds = { "class" };

        private static readonly string[] DefaultWellKnownNames =
        {
            "Id", "Key", "Pk", "Name", "Value", "FriendlyName", "Description", "FirstName", "LastName", "Surname", "Email", "Address", "Url", "Range",
        };

        // a member whose name contains one of these (ignoring "_" and casing) is never written in the generated text: ToString ends up in logs
        private static readonly string[] DefaultSensitiveNamePatterns =
        {
            "password", "passwd", "pwd", "passphrase", "secret", "token", "apikey", "accesskey", "privatekey", "credential", "connectionstring", "cardnumber", "cvv",
        };

        private static readonly string[] DefaultIgnoredAttributes =
        {
            "System.CodeDom.Compiler.GeneratedCodeAttribute", "System.Runtime.CompilerServices.CompilerGeneratedAttribute", "System.Diagnostics.DebuggerDisplayAttribute",
        };

        private static readonly string[] DefaultTestAssemblyNames =
        {
            "xunit.core", "xunit.v3.core", "nunit.framework", "Microsoft.VisualStudio.TestPlatform.TestFramework", "MSTest.TestFramework", "TUnit.Core",
        };

        private static readonly string[] DefaultExcludedBaseTypes = { "System.Attribute" };

        internal static readonly AnalyzerOptionsCache<DSA051Settings> Cache = new(static options => new DSA051Settings(options));

        internal DSA051Settings(AnalyzerConfigOptions options)
        {
            IncludedTypeKinds = ParseTypeKinds(AnalyzerOptionsReader.ReadList(options, IncludedTypeKindsKey, DefaultIncludedTypeKinds));
            IncludeAbstractClasses = AnalyzerOptionsReader.ReadBool(options, IncludeAbstractClassesKey, true);
            IncludeNestedTypes = AnalyzerOptionsReader.ReadBool(options, IncludeNestedTypesKey, true);
            MinDataMembers = AnalyzerOptionsReader.ReadInt(options, MinDataMembersKey, 0, minInclusive: 0);
            ConsiderInheritedOverrides = AnalyzerOptionsReader.ReadBool(options, ConsiderInheritedOverridesKey, true);

            WellKnownNames = AnalyzerOptionsReader.ReadList(options, WellKnownNamesKey, DefaultWellKnownNames);
            WellKnownIncludeKey = AnalyzerOptionsReader.ReadBool(options, WellKnownIncludeKeyKey, false);
            SensitiveNamePatterns = AnalyzerOptionsReader.ReadList(options, SensitiveNamePatternsKey, DefaultSensitiveNamePatterns);
            NormalizedWellKnownNames = Array.ConvertAll(WellKnownNames, DSA051Planner.Normalize);
            NormalizedSensitiveNamePatterns = Array.ConvertAll(SensitiveNamePatterns, DSA051Planner.Normalize);
            MaxMembers = AnalyzerOptionsReader.ReadInt(options, MaxMembersKey, 0, minInclusive: 0);
            UseExpressionBody = AnalyzerOptionsReader.ReadBool(options, UseExpressionBodyKey, true);

            IgnoredAttributes = AnalyzerOptionsReader.ReadList(options, IgnoredAttributesKey, DefaultIgnoredAttributes);
            SkipTestAssemblies = AnalyzerOptionsReader.ReadBool(options, SkipTestAssembliesKey, true);
            TestAssemblyNames = AnalyzerOptionsReader.ReadList(options, TestAssemblyNamesKey, DefaultTestAssemblyNames);
            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, DefaultExcludedBaseTypes);
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>The kinds of type that are looked at.</summary>
        internal DSA051TypeKinds IncludedTypeKinds { get; }

        internal bool IncludeAbstractClasses { get; }

        internal bool IncludeNestedTypes { get; }

        /// <summary>The type must have at least this number of instance properties and fields (0 = also a type with none).</summary>
        internal int MinDataMembers { get; }

        /// <summary>A ToString declared by a base type (other than object) is the type's own: nothing to report.</summary>
        internal bool ConsiderInheritedOverrides { get; }

        /// <summary>The names that identify an instance, in order of priority (matched ignoring "_" and casing).</summary>
        internal string[] WellKnownNames { get; }

        /// <summary>The text generated for a well known member is <c>Name=`value`</c> and not only the value.</summary>
        internal bool WellKnownIncludeKey { get; }

        internal string[] SensitiveNamePatterns { get; }

        /// <summary>The well-known names and the sensitive patterns in the form they are compared in, worked out once.</summary>
        internal string[] NormalizedWellKnownNames { get; }

        internal string[] NormalizedSensitiveNamePatterns { get; }

        /// <summary>The most members the generated text lists (0 = all).</summary>
        internal int MaxMembers { get; }

        internal bool UseExpressionBody { get; }

        internal string[] IgnoredAttributes { get; }

        internal bool SkipTestAssemblies { get; }

        internal string[] TestAssemblyNames { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }

        private static DSA051TypeKinds ParseTypeKinds(string[] names)
        {
            var kinds = DSA051TypeKinds.None;
            foreach (var name in names)
            {
                if (Enum.TryParse(name, ignoreCase: true, out DSA051TypeKinds kind))
                    kinds |= kind;
            }

            return kinds == DSA051TypeKinds.None ? DSA051TypeKinds.Class : kinds;
        }
    }
}
