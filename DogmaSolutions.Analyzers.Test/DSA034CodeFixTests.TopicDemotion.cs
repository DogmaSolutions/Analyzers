using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA034CodeFixTests
{

   [TestMethod]
   public async Task Topic_SinglePropertyDemotedToMisc()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 15
";
      // 16 lines, threshold 15. Topic "Order" has only 1 property (OrderLabel) → not viable → demoted.
      // "Cache" has 2 methods + 1 field → viable. "Sync" is excluded from topic words.
      var source = @"namespace TestApp
{
    public class {|#0:MyRouter|}
    {
        public MyRouter() { }

        public string OrderLabel { get; set; }
        private bool _cacheReady;

        public void SyncOrderCache() { }
        public void WarmCache() { }

        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyRouter
    {
        public MyRouter()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyRouter
    {
        private bool _cacheReady;
        public void SyncOrderCache()
        {
        }

        public void WarmCache()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyRouter
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }

        public string OrderLabel { get; set; }
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
            .WithArguments("Test0.cs", 16, 15));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyRouter.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyRouter.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyRouter.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_SingleEventDemotedToMisc()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 15
";
      // 16 lines, threshold 15. Topic "Order" has only 1 event field (OrderChanged) → not viable → demoted.
      // "Cache" has 2 methods + 1 field → viable. "Sync" is excluded from topic words.
      var source = @"namespace TestApp
{
    public class {|#0:MyTracer|}
    {
        public MyTracer() { }

        public event System.Action OrderChanged;
        private bool _cacheReady;

        public void SyncOrderCache() { }
        public void WarmCache() { }

        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyTracer
    {
        public MyTracer()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyTracer
    {
        private bool _cacheReady;
        public void SyncOrderCache()
        {
        }

        public void WarmCache()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyTracer
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }

        public event System.Action OrderChanged;
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
            .WithArguments("Test0.cs", 16, 15));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyTracer.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyTracer.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyTracer.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_FieldAndPropertySurviveAsTopic()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 14 lines, threshold 13. Topic "Order" has 1 field + 1 property → viable (case B: 2 non-methods).
      var source = @"namespace TestApp
{
    public class {|#0:MyRouter|}
    {
        public MyRouter() { }

        private int _orderCount;
        public string OrderLabel { get; set; }
        public void RefreshCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyRouter
    {
        public MyRouter()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyRouter
    {
        public void RefreshCache()
        {
        }

        public void WarmCache()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyRouter
    {
        private int _orderCount;
        public string OrderLabel { get; set; }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyRouter
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
      test.FixedState.Sources.Add(("MyRouter.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyRouter.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyRouter.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyRouter.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_TwoTopicsDemotedSimultaneously()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 16
";
      // 17 lines, threshold 16. Topics "Order" (1 field) and "Alert" (1 field) both non-viable → demoted.
      // "Cache" has 2 methods + 1 field → viable. "Sync" excluded from topic words.
      var source = @"namespace TestApp
{
    public class {|#0:MyGateway|}
    {
        public MyGateway() { }

        private int _orderCount;
        private int _alertLevel;

        public void SyncOrderCache() { }
        public void SyncAlertCache() { }
        private bool _cacheEnabled;

        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyGateway
    {
        public MyGateway()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyGateway
    {
        public void SyncOrderCache()
        {
        }

        public void SyncAlertCache()
        {
        }

        private bool _cacheEnabled;
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyGateway
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }

        private int _alertLevel;
        private int _orderCount;
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
            .WithArguments("Test0.cs", 17, 16));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyGateway.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyGateway.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyGateway.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }



   [TestMethod]
   public async Task Visibility_WithStruct()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      var source = @"namespace TestApp
{
    public struct {|#0:MyPoint|}
    {
        public int X;
        public int Y;

        public MyPoint(int x, int y) { X = x; Y = y; }

        public double DistanceTo(MyPoint other) { return 0; }
        private void Normalize() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial struct MyPoint
    {
        public int X;
        public int Y;
        public MyPoint(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial struct MyPoint
    {
        public double DistanceTo(MyPoint other)
        {
            return 0;
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial struct MyPoint
    {
        private void Normalize()
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
            .WithArguments("Test0.cs", 13, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyPoint.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyPoint.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyPoint.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithRecord()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      var source = @"namespace TestApp
{
    public record {|#0:MyEvent|}
    {
        public int Id { get; init; }

        public MyEvent() { }

        public void Apply() { }
        private void Validate() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial record MyEvent
    {
        public int Id { get; init; }

        public MyEvent()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial record MyEvent
    {
        public void Apply()
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial record MyEvent
    {
        private void Validate()
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
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyEvent.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyEvent.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyEvent.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithInterface()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      var source = @"namespace TestApp
{
    public interface {|#0:IMyWidget|}
    {
        int Width { get; }
        int Height { get; }

        void Render();
        void Resize(int w, int h);
        void Dispose();
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial interface IMyWidget
    {
        int Width { get; }

        int Height { get; }

        void Dispose();
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial interface IMyWidget
    {
        void Render();
        void Resize(int w, int h);
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
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("IMyWidget.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/IMyWidget.Private.cs", fixedPrivate));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_NoFixForSingleEnum()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 5
";
      var source = @"namespace TestApp
{
    public enum Color
    {
        Red,
        Green,
        Blue,
        Yellow
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 10, 5));

      await test.RunAsync().ConfigureAwait(false);
   }

}
