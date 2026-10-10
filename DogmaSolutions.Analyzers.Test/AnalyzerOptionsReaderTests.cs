using System.Collections.Generic;
using System.Globalization;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class AnalyzerOptionsReaderTests
{
    private const string Key = "dotnet_diagnostic.DSA999.option";

    private sealed class FakeOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values = new();

        public FakeOptions(string value = null)
        {
            if (value != null)
                _values[Key] = value;
        }

        public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);
    }

    [TestMethod]
    [DataRow("5", 5)]
    [DataRow(" 5 ", 5)]
    [DataRow("1", 1)]
    [DataRow("0", 7)]
    [DataRow("-3", 7)]
    [DataRow("abc", 7)]
    [DataRow("5.5", 7)]
    [DataRow("1,000", 7)]
    [DataRow("", 7)]
    [DataRow(null, 7)]
    public void ReadInt_FallsBackToTheDefaultForAnythingButAnIntegerInRange(string value, int expected)
    {
        Assert.AreEqual(expected, AnalyzerOptionsReader.ReadInt(new FakeOptions(value), Key, 7, minInclusive: 1));
    }

    [TestMethod]
    public void ReadInt_AcceptsTheMinimumItself()
    {
        Assert.AreEqual(0, AnalyzerOptionsReader.ReadInt(new FakeOptions("0"), Key, 7, minInclusive: 0));
    }

    [TestMethod]
    public void ReadInt_IsNotAffectedByTheCurrentCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");
            Assert.AreEqual(12, AnalyzerOptionsReader.ReadInt(new FakeOptions("12"), Key, 7, minInclusive: 1));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    [DataRow("true", true, false)]
    [DataRow("TRUE", true, false)]
    [DataRow("false", false, true)]
    [DataRow("yes", true, true)]
    [DataRow("yes", false, false)]
    [DataRow("", false, false)]
    [DataRow(null, true, true)]
    public void ReadBool_FallsBackToTheDefaultForAnythingButABoolean(string value, bool expected, bool defaultValue)
    {
        Assert.AreEqual(expected, AnalyzerOptionsReader.ReadBool(new FakeOptions(value), Key, defaultValue));
    }

    [TestMethod]
    public void ReadList_TrimsAndDropsEmptyItems()
    {
        CollectionAssert.AreEqual(new[] { "a", "b c", "d" }, AnalyzerOptionsReader.ReadList(new FakeOptions(" a , b c,, d ,"), Key, new[] { "x" }));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    public void ReadList_FallsBackToTheDefaultWhenMissingOrBlank(string value)
    {
        var defaults = new[] { "x" };
        Assert.AreSame(defaults, AnalyzerOptionsReader.ReadList(new FakeOptions(value), Key, defaults));
    }

    [TestMethod]
    public void ReadList_OfOnlySeparatorsIsEmpty_NotTheDefault()
    {
        Assert.AreEqual(0, AnalyzerOptionsReader.ReadList(new FakeOptions(" , ,"), Key, new[] { "x" }).Length);
    }

    [TestMethod]
    public void Cache_ComputesOncePerOptionsInstance()
    {
        var calls = 0;
        var cache = new AnalyzerOptionsCache<string[]>(_ =>
        {
            calls++;
            return new[] { "v" };
        });
        var first = new FakeOptions();
        var second = new FakeOptions();

        Assert.AreSame(cache.Get(first), cache.Get(first));
        Assert.AreEqual(1, calls);
        Assert.AreNotSame(cache.Get(first), cache.Get(second));
        Assert.AreEqual(2, calls);
    }
}
