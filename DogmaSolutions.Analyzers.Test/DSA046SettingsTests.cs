using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA046.</summary>
[TestClass]
public class DSA046SettingsTests
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
        var settings = new DSA046Settings(new Options());

        Assert.IsTrue(settings.ReportIncrements);
        Assert.IsTrue(settings.ReportCompoundAssignments);
        Assert.IsTrue(settings.ReportSelfAssignments);
        Assert.IsTrue(settings.ReportInterlockedTypes);
        Assert.IsTrue(settings.ReportOtherValueTypes);
        Assert.IsTrue(settings.ReportReferenceTypes);
        Assert.IsTrue(settings.SkipInsideLock);
        Assert.IsTrue(settings.SkipConstructors);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedFieldNamePatterns.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA046Settings(new Options(
            (DSA046Settings.ReportIncrementsKey, "false"),
            (DSA046Settings.ReportCompoundAssignmentsKey, "false"),
            (DSA046Settings.ReportSelfAssignmentsKey, "false"),
            (DSA046Settings.ReportInterlockedTypesKey, "false"),
            (DSA046Settings.ReportOtherValueTypesKey, "false"),
            (DSA046Settings.ReportReferenceTypesKey, "false"),
            (DSA046Settings.SkipInsideLockKey, "false"),
            (DSA046Settings.SkipConstructorsKey, "false"),
            (DSA046Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA046Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA046Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA046Settings.ExcludedFieldNamePatternsKey, "_x*"),
            (DSA046Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.IsFalse(settings.ReportIncrements);
        Assert.IsFalse(settings.ReportCompoundAssignments);
        Assert.IsFalse(settings.ReportSelfAssignments);
        Assert.IsFalse(settings.ReportInterlockedTypes);
        Assert.IsFalse(settings.ReportOtherValueTypes);
        Assert.IsFalse(settings.ReportReferenceTypes);
        Assert.IsFalse(settings.SkipInsideLock);
        Assert.IsFalse(settings.SkipConstructors);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "_x*" }, settings.ExcludedFieldNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA046Settings(new Options((DSA046Settings.ReportIncrementsKey, value), (DSA046Settings.SkipInsideLockKey, value)));

        Assert.IsTrue(settings.ReportIncrements);
        Assert.IsTrue(settings.SkipInsideLock);
    }
}
