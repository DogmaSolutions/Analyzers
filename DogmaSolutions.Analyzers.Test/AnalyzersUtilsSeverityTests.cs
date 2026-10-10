using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class AnalyzersUtilsSeverityTests
{
    [TestMethod]
    [DataRow("error", DiagnosticSeverity.Error)]
    [DataRow("Error", DiagnosticSeverity.Error)]
    [DataRow(" ERROR ", DiagnosticSeverity.Error)]
    [DataRow("warning", DiagnosticSeverity.Warning)]
    [DataRow("suggestion", DiagnosticSeverity.Info)]
    [DataRow("info", DiagnosticSeverity.Info)]
    [DataRow("silent", DiagnosticSeverity.Hidden)]
    [DataRow("hidden", DiagnosticSeverity.Hidden)]
    public void ParseSeverity_KnownValues(string value, DiagnosticSeverity expected)
    {
        Assert.AreEqual(expected, AnalyzersUtils.ParseSeverity(value, DiagnosticSeverity.Warning == expected ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow("none")]
    [DataRow("default")]
    [DataRow("bogus")]
    public void ParseSeverity_UnmappableValues_YieldDefault(string value)
    {
        Assert.AreEqual(DiagnosticSeverity.Info, AnalyzersUtils.ParseSeverity(value, DiagnosticSeverity.Info));
    }
}
