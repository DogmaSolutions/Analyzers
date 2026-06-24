using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032CodeFixTests
{

   // ──────────────────────────────────────────────────────────────────────────
   //  Comment combinations per occurrence (class field)
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ClassField_PreviousLineComment_PlusInlineAfter()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // previous line
            var a = {|#0:""repeated value""|}; // inline after
            // previous line
            var b = {|#1:""repeated value""|}; // inline after
            // previous line
            var c = {|#2:""repeated value""|}; // inline after
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""repeated value"";
        public void Process()
        {
            // previous line
            var a = RepeatedValue; // inline after
            // previous line
            var b = RepeatedValue; // inline after
            // previous line
            var c = RepeatedValue; // inline after
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.ClassFieldEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ClassField_BlockCommentBefore_PlusInlineAfter()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = /* before */ {|#0:""repeated value""|}; // after
            var b = /* before */ {|#1:""repeated value""|}; // after
            var c = /* before */ {|#2:""repeated value""|}; // after
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""repeated value"";
        public void Process()
        {
            var a = /* before */ RepeatedValue; // after
            var b = /* before */ RepeatedValue; // after
            var c = /* before */ RepeatedValue; // after
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.ClassFieldEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ClassField_PreviousLine_PlusBlockBefore_PlusInlineAfter()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // previous line
            var a = /* before */ {|#0:""repeated value""|}; // after
            // previous line
            var b = /* before */ {|#1:""repeated value""|}; // after
            // previous line
            var c = /* before */ {|#2:""repeated value""|}; // after
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""repeated value"";
        public void Process()
        {
            // previous line
            var a = /* before */ RepeatedValue; // after
            // previous line
            var b = /* before */ RepeatedValue; // after
            // previous line
            var c = /* before */ RepeatedValue; // after
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.ClassFieldEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ClassField_EachOccurrenceDifferentCommentStyle()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // only previous line
            var a = {|#0:""repeated value""|};
            var b = /* only block before */ {|#1:""repeated value""|};
            var c = {|#2:""repeated value""|}; // only inline after
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""repeated value"";
        public void Process()
        {
            // only previous line
            var a = RepeatedValue;
            var b = /* only block before */ RepeatedValue;
            var c = RepeatedValue; // only inline after
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.ClassFieldEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Cross-product: multiline + comments
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_MultilineAssignment_WithCommentOnPreviousLine()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // first
            var a =
                {|#0:""repeated value""|};
            // second
            var b =
                {|#1:""repeated value""|};
            // third
            var c =
                {|#2:""repeated value""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            // first
            var a =
                RepeatedValue;
            // second
            var b =
                RepeatedValue;
            // third
            var c =
                RepeatedValue;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_MultilineAssignment_WithInlineCommentAfter()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a =
                {|#0:""repeated value""|}; // after
            var b =
                {|#1:""repeated value""|}; // after
            var c =
                {|#2:""repeated value""|}; // after
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            var a =
                RepeatedValue; // after
            var b =
                RepeatedValue; // after
            var c =
                RepeatedValue; // after
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_MultilineMethodCall_WithBlockBeforeAndInlineAfter()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        private string Combine(string a, string b) => a + b;

        public void Process()
        {
            var a = Combine(
                /* before */ {|#0:""repeated value""|}, // after
                ""other"");
            var b = Combine(
                /* before */ {|#1:""repeated value""|}, // after
                ""other2"");
            var c = Combine(
                /* before */ {|#2:""repeated value""|}, // after
                ""other3"");
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private string Combine(string a, string b) => a + b;

        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            var a = Combine(
                /* before */ RepeatedValue, // after
                ""other"");
            var b = Combine(
                /* before */ RepeatedValue, // after
                ""other2"");
            var c = Combine(
                /* before */ RepeatedValue, // after
                ""other3"");
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Cross-product: multiline + comments (class field)
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ClassField_MultilineAssignment_WithCommentOnPreviousLine()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // first
            var a =
                {|#0:""repeated value""|};
            // second
            var b =
                {|#1:""repeated value""|};
            // third
            var c =
                {|#2:""repeated value""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""repeated value"";
        public void Process()
        {
            // first
            var a =
                RepeatedValue;
            // second
            var b =
                RepeatedValue;
            // third
            var c =
                RepeatedValue;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.ClassFieldEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Cross-product: blank lines + comments
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_BlankLines_WithComments()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // previous line
            var a = {|#0:""repeated value""|}; // inline

            var b = /* before */ {|#1:""repeated value""|};

            var c = {|#2:""repeated value""|};
            // next line
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            // previous line
            var a = RepeatedValue; // inline

            var b = /* before */ RepeatedValue;

            var c = RepeatedValue;
            // next line
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Structural scenarios
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_StringInIfElseBody()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process(bool flag)
        {
            if (flag)
            {
                var a = {|#0:""repeated value""|};
            }
            else
            {
                var b = {|#1:""repeated value""|};
            }
            var c = {|#2:""repeated value""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process(bool flag)
        {
            const string RepeatedValue = ""repeated value"";
            if (flag)
            {
                var a = RepeatedValue;
            }
            else
            {
                var b = RepeatedValue;
            }
            var c = RepeatedValue;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_StringInNestedBlocks()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process(bool a, bool b)
        {
            if (a)
            {
                if (b)
                {
                    var x = {|#0:""repeated value""|};
                }
                var y = {|#1:""repeated value""|};
            }
            var z = {|#2:""repeated value""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process(bool a, bool b)
        {
            const string RepeatedValue = ""repeated value"";
            if (a)
            {
                if (b)
                {
                    var x = RepeatedValue;
                }
                var y = RepeatedValue;
            }
            var z = RepeatedValue;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_StringInTernary()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process(bool flag)
        {
            var a = flag ? {|#0:""repeated value""|}  : ""other"";
            var b = flag ? {|#1:""repeated value""|}  : ""other2"";
            var c = flag ? {|#2:""repeated value""|}  : ""other3"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process(bool flag)
        {
            const string RepeatedValue = ""repeated value"";
            var a = flag ? RepeatedValue  : ""other"";
            var b = flag ? RepeatedValue  : ""other2"";
            var c = flag ? RepeatedValue  : ""other3"";
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_StringAsSecondArgument()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        private string Combine(string a, string b) => a + b;

        public void Process()
        {
            var a = Combine(""other"", {|#0:""repeated value""|});
            var b = Combine(""other2"", {|#1:""repeated value""|});
            var c = Combine(""other3"", {|#2:""repeated value""|});
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private string Combine(string a, string b) => a + b;

        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            var a = Combine(""other"", RepeatedValue);
            var b = Combine(""other2"", RepeatedValue);
            var c = Combine(""other3"", RepeatedValue);
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }
}
