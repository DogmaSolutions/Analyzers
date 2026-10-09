using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class CheckThenActUtilsTests
{
    // ---- IsNegatedExistenceCheck ------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("!items.Any()", "items")]
    [DataRow("!items.Any(x => x > 1)", "items")]
    [DataRow("!(items.Any())", "items")]
    [DataRow("!await items.AnyAsync()", "items")]
    [DataRow("!this.items.Contains(1)", "this.items")]
    [DataRow("items.Any() == false", "items")]
    [DataRow("false == items.Any()", "items")]
    [DataRow("(items.Any()) == false", "items")]
    [DataRow("items.Count() == 0", "items")]
    [DataRow("0 == items.Count()", "items")]
    [DataRow("items.CountAsync() == 0", "items")]
    [DataRow("items.FirstOrDefault() == null", "items")]
    [DataRow("null == items.FirstOrDefault()", "items")]
    [DataRow("a.b.Find(1) == null", "a.b")]
    public void IsNegatedExistenceCheck_matches(string condition, string expectedReceiver)
    {
        Assert.IsTrue(CheckThenActUtils.IsNegatedExistenceCheck(ParseCondition(condition), out var receiver));
        Assert.AreEqual(expectedReceiver, receiver.ToString());
    }

    [TestMethod]
    [DataRow("items.Any()")]
    [DataRow("!items.Count()")]
    [DataRow("!x")]
    [DataRow("-items.Any()")]
    [DataRow("!Foo()")]
    [DataRow("!items.Other()")]
    [DataRow("items.Any() == true")]
    [DataRow("items.Any() != false")]
    [DataRow("items.Any() == 0")]
    [DataRow("items.Count() == 1")]
    [DataRow("items.Count() == 0.5")]
    [DataRow("items.Count() == null")]
    [DataRow("items.Count() > 0")]
    [DataRow("items.FirstOrDefault() == 1")]
    [DataRow("items.FirstOrDefault() != null")]
    [DataRow("x == false")]
    [DataRow("false == false")]
    [DataRow("x")]
    public void IsNegatedExistenceCheck_does_not_match(string condition)
    {
        Assert.IsFalse(CheckThenActUtils.IsNegatedExistenceCheck(ParseCondition(condition), out _));
    }

    // ---- IsPositiveExistenceCheck -----------------------------------------------------------------------------

    [TestMethod]
    [DataRow("items.Any()", "items")]
    [DataRow("items.Any(x => x > 1)", "items")]
    [DataRow("(items.Any())", "items")]
    [DataRow("await items.AnyAsync()", "items")]
    [DataRow("items.Exists(x => x)", "items")]
    [DataRow("items.ExistsAsync()", "items")]
    [DataRow("items.Contains(1)", "items")]
    [DataRow("items.ContainsAsync(1)", "items")]
    [DataRow("items.ContainsKey(1)", "items")]
    [DataRow("items.TryGetValue(1, out var v)", "items")]
    [DataRow("this.items.Any()", "this.items")]
    [DataRow("items.Count() > 0", "items")]
    [DataRow("await items.CountAsync() > 0", "items")]
    [DataRow("0 < items.Count()", "items")]
    [DataRow("items.Count() != 0", "items")]
    [DataRow("0 != items.Count()", "items")]
    [DataRow("items.FirstOrDefault() != null", "items")]
    [DataRow("null != items.FirstOrDefault()", "items")]
    [DataRow("items.FirstOrDefaultAsync() != null", "items")]
    [DataRow("items.SingleOrDefault() != null", "items")]
    [DataRow("items.SingleOrDefaultAsync() != null", "items")]
    [DataRow("items.Find(1) != null", "items")]
    [DataRow("items.FindAsync(1) != null", "items")]
    public void IsPositiveExistenceCheck_matches(string condition, string expectedReceiver)
    {
        Assert.IsTrue(CheckThenActUtils.IsPositiveExistenceCheck(ParseCondition(condition), out var receiver));
        Assert.AreEqual(expectedReceiver, receiver.ToString());
    }

    [TestMethod]
    [DataRow("x")]
    [DataRow("Foo()")]
    [DataRow("items.Other()")]
    [DataRow("!items.Any()")]
    [DataRow("items.Count()")]
    [DataRow("items.Count() > 1")]
    [DataRow("0 > items.Count()")]
    [DataRow("items.Count() < 0")]
    [DataRow("1 < items.Count()")]
    [DataRow("0 < 0")]
    [DataRow("items.Count() >= 0")]
    [DataRow("items.Count() == 0")]
    [DataRow("items.Count() != 1")]
    [DataRow("items.Count() != null")]
    [DataRow("items.FirstOrDefault() != 1")]
    [DataRow("items.FirstOrDefault() == null")]
    [DataRow("items.Other() != null")]
    [DataRow("x != null")]
    [DataRow("x != 0")]
    public void IsPositiveExistenceCheck_does_not_match(string condition)
    {
        Assert.IsFalse(CheckThenActUtils.IsPositiveExistenceCheck(ParseCondition(condition), out _));
    }

    // ---- ContainsThrowStatement -------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("throw new System.Exception();", true)]
    [DataRow("{ throw new System.Exception(); }", true)]
    [DataRow("{ x++; throw new System.Exception(); }", true)]
    [DataRow("{ }", false)]
    [DataRow("{ x++; }", false)]
    [DataRow("{ if (x) throw new System.Exception(); }", false)]
    [DataRow("x++;", false)]
    [DataRow("return;", false)]
    public void ContainsThrowStatement_detects_only_top_level_throws(string statement, bool expected)
    {
        var parsed = Microsoft.CodeAnalysis.CSharp.SyntaxFactory.ParseStatement(statement);
        Assert.AreEqual(expected, CheckThenActUtils.ContainsThrowStatement(parsed));
    }
}
