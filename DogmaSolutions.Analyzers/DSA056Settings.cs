using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA056, read once per options instance. An invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA056Settings
    {
        internal const string TestAttributesKey = "dotnet_diagnostic.DSA056.test_attributes";
        internal const string ExclusionAttributesKey = "dotnet_diagnostic.DSA056.exclusion_attributes";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA056.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA056.excluded_namespace_patterns";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA056.excluded_base_types";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA056.excluded_file_patterns";

        /// <summary>The assemblies of the test frameworks: a compilation that references none of them has no test fixture, and the rule does not even start.</summary>
        internal static readonly string[] TestFrameworkAssemblies =
        {
            "xunit.core", "xunit.v3.core", "nunit.framework", "Microsoft.VisualStudio.TestPlatform.TestFramework", "Microsoft.VisualStudio.QualityTools.UnitTestFramework", "MSTest.TestFramework", "TUnit.Core",
        };

        private static readonly string[] DefaultTestAttributes =
        {
            "NUnit.Framework.TestFixtureAttribute", "NUnit.Framework.TestAttribute", "NUnit.Framework.TestCaseAttribute", "NUnit.Framework.TestCaseSourceAttribute",
            "NUnit.Framework.TheoryAttribute", "NUnit.Framework.SetUpAttribute", "NUnit.Framework.TearDownAttribute", "NUnit.Framework.OneTimeSetUpAttribute", "NUnit.Framework.OneTimeTearDownAttribute",
            "Xunit.FactAttribute", "Xunit.TheoryAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute", "Microsoft.VisualStudio.TestTools.UnitTesting.TestMethodAttribute", "Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethodAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.TestInitializeAttribute", "Microsoft.VisualStudio.TestTools.UnitTesting.TestCleanupAttribute",
            "Microsoft.VisualStudio.TestTools.UnitTesting.ClassInitializeAttribute", "Microsoft.VisualStudio.TestTools.UnitTesting.ClassCleanupAttribute",
            "TUnit.Core.TestAttribute", "TUnit.Core.BeforeAttribute", "TUnit.Core.AfterAttribute",
        };

        private static readonly string[] DefaultExclusionAttributes = { "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" };

        internal static readonly AnalyzerOptionsCache<DSA056Settings> Cache = new(static options => new DSA056Settings(options));

        internal DSA056Settings(AnalyzerConfigOptions options)
        {
            TestAttributes = AnalyzerOptionsReader.ReadList(options, TestAttributesKey, DefaultTestAttributes);
            ExclusionAttributes = AnalyzerOptionsReader.ReadList(options, ExclusionAttributesKey, DefaultExclusionAttributes);
            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>The full names of the attributes that make a class a test fixture (with or without the <c>Attribute</c> suffix); the attributes derived from them count too.</summary>
        internal string[] TestAttributes { get; }

        /// <summary>The full names of the attributes that exclude a type from the code coverage.</summary>
        internal string[] ExclusionAttributes { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
