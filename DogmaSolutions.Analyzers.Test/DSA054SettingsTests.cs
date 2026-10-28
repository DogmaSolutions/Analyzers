using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA054.</summary>
[TestClass]
public class DSA054SettingsTests
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
        var settings = new DSA054Settings(new Options());

        Assert.IsFalse(settings.ReportNeverRead);
        Assert.IsTrue(settings.ReportStaticFields);
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
        var settings = new DSA054Settings(new Options(
            (DSA054Settings.ReportNeverReadKey, "true"),
            (DSA054Settings.ReportStaticFieldsKey, "false"),
            (DSA054Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA054Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA054Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA054Settings.ExcludedFieldNamePatternsKey, "_scratch*"),
            (DSA054Settings.ExcludedMemberNamePatternsKey, "Render*"),
            (DSA054Settings.ExcludedFilePatternsKey, "*.g.cs")));

        Assert.IsTrue(settings.ReportNeverRead);
        Assert.IsFalse(settings.ReportStaticFields);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "_scratch*" }, settings.ExcludedFieldNamePatterns);
        CollectionAssert.AreEqual(new[] { "Render*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.g.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void An_invalid_value_falls_back_to_the_default()
    {
        var settings = new DSA054Settings(new Options((DSA054Settings.ReportNeverReadKey, "maybe"), (DSA054Settings.ReportStaticFieldsKey, "2")));

        Assert.IsFalse(settings.ReportNeverRead);
        Assert.IsTrue(settings.ReportStaticFields);
    }
}
