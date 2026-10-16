using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA049.</summary>
[TestClass]
public class DSA049SettingsTests
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
        var settings = new DSA049Settings(new Options());

        Assert.IsTrue(settings.ReportUnlockedWrites);
        Assert.IsTrue(settings.ReportUnlockedReads);
        Assert.IsTrue(settings.ReportDifferentLocks);
        Assert.IsTrue(settings.InferLocksOfPrivateHelpers);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedFieldNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedMemberNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA049Settings(new Options(
            (DSA049Settings.ReportUnlockedWritesKey, "false"),
            (DSA049Settings.ReportUnlockedReadsKey, "false"),
            (DSA049Settings.ReportDifferentLocksKey, "false"),
            (DSA049Settings.InferLocksOfPrivateHelpersKey, "false"),
            (DSA049Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA049Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA049Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA049Settings.ExcludedFieldNamePatternsKey, "_x*"),
            (DSA049Settings.ExcludedMemberNamePatternsKey, "Reset*"),
            (DSA049Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.IsFalse(settings.ReportUnlockedWrites);
        Assert.IsFalse(settings.ReportUnlockedReads);
        Assert.IsFalse(settings.ReportDifferentLocks);
        Assert.IsFalse(settings.InferLocksOfPrivateHelpers);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "_x*" }, settings.ExcludedFieldNamePatterns);
        CollectionAssert.AreEqual(new[] { "Reset*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA049Settings(new Options((DSA049Settings.ReportUnlockedWritesKey, value), (DSA049Settings.InferLocksOfPrivateHelpersKey, value)));

        Assert.IsTrue(settings.ReportUnlockedWrites);
        Assert.IsTrue(settings.InferLocksOfPrivateHelpers);
    }
}
