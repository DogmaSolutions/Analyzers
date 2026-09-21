using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Direct unit tests for the <see cref="DSA042Analyzer"/> string helpers, exercising the defensive guards that the
/// end-to-end analyzer fixtures cannot reach (a real namespace declaration never yields a null/empty path nor a
/// negative own-segment boundary).
/// </summary>
[TestClass]
public class DSA042HelpersTests
{
    [TestMethod]
    public void FindRepeatedSegment_NullOrEmpty_ReturnsNull()
    {
        Assert.IsNull(DSA042Analyzer.FindRepeatedSegment(null, 0));
        Assert.IsNull(DSA042Analyzer.FindRepeatedSegment(string.Empty, 0));
    }

    [TestMethod]
    public void FindRepeatedSegment_NegativeOwnBoundary_ClampsToZero()
    {
        // A negative boundary is clamped to 0, so the whole path counts as "introduced here" and the repeat fires.
        Assert.AreEqual("A", DSA042Analyzer.FindRepeatedSegment("A.A", -5));
    }

    [TestMethod]
    public void FindRepeatedSegment_RepeatBeforeOwnBoundary_NotReported()
    {
        // "A" repeats within the inherited prefix (indices 0..1); the own boundary starts at 2, so it is not reported.
        Assert.IsNull(DSA042Analyzer.FindRepeatedSegment("A.A.B", 2));
    }

    [TestMethod]
    public void CountSegments_Cases()
    {
        Assert.AreEqual(0, DSA042Analyzer.CountSegments(null));
        Assert.AreEqual(0, DSA042Analyzer.CountSegments(string.Empty));
        Assert.AreEqual(3, DSA042Analyzer.CountSegments("A.B.C"));
    }
}
