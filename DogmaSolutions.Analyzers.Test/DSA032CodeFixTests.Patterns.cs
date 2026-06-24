using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032CodeFixTests
{

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

   // ──────────────────────────────────────────────────────────────────────────
   //  Known replacement fix — file optionality
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task UseReplacement_FileNotPresent_StandardFixesStillWork()
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
   public async Task UseReplacement_EmptyFile_StandardFixesStillWork()
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
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, string.Empty));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, string.Empty));
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
   public async Task UseReplacement_CommentsOnlyFile_StandardFixesStillWork()
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

      var fileContent = @"# This file is intentionally left with comments only
# No actual replacements
";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, fileContent));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, fileContent));
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
   public async Task UseReplacement_NoMatchingEntry_StandardFixesStillWork()
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
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`SomeUnrelatedString` -> `Replacement.Value`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`SomeUnrelatedString` -> `Replacement.Value`"));
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
   //  Known replacement fix — code chunk replacements
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task UseReplacement_MemberAccessExpression()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string SecretConnection = ""ConnectionStrings:Secret"";
    }
}";

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
            var a = MyConstants.SecretConnection;
            var b = MyConstants.SecretConnection;
            var c = MyConstants.SecretConnection;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.SecretConnection";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.SecretConnection`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.SecretConnection`"));
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
   public async Task UseReplacement_SimpleIdentifier()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        private const string SecretConnectionString = ""ConnectionStrings:Secret"";

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
        private const string SecretConnectionString = ""ConnectionStrings:Secret"";

        public void Process()
        {
            var a = SecretConnectionString;
            var b = SecretConnectionString;
            var c = SecretConnectionString;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":SecretConnectionString";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `SecretConnectionString`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `SecretConnectionString`"));
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
   public async Task UseReplacement_StringLiteralCodeChunk()
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
            var a = ""ReplacedValue"";
            var b = ""ReplacedValue"";
            var c = ""ReplacedValue"";
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + @":""ReplacedValue""";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.NumberOfFixAllIterations = 1;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         @"`ConnectionStrings:Secret` -> `""ReplacedValue""`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         @"`ConnectionStrings:Secret` -> `""ReplacedValue""`"));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("ConnectionStrings:Secret", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("ConnectionStrings:Secret", 3));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithSpan(8, 21, 8, 36).WithArguments("ReplacedValue", 3));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithSpan(9, 21, 9, 36).WithArguments("ReplacedValue", 3));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithSpan(10, 21, 10, 36).WithArguments("ReplacedValue", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task UseReplacement_NameofExpression()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""MyService""|};
            var b = {|#1:""MyService""|};
            var c = {|#2:""MyService""|};
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
            var a = nameof(MyService);
            var b = nameof(MyService);
            var c = nameof(MyService);
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":nameof(MyService)";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`MyService` -> `nameof(MyService)`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`MyService` -> `nameof(MyService)`"));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("MyService", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("MyService", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("MyService", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task UseReplacement_DeepMemberAccess()
   {
      var stubs = @"
namespace TestApp
{
    public static class Config
    {
        public static class Keys
        {
            public const string Secret = ""ConnectionStrings:Secret"";
        }
    }
}";

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
            var a = Config.Keys.Secret;
            var b = Config.Keys.Secret;
            var c = Config.Keys.Secret;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":Config.Keys.Secret";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `Config.Keys.Secret`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `Config.Keys.Secret`"));
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
   //  Known replacement fix — multiple replacements for same string
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task UseReplacement_MultipleReplacementsForSameString_FirstOption()
   {
      var stubs = @"
namespace TestApp
{
    public static class ConstantsA
    {
        public const string Secret = ""ConnectionStrings:Secret"";
    }
    public static class ConstantsB
    {
        public const string ConnStr = ""ConnectionStrings:Secret"";
    }
}";

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
            var a = ConstantsA.Secret;
            var b = ConstantsA.Secret;
            var c = ConstantsA.Secret;
        }
    }
}";

      var replacementsContent = @"`ConnectionStrings:Secret` -> `ConstantsA.Secret`
`ConnectionStrings:Secret` -> `ConstantsB.ConnStr`";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":ConstantsA.Secret";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, replacementsContent));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, replacementsContent));
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
}
