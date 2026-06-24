using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032Tests
{

   [TestMethod]
   public async Task DoesNotFlagMigrationConstructor()
   {
      var source = @"
namespace Microsoft.EntityFrameworkCore.Migrations
{
    public abstract class Migration
    {
    }
}

namespace TestApp
{
    public class InitialCreate : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        public InitialCreate()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagEf6MigrationClass()
   {
      var source = @"
namespace System.Data.Entity.Migrations
{
    public abstract class DbMigration
    {
    }
}

namespace TestApp
{
    public class InitialCreate : System.Data.Entity.Migrations.DbMigration
    {
        public void Up()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagEfCoreDbContextClass()
   {
      var source = @"
namespace Microsoft.EntityFrameworkCore
{
    public abstract class DbContext
    {
    }
}

namespace TestApp
{
    public class MyDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public void OnModelCreating()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInWinFormsForm()
   {
      var source = @"
namespace System.Windows.Forms
{
    public class Control { }
    public class ScrollableControl : Control { }
    public class ContainerControl : ScrollableControl { }
    public class Form : ContainerControl { }
}

namespace TestApp
{
    public class MyForm : System.Windows.Forms.Form
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInWinFormsUserControl()
   {
      var source = @"
namespace System.Windows.Forms
{
    public class Control { }
    public class ScrollableControl : Control { }
    public class ContainerControl : ScrollableControl { }
    public class UserControl : ContainerControl { }
}

namespace TestApp
{
    public class MyControl : System.Windows.Forms.UserControl
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInIndirectFormSubclass()
   {
      var source = @"
namespace System.Windows.Forms
{
    public class Control { }
    public class ScrollableControl : Control { }
    public class ContainerControl : ScrollableControl { }
    public class Form : ContainerControl { }
}

namespace TestApp
{
    public class BaseForm : System.Windows.Forms.Form { }

    public class MyForm : BaseForm
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task StillFlagsOtherMethodsInWinFormsForm()
   {
      var source = @"
namespace System.Windows.Forms
{
    public class Control { }
    public class ScrollableControl : Control { }
    public class ContainerControl : ScrollableControl { }
    public class Form : ContainerControl { }
}

namespace TestApp
{
    public class MyForm : System.Windows.Forms.Form
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }

        public void SomeOtherMethod()
        {
            var a = {|#0:""ConnectionStrings:Secret""|};
            var b = {|#1:""ConnectionStrings:Secret""|};
            var c = {|#2:""ConnectionStrings:Secret""|};
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("ConnectionStrings:Secret", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task StillFlagsInitializeComponentInNonFormClass()
   {
      var source = @"
namespace TestApp
{
    public class RegularClass
    {
        private void InitializeComponent()
        {
            var a = {|#0:""ConnectionStrings:Secret""|};
            var b = {|#1:""ConnectionStrings:Secret""|};
            var c = {|#2:""ConnectionStrings:Secret""|};
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("ConnectionStrings:Secret", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInWpfWindow()
   {
      var source = @"
namespace System.Windows
{
    public class DependencyObject { }
    public class Visual : DependencyObject { }
    public class UIElement : Visual { }
    public class FrameworkElement : UIElement { }
    public class Window : FrameworkElement { }
}

namespace TestApp
{
    public class MainWindow : System.Windows.Window
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInWpfUserControl()
   {
      var source = @"
namespace System.Windows.Controls
{
    public class UserControl { }
}

namespace TestApp
{
    public class MyControl : System.Windows.Controls.UserControl
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInWpfPage()
   {
      var source = @"
namespace System.Windows.Controls
{
    public class Page { }
}

namespace TestApp
{
    public class MyPage : System.Windows.Controls.Page
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInMauiContentPage()
   {
      var source = @"
namespace Microsoft.Maui.Controls
{
    public class ContentPage { }
}

namespace TestApp
{
    public class MyPage : Microsoft.Maui.Controls.ContentPage
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInMauiContentView()
   {
      var source = @"
namespace Microsoft.Maui.Controls
{
    public class ContentView { }
}

namespace TestApp
{
    public class MyView : Microsoft.Maui.Controls.ContentView
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInAvaloniaWindow()
   {
      var source = @"
namespace Avalonia.Controls
{
    public class Window { }
}

namespace TestApp
{
    public class MainWindow : Avalonia.Controls.Window
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInAvaloniaUserControl()
   {
      var source = @"
namespace Avalonia.Controls
{
    public class UserControl { }
}

namespace TestApp
{
    public class MyControl : Avalonia.Controls.UserControl
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInXamarinFormsContentPage()
   {
      var source = @"
namespace Xamarin.Forms
{
    public class ContentPage { }
}

namespace TestApp
{
    public class MyPage : Xamarin.Forms.ContentPage
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInitializeComponentInXamarinFormsContentView()
   {
      var source = @"
namespace Xamarin.Forms
{
    public class ContentView { }
}

namespace TestApp
{
    public class MyView : Xamarin.Forms.ContentView
    {
        private void InitializeComponent()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }
}
