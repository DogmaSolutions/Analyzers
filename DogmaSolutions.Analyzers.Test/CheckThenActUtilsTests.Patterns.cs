using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class CheckThenActUtilsTests
{
    // ---- Pattern A: if (!exists) { insert } -------------------------------------------------------------------

    [TestMethod]
    [DataRow("if (!items.Any()) { items.Add(1); }", "items")]
    [DataRow("if (items.Count() == 0) { items.AddRange(other); }", "items")]
    [DataRow("if (!items.Contains(1)) { x++; if (y) { items.AddAsync(1); } }", "items")]
    [DataRow("if (!items.Any()) items.Add(1);", "items")]
    [DataRow("if (!this.a  .b.Any()) { this.a .b.Add(1); }", "this.a  .b")]
    public void TryMatchCheckThenAct_PatternA_matches(string code, string expectedReceiver)
    {
        Assert.IsTrue(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out var receiver));
        Assert.AreEqual(expectedReceiver, receiver.ToString());
    }

    [TestMethod]
    [DataRow("if (!items.Any()) { other.Add(1); }")]
    [DataRow("if (!items.Any()) { items.Remove(1); }")]
    [DataRow("if (!items.Any()) { Add(1); }")]
    [DataRow("if (!items.Any()) { }")]
    public void TryMatchCheckThenAct_PatternA_does_not_match(string code)
    {
        Assert.IsFalse(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out _));
    }

    // ---- Pattern B: if (exists) { throw; } ... insert ---------------------------------------------------------

    [TestMethod]
    [DataRow("if (items.Any()) { throw new E(); } items.Add(1);", "items")]
    [DataRow("if (items.Any()) throw new E(); items.Add(1);", "items")]
    [DataRow("if (items.Any()) { throw new E(); } x++; y++; if (z) { items.Add(1); }", "items")]
    [DataRow("if (items.Count() > 0) { x++; throw new E(); } items.AddRange(other);", "items")]
    public void TryMatchCheckThenAct_PatternB_matches(string code, string expectedReceiver)
    {
        Assert.IsTrue(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out var receiver));
        Assert.AreEqual(expectedReceiver, receiver.ToString());
    }

    [TestMethod]
    [DataRow("if (items.Any()) { throw new E(); }")]
    [DataRow("if (items.Any()) { throw new E(); } other.Add(1);")]
    [DataRow("items.Add(0); if (items.Any()) { throw new E(); }")]
    [DataRow("if (items.Any()) { x++; } items.Add(1);")]
    [DataRow("if (items.Any()) { throw new E(); } else { x++; } items.Add(1);")]
    [DataRow("while (c) if (items.Any()) throw new E();")]
    [DataRow("switch (c) { case 1: if (items.Any()) throw new E(); break; }")]
    public void TryMatchCheckThenAct_PatternB_does_not_match(string code)
    {
        Assert.IsFalse(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out _));
    }

    // ---- Pattern C: if (exists) { ... } else { insert } -------------------------------------------------------

    [TestMethod]
    [DataRow("if (items.Any()) { x++; } else { items.Add(1); }", "items")]
    [DataRow("if (items.ContainsKey(1)) x++; else items.Add(1);", "items")]
    [DataRow("if (items.FirstOrDefault() != null) { } else if (c) { items.AddAsync(1); }", "items")]
    public void TryMatchCheckThenAct_PatternC_matches(string code, string expectedReceiver)
    {
        Assert.IsTrue(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out var receiver));
        Assert.AreEqual(expectedReceiver, receiver.ToString());
    }

    [TestMethod]
    [DataRow("if (items.Any()) { x++; } else { other.Add(1); }")]
    [DataRow("if (items.Any()) { x++; } else { }")]
    [DataRow("if (items.Any()) { items.Add(1); }")]
    public void TryMatchCheckThenAct_PatternC_does_not_match(string code)
    {
        Assert.IsFalse(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out _));
    }

    [TestMethod]
    [DataRow("if (x) { items.Add(1); }")]
    [DataRow("if (x > 1) { } else { items.Add(1); }")]
    public void TryMatchCheckThenAct_ignores_conditions_that_are_not_existence_checks(string code)
    {
        Assert.IsFalse(CheckThenActUtils.TryMatchCheckThenAct(ParseIf(code), out var receiver));
        Assert.IsNull(receiver);
    }
}
