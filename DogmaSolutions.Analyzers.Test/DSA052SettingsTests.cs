using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA052.</summary>
[TestClass]
public class DSA052SettingsTests
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
        var settings = new DSA052Settings(new Options());

        CollectionAssert.AreEquivalent(new[] { "FirstOrDefault", "FirstOrDefaultAsync", "SingleOrDefault", "SingleOrDefaultAsync" }, settings.IncludedMethods.ToArray());
        Assert.IsTrue(settings.ReportLocals);
        Assert.IsTrue(settings.ReportQueryableSources);
        Assert.IsTrue(settings.ReportEnumerableSources);
        Assert.IsTrue(settings.ReportDirectComparisons);
        Assert.IsTrue(settings.SkipPossiblyNullElements);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedMemberNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA052Settings(new Options(
            (DSA052Settings.IncludedMethodsKey, "LastOrDefault, LastOrDefaultAsync"),
            (DSA052Settings.ReportLocalsKey, "false"),
            (DSA052Settings.ReportQueryableSourcesKey, "false"),
            (DSA052Settings.ReportEnumerableSourcesKey, "false"),
            (DSA052Settings.ReportDirectComparisonsKey, "false"),
            (DSA052Settings.SkipPossiblyNullElementsKey, "false"),
            (DSA052Settings.ExcludedTypeNamePatternsKey, "*Dto"),
            (DSA052Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA052Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA052Settings.ExcludedMemberNamePatternsKey, "Initialize*"),
            (DSA052Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        CollectionAssert.AreEquivalent(new[] { "LastOrDefault", "LastOrDefaultAsync" }, settings.IncludedMethods.ToArray());
        Assert.IsFalse(settings.ReportLocals);
        Assert.IsFalse(settings.ReportQueryableSources);
        Assert.IsFalse(settings.ReportEnumerableSources);
        Assert.IsFalse(settings.ReportDirectComparisons);
        Assert.IsFalse(settings.SkipPossiblyNullElements);
        CollectionAssert.AreEqual(new[] { "*Dto" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "Initialize*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void A_malformed_boolean_falls_back_to_the_default()
    {
        var settings = new DSA052Settings(new Options((DSA052Settings.ReportLocalsKey, "maybe")));

        Assert.IsTrue(settings.ReportLocals);
    }

    [TestMethod]
    public void Settings_are_cached_per_options_instance()
    {
        var options = new Options();

        Assert.AreSame(DSA052Settings.Cache.Get(options), DSA052Settings.Cache.Get(options));
    }
}
