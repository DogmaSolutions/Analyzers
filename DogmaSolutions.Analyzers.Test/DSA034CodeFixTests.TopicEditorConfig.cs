using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA034CodeFixTests
{

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

}
