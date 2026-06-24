using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA034CodeFixTests
{

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

}
