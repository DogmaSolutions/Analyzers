using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA034CodeFixTests
{
   #region Split by visibility

   [TestMethod]
   public async Task Visibility_SplitsByMemberVisibility()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 15
";
      var source = @"using System;

namespace TestApp
{
    public class {|#0:MyService|} : IDisposable
    {
        private readonly int _counter;

        public MyService() { }
        public void Dispose() { }

        public void DoWork() { }
        internal void Validate() { }
        protected virtual void OnCompleted() { }
        private void LogStep() { }
    }
}";

      var fixedCtors = @"using System;

namespace TestApp
{
    public partial class MyService : IDisposable
    {
        private readonly int _counter;
        public MyService()
        {
        }

        public void Dispose()
        {
        }
    }
}";

      var fixedInternal = @"using System;

namespace TestApp
{
    public partial class MyService
    {
        internal void Validate()
        {
        }
    }
}";

      var fixedPrivate = @"using System;

namespace TestApp
{
    public partial class MyService
    {
        private void LogStep()
        {
        }
    }
}";

      var fixedProtected = @"using System;

namespace TestApp
{
    public partial class MyService
    {
        protected virtual void OnCompleted()
        {
        }
    }
}";

      var fixedPublic = @"using System;

namespace TestApp
{
    public partial class MyService
    {
        public void DoWork()
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
            .WithSpan(1, 1, 1, 14)
            .WithArguments("Test0.cs", 17, 15));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyService.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyService.Internal.cs", fixedInternal));
      test.FixedState.Sources.Add(("/0/MyService.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyService.Protected.cs", fixedProtected));
      test.FixedState.Sources.Add(("/0/MyService.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_NoFixForMultiTypeFile()
   {
      var source = @"namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithNestedType()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 12
";
      var source = @"namespace TestApp
{
    public class {|#0:MyContainer|}
    {
        private int _state;

        public MyContainer() { }

        public void Execute() { }

        public enum StatusCode { Active, Inactive }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyContainer
    {
        private int _state;
        public MyContainer()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyContainer
    {
        public void Execute()
        {
        }
    }
}";

      var fixedNested = @"namespace TestApp
{
    public partial class MyContainer
    {
        public enum StatusCode
        {
            Active,
            Inactive
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
            .WithArguments("Test0.cs", 13, 12));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyContainer.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyContainer.Public.cs", fixedPublic));
      test.FixedState.Sources.Add(("/0/MyContainer.StatusCode.cs", fixedNested));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Split by topic

   [TestMethod]
   public async Task Topic_SplitsByTopicWords()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";

      // Topics: Order(2), Permission(2), Cache(2), Alert(2). Shutdown/Restart → Misc.
      // Note: Find, Check, Send, Emit are excluded words — they don't become topics.
      var source = @"namespace TestApp
{
    public class {|#0:MyEngine|}
    {
        public MyEngine() { }
        public void ImportOrder() { }
        public void ExportOrder() { }
        public void GrantPermission() { }
        public void RevokePermission() { }
        public void ClearCache() { }
        public void WarmCache() { }
        public void DismissAlert() { }
        public void QuietAlert() { }
        public void Shutdown() { }
        public void Restart() { }
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

      var fixedAlert = @"namespace TestApp
{
    public partial class MyEngine
    {
        public void DismissAlert()
        {
        }

        public void QuietAlert()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyEngine
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
    public partial class MyEngine
    {
        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }
    }
}";

      var fixedPermission = @"namespace TestApp
{
    public partial class MyEngine
    {
        public void GrantPermission()
        {
        }

        public void RevokePermission()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyEngine
    {
        public void Shutdown()
        {
        }

        public void Restart()
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
            .WithArguments("Test0.cs", 17, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyEngine.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyEngine.Alert.cs", fixedAlert));
      test.FixedState.Sources.Add(("/0/MyEngine.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyEngine.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyEngine.Permission.cs", fixedPermission));
      test.FixedState.Sources.Add(("/0/MyEngine.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_UnmatchedMembersGoToMisc()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 15
";
      // Topics: Order(2), Signal(2). Shutdown/Restart → no topic match → Misc.
      // Note: "Emit" is an excluded word, so EmitSignal only contributes "Signal".
      var source = @"namespace TestApp
{
    public class {|#0:MyHandler|}
    {
        public MyHandler() { }

        public void ImportOrder() { }
        public void ExportOrder() { }

        public void EmitSignal() { }
        public void MuteSignal() { }

        public void Shutdown() { }
        public void Restart() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyHandler
    {
        public MyHandler()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyHandler
    {
        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }
    }
}";

      var fixedSignal = @"namespace TestApp
{
    public partial class MyHandler
    {
        public void EmitSignal()
        {
        }

        public void MuteSignal()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyHandler
    {
        public void Shutdown()
        {
        }

        public void Restart()
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
            .WithArguments("Test0.cs", 16, 15));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyHandler.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyHandler.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyHandler.Signal.cs", fixedSignal));
      test.FixedState.Sources.Add(("/0/MyHandler.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_FieldsAndPropertiesAssignedByTopic()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 15
";
      var source = @"namespace TestApp
{
    public class {|#0:MyProcessor|}
    {
        public MyProcessor() { }

        private int _orderCount;
        public void ImportOrder() { }
        public void ExportOrder() { }

        private bool _cacheEnabled;
        public void ClearCache() { }
        public void WarmCache() { }

        public void Shutdown() { }
        public void Restart() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyProcessor
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
        private bool _cacheEnabled;
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
        private int _orderCount;
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
        public void Shutdown()
        {
        }

        public void Restart()
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
            .WithArguments("Test0.cs", 18, 15));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyProcessor.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyProcessor.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyProcessor.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyProcessor.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_SingleFieldDemotedToMisc()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 15
";
      // 16 lines, threshold 15. Topic "Order" has only 1 field (_orderCount) → not viable → demoted.
      // "Cache" has 2 methods + 1 field → viable. "Sync" is excluded from topic words.
      var source = @"namespace TestApp
{
    public class {|#0:MyHandler|}
    {
        public MyHandler() { }

        private int _orderCount;
        private bool _cacheEnabled;

        public void SyncOrderCache() { }
        public void WarmCache() { }

        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyHandler
    {
        public MyHandler()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyHandler
    {
        private bool _cacheEnabled;
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
    public partial class MyHandler
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }

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
            .WithArguments("Test0.cs", 16, 15));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyHandler.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyHandler.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyHandler.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_TwoFieldsSurviveAsTopic()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 14 lines, threshold 13. Topic "Order" has 2 fields → viable (2 non-method items).
      var source = @"namespace TestApp
{
    public class {|#0:MyHandler|}
    {
        public MyHandler() { }

        private int _orderCount;
        private string _orderLabel;
        public void RefreshCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyHandler
    {
        public MyHandler()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyHandler
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
    public partial class MyHandler
    {
        private int _orderCount;
        private string _orderLabel;
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyHandler
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
      test.FixedState.Sources.Add(("MyHandler.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyHandler.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyHandler.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyHandler.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

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

   #endregion

   #region Visibility — type variety

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

   #endregion

   #region Visibility — namespace variations

   [TestMethod]
   public async Task Visibility_FileScopedNamespace()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      var source = @"namespace TestApp;

public class {|#0:MyGateway|}
{
    private int _port;

    public MyGateway() { }

    public void Connect() { }
    internal void Ping() { }
    private void Reconnect() { }
}";

      var fixedCtors = @"namespace TestApp;
public partial class MyGateway
{
    private int _port;
    public MyGateway()
    {
    }
}";

      var fixedInternal = @"namespace TestApp;
public partial class MyGateway
{
    internal void Ping()
    {
    }
}";

      var fixedPrivate = @"namespace TestApp;
public partial class MyGateway
{
    private void Reconnect()
    {
    }
}";

      var fixedPublic = @"namespace TestApp;
public partial class MyGateway
{
    public void Connect()
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
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyGateway.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyGateway.Internal.cs", fixedInternal));
      test.FixedState.Sources.Add(("/0/MyGateway.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyGateway.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Visibility — member classification edge cases

   [TestMethod]
   public async Task Visibility_WithDisposeDestructorAndProperties()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      var source = @"using System;

namespace TestApp
{
    public class {|#0:MyResource|} : IDisposable
    {
        private bool _disposed;
        public string Name { get; set; }

        public MyResource() { }
        ~MyResource() { }
        public void Dispose() { }

        public void DoWork() { }
        private void Cleanup() { }
    }
}";

      var fixedCtors = @"using System;

namespace TestApp
{
    public partial class MyResource : IDisposable
    {
        private bool _disposed;
        public string Name { get; set; }

        public MyResource()
        {
        }

        ~MyResource()
        {
        }

        public void Dispose()
        {
        }
    }
}";

      var fixedPublic = @"using System;

namespace TestApp
{
    public partial class MyResource
    {
        public void DoWork()
        {
        }
    }
}";

      var fixedPrivate = @"using System;

namespace TestApp
{
    public partial class MyResource
    {
        private void Cleanup()
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
            .WithSpan(1, 1, 1, 14)
            .WithArguments("Test0.cs", 17, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyResource.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyResource.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyResource.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_ProtectedInternalAndPrivateProtected()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      var source = @"namespace TestApp
{
    public class {|#0:MyBase|}
    {
        public MyBase() { }

        public void Alpha() { }
        protected internal void Beta() { }
        private protected void Gamma() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyBase
    {
        public MyBase()
        {
        }
    }
}";

      var fixedPI = @"namespace TestApp
{
    public partial class MyBase
    {
        protected internal void Beta()
        {
        }
    }
}";

      var fixedPP = @"namespace TestApp
{
    public partial class MyBase
    {
        private protected void Gamma()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyBase
    {
        public void Alpha()
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
            .WithArguments("Test0.cs", 11, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyBase.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBase.PrivateProtected.cs", fixedPP));
      test.FixedState.Sources.Add(("/0/MyBase.ProtectedInternal.cs", fixedPI));
      test.FixedState.Sources.Add(("/0/MyBase.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithGenericType()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      // Source has 13 lines (> threshold 10). Each split file has at most 10 lines (<= 10).
      // Padded with blank lines so source exceeds threshold while split files stay small.
      var source = @"namespace TestApp
{
    public class {|#0:Repository<T>|}
    {
        public Repository() { }


        public T FindById(int id) { return default; }


        private void Log() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class Repository<T>
    {
        public Repository()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class Repository<T>
    {
        public T FindById(int id)
        {
            return default;
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial class Repository<T>
    {
        private void Log()
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
      test.FixedState.Sources.Add(("Repository.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/Repository.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/Repository.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Topic — editorconfig variations

   [TestMethod]
   public async Task Topic_CustomMaxTopics()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
dotnet_diagnostic.DSA034.max_topics = 2
";
      // Topics by frequency: Order(2), Cache(2), Signal(2). max_topics=2 → top 2 alphabetically: Cache, Order.
      // Signal members → Misc
      // Padded with blank lines so source (15 lines) > threshold (13).
      // Each split file has at most 13 lines (<= 13).
      var source = @"namespace TestApp
{
    public class {|#0:MyRouter|}
    {
        public MyRouter() { }

        public void ImportOrder() { }
        public void ExportOrder() { }

        public void ClearCache() { }
        public void WarmCache() { }

        public void EmitSignal() { }
        public void MuteSignal() { }
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
    public partial class MyRouter
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
    public partial class MyRouter
    {
        public void EmitSignal()
        {
        }

        public void MuteSignal()
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
            .WithArguments("Test0.cs", 16, 13));
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
   public async Task Topic_CustomExcludedWords()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
dotnet_diagnostic.DSA034.excluded_topic_words = Order
";
      // Custom exclusion: "Order" is now excluded. Remaining topics: Import(2), Export(2).
      // "Order" doesn't become a topic.
      // Padded with blank lines so source (16 lines) > threshold (13).
      var source = @"namespace TestApp
{
    public class {|#0:MyBridge|}
    {
        public MyBridge() { }

        public void ImportOrder() { }
        public void ImportItem() { }

        public void ExportOrder() { }
        public void ExportItem() { }

        public void Shutdown() { }
        public void Restart() { }
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

      var fixedExport = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void ExportOrder()
        {
        }

        public void ExportItem()
        {
        }
    }
}";

      var fixedImport = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void ImportOrder()
        {
        }

        public void ImportItem()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void Shutdown()
        {
        }

        public void Restart()
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
            .WithArguments("Test0.cs", 16, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyBridge.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBridge.Export.cs", fixedExport));
      test.FixedState.Sources.Add(("/0/MyBridge.Import.cs", fixedImport));
      test.FixedState.Sources.Add(("/0/MyBridge.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_AllMembersUnmatched()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 14
";
      // All single-word methods — no word has frequency >= 2, so no topics. All go to Misc.
      // Source is padded with blank lines so it exceeds threshold (17 > 14).
      // Misc.cs with 2 methods = 13 lines, which is <= 14 and won't re-trigger.
      var source = @"namespace TestApp
{
    public class {|#0:MyRunner|}
    {
        public MyRunner() { }




        public void Alpha() { }




        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyRunner
    {
        public MyRunner()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyRunner
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
            .WithArguments("Test0.cs", 17, 14));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyRunner.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyRunner.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_NoNamespace()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 8
";
      // 9 lines, threshold 8 → fires. No namespace, no usings.
      var source = @"public class {|#0:MyThing|}
{
    private int _state;

    public MyThing() { }

    public void Execute() { }
    private void Reset() { }
}";

      var fixedCtors = @"public partial class MyThing
{
    private int _state;
    public MyThing()
    {
    }
}";

      var fixedPrivate = @"public partial class MyThing
{
    private void Reset()
    {
    }
}";

      var fixedPublic = @"public partial class MyThing
{
    public void Execute()
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
            .WithSpan(1, 1, 1, 21)
            .WithArguments("Test0.cs", 9, 8));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyThing.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyThing.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyThing.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_EventFieldInCtorsGroup()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      // 12 lines, threshold 10. Event field → IsCtorsGroupMember → Ctors.
      var source = @"namespace TestApp
{
    public class {|#0:MyObserver|}
    {
        public event System.Action OrderChanged;

        public MyObserver() { }

        public void Track() { }
        private void Forget() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyObserver
    {
        public event System.Action OrderChanged;
        public MyObserver()
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial class MyObserver
    {
        private void Forget()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyObserver
    {
        public void Track()
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
      test.FixedState.Sources.Add(("MyObserver.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyObserver.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyObserver.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_DisposeAsyncInCtorsGroup()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      // 12 lines, threshold 10. DisposeAsync → IsDisposeMethod → Ctors.
      var source = @"namespace TestApp
{
    public class {|#0:MyChannel|}
    {
        public MyChannel() { }

        public void DisposeAsync() { }

        public void Transmit() { }
        private void Buffer() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyChannel
    {
        public MyChannel()
        {
        }

        public void DisposeAsync()
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial class MyChannel
    {
        private void Buffer()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyChannel
    {
        public void Transmit()
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
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyChannel.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyChannel.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyChannel.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_AlreadyPartialType()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      // 12 lines, threshold 10. Already partial → EnsurePartialModifier returns same list.
      var source = @"namespace TestApp
{
    public partial class {|#0:MyWidget|}
    {
        private int _mode;

        public MyWidget() { }

        public void Draw() { }
        private void Erase() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyWidget
    {
        private int _mode;
        public MyWidget()
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial class MyWidget
    {
        private void Erase()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyWidget
    {
        public void Draw()
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
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyWidget.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyWidget.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyWidget.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Topic — type variety and namespace variations

   [TestMethod]
   public async Task Topic_NoNamespace()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      // 11 lines, threshold 10 → fires. No namespace, no usings.
      var source = @"public class {|#0:MyBroker|}
{
    public MyBroker() { }

    public void ImportOrder() { }
    public void ExportOrder() { }
    public void ClearCache() { }
    public void WarmCache() { }
    public void Alpha() { }
    public void Beta() { }
}";

      var fixedCtors = @"public partial class MyBroker
{
    public MyBroker()
    {
    }
}";

      var fixedCache = @"public partial class MyBroker
{
    public void ClearCache()
    {
    }

    public void WarmCache()
    {
    }
}";

      var fixedOrder = @"public partial class MyBroker
{
    public void ImportOrder()
    {
    }

    public void ExportOrder()
    {
    }
}";

      var fixedMisc = @"public partial class MyBroker
{
    public void Alpha()
    {
    }

    public void Beta()
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
            .WithSpan(1, 1, 1, 22)
            .WithArguments("Test0.cs", 11, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyBroker.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBroker.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyBroker.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyBroker.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_DestructorInCtorsGroup()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 14 lines, threshold 13. Destructor → IsCtorsGroupMemberForTopic → Ctors.
      var source = @"namespace TestApp
{
    public class {|#0:MyBuffer|}
    {
        ~MyBuffer() { }

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
    public partial class MyBuffer
    {
        ~MyBuffer()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyBuffer
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
    public partial class MyBuffer
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
    public partial class MyBuffer
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
      test.FixedState.Sources.Add(("MyBuffer.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBuffer.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyBuffer.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyBuffer.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_DisposeInCtorsGroup()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 14 lines, threshold 13. Dispose → IsCtorsGroupMemberForTopic → Ctors.
      var source = @"namespace TestApp
{
    public class {|#0:MyStream|}
    {
        public void Dispose() { }

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
    public partial class MyStream
    {
        public void Dispose()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyStream
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
    public partial class MyStream
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
    public partial class MyStream
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
      test.FixedState.Sources.Add(("MyStream.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyStream.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyStream.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyStream.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_WithInterface()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 10
";
      // 11 lines, threshold 10. Interface — no constructor possible.
      // No Ctors file since interface has no ctor/dtor/Dispose.
      // Topics: Order(2), Cache(2). First file = Cache (alphabetically first topic, since same freq).
      // Interface methods: no blank lines between them.
      var source = @"namespace TestApp
{
    public interface {|#0:IMyBridge|}
    {
        void ImportOrder();
        void ExportOrder();

        void ClearCache();
        void WarmCache();
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial interface IMyBridge
    {
        void ClearCache();
        void WarmCache();
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial interface IMyBridge
    {
        void ImportOrder();
        void ExportOrder();
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
            .WithArguments("Test0.cs", 11, 10));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("IMyBridge.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/IMyBridge.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_WithRecord()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 14 lines, threshold 13. Record with constructor.
      var source = @"namespace TestApp
{
    public record {|#0:MyCommand|}
    {
        public MyCommand() { }

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
    public partial record MyCommand
    {
        public MyCommand()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial record MyCommand
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
    public partial record MyCommand
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
    public partial record MyCommand
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
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 14, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyCommand.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyCommand.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyCommand.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyCommand.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_NestedTypeExtraction()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 14
";
      // 16 lines, threshold 14. Nested enum gets own file.
      var source = @"namespace TestApp
{
    public class {|#0:MyLedger|}
    {
        public MyLedger() { }

        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }

        public enum Priority { Low, High }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyLedger
    {
        public MyLedger()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyLedger
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
    public partial class MyLedger
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
    public partial class MyLedger
    {
        public void Alpha()
        {
        }

        public void Beta()
        {
        }
    }
}";

      var fixedNested = @"namespace TestApp
{
    public partial class MyLedger
    {
        public enum Priority
        {
            Low,
            High
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
            .WithArguments("Test0.cs", 16, 14));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyLedger.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyLedger.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyLedger.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyLedger.Misc.cs", fixedMisc));
      test.FixedState.Sources.Add(("/0/MyLedger.Priority.cs", fixedNested));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_EventFieldClassifiedByTopic()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 13
";
      // 15 lines, threshold 13. Event field is NOT in IsCtorsGroupMemberForTopic,
      // so it flows into topic classification. "OrderChanged" → ["Order", "Changed"].
      // "Order" matches topic (freq 2: from OrderChanged, ProcessOrder).
      var source = @"namespace TestApp
{
    public class {|#0:MyTracker|}
    {
        public MyTracker() { }

        public event System.Action OrderChanged;
        public void ProcessOrder() { }

        public void ClearCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyTracker
    {
        public MyTracker()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyTracker
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
    public partial class MyTracker
    {
        public event System.Action OrderChanged;
        public void ProcessOrder()
        {
        }
    }
}";

      var fixedMisc = @"namespace TestApp
{
    public partial class MyTracker
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
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 15, 13));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyTracker.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyTracker.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyTracker.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyTracker.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

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

   #endregion
}
