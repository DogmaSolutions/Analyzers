using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA034CodeFixTests
{

   [TestMethod]
   public async Task Topic_WithStruct()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 14
";
      // Topics: Axis(3) from _axisCount, ScaleAxis, ResetAxis.
      // X, Y fields have no PascalCase words of length > 1, so they go to Misc.
      // 3 output files: Ctors, Axis, Misc.
      var source = @"namespace TestApp
{
    public struct {|#0:MyVector|}
    {
        public MyVector(int x, int y) { X = x; Y = y; }

        private int _axisCount;
        public int X;
        public int Y;

        public void ScaleAxis(int factor) { }

        public void ResetAxis() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial struct MyVector
    {
        public MyVector(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}";

      var fixedAxis = @"namespace TestApp
{
    public partial struct MyVector
    {
        private int _axisCount;
        public void ScaleAxis(int factor)
        {
        }

        public void ResetAxis()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial struct MyVector
    {
        public int X;
        public int Y;
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
            .WithArguments("Test0.cs", 15, 14));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyVector.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyVector.Axis.cs", fixedAxis));
      test.FixedState.Sources.Add(("/0/MyVector.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_FileScopedNamespace()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 11
";
      var source = @"namespace TestApp;

public class {|#0:MyDispatcher|}
{
    public MyDispatcher() { }
    public void ImportOrder() { }
    public void ExportOrder() { }
    public void ClearCache() { }
    public void WarmCache() { }
    public void Shutdown() { }
    public void Restart() { }
}";

      var fixedCtors = @"namespace TestApp;
public partial class MyDispatcher
{
    public MyDispatcher()
    {
    }
}";

      var fixedCache = @"namespace TestApp;
public partial class MyDispatcher
{
    public void ClearCache()
    {
    }

    public void WarmCache()
    {
    }
}";

      var fixedOrder = @"namespace TestApp;
public partial class MyDispatcher
{
    public void ImportOrder()
    {
    }

    public void ExportOrder()
    {
    }
}";

      var fixedMisc = @"namespace TestApp;
public partial class MyDispatcher
{
    public void Shutdown()
    {
    }

    public void Restart()
    {
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 19)
            .WithArguments("Test0.cs", 12, 11));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyDispatcher.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyDispatcher.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyDispatcher.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyDispatcher.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithRecordStruct()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 12
";
      var source = @"namespace TestApp
{
    public record struct {|#0:MyMeasure|}
    {
        public int Value;

        public MyMeasure(int v) { Value = v; }

        public void Scale(int factor) { }

        private void Reset() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial record struct MyMeasure
    {
        public int Value;
        public MyMeasure(int v)
        {
            Value = v;
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial record struct MyMeasure
    {
        public void Scale(int factor)
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial record struct MyMeasure
    {
        private void Reset()
        {
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 13, 12));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyMeasure.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyMeasure.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyMeasure.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_PreservesBaseListOnCtorsFile()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 14 lines, threshold 13. Ctors file gets base list (: ServiceBase).
      // Topics: Order(2), Cache(2). Alpha, Beta → Misc.
      var baseSource = @"namespace TestApp
{
    public abstract class ServiceBase { }
}";

      var source = @"namespace TestApp
{
    public class {|#0:MyProcessor|} : ServiceBase
    {
        public MyProcessor() { }

        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyProcessor : ServiceBase
    {
        public MyProcessor()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyProcessor
    {
        public void ClearCache()
        {
        }

        public void WarmCache()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyProcessor
    {
        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyProcessor
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
      test.TestState.Sources.Add(("ServiceBase.cs", baseSource));
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 14, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyProcessor.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("ServiceBase.cs", baseSource));
      test.FixedState.Sources.Add(("/0/MyProcessor.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyProcessor.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyProcessor.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }



   [TestMethod]
   public async Task Topic_UnderscoresInMemberNamesSplitAsWordBoundaries()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // Member names use underscores: e.g. Check_Quota, Verify_Quota.
      // Underscore splitting produces ["Check"(excl),"Quota"] and ["Verify"(excl),"Quota"] → Quota(2).
      // Without underscore splitting, "Check_Quota" is one word — no topic possible.
      // "Validate" and "Ensure" are also excluded, so only "Quota" and "Limit" remain as topic words.
      // 14 lines, threshold 13.
      var source = @"namespace TestApp
{
    public class {|#0:MyGuard|}
    {
        public MyGuard() { }

        public void Check_Quota() { }
        public void Verify_Quota() { }
        public void Validate_Limit() { }
        public void Ensure_Limit() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyGuard
    {
        public MyGuard()
        {
        }
    }
}";

      var fixedLimit = @"namespace TestApp
{
    public partial class MyGuard
    {
        public void Validate_Limit()
        {
        }

        public void Ensure_Limit()
        {
        }
    }
}";

      var fixedQuota = @"namespace TestApp
{
    public partial class MyGuard
    {
        public void Check_Quota()
        {
        }

        public void Verify_Quota()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyGuard
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
      test.FixedState.Sources.Add(("MyGuard.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyGuard.Limit.cs", fixedLimit));
      test.FixedState.Sources.Add(("/0/MyGuard.Quota.cs", fixedQuota));
      test.FixedState.Sources.Add(("/0/MyGuard.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_TrailingUnderscoreDoesNotProduceEmptyWord()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // Trailing underscores on member names should not produce empty words or ugly topic names.
      // "LoadWidget_" → ["Load","Widget"] (Load excluded → word is "Widget").
      // "SaveWidget_" → ["Save","Widget"] (Save excluded → word is "Widget").
      // Widget freq=2 → topic.
      // 14 lines, threshold 13.
      var source = @"namespace TestApp
{
    public class {|#0:MyFactory|}
    {
        public MyFactory() { }

        public void LoadWidget_() { }
        public void SaveWidget_() { }
        public void ClearCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyFactory
    {
        public MyFactory()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyFactory
    {
        public void ClearCache()
        {
        }

        public void WarmCache()
        {
        }
    }
}";

      var fixedWidget = @"namespace TestApp
{
    public partial class MyFactory
    {
        public void LoadWidget_()
        {
        }

        public void SaveWidget_()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyFactory
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
      test.FixedState.Sources.Add(("MyFactory.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyFactory.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyFactory.Widget.cs", fixedWidget));
      test.FixedState.Sources.Add(("/0/MyFactory.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }



   [TestMethod]
   public async Task Topic_PluralAndSingularMergedIntoSameTopic()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // "Orders" normalizes to "Order", merging with "ImportOrder" → Order(2).
      // "Widgets" normalizes to "Widget", merging with "PaintWidget" → Widget(2).
      var source = @"namespace TestApp
{
    public class {|#0:MyEngine|}
    {
        public MyEngine() { }

        public void ProcessOrders() { }
        public void ImportOrder() { }
        public void RenderWidgets() { }
        public void PaintWidget() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyEngine
    {
        public MyEngine()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyEngine
    {
        public void ProcessOrders()
        {
        }

        public void ImportOrder()
        {
        }
    }
}";

      var fixedWidget = @"namespace TestApp
{
    public partial class MyEngine
    {
        public void RenderWidgets()
        {
        }

        public void PaintWidget()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyEngine
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
      test.FixedState.Sources.Add(("MyEngine.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyEngine.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyEngine.Widget.cs", fixedWidget));
      test.FixedState.Sources.Add(("/0/MyEngine.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_PluralExcludedWordFiltered()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // "Reads" normalizes to "Read" which is in the excluded list → filtered out.
      // "Writes" normalizes to "Write" which is in the excluded list → filtered out.
      // So methods like "ConcurrentReads" only contribute "Concurrent" after normalization.
      // Topics: Concurrent(2), Packet(2).
      // 14 lines, threshold 13.
      var source = @"namespace TestApp
{
    public class {|#0:MyChannel|}
    {
        public MyChannel() { }

        public void ConcurrentReads() { }
        public void ConcurrentWrites() { }
        public void InspectPacket() { }
        public void DropPacket() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyChannel
    {
        public MyChannel()
        {
        }
    }
}";

      var fixedConcurrent = @"namespace TestApp
{
    public partial class MyChannel
    {
        public void ConcurrentReads()
        {
        }

        public void ConcurrentWrites()
        {
        }
    }
}";

      var fixedPacket = @"namespace TestApp
{
    public partial class MyChannel
    {
        public void InspectPacket()
        {
        }

        public void DropPacket()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyChannel
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
      test.FixedState.Sources.Add(("MyChannel.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyChannel.Concurrent.cs", fixedConcurrent));
      test.FixedState.Sources.Add(("/0/MyChannel.Packet.cs", fixedPacket));
      test.FixedState.Sources.Add(("/0/MyChannel.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

}
