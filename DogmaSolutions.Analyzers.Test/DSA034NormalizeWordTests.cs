using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA034NormalizeWordTests
{
   [TestMethod]
   public void Null_ReturnsNull() =>
      Assert.IsNull(DSA034CodeFixProvider.NormalizeWord(null));

   [TestMethod]
   public void Empty_ReturnsEmpty() =>
      Assert.AreEqual(string.Empty, DSA034CodeFixProvider.NormalizeWord(string.Empty));

   [TestMethod]
   public void ShortWord_TwoChars_Unchanged() =>
      Assert.AreEqual("Is", DSA034CodeFixProvider.NormalizeWord("Is"));

   [TestMethod]
   public void ShortWord_OneChar_Unchanged() =>
      Assert.AreEqual("I", DSA034CodeFixProvider.NormalizeWord("I"));

   // --- Irregular plurals ---

   [TestMethod]
   public void Irregular_Children() =>
      Assert.AreEqual("Child", DSA034CodeFixProvider.NormalizeWord("Children"));

   [TestMethod]
   public void Irregular_Men() =>
      Assert.AreEqual("Man", DSA034CodeFixProvider.NormalizeWord("Men"));

   [TestMethod]
   public void Irregular_Women() =>
      Assert.AreEqual("Woman", DSA034CodeFixProvider.NormalizeWord("Women"));

   [TestMethod]
   public void Irregular_People() =>
      Assert.AreEqual("Person", DSA034CodeFixProvider.NormalizeWord("People"));

   [TestMethod]
   public void Irregular_Mice() =>
      Assert.AreEqual("Mouse", DSA034CodeFixProvider.NormalizeWord("Mice"));

   [TestMethod]
   public void Irregular_Indices() =>
      Assert.AreEqual("Index", DSA034CodeFixProvider.NormalizeWord("Indices"));

   [TestMethod]
   public void Irregular_Vertices() =>
      Assert.AreEqual("Vertex", DSA034CodeFixProvider.NormalizeWord("Vertices"));

   [TestMethod]
   public void Irregular_Matrices() =>
      Assert.AreEqual("Matrix", DSA034CodeFixProvider.NormalizeWord("Matrices"));

   [TestMethod]
   public void Irregular_Criteria() =>
      Assert.AreEqual("Criterion", DSA034CodeFixProvider.NormalizeWord("Criteria"));

   [TestMethod]
   public void Irregular_Aliases() =>
      Assert.AreEqual("Alias", DSA034CodeFixProvider.NormalizeWord("Aliases"));

   [TestMethod]
   public void Irregular_Statuses() =>
      Assert.AreEqual("Status", DSA034CodeFixProvider.NormalizeWord("Statuses"));

   [TestMethod]
   public void Irregular_Buses() =>
      Assert.AreEqual("Bus", DSA034CodeFixProvider.NormalizeWord("Buses"));

   [TestMethod]
   public void Irregular_Focuses() =>
      Assert.AreEqual("Focus", DSA034CodeFixProvider.NormalizeWord("Focuses"));

   [TestMethod]
   public void Irregular_CaseInsensitive() =>
      Assert.AreEqual("Child", DSA034CodeFixProvider.NormalizeWord("CHILDREN"));

   [TestMethod]
   public void Irregular_LowercaseInput_LowercaseOutput() =>
      Assert.AreEqual("child", DSA034CodeFixProvider.NormalizeWord("children"));

   // --- Words that should NOT be singularized ---

   [TestMethod]
   public void NotPlural_Class_Unchanged() =>
      Assert.AreEqual("Class", DSA034CodeFixProvider.NormalizeWord("Class"));

   [TestMethod]
   public void NotPlural_Address_Unchanged() =>
      Assert.AreEqual("Address", DSA034CodeFixProvider.NormalizeWord("Address"));

   [TestMethod]
   public void NotPlural_Status_Unchanged() =>
      Assert.AreEqual("Status", DSA034CodeFixProvider.NormalizeWord("Status"));

   [TestMethod]
   public void NotPlural_Bus_Unchanged() =>
      Assert.AreEqual("Bus", DSA034CodeFixProvider.NormalizeWord("Bus"));

   [TestMethod]
   public void NotPlural_Analysis_Unchanged() =>
      Assert.AreEqual("Analysis", DSA034CodeFixProvider.NormalizeWord("Analysis"));

   [TestMethod]
   public void NotPlural_Axis_Unchanged() =>
      Assert.AreEqual("Axis", DSA034CodeFixProvider.NormalizeWord("Axis"));

   [TestMethod]
   public void NotPlural_Process_Unchanged() =>
      Assert.AreEqual("Process", DSA034CodeFixProvider.NormalizeWord("Process"));

   [TestMethod]
   public void NotPlural_NoS_Unchanged() =>
      Assert.AreEqual("Order", DSA034CodeFixProvider.NormalizeWord("Order"));

   // --- -ies → -y ---

   [TestMethod]
   public void Ies_Properties() =>
      Assert.AreEqual("Property", DSA034CodeFixProvider.NormalizeWord("Properties"));

   [TestMethod]
   public void Ies_Entries() =>
      Assert.AreEqual("Entry", DSA034CodeFixProvider.NormalizeWord("Entries"));

   [TestMethod]
   public void Ies_Factories() =>
      Assert.AreEqual("Factory", DSA034CodeFixProvider.NormalizeWord("Factories"));

   [TestMethod]
   public void Ies_Repositories() =>
      Assert.AreEqual("Repository", DSA034CodeFixProvider.NormalizeWord("Repositories"));

   [TestMethod]
   public void Ies_Categories() =>
      Assert.AreEqual("Category", DSA034CodeFixProvider.NormalizeWord("Categories"));

   [TestMethod]
   public void Ies_Queries() =>
      Assert.AreEqual("Query", DSA034CodeFixProvider.NormalizeWord("Queries"));

   [TestMethod]
   public void Ies_Strategies() =>
      Assert.AreEqual("Strategy", DSA034CodeFixProvider.NormalizeWord("Strategies"));

   [TestMethod]
   public void Ies_Exception_Series() =>
      Assert.AreEqual("Series", DSA034CodeFixProvider.NormalizeWord("Series"));

   [TestMethod]
   public void Ies_Exception_Species() =>
      Assert.AreEqual("Species", DSA034CodeFixProvider.NormalizeWord("Species"));

   // --- -sses → -ss ---

   [TestMethod]
   public void Sses_Classes() =>
      Assert.AreEqual("Class", DSA034CodeFixProvider.NormalizeWord("Classes"));

   [TestMethod]
   public void Sses_Addresses() =>
      Assert.AreEqual("Address", DSA034CodeFixProvider.NormalizeWord("Addresses"));

   [TestMethod]
   public void Sses_Processes() =>
      Assert.AreEqual("Process", DSA034CodeFixProvider.NormalizeWord("Processes"));

   // --- -shes → -sh ---

   [TestMethod]
   public void Shes_Crashes() =>
      Assert.AreEqual("Crash", DSA034CodeFixProvider.NormalizeWord("Crashes"));

   [TestMethod]
   public void Shes_Flashes() =>
      Assert.AreEqual("Flash", DSA034CodeFixProvider.NormalizeWord("Flashes"));

   [TestMethod]
   public void Shes_Bushes() =>
      Assert.AreEqual("Bush", DSA034CodeFixProvider.NormalizeWord("Bushes"));

   // --- -ches (consonant before → -ch, vowel before → -che) ---

   [TestMethod]
   public void Ches_Batches() =>
      Assert.AreEqual("Batch", DSA034CodeFixProvider.NormalizeWord("Batches"));

   [TestMethod]
   public void Ches_Matches() =>
      Assert.AreEqual("Match", DSA034CodeFixProvider.NormalizeWord("Matches"));

   [TestMethod]
   public void Ches_Dispatches() =>
      Assert.AreEqual("Dispatch", DSA034CodeFixProvider.NormalizeWord("Dispatches"));

   [TestMethod]
   public void Ches_Searches() =>
      Assert.AreEqual("Search", DSA034CodeFixProvider.NormalizeWord("Searches"));

   [TestMethod]
   public void Ches_Caches_Irregular() =>
      Assert.AreEqual("Cache", DSA034CodeFixProvider.NormalizeWord("Caches"));

   [TestMethod]
   public void Ches_Niches_Irregular() =>
      Assert.AreEqual("Niche", DSA034CodeFixProvider.NormalizeWord("Niches"));

   [TestMethod]
   public void Ches_Aches_Irregular() =>
      Assert.AreEqual("Ache", DSA034CodeFixProvider.NormalizeWord("Aches"));

   // --- -xes → -x ---

   [TestMethod]
   public void Xes_Boxes() =>
      Assert.AreEqual("Box", DSA034CodeFixProvider.NormalizeWord("Boxes"));

   [TestMethod]
   public void Xes_Indexes() =>
      Assert.AreEqual("Index", DSA034CodeFixProvider.NormalizeWord("Indexes"));

   [TestMethod]
   public void Xes_Mixes() =>
      Assert.AreEqual("Mix", DSA034CodeFixProvider.NormalizeWord("Mixes"));

   // --- -zes → -ze ---

   [TestMethod]
   public void Zes_Freezes() =>
      Assert.AreEqual("Freeze", DSA034CodeFixProvider.NormalizeWord("Freezes"));

   [TestMethod]
   public void Zes_Sizes() =>
      Assert.AreEqual("Size", DSA034CodeFixProvider.NormalizeWord("Sizes"));

   [TestMethod]
   public void Zes_Analyzes() =>
      Assert.AreEqual("Analyze", DSA034CodeFixProvider.NormalizeWord("Analyzes"));

   // --- -oes (irregular dictionary for consonant+oes, general -s for others) ---

   [TestMethod]
   public void Oes_Heroes_Irregular() =>
      Assert.AreEqual("Hero", DSA034CodeFixProvider.NormalizeWord("Heroes"));

   [TestMethod]
   public void Oes_Potatoes_Irregular() =>
      Assert.AreEqual("Potato", DSA034CodeFixProvider.NormalizeWord("Potatoes"));

   [TestMethod]
   public void Oes_Shoes_GeneralS() =>
      Assert.AreEqual("Shoe", DSA034CodeFixProvider.NormalizeWord("Shoes"));

   [TestMethod]
   public void Oes_Canoes_GeneralS() =>
      Assert.AreEqual("Canoe", DSA034CodeFixProvider.NormalizeWord("Canoes"));

   // --- General -s removal ---

   [TestMethod]
   public void SimpleS_Readers() =>
      Assert.AreEqual("Reader", DSA034CodeFixProvider.NormalizeWord("Readers"));

   [TestMethod]
   public void SimpleS_Frames() =>
      Assert.AreEqual("Frame", DSA034CodeFixProvider.NormalizeWord("Frames"));

   [TestMethod]
   public void SimpleS_Items() =>
      Assert.AreEqual("Item", DSA034CodeFixProvider.NormalizeWord("Items"));

   [TestMethod]
   public void SimpleS_Handlers() =>
      Assert.AreEqual("Handler", DSA034CodeFixProvider.NormalizeWord("Handlers"));

   [TestMethod]
   public void SimpleS_Controllers() =>
      Assert.AreEqual("Controller", DSA034CodeFixProvider.NormalizeWord("Controllers"));

   [TestMethod]
   public void SimpleS_Services() =>
      Assert.AreEqual("Service", DSA034CodeFixProvider.NormalizeWord("Services"));

   [TestMethod]
   public void SimpleS_Nodes() =>
      Assert.AreEqual("Node", DSA034CodeFixProvider.NormalizeWord("Nodes"));

   [TestMethod]
   public void SimpleS_Values() =>
      Assert.AreEqual("Value", DSA034CodeFixProvider.NormalizeWord("Values"));

   [TestMethod]
   public void SimpleS_Events() =>
      Assert.AreEqual("Event", DSA034CodeFixProvider.NormalizeWord("Events"));

   [TestMethod]
   public void SimpleS_Buffers() =>
      Assert.AreEqual("Buffer", DSA034CodeFixProvider.NormalizeWord("Buffers"));

   [TestMethod]
   public void SimpleS_Connections() =>
      Assert.AreEqual("Connection", DSA034CodeFixProvider.NormalizeWord("Connections"));

   [TestMethod]
   public void SimpleS_Settings() =>
      Assert.AreEqual("Setting", DSA034CodeFixProvider.NormalizeWord("Settings"));

   [TestMethod]
   public void SimpleS_Responses() =>
      Assert.AreEqual("Response", DSA034CodeFixProvider.NormalizeWord("Responses"));

   [TestMethod]
   public void SimpleS_Databases() =>
      Assert.AreEqual("Database", DSA034CodeFixProvider.NormalizeWord("Databases"));

   [TestMethod]
   public void SimpleS_Drives() =>
      Assert.AreEqual("Drive", DSA034CodeFixProvider.NormalizeWord("Drives"));

   [TestMethod]
   public void SimpleS_Archives() =>
      Assert.AreEqual("Archive", DSA034CodeFixProvider.NormalizeWord("Archives"));

   [TestMethod]
   public void SimpleS_Curves() =>
      Assert.AreEqual("Curve", DSA034CodeFixProvider.NormalizeWord("Curves"));

   // --- -zzes guard (skips -zes rule, falls to general -s) ---

   [TestMethod]
   public void Zzes_Buzzes_FallsToGeneralS() =>
      Assert.AreEqual("Buzze", DSA034CodeFixProvider.NormalizeWord("Buzzes"));

   [TestMethod]
   public void Zzes_Fizzes_FallsToGeneralS() =>
      Assert.AreEqual("Fizze", DSA034CodeFixProvider.NormalizeWord("Fizzes"));

   // --- All-uppercase input ---

   [TestMethod]
   public void AllUppercase_Irregular_MICE() =>
      Assert.AreEqual("Mouse", DSA034CodeFixProvider.NormalizeWord("MICE"));

   [TestMethod]
   public void AllUppercase_Ies_FACTORIES() =>
      Assert.AreEqual("FACTORY", DSA034CodeFixProvider.NormalizeWord("FACTORIES"));

   [TestMethod]
   public void AllUppercase_GeneralS_NODES() =>
      Assert.AreEqual("NODE", DSA034CodeFixProvider.NormalizeWord("NODES"));

   // --- Short words (3 chars) ending in s ---

   [TestMethod]
   public void ThreeLetterWord_Ads() =>
      Assert.AreEqual("Ad", DSA034CodeFixProvider.NormalizeWord("Ads"));

   [TestMethod]
   public void ThreeLetterWord_Bus_ProtectedByUs() =>
      Assert.AreEqual("Bus", DSA034CodeFixProvider.NormalizeWord("Bus"));

   // --- -ches with other vowels before ---

   [TestMethod]
   public void Ches_Pouches_ConsonantBefore() =>
      Assert.AreEqual("Pouch", DSA034CodeFixProvider.NormalizeWord("Pouches"));

   [TestMethod]
   public void Ches_Touches_ConsonantBefore() =>
      Assert.AreEqual("Touch", DSA034CodeFixProvider.NormalizeWord("Touches"));

   // --- Rule ordering edge cases ---

   [TestMethod]
   public void Ies_Lassies_MatchesIesNotSses() =>
      Assert.AreEqual("Lassy", DSA034CodeFixProvider.NormalizeWord("Lassies"));

   [TestMethod]
   public void NotPlural_Virus_ProtectedByUs() =>
      Assert.AreEqual("Virus", DSA034CodeFixProvider.NormalizeWord("Virus"));

   [TestMethod]
   public void NotPlural_Basis_ProtectedByIs() =>
      Assert.AreEqual("Basis", DSA034CodeFixProvider.NormalizeWord("Basis"));

   // --- Irregular case transfer: lowercase input ---

   [TestMethod]
   public void Irregular_Lowercase_children() =>
      Assert.AreEqual("child", DSA034CodeFixProvider.NormalizeWord("children"));

   [TestMethod]
   public void Irregular_Lowercase_mice() =>
      Assert.AreEqual("mouse", DSA034CodeFixProvider.NormalizeWord("mice"));

   // --- Words that don't end in s ---

   [TestMethod]
   public void NoS_Child_Unchanged() =>
      Assert.AreEqual("Child", DSA034CodeFixProvider.NormalizeWord("Child"));

   [TestMethod]
   public void NoS_Factory_Unchanged() =>
      Assert.AreEqual("Factory", DSA034CodeFixProvider.NormalizeWord("Factory"));
}
