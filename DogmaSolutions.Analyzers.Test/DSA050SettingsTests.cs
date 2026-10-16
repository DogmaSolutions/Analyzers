using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA050.</summary>
[TestClass]
public class DSA050SettingsTests
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
        var settings = new DSA050Settings(new Options());

        Assert.IsTrue(settings.ReportSetOnce);
        Assert.IsTrue(settings.ReportConsume);
        Assert.IsTrue(settings.ReportStateTransitions);
        Assert.IsTrue(settings.ReportVolatileFields);
        Assert.IsTrue(settings.SkipInsideLock);
        Assert.IsTrue(settings.SkipConstructors);
        Assert.IsTrue(settings.RequireSharedAccess);
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
        var settings = new DSA050Settings(new Options(
            (DSA050Settings.ReportSetOnceKey, "false"),
            (DSA050Settings.ReportConsumeKey, "false"),
            (DSA050Settings.ReportTransitionsKey, "false"),
            (DSA050Settings.ReportVolatileFieldsKey, "false"),
            (DSA050Settings.SkipInsideLockKey, "false"),
            (DSA050Settings.SkipConstructorsKey, "false"),
            (DSA050Settings.RequireSharedAccessKey, "false"),
            (DSA050Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA050Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA050Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA050Settings.ExcludedFieldNamePatternsKey, "_x*"),
            (DSA050Settings.ExcludedMemberNamePatternsKey, "Start*"),
            (DSA050Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.IsFalse(settings.ReportSetOnce);
        Assert.IsFalse(settings.ReportConsume);
        Assert.IsFalse(settings.ReportStateTransitions);
        Assert.IsFalse(settings.ReportVolatileFields);
        Assert.IsFalse(settings.SkipInsideLock);
        Assert.IsFalse(settings.SkipConstructors);
        Assert.IsFalse(settings.RequireSharedAccess);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "_x*" }, settings.ExcludedFieldNamePatterns);
        CollectionAssert.AreEqual(new[] { "Start*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA050Settings(new Options((DSA050Settings.ReportSetOnceKey, value), (DSA050Settings.RequireSharedAccessKey, value)));

        Assert.IsTrue(settings.ReportSetOnce);
        Assert.IsTrue(settings.RequireSharedAccess);
    }
}
