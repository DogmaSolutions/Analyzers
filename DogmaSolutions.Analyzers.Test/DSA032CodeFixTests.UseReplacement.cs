using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032CodeFixTests
{

   [TestMethod]
   public async Task UseReplacement_MultipleReplacementsForSameString_SecondOption()
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
            var a = ConstantsB.ConnStr;
            var b = ConstantsB.ConnStr;
            var c = ConstantsB.ConnStr;
        }
    }
}";

      var replacementsContent = @"`ConnectionStrings:Secret` -> `ConstantsA.Secret`
`ConnectionStrings:Secret` -> `ConstantsB.ConnStr`";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":ConstantsB.ConnStr";
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

   [TestMethod]
   public async Task UseReplacement_ThreeReplacementsForSameString_ThirdOption()
   {
      var stubs = @"
namespace TestApp
{
    public static class A { public const string V = ""ConnectionStrings:Secret""; }
    public static class B { public const string V = ""ConnectionStrings:Secret""; }
    public static class C { public const string V = ""ConnectionStrings:Secret""; }
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
            var a = C.V;
            var b = C.V;
            var c = C.V;
        }
    }
}";

      var replacementsContent = @"`ConnectionStrings:Secret` -> `A.V`
`ConnectionStrings:Secret` -> `B.V`
`ConnectionStrings:Secret` -> `C.V`";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":C.V";
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

   [TestMethod]
   public async Task UseReplacement_MultipleReplacementsOnlyMatchingStringIsOffered()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string SecretConnection = ""ConnectionStrings:Secret"";
        public const string OtherValue = ""SomeOtherString"";
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

      var replacementsContent = @"`ConnectionStrings:Secret` -> `MyConstants.SecretConnection`
`SomeOtherString` -> `MyConstants.OtherValue`";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.SecretConnection";
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

   // ──────────────────────────────────────────────────────────────────────────
   //  Known replacement fix — file parsing edge cases
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task UseReplacement_CommentsAndEmptyLinesIgnored()
   {
      var stubs = @"
namespace TestApp
{
    public static class AppConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
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
            var a = AppConstants.Secret;
            var b = AppConstants.Secret;
            var c = AppConstants.Secret;
        }
    }
}";

      var replacementsContent = @"# Known connection strings

`ConnectionStrings:Secret` -> `AppConstants.Secret`

# Other replacements
`SomeOtherValue` -> `OtherConstants.Value`
";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":AppConstants.Secret";
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

   [TestMethod]
   public async Task UseReplacement_LinesWithoutArrowIgnored()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
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
            var a = MyConstants.Secret;
            var b = MyConstants.Secret;
            var c = MyConstants.Secret;
        }
    }
}";

      var replacementsContent = @"this line has no arrow separator
also invalid
`ConnectionStrings:Secret` -> `MyConstants.Secret`
another bad line";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.Secret";
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

   [TestMethod]
   public async Task UseReplacement_WhitespaceTrimmed()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
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
            var a = MyConstants.Secret;
            var b = MyConstants.Secret;
            var c = MyConstants.Secret;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.Secret";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "   `ConnectionStrings:Secret`   ->   `MyConstants.Secret`   "));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "   `ConnectionStrings:Secret`   ->   `MyConstants.Secret`   "));
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
   public async Task UseReplacement_EmptyRightSideIgnored()
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
         "`ConnectionStrings:Secret` ->   "));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` ->   "));
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
   //  Known replacement fix — interactions with other features
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task UseReplacement_InConstructorBody()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
    }
}";

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
            var a = MyConstants.Secret;
            var b = MyConstants.Secret;
            var c = MyConstants.Secret;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.Secret";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));
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
   public async Task UseReplacement_ReplacementAndLocalConstCoexist()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
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

      var fixedLocalConst = @"
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
      test.FixedCode = fixedLocalConst;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.LocalConstEquivalenceKey;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));
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
   public async Task UseReplacement_StringContainingArrow()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string ArrowValue = ""value -> other"";
    }
}";

      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""value -> other""|};
            var b = {|#1:""value -> other""|};
            var c = {|#2:""value -> other""|};
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
            var a = MyConstants.ArrowValue;
            var b = MyConstants.ArrowValue;
            var c = MyConstants.ArrowValue;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.ArrowValue";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`value -> other` -> `MyConstants.ArrowValue`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`value -> other` -> `MyConstants.ArrowValue`"));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("value -> other", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("value -> other", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("value -> other", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Known replacement fix — missing permutations
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task UseReplacement_ArrowWithoutSpaces()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
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
            var a = MyConstants.Secret;
            var b = MyConstants.Secret;
            var c = MyConstants.Secret;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.Secret";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret`->`MyConstants.Secret`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret`->`MyConstants.Secret`"));
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
   public async Task UseReplacement_PartialLeftSideDoesNotMatch()
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
         "`ConnectionStrings` -> `MyConstants.Partial`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings` -> `MyConstants.Partial`"));
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
