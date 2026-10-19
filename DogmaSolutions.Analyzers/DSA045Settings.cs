using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The kinds of type DSA045 can look at.</summary>
    [Flags]
    internal enum DSA045TypeKinds
    {
        None = 0,
        Class = 1,
        Struct = 2,
        Record = 4,
        RecordStruct = 8,
        All = Class | Struct | Record | RecordStruct,
    }

    /// <summary>
    /// The editorconfig options of DSA045, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA045Settings
    {
        internal const string IncludedTypeKindsKey = "dotnet_diagnostic.DSA045.included_type_kinds";
        internal const string IncludeAbstractClassesKey = "dotnet_diagnostic.DSA045.include_abstract_classes";
        internal const string IncludeNestedTypesKey = "dotnet_diagnostic.DSA045.include_nested_types";
        internal const string MinDataMembersKey = "dotnet_diagnostic.DSA045.min_data_members";
        internal const string AllowFieldsKey = "dotnet_diagnostic.DSA045.allow_fields";
        internal const string AllowPropertyInitializersKey = "dotnet_diagnostic.DSA045.allow_property_initializers";
        internal const string AllowBackingFieldPropertiesKey = "dotnet_diagnostic.DSA045.allow_backing_field_properties";
        internal const string AllowComputedPropertiesKey = "dotnet_diagnostic.DSA045.allow_computed_properties";
        internal const string AllowParameterlessConstructorKey = "dotnet_diagnostic.DSA045.allow_parameterless_constructor";
        internal const string AllowNestedTypesKey = "dotnet_diagnostic.DSA045.allow_nested_types";
        internal const string InspectBaseTypesKey = "dotnet_diagnostic.DSA045.inspect_base_types";
        internal const string ReportExcludedTypesWithLogicKey = "dotnet_diagnostic.DSA045.report_excluded_types_with_logic";
        internal const string ExclusionAttributesKey = "dotnet_diagnostic.DSA045.exclusion_attributes";
        internal const string IgnoredAttributesKey = "dotnet_diagnostic.DSA045.ignored_attributes";
        internal const string SkipTestAssembliesKey = "dotnet_diagnostic.DSA045.skip_test_assemblies";
        internal const string TestAssemblyNamesKey = "dotnet_diagnostic.DSA045.test_assembly_names";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA045.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA045.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA045.excluded_base_types";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA045.excluded_file_patterns";
        internal const string CodeFixJustificationKey = "dotnet_diagnostic.DSA045.code_fix_justification";

        internal const string ExcludeFromCodeCoverageType = "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute";

        private static readonly string[] DefaultIncludedTypeKinds = { "class", "struct", "record", "record_struct" };
        private static readonly string[] DefaultExclusionAttributes = { ExcludeFromCodeCoverageType };
        private static readonly string[] DefaultIgnoredAttributes = { "System.CodeDom.Compiler.GeneratedCodeAttribute", "System.Runtime.CompilerServices.CompilerGeneratedAttribute" };

        private static readonly string[] DefaultTestAssemblyNames =
        {
            "xunit.core", "xunit.v3.core", "nunit.framework", "Microsoft.VisualStudio.TestPlatform.TestFramework", "MSTest.TestFramework", "TUnit.Core",
        };

        internal static readonly AnalyzerOptionsCache<DSA045Settings> Cache = new(static options => new DSA045Settings(options));

        internal DSA045Settings(AnalyzerConfigOptions options)
        {
            IncludedTypeKinds = ParseTypeKinds(AnalyzerOptionsReader.ReadList(options, IncludedTypeKindsKey, DefaultIncludedTypeKinds));
            IncludeAbstractClasses = AnalyzerOptionsReader.ReadBool(options, IncludeAbstractClassesKey, true);
            IncludeNestedTypes = AnalyzerOptionsReader.ReadBool(options, IncludeNestedTypesKey, true);
            MinDataMembers = AnalyzerOptionsReader.ReadInt(options, MinDataMembersKey, 1, minInclusive: 0);
            AllowFields = AnalyzerOptionsReader.ReadBool(options, AllowFieldsKey, true);
            AllowPropertyInitializers = AnalyzerOptionsReader.ReadBool(options, AllowPropertyInitializersKey, true);
            AllowBackingFieldProperties = AnalyzerOptionsReader.ReadBool(options, AllowBackingFieldPropertiesKey, true);
            AllowComputedProperties = AnalyzerOptionsReader.ReadBool(options, AllowComputedPropertiesKey, false);
            AllowParameterlessConstructor = AnalyzerOptionsReader.ReadBool(options, AllowParameterlessConstructorKey, false);
            AllowNestedTypes = AnalyzerOptionsReader.ReadBool(options, AllowNestedTypesKey, false);
            InspectBaseTypes = AnalyzerOptionsReader.ReadBool(options, InspectBaseTypesKey, true);
            ReportExcludedTypesWithLogic = AnalyzerOptionsReader.ReadBool(options, ReportExcludedTypesWithLogicKey, true);

            ExclusionAttributes = AnalyzerOptionsReader.ReadList(options, ExclusionAttributesKey, DefaultExclusionAttributes);
            IgnoredAttributes = AnalyzerOptionsReader.ReadList(options, IgnoredAttributesKey, DefaultIgnoredAttributes);
            SkipTestAssemblies = AnalyzerOptionsReader.ReadBool(options, SkipTestAssembliesKey, true);
            TestAssemblyNames = AnalyzerOptionsReader.ReadList(options, TestAssemblyNamesKey, DefaultTestAssemblyNames);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
            CodeFixJustification = ReadJustification(options);
        }

        /// <summary>The kinds of type that are looked at.</summary>
        internal DSA045TypeKinds IncludedTypeKinds { get; }

        internal bool IncludeAbstractClasses { get; }

        internal bool IncludeNestedTypes { get; }

        /// <summary>The type (base types included) must have at least this number of properties and fields (0 = also an empty type).</summary>
        internal int MinDataMembers { get; }

        /// <summary>Instance fields are data, as properties are.</summary>
        internal bool AllowFields { get; }

        /// <summary>Property and field initializers made of literals, constants and object creations are not logic.</summary>
        internal bool AllowPropertyInitializers { get; }

        /// <summary>
        /// Old-style properties that an auto-property could replace (<c>get { return _a; } set { _a = value; }</c> over a private field that nothing else
        /// uses) are data as auto-properties are, and their field is not a data member of its own.
        /// </summary>
        internal bool AllowBackingFieldProperties { get; }

        /// <summary>Expression-bodied properties and properties with accessor bodies are not logic.</summary>
        internal bool AllowComputedProperties { get; }

        /// <summary>An explicit, empty, parameterless constructor is not logic.</summary>
        internal bool AllowParameterlessConstructor { get; }

        /// <summary>Nested types do not make the type something else than a data carrier.</summary>
        internal bool AllowNestedTypes { get; }

        /// <summary>The base types must be data carriers too.</summary>
        internal bool InspectBaseTypes { get; }

        /// <summary>The opposite scenario: a type with logic that is excluded from the coverage by an attribute on the type itself is reported too.</summary>
        internal bool ReportExcludedTypesWithLogic { get; }

        /// <summary>Attributes (fully qualified names) that already exclude a type from the coverage.</summary>
        internal string[] ExclusionAttributes { get; }

        /// <summary>Attributes (fully qualified names) of types that are not looked at (generated code).</summary>
        internal string[] IgnoredAttributes { get; }

        internal bool SkipTestAssemblies { get; }

        /// <summary>Names of the assemblies that tell a project is a test project.</summary>
        internal string[] TestAssemblyNames { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }

        /// <summary>The text of the <c>Justification</c> the code fix adds to the attribute (empty: no justification).</summary>
        internal string CodeFixJustification { get; }

        private static string ReadJustification(AnalyzerConfigOptions options) =>
            options.TryGetValue(CodeFixJustificationKey, out var value) ? value.Trim() : string.Empty;

        private static DSA045TypeKinds ParseTypeKinds(string[] names)
        {
            var kinds = DSA045TypeKinds.None;
            foreach (var name in names)
            {
                if (Enum.TryParse(name.Replace("_", string.Empty).Replace(" ", string.Empty), ignoreCase: true, out DSA045TypeKinds kind))
                    kinds |= kind;
            }

            return kinds == DSA045TypeKinds.None ? DSA045TypeKinds.All : kinds;
        }
    }
}
