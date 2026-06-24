using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA034SplitPascalCaseTests
{
   [TestMethod]
   public void Null_ReturnsEmpty() =>
      CollectionAssert.AreEqual(new List<string>(), DSA034CodeFixProvider.SplitPascalCase(null));

   [TestMethod]
   public void Empty_ReturnsEmpty() =>
      CollectionAssert.AreEqual(new List<string>(), DSA034CodeFixProvider.SplitPascalCase(string.Empty));

   [TestMethod]
   public void SingleWord_ReturnsSingleElement() =>
      CollectionAssert.AreEqual(new List<string> { "hello" }, DSA034CodeFixProvider.SplitPascalCase("hello"));

   [TestMethod]
   public void PascalCase_SplitsOnUppercase() =>
      CollectionAssert.AreEqual(
         new List<string> { "Order", "Manager" },
         DSA034CodeFixProvider.SplitPascalCase("OrderManager"));

   [TestMethod]
   public void ThreeWords_PascalCase() =>
      CollectionAssert.AreEqual(
         new List<string> { "Get", "Active", "Items" },
         DSA034CodeFixProvider.SplitPascalCase("GetActiveItems"));

   [TestMethod]
   public void LeadingUnderscore_IsStripped() =>
      CollectionAssert.AreEqual(
         new List<string> { "data", "Store" },
         DSA034CodeFixProvider.SplitPascalCase("_dataStore"));

   [TestMethod]
   public void MultipleLeadingUnderscores_AllStripped() =>
      CollectionAssert.AreEqual(
         new List<string> { "field" },
         DSA034CodeFixProvider.SplitPascalCase("___field"));

   [TestMethod]
   public void OnlyUnderscores_ReturnsEmpty() =>
      CollectionAssert.AreEqual(new List<string>(), DSA034CodeFixProvider.SplitPascalCase("___"));

   [TestMethod]
   public void InternalUnderscore_SplitsAsWordBoundary() =>
      CollectionAssert.AreEqual(
         new List<string> { "Create", "Order", "Async" },
         DSA034CodeFixProvider.SplitPascalCase("Create_Order_Async"));

   [TestMethod]
   public void InternalUnderscore_LowercaseWords() =>
      CollectionAssert.AreEqual(
         new List<string> { "max", "retry", "count" },
         DSA034CodeFixProvider.SplitPascalCase("max_retry_count"));

   [TestMethod]
   public void LeadingAndInternalUnderscores_Combined() =>
      CollectionAssert.AreEqual(
         new List<string> { "data", "Store", "Handler" },
         DSA034CodeFixProvider.SplitPascalCase("_data_StoreHandler"));

   [TestMethod]
   public void TrailingUnderscore_Ignored() =>
      CollectionAssert.AreEqual(
         new List<string> { "Readers" },
         DSA034CodeFixProvider.SplitPascalCase("Readers_"));

   [TestMethod]
   public void ConsecutiveInternalUnderscores_NoEmptyTokens() =>
      CollectionAssert.AreEqual(
         new List<string> { "Alpha", "Beta" },
         DSA034CodeFixProvider.SplitPascalCase("Alpha__Beta"));

   [TestMethod]
   public void UnderscoreAndPascalCase_Mixed() =>
      CollectionAssert.AreEqual(
         new List<string> { "Sequential", "Writes", "Multiple", "Readers", "All", "Data" },
         DSA034CodeFixProvider.SplitPascalCase("SequentialWrites_MultipleReaders_AllData"));

   [TestMethod]
   public void SnakeCase_AllLowercase() =>
      CollectionAssert.AreEqual(
         new List<string> { "send", "email", "notification" },
         DSA034CodeFixProvider.SplitPascalCase("send_email_notification"));

   [TestMethod]
   public void LeadingUnderscoreWithInternalUnderscore() =>
      CollectionAssert.AreEqual(
         new List<string> { "frame", "Factory" },
         DSA034CodeFixProvider.SplitPascalCase("_frame_Factory"));

   [TestMethod]
   public void SingleLetterBetweenUnderscores() =>
      CollectionAssert.AreEqual(
         new List<string> { "A", "B", "C" },
         DSA034CodeFixProvider.SplitPascalCase("A_B_C"));

   [TestMethod]
   public void UnderscoreAtStartMiddleAndEnd() =>
      CollectionAssert.AreEqual(
         new List<string> { "Foo", "Bar" },
         DSA034CodeFixProvider.SplitPascalCase("_Foo_Bar_"));

   [TestMethod]
   public void DigitsStayWithPrecedingLowercase() =>
      CollectionAssert.AreEqual(
         new List<string> { "Order2", "Go" },
         DSA034CodeFixProvider.SplitPascalCase("Order2Go"));

   [TestMethod]
   public void DigitPrefixedWord() =>
      CollectionAssert.AreEqual(
         new List<string> { "v2", "Import" },
         DSA034CodeFixProvider.SplitPascalCase("v2Import"));

   [TestMethod]
   public void AllUppercaseAcronym_SplitsEachLetter() =>
      CollectionAssert.AreEqual(
         new List<string> { "H", "T", "T", "P", "Client" },
         DSA034CodeFixProvider.SplitPascalCase("HTTPClient"));

   [TestMethod]
   public void DigitsWithUnderscore() =>
      CollectionAssert.AreEqual(
         new List<string> { "max", "retry2", "count" },
         DSA034CodeFixProvider.SplitPascalCase("max_retry2_count"));

   [TestMethod]
   public void SingleCharWord() =>
      CollectionAssert.AreEqual(
         new List<string> { "X" },
         DSA034CodeFixProvider.SplitPascalCase("X"));
}
