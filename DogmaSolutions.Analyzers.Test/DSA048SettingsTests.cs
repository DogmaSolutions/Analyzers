using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA048.</summary>
[TestClass]
public class DSA048SettingsTests
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
        var settings = new DSA048Settings(new Options());

        Assert.IsTrue(settings.ReportLoopsWithTimeout);
        Assert.IsTrue(settings.IncludeTaskDelay);
        Assert.IsTrue(settings.IncludeDoLoops);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedMemberNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA048Settings(new Options(
            (DSA048Settings.ReportLoopsWithTimeoutKey, "false"),
            (DSA048Settings.IncludeTaskDelayKey, "false"),
            (DSA048Settings.IncludeDoLoopsKey, "false"),
            (DSA048Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA048Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA048Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA048Settings.ExcludedMemberNamePatternsKey, "Wait*"),
            (DSA048Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.IsFalse(settings.ReportLoopsWithTimeout);
        Assert.IsFalse(settings.IncludeTaskDelay);
        Assert.IsFalse(settings.IncludeDoLoops);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "Wait*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA048Settings(new Options((DSA048Settings.ReportLoopsWithTimeoutKey, value), (DSA048Settings.IncludeDoLoopsKey, value)));

        Assert.IsTrue(settings.ReportLoopsWithTimeout);
        Assert.IsTrue(settings.IncludeDoLoops);
    }
}
