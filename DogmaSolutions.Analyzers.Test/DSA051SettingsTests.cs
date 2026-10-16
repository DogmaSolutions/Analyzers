using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA051.</summary>
[TestClass]
public class DSA051SettingsTests
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
        var settings = new DSA051Settings(new Options());

        Assert.AreEqual(DSA051TypeKinds.Class, settings.IncludedTypeKinds);
        Assert.IsTrue(settings.IncludeAbstractClasses);
        Assert.IsTrue(settings.IncludeNestedTypes);
        Assert.AreEqual(0, settings.MinDataMembers);
        Assert.IsTrue(settings.ConsiderInheritedOverrides);
        CollectionAssert.AreEqual(
            new[] { "Id", "Key", "Pk", "Name", "Value", "FriendlyName", "Description", "FirstName", "LastName", "Surname", "Email", "Address", "Url", "Range" },
            settings.WellKnownNames);
        Assert.IsFalse(settings.WellKnownIncludeKey);
        Assert.IsTrue(settings.SensitiveNamePatterns.Contains("password"));
        Assert.AreEqual(0, settings.MaxMembers);
        Assert.IsTrue(settings.UseExpressionBody);
        CollectionAssert.AreEqual(
            new[] { "System.CodeDom.Compiler.GeneratedCodeAttribute", "System.Runtime.CompilerServices.CompilerGeneratedAttribute", "System.Diagnostics.DebuggerDisplayAttribute" },
            settings.IgnoredAttributes);
        Assert.IsTrue(settings.SkipTestAssemblies);
        Assert.IsTrue(settings.TestAssemblyNames.Contains("nunit.framework"));
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        CollectionAssert.AreEqual(new[] { "System.Attribute" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA051Settings(new Options(
            (DSA051Settings.IncludedTypeKindsKey, "class, struct"),
            (DSA051Settings.IncludeAbstractClassesKey, "false"),
            (DSA051Settings.IncludeNestedTypesKey, "false"),
            (DSA051Settings.MinDataMembersKey, "3"),
            (DSA051Settings.ConsiderInheritedOverridesKey, "false"),
            (DSA051Settings.WellKnownNamesKey, "Code, Title"),
            (DSA051Settings.WellKnownIncludeKeyKey, "true"),
            (DSA051Settings.SensitiveNamePatternsKey, "pin"),
            (DSA051Settings.MaxMembersKey, "5"),
            (DSA051Settings.UseExpressionBodyKey, "false"),
            (DSA051Settings.IgnoredAttributesKey, "My.Skip"),
            (DSA051Settings.SkipTestAssembliesKey, "false"),
            (DSA051Settings.TestAssemblyNamesKey, "my.tests"),
            (DSA051Settings.ExcludedTypeNamePatternsKey, "*Dto"),
            (DSA051Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA051Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA051Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.AreEqual(DSA051TypeKinds.All, settings.IncludedTypeKinds);
        Assert.IsFalse(settings.IncludeAbstractClasses);
        Assert.IsFalse(settings.IncludeNestedTypes);
        Assert.AreEqual(3, settings.MinDataMembers);
        Assert.IsFalse(settings.ConsiderInheritedOverrides);
        CollectionAssert.AreEqual(new[] { "Code", "Title" }, settings.WellKnownNames);
        Assert.IsTrue(settings.WellKnownIncludeKey);
        CollectionAssert.AreEqual(new[] { "pin" }, settings.SensitiveNamePatterns);
        Assert.AreEqual(5, settings.MaxMembers);
        Assert.IsFalse(settings.UseExpressionBody);
        CollectionAssert.AreEqual(new[] { "My.Skip" }, settings.IgnoredAttributes);
        Assert.IsFalse(settings.SkipTestAssemblies);
        CollectionAssert.AreEqual(new[] { "my.tests" }, settings.TestAssemblyNames);
        CollectionAssert.AreEqual(new[] { "*Dto" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("class", 1)]
    [DataRow("Struct", 2)]
    [DataRow("class,struct", 3)]
    [DataRow("all", 3)]
    [DataRow("record", 1)]
    [DataRow("none", 1)]
    [DataRow("nothing", 1)]
    public void The_kinds_are_parsed(string value, int expected)
    {
        var settings = new DSA051Settings(new Options((DSA051Settings.IncludedTypeKindsKey, value)));

        Assert.AreEqual((DSA051TypeKinds)expected, settings.IncludedTypeKinds);
    }

    [TestMethod]
    [DataRow("-1")]
    [DataRow("many")]
    public void An_invalid_number_falls_back_to_the_default(string value)
    {
        var settings = new DSA051Settings(new Options((DSA051Settings.MinDataMembersKey, value), (DSA051Settings.MaxMembersKey, value)));

        Assert.AreEqual(0, settings.MinDataMembers);
        Assert.AreEqual(0, settings.MaxMembers);
    }

    [TestMethod]
    public void Settings_are_cached_per_options_instance()
    {
        var options = new Options();

        Assert.AreSame(DSA051Settings.Cache.Get(options), DSA051Settings.Cache.Get(options));
    }
}
