using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032CodeFixTests
{

   [TestMethod]
   public async Task UseReplacement_TwoDifferentStringsEachWithReplacement()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string Secret = ""ConnectionStrings:Secret"";
        public const string LogLevel = ""Logging:LogLevel"";
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
            var d = {|#3:""Logging:LogLevel""|};
            var e = {|#4:""Logging:LogLevel""|};
            var f = {|#5:""Logging:LogLevel""|};
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
            var d = {|#3:""Logging:LogLevel""|};
            var e = {|#4:""Logging:LogLevel""|};
            var f = {|#5:""Logging:LogLevel""|};
        }
    }
}";

      var replacementsContent = @"`ConnectionStrings:Secret` -> `MyConstants.Secret`
`Logging:LogLevel` -> `MyConstants.LogLevel`";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.Secret";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.NumberOfFixAllIterations = 1;
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
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(3).WithArguments("Logging:LogLevel", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(4).WithArguments("Logging:LogLevel", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(5).WithArguments("Logging:LogLevel", 3));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithSpan(11, 21, 11, 39).WithArguments("Logging:LogLevel", 3));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithSpan(12, 21, 12, 39).WithArguments("Logging:LogLevel", 3));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithSpan(13, 21, 13, 39).WithArguments("Logging:LogLevel", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task UseReplacement_StringInIgnoredStringsFile_NoDiagnosticNoReplacement()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
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
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, "ConnectionStrings:Secret"));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task UseReplacement_FileInIgnoredFileNames_NoDiagnosticNoReplacement()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestState.Sources.Add(("MyAutoGenerated.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, "*AutoGenerated*"));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task UseReplacement_IgnoredStringsSuppressesOneButReplacementOfferedForAnother()
   {
      var stubs = @"
namespace TestApp
{
    public static class MyConstants
    {
        public const string LogLevel = ""Logging:LogLevel"";
    }
}";

      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
            var d = {|#0:""Logging:LogLevel""|};
            var e = {|#1:""Logging:LogLevel""|};
            var f = {|#2:""Logging:LogLevel""|};
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
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
            var d = MyConstants.LogLevel;
            var e = MyConstants.LogLevel;
            var f = MyConstants.LogLevel;
        }
    }
}";

      var replacementsContent = @"`ConnectionStrings:Secret` -> `MyConstants.Secret`
`Logging:LogLevel` -> `MyConstants.LogLevel`";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":MyConstants.LogLevel";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.Sources.Add(("Stubs.cs", stubs));
      test.FixedState.Sources.Add(("Stubs.cs", stubs));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, "ConnectionStrings:Secret"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, "ConnectionStrings:Secret"));
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, replacementsContent));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName, replacementsContent));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("Logging:LogLevel", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("Logging:LogLevel", 3));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("Logging:LogLevel", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task UseReplacement_ReplacementAndClassFieldCoexist()
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

      var fixedClassField = @"
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
      test.FixedCode = fixedClassField;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.ClassFieldEquivalenceKey;
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
   public async Task UseReplacement_StringDotEmptyExpression()
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
            var a = string.Empty;
            var b = string.Empty;
            var c = string.Empty;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA032Analyzer, DSA032CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA032CodeFixProvider.UseReplacementEquivalenceKey + ":string.Empty";
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`hello world value` -> `string.Empty`"));
      test.FixedState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`hello world value` -> `string.Empty`"));
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
   public async Task UseReplacement_IgnoredBaseType_NoDiagnosticEvenWithReplacement()
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
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.StringReplacementsFileName,
         "`ConnectionStrings:Secret` -> `MyConstants.Secret`"));

      await test.RunAsync().ConfigureAwait(false);
   }
}
