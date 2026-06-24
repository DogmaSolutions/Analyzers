using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA032Tests
{

   [TestMethod]
   public async Task NonMatchingFilePatternDoesNotSuppressDiagnostics()
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

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, "*SomethingElse*"));
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
   public void GlobPatternMatchesCorrectly()
   {
      // Standard wildcard patterns (leading wildcard)
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Resources.Designer.cs", "*.Designer.cs"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("MyTypes.generated.cs", "*.generated.cs"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Window.g.cs", "*.g.cs"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Window.g.i.cs", "*.g.i.cs"));

      // Case insensitivity
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Resources.designer.cs", "*.Designer.cs"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Resources.DESIGNER.CS", "*.Designer.cs"));

      // Multiple wildcards
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("My.Auto.Generated.Data.cs", "*Auto*Data*"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("AutoData.cs", "*Auto*Data*"));

      // Trailing wildcard only
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("MyService.cs", "MyService*"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("MyService.Designer.cs", "MyService*"));
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("OtherService.cs", "MyService*"));

      // Middle wildcard only
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("MyTestService.cs", "My*Service.cs"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("MyService.cs", "My*Service.cs"));
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("MyTestController.cs", "My*Service.cs"));

      // Wildcard matching empty string
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Designer.cs", "*Designer.cs"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("AutoData", "*Auto*Data*"));

      // Single wildcard matches everything
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Anything.cs", "*"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern(string.Empty, "*"));

      // Consecutive wildcards
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Anything.cs", "**"));
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("My.File.cs", "***"));

      // *.g.cs must NOT match *.g.i.cs (critical overlap check)
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("Window.g.i.cs", "*.g.cs"));

      // No match
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("MyService.cs", "*.Designer.cs"));
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("MyService.cs", "*.generated.cs"));
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("Designer.txt", "*.Designer.cs"));

      // Exact match (no wildcards)
      Assert.IsTrue(DSA032Analyzer.MatchesGlobPattern("Exact.cs", "Exact.cs"));
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("NotExact.cs", "Exact.cs"));

      // Pattern longer than text
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern("A.cs", "ALongerName.cs"));

      // Empty text with non-wildcard pattern
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern(string.Empty, "*.cs"));
      Assert.IsFalse(DSA032Analyzer.MatchesGlobPattern(string.Empty, "Exact.cs"));
   }

   [TestMethod]
   public async Task DoesNotFlagGCsFilesByDefault()
   {
      var source = @"
namespace TestApp
{
    public class MyWindow
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
      test.TestState.Sources.Add(("MainWindow.g.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagGICsFilesByDefault()
   {
      var source = @"
namespace TestApp
{
    public class MyWindow
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
      test.TestState.Sources.Add(("MainWindow.g.i.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task IgnoredFileNamesFileSupportsCommentsAndEmptyLines()
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

      var fileContent = @"# Custom generated files

*AutoGen*

# Template output
*Template*
";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestState.Sources.Add(("MyAutoGenCode.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, fileContent));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task IgnoredFileNamesFileSupportsMultiplePatterns()
   {
      var sourceA = @"
namespace TestApp
{
    public class ClassA
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";
      var sourceB = @"
namespace TestApp
{
    public class ClassB
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var fileContent = @"*AutoGen*
*Template*";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestState.Sources.Add(("MyAutoGenCode.cs", sourceA));
      test.TestState.Sources.Add(("MyTemplateOutput.cs", sourceB));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, fileContent));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task EmptyIgnoredFileNamesFilePreservesDefaults()
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
      test.TestState.Sources.Add(("Resources.Designer.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, string.Empty));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task MixedIgnoredAndFlaggedFilesInSameCompilation()
   {
      var ignoredSource = @"
namespace TestApp
{
    public class DesignerClass
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var flaggedSource = @"
namespace TestApp
{
    public class RegularClass
    {
        public void Process()
        {
            var a = {|#0:""ConnectionStrings:Secret""|};
            var b = {|#1:""ConnectionStrings:Secret""|};
            var c = {|#2:""ConnectionStrings:Secret""|};
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestState.Sources.Add(("Resources.Designer.cs", ignoredSource));
      test.TestState.Sources.Add(("/0/RegularService.cs", flaggedSource));
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
   public async Task IgnoredFileAndIgnoredStringsCombined()
   {
      var designerSource = @"
namespace TestApp
{
    public class DesignerClass
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      var regularSource = @"
namespace TestApp
{
    public class RegularClass
    {
        public void Process()
        {
            var a = ""IgnoredByStringFile"";
            var b = ""IgnoredByStringFile"";
            var c = ""IgnoredByStringFile"";
            var d = {|#0:""NotIgnoredAtAll""|};
            var e = {|#1:""NotIgnoredAtAll""|};
            var f = {|#2:""NotIgnoredAtAll""|};
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestState.Sources.Add(("Resources.Designer.cs", designerSource));
      test.TestState.Sources.Add(("/0/RegularService.cs", regularSource));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, "IgnoredByStringFile"));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("NotIgnoredAtAll", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("NotIgnoredAtAll", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("NotIgnoredAtAll", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagEfCoreMigrationClass()
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

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagEfCoreModelSnapshotClass()
   {
      var source = @"
namespace Microsoft.EntityFrameworkCore.Infrastructure
{
    public abstract class ModelSnapshot
    {
    }
}

namespace TestApp
{
    public class MyDbContextModelSnapshot : Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot
    {
        public void BuildModel()
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
   public async Task DoesNotFlagIndirectMigrationInheritance()
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
    public abstract class BaseMigration : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
    }

    public class MyMigration : BaseMigration
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
   public async Task StillFlagsNonMigrationClassInSameCompilation()
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
    public class MyMigration : Microsoft.EntityFrameworkCore.Migrations.Migration
    {
        public void Up()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
            var c = ""ConnectionStrings:Secret"";
        }
    }

    public class RegularService
    {
        public void Process()
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
}
