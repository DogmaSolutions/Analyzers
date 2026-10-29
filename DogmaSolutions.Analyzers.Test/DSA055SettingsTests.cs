using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA055.</summary>
[TestClass]
public class DSA055SettingsTests
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
        var settings = new DSA055Settings(new Options());

        Assert.IsTrue(settings.ReportEquality);
        Assert.IsTrue(settings.ReportMembers);
        Assert.IsTrue(settings.ReportUnsignedTypes);
        CollectionAssert.AreEqual(new[] { "Id", "Key" }, settings.KeyNameSuffixes);
        CollectionAssert.AreEqual(new[] { "id", "key", "Id", "Key", "Pk", "pk", "primaryKey" }, settings.KeyExactNames);
        Assert.AreEqual(0, settings.ExcludedNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedMemberNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA055Settings(new Options(
            (DSA055Settings.ReportEqualityKey, "false"),
            (DSA055Settings.ReportMembersKey, "false"),
            (DSA055Settings.ReportUnsignedTypesKey, "false"),
            (DSA055Settings.KeyNameSuffixesKey, "Code, Ref"),
            (DSA055Settings.KeyExactNamesKey, "pk2"),
            (DSA055Settings.ExcludedNamePatternsKey, "parent*"),
            (DSA055Settings.ExcludedTypeNamePatternsKey, "*Dto"),
            (DSA055Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA055Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA055Settings.ExcludedMemberNamePatternsKey, "Render*"),
            (DSA055Settings.ExcludedFilePatternsKey, "*.g.cs")));

        Assert.IsFalse(settings.ReportEquality);
        Assert.IsFalse(settings.ReportMembers);
        Assert.IsFalse(settings.ReportUnsignedTypes);
        CollectionAssert.AreEqual(new[] { "Code", "Ref" }, settings.KeyNameSuffixes);
        CollectionAssert.AreEqual(new[] { "pk2" }, settings.KeyExactNames);
        CollectionAssert.AreEqual(new[] { "parent*" }, settings.ExcludedNamePatterns);
        CollectionAssert.AreEqual(new[] { "*Dto" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "Render*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.g.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void A_name_is_a_key_by_its_exact_text_or_by_its_end_and_always_with_the_case()
    {
        var settings = new DSA055Settings(new Options());

        foreach (var key in new[] { "id", "Id", "key", "Key", "pk", "Pk", "primaryKey", "userId", "ResolvedKey" })
            Assert.IsTrue(settings.IsKeyName(key), key);

        foreach (var other in new[] { "ID", "PK", "PrimaryKEY", "primarykey", "valid", "paid", "monkey", "Hybrid", "idx", "keys", string.Empty })
            Assert.IsFalse(settings.IsKeyName(other), other);
    }

    [TestMethod]
    public void An_invalid_value_falls_back_to_the_default()
    {
        var settings = new DSA055Settings(new Options((DSA055Settings.ReportEqualityKey, "maybe"), (DSA055Settings.ReportMembersKey, "2")));

        Assert.IsTrue(settings.ReportEquality);
        Assert.IsTrue(settings.ReportMembers);
    }
}
