using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032CodeFixTests
{

   // ──────────────────────────────────────────────────────────────────────────
   //  Class field constant fix
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ClassField_BasicExtraction()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""ConnectionStrings:Secret""|};
            var b = {|#1:""ConnectionStrings:Secret""|};
            var c = {|#2:""ConnectionStrings:Secret""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string ConnectionStringsSecret = ""ConnectionStrings:Secret"";
        public void Process()
        {
            var a = ConnectionStringsSecret;
            var b = ConnectionStringsSecret;
            var c = ConnectionStringsSecret;
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
            .WithLocation(0).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("ConnectionStrings:Secret", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ClassField_WithExistingFields()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        private readonly int _count;

        public void Process()
        {
            var a = {|#0:""repeated value""|};
            var b = {|#1:""repeated value""|};
            var c = {|#2:""repeated value""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private readonly int _count;
        private const string RepeatedValue = ""repeated value"";

        public void Process()
        {
            var a = RepeatedValue;
            var b = RepeatedValue;
            var c = RepeatedValue;
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
   public async Task ClassField_NameConflictResolution()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""something else"";

        public void Process()
        {
            var a = {|#0:""repeated value""|};
            var b = {|#1:""repeated value""|};
            var c = {|#2:""repeated value""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string RepeatedValue = ""something else"";
        private const string RepeatedValue1 = ""repeated value"";

        public void Process()
        {
            var a = RepeatedValue1;
            var b = RepeatedValue1;
            var c = RepeatedValue1;
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
   public async Task ClassField_InConstructor()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public MyService()
        {
            var a = {|#0:""ConnectionStrings:Secret""|};
            var b = {|#1:""ConnectionStrings:Secret""|};
            var c = {|#2:""ConnectionStrings:Secret""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string ConnectionStringsSecret = ""ConnectionStrings:Secret"";
        public MyService()
        {
            var a = ConnectionStringsSecret;
            var b = ConnectionStringsSecret;
            var c = ConnectionStringsSecret;
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
            .WithLocation(0).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("ConnectionStrings:Secret", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Class field: multiline scenarios
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ClassField_MultilineAssignment()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a =
                {|#0:""ConnectionStrings:Secret""|};
            var b =
                {|#1:""ConnectionStrings:Secret""|};
            var c =
                {|#2:""ConnectionStrings:Secret""|};
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string ConnectionStringsSecret = ""ConnectionStrings:Secret"";
        public void Process()
        {
            var a =
                ConnectionStringsSecret;
            var b =
                ConnectionStringsSecret;
            var c =
                ConnectionStringsSecret;
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
            .WithLocation(0).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("ConnectionStrings:Secret", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Class field: comment scenarios
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ClassField_CommentOnPreviousLine()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // comment before
            var a = {|#0:""repeated value""|};
            // comment before
            var b = {|#1:""repeated value""|};
            // comment before
            var c = {|#2:""repeated value""|};
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
            // comment before
            var a = RepeatedValue;
            // comment before
            var b = RepeatedValue;
            // comment before
            var c = RepeatedValue;
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
   public async Task ClassField_InlineCommentAfterString()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""repeated value""|}; // inline comment
            var b = {|#1:""repeated value""|}; // another
            var c = {|#2:""repeated value""|}; // third
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
            var a = RepeatedValue; // inline comment
            var b = RepeatedValue; // another
            var c = RepeatedValue; // third
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
   public async Task ClassField_BlockCommentBeforeString()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = /* before */ {|#0:""repeated value""|};
            var b = /* before */ {|#1:""repeated value""|};
            var c = /* before */ {|#2:""repeated value""|};
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
            var a = /* before */ RepeatedValue;
            var b = /* before */ RepeatedValue;
            var c = /* before */ RepeatedValue;
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
   public async Task ClassField_MixedComments()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // previous line comment
            var a = {|#0:""repeated value""|};
            var b = /* before */ {|#1:""repeated value""|}; // after
            var c = {|#2:""repeated value""|};
            // next line comment
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
            // previous line comment
            var a = RepeatedValue;
            var b = /* before */ RepeatedValue; // after
            var c = RepeatedValue;
            // next line comment
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
   //  Comment combinations per occurrence (local const)
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_PreviousLineComment_PlusInlineAfter()
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
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
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
   public async Task LocalConst_BlockCommentBefore_PlusInlineAfter()
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
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            var a = /* before */ RepeatedValue; // after
            var b = /* before */ RepeatedValue; // after
            var c = /* before */ RepeatedValue; // after
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
   public async Task LocalConst_PreviousLine_PlusBlockBefore_PlusInlineAfter()
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
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
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
   public async Task LocalConst_EachOccurrenceDifferentCommentStyle()
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
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
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
   public async Task LocalConst_CommentOnNextLine_PlusBlockBefore()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = /* before */ {|#0:""repeated value""|};
            // next line
            var b = /* before */ {|#1:""repeated value""|};
            // next line
            var c = /* before */ {|#2:""repeated value""|};
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
            var a = /* before */ RepeatedValue;
            // next line
            var b = /* before */ RepeatedValue;
            // next line
            var c = /* before */ RepeatedValue;
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
}
