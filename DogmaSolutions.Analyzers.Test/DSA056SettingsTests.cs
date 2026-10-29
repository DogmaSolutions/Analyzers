using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA056.</summary>
[TestClass]
public class DSA056SettingsTests
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
        var settings = new DSA056Settings(new Options());

        CollectionAssert.Contains(settings.TestAttributes, "NUnit.Framework.TestFixtureAttribute");
        CollectionAssert.Contains(settings.TestAttributes, "Xunit.FactAttribute");
        CollectionAssert.Contains(settings.TestAttributes, "Microsoft.VisualStudio.TestTools.UnitTesting.TestClassAttribute");
        CollectionAssert.Contains(settings.TestAttributes, "TUnit.Core.TestAttribute");
        CollectionAssert.AreEqual(new[] { "System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute" }, settings.ExclusionAttributes);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA056Settings(new Options(
            (DSA056Settings.TestAttributesKey, "My.SpecAttribute, My.Scenario"),
            (DSA056Settings.ExclusionAttributesKey, "My.SkipCoverageAttribute"),
            (DSA056Settings.ExcludedTypeNamePatternsKey, "*Fakes"),
            (DSA056Settings.ExcludedNamespacePatternsKey, "*.Legacy"),
            (DSA056Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA056Settings.ExcludedFilePatternsKey, "*.g.cs")));

        CollectionAssert.AreEqual(new[] { "My.SpecAttribute", "My.Scenario" }, settings.TestAttributes);
        CollectionAssert.AreEqual(new[] { "My.SkipCoverageAttribute" }, settings.ExclusionAttributes);
        CollectionAssert.AreEqual(new[] { "*Fakes" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Legacy" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "*.g.cs" }, settings.ExcludedFilePatterns.ToArray());
    }
}
