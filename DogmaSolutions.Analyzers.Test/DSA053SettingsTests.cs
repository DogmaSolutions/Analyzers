using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA053.</summary>
[TestClass]
public class DSA053SettingsTests
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
        var settings = new DSA053Settings(new Options());

        CollectionAssert.AreEquivalent(new[] { "Count", "LongCount", "CountAsync", "LongCountAsync" }, settings.IncludedMethods.ToArray());
        Assert.IsTrue(settings.ReportLocals);
        Assert.IsTrue(settings.ReportQueryableSources);
        Assert.IsTrue(settings.ReportEnumerableSources);
        Assert.IsFalse(settings.ReportDirectComparisons);
        Assert.IsTrue(settings.ReportAsyncPredicateComparisons);
        Assert.IsFalse(settings.ReportTypesWithCheapCount);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedMemberNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA053Settings(new Options(
            (DSA053Settings.IncludedMethodsKey, "Count, CountAsync"),
            (DSA053Settings.ReportLocalsKey, "false"),
            (DSA053Settings.ReportQueryableSourcesKey, "false"),
            (DSA053Settings.ReportEnumerableSourcesKey, "false"),
            (DSA053Settings.ReportDirectComparisonsKey, "true"),
            (DSA053Settings.ReportAsyncPredicateComparisonsKey, "false"),
            (DSA053Settings.ReportTypesWithCheapCountKey, "true"),
            (DSA053Settings.ExcludedTypeNamePatternsKey, "*Dto"),
            (DSA053Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA053Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA053Settings.ExcludedMemberNamePatternsKey, "Initialize*"),
            (DSA053Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        CollectionAssert.AreEquivalent(new[] { "Count", "CountAsync" }, settings.IncludedMethods.ToArray());
        Assert.IsFalse(settings.ReportLocals);
        Assert.IsFalse(settings.ReportQueryableSources);
        Assert.IsFalse(settings.ReportEnumerableSources);
        Assert.IsTrue(settings.ReportDirectComparisons);
        Assert.IsFalse(settings.ReportAsyncPredicateComparisons);
        Assert.IsTrue(settings.ReportTypesWithCheapCount);
        CollectionAssert.AreEqual(new[] { "*Dto" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "Initialize*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void A_malformed_boolean_falls_back_to_the_default()
    {
        var settings = new DSA053Settings(new Options((DSA053Settings.ReportLocalsKey, "maybe")));

        Assert.IsTrue(settings.ReportLocals);
    }

    [TestMethod]
    public void Settings_are_cached_per_options_instance()
    {
        var options = new Options();

        Assert.AreSame(DSA053Settings.Cache.Get(options), DSA053Settings.Cache.Get(options));
    }
}
