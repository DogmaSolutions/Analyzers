using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA032CodeFixTests
{
   // ──────────────────────────────────────────────────────────────────────────
   //  Local constant fix
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_BasicExtraction()
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
        public void Process()
        {
            const string ConnectionStringsSecret = ""ConnectionStrings:Secret"";
            var a = ConnectionStringsSecret;
            var b = ConnectionStringsSecret;
            var c = ConnectionStringsSecret;
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
   public async Task LocalConst_StringWithSpaces()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""hello world value""|};
            var b = {|#1:""hello world value""|};
            var c = {|#2:""hello world value""|};
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
            const string HelloWorldValue = ""hello world value"";
            var a = HelloWorldValue;
            var b = HelloWorldValue;
            var c = HelloWorldValue;
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
            .WithLocation(0).WithArguments("hello world value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("hello world value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("hello world value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_NameConflictResolution()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var ConnectionStringsSecret = ""other"";
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
        public void Process()
        {
            var ConnectionStringsSecret = ""other"";
            const string ConnectionStringsSecret1 = ""ConnectionStrings:Secret"";
            var a = ConnectionStringsSecret1;
            var b = ConnectionStringsSecret1;
            var c = ConnectionStringsSecret1;
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
   public async Task LocalConst_InMethodArguments()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            System.Console.WriteLine({|#0:""repeated value""|});
            System.Console.WriteLine({|#1:""repeated value""|});
            System.Console.WriteLine({|#2:""repeated value""|});
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
            System.Console.WriteLine(RepeatedValue);
            System.Console.WriteLine(RepeatedValue);
            System.Console.WriteLine(RepeatedValue);
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
   public async Task LocalConst_InConstructor()
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
        public MyService()
        {
            const string ConnectionStringsSecret = ""ConnectionStrings:Secret"";
            var a = ConnectionStringsSecret;
            var b = ConnectionStringsSecret;
            var c = ConnectionStringsSecret;
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
   //  Multiline statement scenarios
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_MultilineAssignment()
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
        public void Process()
        {
            const string ConnectionStringsSecret = ""ConnectionStrings:Secret"";
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
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
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
   public async Task LocalConst_MultilineMethodCall()
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
                {|#0:""repeated value""|},
                ""other"");
            var b = Combine(
                {|#1:""repeated value""|},
                ""other2"");
            var c = Combine(
                {|#2:""repeated value""|},
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
                RepeatedValue,
                ""other"");
            var b = Combine(
                RepeatedValue,
                ""other2"");
            var c = Combine(
                RepeatedValue,
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
   //  Whitespace and newline variations
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_ExtraBlankLinesBetweenStatements()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
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
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            var a = RepeatedValue;

            var b = RepeatedValue;

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
   public async Task LocalConst_StringsWithLeadingWhitespace()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a =     {|#0:""repeated value""|};
            var b =     {|#1:""repeated value""|};
            var c =     {|#2:""repeated value""|};
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
            var a =     RepeatedValue;
            var b =     RepeatedValue;
            var c =     RepeatedValue;
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
   //  Comment scenarios
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_CommentOnPreviousLine()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // This is the first usage
            var a = {|#0:""repeated value""|};
            // This is the second usage
            var b = {|#1:""repeated value""|};
            // This is the third usage
            var c = {|#2:""repeated value""|};
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
            // This is the first usage
            var a = RepeatedValue;
            // This is the second usage
            var b = RepeatedValue;
            // This is the third usage
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
   public async Task LocalConst_CommentOnNextLine()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""repeated value""|};
            // comment after first
            var b = {|#1:""repeated value""|};
            // comment after second
            var c = {|#2:""repeated value""|};
            // comment after third
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
            var a = RepeatedValue;
            // comment after first
            var b = RepeatedValue;
            // comment after second
            var c = RepeatedValue;
            // comment after third
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
   public async Task LocalConst_InlineCommentAfterString()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""repeated value""|}; // inline comment
            var b = {|#1:""repeated value""|}; // another inline comment
            var c = {|#2:""repeated value""|}; // yet another
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
            var a = RepeatedValue; // inline comment
            var b = RepeatedValue; // another inline comment
            var c = RepeatedValue; // yet another
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
   public async Task LocalConst_BlockCommentBeforeString()
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
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            var a = /* before */ RepeatedValue;
            var b = /* before */ RepeatedValue;
            var c = /* before */ RepeatedValue;
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
   public async Task LocalConst_MixedComments()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            // comment on previous line
            var a = {|#0:""repeated value""|};
            var b = /* before */ {|#1:""repeated value""|}; // after
            var c = {|#2:""repeated value""|};
            // comment on next line
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
            // comment on previous line
            var a = RepeatedValue;
            var b = /* before */ RepeatedValue; // after
            var c = RepeatedValue;
            // comment on next line
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

   [TestMethod]
   public async Task ClassField_StringInNestedBlocks()
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
        private const string RepeatedValue = ""repeated value"";
        public void Process(bool a, bool b)
        {
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
   //  Naming edge cases
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_StringWithSpecialChars()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""key=value;host=prod""|};
            var b = {|#1:""key=value;host=prod""|};
            var c = {|#2:""key=value;host=prod""|};
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
            const string KeyValueHostProd = ""key=value;host=prod"";
            var a = KeyValueHostProd;
            var b = KeyValueHostProd;
            var c = KeyValueHostProd;
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
            .WithLocation(0).WithArguments("key=value;host=prod", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("key=value;host=prod", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("key=value;host=prod", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task LocalConst_StringWithDots()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""app.settings.value""|};
            var b = {|#1:""app.settings.value""|};
            var c = {|#2:""app.settings.value""|};
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
            const string AppSettingsValue = ""app.settings.value"";
            var a = AppSettingsValue;
            var b = AppSettingsValue;
            var c = AppSettingsValue;
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
            .WithLocation(0).WithArguments("app.settings.value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("app.settings.value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("app.settings.value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }
}
