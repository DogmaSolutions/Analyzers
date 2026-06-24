using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA034CodeFixTests
{

   [TestMethod]
   public async Task Topic_IesPluralNormalized()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // "LoadFactories" → normalize → ["Load"(excl),"Factory"] → ["Factory"]
      // "BuildFactory" → ["Build","Factory"] → ["Build","Factory"]
      // "GetEntries" → ["Get"(excl),"Entry"] → ["Entry"]
      // "PurgeEntry" → ["Purge","Entry"] → ["Purge","Entry"]
      // Topics: Entry(2), Factory(2). Build(1), Purge(1) too low.
      // 14 lines, threshold 13.
      var source = @"namespace TestApp
{
    public class {|#0:MyRegistry|}
    {
        public MyRegistry() { }

        public void LoadFactories() { }
        public void BuildFactory() { }
        public void GetEntries() { }
        public void PurgeEntry() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyRegistry
    {
        public MyRegistry()
        {
        }
    }
}";

      var fixedEntry = @"namespace TestApp
{
    public partial class MyRegistry
    {
        public void GetEntries()
        {
        }

        public void PurgeEntry()
        {
        }
    }
}";

      var fixedFactory = @"namespace TestApp
{
    public partial class MyRegistry
    {
        public void LoadFactories()
        {
        }

        public void BuildFactory()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyRegistry
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 14, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyRegistry.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyRegistry.Entry.cs", fixedEntry));
      test.FixedState.Sources.Add(("/0/MyRegistry.Factory.cs", fixedFactory));
      test.FixedState.Sources.Add(("/0/MyRegistry.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }



   [TestMethod]
   public async Task Topic_EmptyTopicPrunedAndSlotRefilledFromCandidates()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 17
dotnet_diagnostic.DSA034.max_topics = 3
";
      // Tag(3) and Zone(3) tie; Zone matches first in word order, so Tag gets 0 members.
      // Tag is pruned, Reward fills the slot from the 4th candidate.
      // Result: Zone(3), Pulse(2), Reward(2). Misc: Alpha, Beta.
      var source = @"namespace TestApp
{
    public class {|#0:MyAgent|}
    {
        public MyAgent() { }

        public void BuildZoneTag() { }
        public void PaintZoneTag() { }
        public void CheckZoneTag() { }

        public void ClaimReward() { }
        public void GrantReward() { }
        public void MonitorPulse() { }
        public void CheckPulse() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyAgent
    {
        public MyAgent()
        {
        }
    }
}";

      var fixedPulse = @"namespace TestApp
{
    public partial class MyAgent
    {
        public void MonitorPulse()
        {
        }

        public void CheckPulse()
        {
        }
    }
}";

      var fixedReward = @"namespace TestApp
{
    public partial class MyAgent
    {
        public void ClaimReward()
        {
        }

        public void GrantReward()
        {
        }
    }
}";

      var fixedZone = @"namespace TestApp
{
    public partial class MyAgent
    {
        public void BuildZoneTag()
        {
        }

        public void PaintZoneTag()
        {
        }

        public void CheckZoneTag()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyAgent
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 18, 17));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyAgent.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyAgent.Zone.cs", fixedZone));
      test.FixedState.Sources.Add(("/0/MyAgent.Pulse.cs", fixedPulse));
      test.FixedState.Sources.Add(("/0/MyAgent.Reward.cs", fixedReward));
      test.FixedState.Sources.Add(("/0/MyAgent.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_EmptyTopicPrunedButNoReplacementCandidates()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 17
dotnet_diagnostic.DSA034.max_topics = 3
";
      // Freq: Orbit(3), Flare(3), Ring(2). max_topics=3: Orbit, Flare, Ring.
      // Orbit members: LaunchOrbitFlare, TrackOrbitFlare, ScanOrbitFlare → all have Flare(3) too.
      // Assignment: word "Orbit" matches first (in word list for LaunchOrbitFlare → [Launch,Orbit,Flare]),
      //   Orbit(3) == Flare(3) → first match wins → Orbit.
      // Ring: PaintRing, DrawRing → Ring(2) → Ring.
      // Flare: all Flare members went to Orbit → Flare empty!
      // No 4th candidate exists → pruned without refill.
      // Result: Orbit(3), Ring(2). Misc: Alpha, Beta.
      var source = @"namespace TestApp
{
    public class {|#0:MyOrbit|}
    {
        public MyOrbit() { }

        public void LaunchOrbitFlare() { }
        public void TrackOrbitFlare() { }
        public void ScanOrbitFlare() { }

        public void PaintRing() { }
        public void DrawRing() { }
        public void MeltRing() { }

        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyOrbit
    {
        public MyOrbit()
        {
        }
    }
}";

      var fixedOrbit = @"namespace TestApp
{
    public partial class MyOrbit
    {
        public void LaunchOrbitFlare()
        {
        }

        public void TrackOrbitFlare()
        {
        }

        public void ScanOrbitFlare()
        {
        }
    }
}";

      var fixedRing = @"namespace TestApp
{
    public partial class MyOrbit
    {
        public void PaintRing()
        {
        }

        public void DrawRing()
        {
        }

        public void MeltRing()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyOrbit
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 18, 17));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyOrbit.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyOrbit.Orbit.cs", fixedOrbit));
      test.FixedState.Sources.Add(("/0/MyOrbit.Ring.cs", fixedRing));
      test.FixedState.Sources.Add(("/0/MyOrbit.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_MultipleEmptyTopicsPruned()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 17
dotnet_diagnostic.DSA034.max_topics = 4
";
      // Coin(3), Seal(3), Stamp(3) all share members; Coin matches first in word order.
      // Seal and Stamp both get 0 members and are pruned. No replacement candidates.
      // Result: Coin(3), Crest(3). Misc: Alpha, Beta.
      var source = @"namespace TestApp
{
    public class {|#0:MyMint|}
    {
        public MyMint() { }

        public void MintCoinStampSeal() { }
        public void CastCoinStampSeal() { }
        public void BurnCoinStampSeal() { }

        public void PaintCrest() { }
        public void CarveCrest() { }
        public void MoldCrest() { }

        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyMint
    {
        public MyMint()
        {
        }
    }
}";

      var fixedCoin = @"namespace TestApp
{
    public partial class MyMint
    {
        public void MintCoinStampSeal()
        {
        }

        public void CastCoinStampSeal()
        {
        }

        public void BurnCoinStampSeal()
        {
        }
    }
}";

      var fixedCrest = @"namespace TestApp
{
    public partial class MyMint
    {
        public void PaintCrest()
        {
        }

        public void CarveCrest()
        {
        }

        public void MoldCrest()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyMint
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 18, 17));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyMint.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyMint.Coin.cs", fixedCoin));
      test.FixedState.Sources.Add(("/0/MyMint.Crest.cs", fixedCrest));
      test.FixedState.Sources.Add(("/0/MyMint.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_UnderscoreAndPluralCombined()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
dotnet_diagnostic.DSA034.max_topics = 5
dotnet_diagnostic.DSA034.excluded_topic_words = Load,Save,Check,Get,Set,Process
";
      // Underscore splitting + plural normalization combined:
      // "Load_Widgets" splits to [Load, Widget] after normalization; excluded prefixes filter out.
      // Topics: Invoice(2), Widget(2). Misc: Alpha, Beta.
      var source = @"namespace TestApp
{
    public class {|#0:MyBridge|}
    {
        public MyBridge() { }

        public void Load_Widgets() { }
        public void Save_Widget() { }
        public void Process_Invoices() { }
        public void Check_Invoice() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyBridge
    {
        public MyBridge()
        {
        }
    }
}";

      var fixedInvoice = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void Process_Invoices()
        {
        }

        public void Check_Invoice()
        {
        }
    }
}";

      var fixedWidget = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void Load_Widgets()
        {
        }

        public void Save_Widget()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 14, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyBridge.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBridge.Invoice.cs", fixedInvoice));
      test.FixedState.Sources.Add(("/0/MyBridge.Widget.cs", fixedWidget));
      test.FixedState.Sources.Add(("/0/MyBridge.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

}
