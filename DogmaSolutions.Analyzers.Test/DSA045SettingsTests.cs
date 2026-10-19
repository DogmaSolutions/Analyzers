using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA045.</summary>
[TestClass]
public class DSA045SettingsTests
{
    private sealed class Options : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        internal Options(params (string Key, string Value)[] values) => _values = values.ToDictionary(v => v.Key, v => v.Value);

        public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);
    }

    [TestMethod]
    public void The_defaults_apply_when_nothing_is_configured()
    {
        var settings = new DSA045Settings(new Options());

        Assert.AreEqual(DSA045TypeKinds.All, settings.IncludedTypeKinds);
        Assert.IsTrue(settings.IncludeAbstractClasses);
        Assert.IsTrue(settings.IncludeNestedTypes);
        Assert.AreEqual(1, settings.MinDataMembers);
        Assert.IsTrue(settings.AllowFields);
        Assert.IsTrue(settings.AllowPropertyInitializers);
        Assert.IsTrue(settings.AllowBackingFieldProperties);
        Assert.IsFalse(settings.AllowComputedProperties);
        Assert.IsFalse(settings.AllowParameterlessConstructor);
        Assert.IsFalse(settings.AllowNestedTypes);
        Assert.IsTrue(settings.InspectBaseTypes);
        Assert.IsTrue(settings.ReportExcludedTypesWithLogic);
        CollectionAssert.AreEqual(new[] { "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" }, settings.ExclusionAttributes);
        CollectionAssert.AreEqual(new[] { "System.CodeDom.Compiler.GeneratedCodeAttribute", "System.Runtime.CompilerServices.CompilerGeneratedAttribute" }, settings.IgnoredAttributes);
        Assert.IsTrue(settings.SkipTestAssemblies);
        CollectionAssert.AreEqual(new[] { "xunit.core", "xunit.v3.core", "nunit.framework", "Microsoft.VisualStudio.TestPlatform.TestFramework", "MSTest.TestFramework", "TUnit.Core" }, settings.TestAssemblyNames);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
        Assert.AreEqual(string.Empty, settings.CodeFixJustification);
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA045Settings(new Options(
            (DSA045Settings.IncludedTypeKindsKey, "struct, record_struct"),
            (DSA045Settings.IncludeAbstractClassesKey, "false"),
            (DSA045Settings.IncludeNestedTypesKey, "false"),
            (DSA045Settings.MinDataMembersKey, "3"),
            (DSA045Settings.AllowFieldsKey, "false"),
            (DSA045Settings.AllowPropertyInitializersKey, "false"),
            (DSA045Settings.AllowBackingFieldPropertiesKey, "false"),
            (DSA045Settings.AllowComputedPropertiesKey, "true"),
            (DSA045Settings.AllowParameterlessConstructorKey, "true"),
            (DSA045Settings.AllowNestedTypesKey, "true"),
            (DSA045Settings.InspectBaseTypesKey, "false"),
            (DSA045Settings.ReportExcludedTypesWithLogicKey, "false"),
            (DSA045Settings.ExclusionAttributesKey, "My.NoCoverage, My.Other"),
            (DSA045Settings.IgnoredAttributesKey, "My.Skip"),
            (DSA045Settings.SkipTestAssembliesKey, "false"),
            (DSA045Settings.TestAssemblyNamesKey, "my.tests"),
            (DSA045Settings.ExcludedTypeNamePatternsKey, "*Dto"),
            (DSA045Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA045Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA045Settings.ExcludedFilePatternsKey, "*.Legacy.cs"),
            (DSA045Settings.CodeFixJustificationKey, "  Data carrier  ")));

        Assert.AreEqual(DSA045TypeKinds.Struct | DSA045TypeKinds.RecordStruct, settings.IncludedTypeKinds);
        Assert.IsFalse(settings.IncludeAbstractClasses);
        Assert.IsFalse(settings.IncludeNestedTypes);
        Assert.AreEqual(3, settings.MinDataMembers);
        Assert.IsFalse(settings.AllowFields);
        Assert.IsFalse(settings.AllowPropertyInitializers);
        Assert.IsFalse(settings.AllowBackingFieldProperties);
        Assert.IsTrue(settings.AllowComputedProperties);
        Assert.IsTrue(settings.AllowParameterlessConstructor);
        Assert.IsTrue(settings.AllowNestedTypes);
        Assert.IsFalse(settings.InspectBaseTypes);
        Assert.IsFalse(settings.ReportExcludedTypesWithLogic);
        CollectionAssert.AreEqual(new[] { "My.NoCoverage", "My.Other" }, settings.ExclusionAttributes);
        CollectionAssert.AreEqual(new[] { "My.Skip" }, settings.IgnoredAttributes);
        Assert.IsFalse(settings.SkipTestAssemblies);
        CollectionAssert.AreEqual(new[] { "my.tests" }, settings.TestAssemblyNames);
        CollectionAssert.AreEqual(new[] { "*Dto" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
        Assert.AreEqual("Data carrier", settings.CodeFixJustification);
    }

    [TestMethod]
    [DataRow("class", 1)]
    [DataRow("Struct", 2)]
    [DataRow("record", 4)]
    [DataRow("record_struct", 8)]
    [DataRow("record struct", 8)]
    [DataRow("RecordStruct", 8)]
    [DataRow("class, nonsense", 1)]
    [DataRow("nonsense", 15)]
    [DataRow("none", 15)]
    [DataRow("", 15)]
    public void The_kinds_of_type(string value, int expected)
    {
        Assert.AreEqual((DSA045TypeKinds)expected, new DSA045Settings(new Options((DSA045Settings.IncludedTypeKindsKey, value))).IncludedTypeKinds);
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("abc")]
    [DataRow("")]
    public void An_invalid_number_falls_back_to_the_default(string value)
    {
        Assert.AreEqual(1, new DSA045Settings(new Options((DSA045Settings.MinDataMembersKey, value))).MinDataMembers);
    }

    [TestMethod]
    public void No_minimum_means_that_an_empty_type_is_a_data_carrier()
    {
        Assert.AreEqual(0, new DSA045Settings(new Options((DSA045Settings.MinDataMembersKey, "0"))).MinDataMembers);
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA045Settings(new Options((DSA045Settings.InspectBaseTypesKey, value), (DSA045Settings.AllowFieldsKey, value), (DSA045Settings.AllowNestedTypesKey, value), (DSA045Settings.AllowBackingFieldPropertiesKey, value)));

        Assert.IsTrue(settings.InspectBaseTypes);
        Assert.IsTrue(settings.AllowFields);
        Assert.IsTrue(settings.AllowBackingFieldProperties);
        Assert.IsFalse(settings.AllowNestedTypes);
    }

    [TestMethod]
    public void The_settings_are_computed_once_per_options_instance()
    {
        var options = new Options();

        Assert.AreSame(DSA045Settings.Cache.Get(options), DSA045Settings.Cache.Get(options));
    }
}
