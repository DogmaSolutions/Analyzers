using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSR004RefactoringTests
{

   [TestMethod]
   public async Task Visibility_WithInterfaceRefactoring()
   {
      // Interface visibility split.
      // Properties → IsCtorsGroupMember → Ctors. Dispose → IsCtorsGroupMember → Ctors.
      // Methods without modifiers → GetEffectiveVisibility → "Private".
      // Interface properties get blank lines between them.
      // Interface methods do NOT get blank lines between them.
      var source = @"namespace TestApp
{
    public interface [|IMyProcessor|]
    {
        int Width { get; }
        int Height { get; }

        void Execute();
        void Validate();
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial interface IMyProcessor
    {
        int Width { get; }

        int Height { get; }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial interface IMyProcessor
    {
        void Execute();
        void Validate();
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("IMyProcessor.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/IMyProcessor.Private.cs", fixedPrivate));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_WithStructRefactoring()
   {
      // Topic split of struct.
      var source = @"namespace TestApp
{
    public struct [|MyCell|]
    {
        public MyCell(int v) { }
        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial struct MyCell
    {
        public MyCell(int v)
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial struct MyCell
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
    public partial struct MyCell
    {
        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyCell.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyCell.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyCell.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_WithRecordRefactoring()
   {
      // Topic split of record.
      var source = @"namespace TestApp
{
    public record [|MyMessage|]
    {
        public MyMessage() { }
        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial record MyMessage
    {
        public MyMessage()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial record MyMessage
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
    public partial record MyMessage
    {
        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyMessage.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyMessage.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyMessage.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_WithRecordStructRefactoring()
   {
      var source = @"namespace TestApp
{
    public record struct [|MyGauge|]
    {
        public MyGauge() { }
        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial record struct MyGauge
    {
        public MyGauge()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial record struct MyGauge
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
    public partial record struct MyGauge
    {
        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyGauge.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyGauge.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyGauge.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_SingleFieldDemotedToMiscViaRefactoring()
   {
      // Topic "Order" has only 1 field (_orderCount) → not viable → demoted to Misc.
      // "Cache" has 2 methods + 1 field → viable. "Sync" excluded from topic words.
      var source = @"namespace TestApp
{
    public class [|MyRouter|]
    {
        public MyRouter() { }

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
    public partial class MyRouter
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

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyRouter.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyRouter.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyRouter.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_TwoTopicsDemotedSimultaneouslyViaRefactoring()
   {
      // Topics "Order" (1 field) and "Alert" (1 field) both non-viable → demoted to Misc.
      // "Cache" has 2 methods + 1 field → viable. "Sync" excluded.
      var source = @"namespace TestApp
{
    public class [|MyGateway|]
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

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyGateway.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyGateway.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyGateway.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_PreservesBaseListViaRefactoring()
   {
      // Base class (ServiceBase) should be preserved on the Ctors file.
      var baseSource = @"namespace TestApp
{
    public abstract class ServiceBase { }
}";

      var source = @"namespace TestApp
{
    public class [|MyProcessor|] : ServiceBase
    {
        public MyProcessor() { }

        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
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

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestState.Sources.Add(("ServiceBase.cs", baseSource));
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyProcessor.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("ServiceBase.cs", baseSource));
      test.FixedState.Sources.Add(("/0/MyProcessor.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyProcessor.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_UnderscoreSplittingViaRefactoring()
   {
      // Underscore-bearing member names should split correctly in the refactoring path.
      // Load_Widget, Save_Widget → Widget(2). Check_Invoice, Get_Invoice → Invoice(2).
      var source = @"namespace TestApp
{
    public class [|MyBridge|]
    {
        public MyBridge() { }

        public void Load_Widget() { }
        public void Save_Widget() { }
        public void Check_Invoice() { }
        public void Get_Invoice() { }
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
        public void Check_Invoice()
        {
        }

        public void Get_Invoice()
        {
        }
    }
}";

      var fixedWidget = @"namespace TestApp
{
    public partial class MyBridge
    {
        public void Load_Widget()
        {
        }

        public void Save_Widget()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyBridge.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBridge.Invoice.cs", fixedInvoice));
      test.FixedState.Sources.Add(("/0/MyBridge.Widget.cs", fixedWidget));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_PluralNormalizationViaRefactoring()
   {
      // Plural and singular forms merge into the same topic.
      // ProcessOrders + ImportOrder → Order(2). RenderWidgets + PaintWidget → Widget(2).
      var source = @"namespace TestApp
{
    public class [|MyDirector|]
    {
        public MyDirector() { }

        public void ProcessOrders() { }
        public void ImportOrder() { }
        public void RenderWidgets() { }
        public void PaintWidget() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyDirector
    {
        public MyDirector()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyDirector
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
    public partial class MyDirector
    {
        public void RenderWidgets()
        {
        }

        public void PaintWidget()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyDirector.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyDirector.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyDirector.Widget.cs", fixedWidget));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_UnderscoreAndPluralCombinedViaRefactoring()
   {
      // Both underscore splitting and plural normalization together.
      // Load_Widgets → [Load(excl), Widget]. Save_Widget → [Save(excl), Widget].
      // Get_Invoices → [Get(excl), Invoice]. Check_Invoice → [Check(excl), Invoice].
      var source = @"namespace TestApp
{
    public class [|MyAdapter|]
    {
        public MyAdapter() { }

        public void Load_Widgets() { }
        public void Save_Widget() { }
        public void Get_Invoices() { }
        public void Check_Invoice() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyAdapter
    {
        public MyAdapter()
        {
        }
    }
}";

      var fixedInvoice = @"namespace TestApp
{
    public partial class MyAdapter
    {
        public void Get_Invoices()
        {
        }

        public void Check_Invoice()
        {
        }
    }
}";

      var fixedWidget = @"namespace TestApp
{
    public partial class MyAdapter
    {
        public void Load_Widgets()
        {
        }

        public void Save_Widget()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyAdapter.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyAdapter.Invoice.cs", fixedInvoice));
      test.FixedState.Sources.Add(("/0/MyAdapter.Widget.cs", fixedWidget));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_PreservesBaseListViaRefactoring()
   {
      // Base class should be preserved on the Ctors file for visibility split too.
      var baseSource = @"namespace TestApp
{
    public abstract class ServiceBase { }
}";

      var source = @"namespace TestApp
{
    public class [|MyAgent|] : ServiceBase
    {
        public MyAgent() { }

        public void Execute() { }
        private void Reset() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyAgent : ServiceBase
    {
        public MyAgent()
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial class MyAgent
    {
        private void Reset()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyAgent
    {
        public void Execute()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestState.Sources.Add(("ServiceBase.cs", baseSource));
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyAgent.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("ServiceBase.cs", baseSource));
      test.FixedState.Sources.Add(("/0/MyAgent.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyAgent.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }
}
