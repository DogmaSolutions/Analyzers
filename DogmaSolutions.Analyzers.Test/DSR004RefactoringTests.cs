using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSR004RefactoringTests
{
   [TestMethod]
   public async Task Visibility_OfferedForSingleTypeWithMultipleMembers()
   {
      var source = @"namespace TestApp
{
    public class [|MyWidget|]
    {
        private int _state;

        public MyWidget() { }

        public void Render() { }
        private void Reset() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyWidget
    {
        private int _state;
        public MyWidget()
        {
        }
    }
}";

      var fixedPrivate = @"namespace TestApp
{
    public partial class MyWidget
    {
        private void Reset()
        {
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial class MyWidget
    {
        public void Render()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyWidget.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyWidget.Private.cs", fixedPrivate));
      test.FixedState.Sources.Add(("/0/MyWidget.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_OfferedForSingleTypeWithMultipleMembers()
   {
      var source = @"namespace TestApp
{
    public class [|MyGateway|]
    {
        public MyGateway() { }

        public void ImportOrder() { }
        public void ExportOrder() { }

        public void GrantPermission() { }
        public void RevokePermission() { }
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

      var fixedOrder = @"namespace TestApp
{
    public partial class MyGateway
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
    public partial class MyGateway
    {
        public void GrantPermission()
        {
        }

        public void RevokePermission()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyGateway.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyGateway.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyGateway.Permission.cs", fixedPermission));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithStruct()
   {
      var source = @"namespace TestApp
{
    public struct [|MyData|]
    {
        public int X;
        public int Y;

        public MyData(int x, int y) { X = x; Y = y; }

        public double Length() { return 0; }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial struct MyData
    {
        public int X;
        public int Y;
        public MyData(int x, int y)
        {
            X = x;
            Y = y;
        }
    }
}";

      var fixedPublic = @"namespace TestApp
{
    public partial struct MyData
    {
        public double Length()
        {
            return 0;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyData.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyData.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Visibility_WithRecord()
   {
      var source = @"namespace TestApp
{
    public record [|MyEvent|]
    {
        public int Id { get; init; }

        public MyEvent() { }

        public void Apply() { }
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

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.VisibilityEquivalenceKey;
      test.FixedState.Sources.Add(("MyEvent.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyEvent.Public.cs", fixedPublic));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_FileScopedNamespace()
   {
      var source = @"namespace TestApp;

public class [|MyFacade|]
{
    public MyFacade() { }

    public void ImportOrder() { }
    public void ExportOrder() { }

    public void ClearCache() { }
    public void WarmCache() { }
}";

      var fixedCtors = @"namespace TestApp;
public partial class MyFacade
{
    public MyFacade()
    {
    }
}";

      var fixedCache = @"namespace TestApp;
public partial class MyFacade
{
    public void ClearCache()
    {
    }

    public void WarmCache()
    {
    }
}";

      var fixedOrder = @"namespace TestApp;
public partial class MyFacade
{
    public void ImportOrder()
    {
    }

    public void ExportOrder()
    {
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyFacade.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyFacade.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyFacade.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NoRefactoring_EmptyType()
   {
      var source = @"namespace TestApp
{
    public class [||]MyEmpty
    {
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NoRefactoring_MultiTypeFile()
   {
      var source = @"namespace TestApp
{
    public class [||]ClassA
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NoRefactoring_SingleMember()
   {
      var source = @"namespace TestApp
{
    public class [||]MyClass
    {
        public int Value { get; set; }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_EventDeclarationClassified()
   {
      // EventDeclarationSyntax (explicit add/remove) → GetMemberName returns Identifier.
      // "OrderReceived" → ["Order", "Received"]. "Order" freq=2 (OrderReceived + ProcessOrder).
      var source = @"namespace TestApp
{
    public class [|MyRelay|]
    {
        public MyRelay() { }
        public event System.Action OrderReceived { add { } remove { } }
        public void ProcessOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyRelay
    {
        public MyRelay()
        {
        }
    }
}";

      var fixedCache = @"namespace TestApp
{
    public partial class MyRelay
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
    public partial class MyRelay
    {
        public event System.Action OrderReceived
        {
            add
            {
            }

            remove
            {
            }
        }

        public void ProcessOrder()
        {
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyRelay.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyRelay.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyRelay.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_IndexerGoesToMisc()
   {
      // IndexerDeclarationSyntax → GetMemberName returns "Indexer".
      // SplitPascalCase → ["Indexer"]. Freq 1 → no topic → Misc.
      var source = @"namespace TestApp
{
    public class [|MyVault|]
    {
        public MyVault() { }
        public int this[int index] { get { return 0; } }
        public void ImportOrder() { }
        public void ExportOrder() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyVault
    {
        public MyVault()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyVault
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
    public partial class MyVault
    {
        public int this[int index]
        {
            get
            {
                return 0;
            }
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyVault.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyVault.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyVault.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_OperatorGoesToMisc()
   {
      // OperatorDeclarationSyntax → GetMemberName returns "+".
      // SplitPascalCase → ["+"], length 1 → filtered → no words → Misc.
      var source = @"namespace TestApp
{
    public class [|MyToken|]
    {
        public MyToken() { }
        public static MyToken operator +(MyToken a, MyToken b) { return a; }
        public void ImportOrder() { }
        public void ExportOrder() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyToken
    {
        public MyToken()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyToken
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
    public partial class MyToken
    {
        public static MyToken operator +(MyToken a, MyToken b)
        {
            return a;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyToken.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyToken.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyToken.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_ConversionOperatorGoesToMisc()
   {
      // ConversionOperatorDeclarationSyntax → GetMemberName returns "ConversionOperator".
      // SplitPascalCase → ["Conversion", "Operator"]. Freq 1 each → Misc.
      var source = @"namespace TestApp
{
    public class [|MyUnit|]
    {
        public MyUnit() { }
        public static implicit operator int(MyUnit c) { return 0; }
        public void ImportOrder() { }
        public void ExportOrder() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyUnit
    {
        public MyUnit()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyUnit
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
    public partial class MyUnit
    {
        public static implicit operator int (MyUnit c)
        {
            return 0;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.CodeActionEquivalenceKey = DSR004RefactoringProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyUnit.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyUnit.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyUnit.Misc.cs", fixedMisc));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Topic_DelegateClassifiedByTopic()
   {
      // DelegateDeclarationSyntax → GetMemberName returns "OrderCallback".
      // SplitPascalCase → ["Order", "Callback"]. "Order" freq=3 (OrderCallback, ImportOrder, ExportOrder).
      // Delegate goes to Order group.
      var source = @"namespace TestApp
{
    public class [|MyBus|]
    {
        public MyBus() { }
        public delegate void OrderCallback(int id);
        public void ImportOrder() { }
        public void ExportOrder() { }
    }
}";

      var fixedCtors = @"namespace TestApp
{
    public partial class MyBus
    {
        public MyBus()
        {
        }
    }
}";

      var fixedOrder = @"namespace TestApp
{
    public partial class MyBus
    {
        public delegate void OrderCallback(int id);
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
      test.FixedState.Sources.Add(("MyBus.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyBus.Order.cs", fixedOrder));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NoRefactoring_Enum()
   {
      // EnumDeclarationSyntax is NOT TypeDeclarationSyntax → DSR004 guard blocks refactoring.
      var source = @"public enum [||]Color { Red, Green, Blue }";

      var test = new CSharpCodeRefactoringVerifier<DSR004RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

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
}
