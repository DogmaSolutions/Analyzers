using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA047.</summary>
[TestClass]
public class DSA047SettingsTests
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
        var settings = new DSA047Settings(new Options());

        Assert.IsTrue(settings.ReportSwappedLocks);
        Assert.IsTrue(settings.ReportExposedLocks);
        Assert.IsFalse(settings.ReportInternalLocks);
        Assert.IsTrue(settings.ReportFields);
        Assert.IsTrue(settings.ReportProperties);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedLockNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA047Settings(new Options(
            (DSA047Settings.ReportSwappedLocksKey, "false"),
            (DSA047Settings.ReportExposedLocksKey, "false"),
            (DSA047Settings.ReportInternalLocksKey, "true"),
            (DSA047Settings.ReportFieldsKey, "false"),
            (DSA047Settings.ReportPropertiesKey, "false"),
            (DSA047Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA047Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA047Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA047Settings.ExcludedLockNamePatternsKey, "_x*"),
            (DSA047Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.IsFalse(settings.ReportSwappedLocks);
        Assert.IsFalse(settings.ReportExposedLocks);
        Assert.IsTrue(settings.ReportInternalLocks);
        Assert.IsFalse(settings.ReportFields);
        Assert.IsFalse(settings.ReportProperties);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "_x*" }, settings.ExcludedLockNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA047Settings(new Options((DSA047Settings.ReportSwappedLocksKey, value), (DSA047Settings.ReportInternalLocksKey, value)));

        Assert.IsTrue(settings.ReportSwappedLocks);
        Assert.IsFalse(settings.ReportInternalLocks);
    }
}
