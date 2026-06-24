using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA034CodeFixTests
{



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

}
