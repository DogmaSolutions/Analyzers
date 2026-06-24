using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA032Tests
{
   [TestMethod]
   public async Task FlagsThreeOccurrencesOfSameString()
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
   public async Task DoesNotFlagTwoOccurrencesWithDefaultThreshold()
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
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagShortStrings()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""key"";
            var b = ""key"";
            var c = ""key"";
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagSingleOccurrence()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""ConnectionStrings:Secret"";
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagStringsInDifferentMethods()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Method1()
        {
            var a = ""ConnectionStrings:Secret"";
            var b = ""ConnectionStrings:Secret"";
        }

        public void Method2()
        {
            var c = ""ConnectionStrings:Secret"";
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FlagsDuplicatesInConstructor()
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
   public async Task FlagsMultipleDistinctDuplicatedStrings()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a1 = {|#0:""ConnectionStrings:Secret""|};
            var a2 = {|#1:""ConnectionStrings:Secret""|};
            var a3 = {|#2:""ConnectionStrings:Secret""|};
            var b1 = {|#3:""AnotherLongString""|};
            var b2 = {|#4:""AnotherLongString""|};
            var b3 = {|#5:""AnotherLongString""|};
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
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(3).WithArguments("AnotherLongString", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(4).WithArguments("AnotherLongString", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(5).WithArguments("AnotherLongString", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagEmptyStrings()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = """";
            var b = """";
            var c = """";
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagInterpolatedStrings()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var x = ""world"";
            var a = $""hello {x}"";
            var b = $""hello {x}"";
            var c = $""hello {x}"";
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FlagsVerbatimStrings()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:@""C:\Users\Test\file.txt""|};
            var b = {|#1:@""C:\Users\Test\file.txt""|};
            var c = {|#2:@""C:\Users\Test\file.txt""|};
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("C:\\Users\\Test\\file.txt", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("C:\\Users\\Test\\file.txt", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("C:\\Users\\Test\\file.txt", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FlagsStringInMethodArguments()
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

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagStringsAtExactMinLength()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""abcd"";
            var b = ""abcd"";
            var c = ""abcd"";
        }
    }
}";

      await CSharpAnalyzerVerifier<DSA032Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FlagsStringsAtMinLength()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = {|#0:""abcde""|};
            var b = {|#1:""abcde""|};
            var c = {|#2:""abcde""|};
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("abcde", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("abcde", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("abcde", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FlagsStringsInsideLambdasWithinMethod()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            Action act1 = () => System.Console.WriteLine({|#0:""repeated value""|});
            Action act2 = () => System.Console.WriteLine({|#1:""repeated value""|});
            System.Console.WriteLine({|#2:""repeated value""|});
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(0).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(1).WithArguments("repeated value", 3));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA032Analyzer>.Diagnostic(DSA032Analyzer.DiagnosticId)
            .WithLocation(2).WithArguments("repeated value", 3));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagExcludedStrings()
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

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ExclusionFileDoesNotAffectNonExcludedStrings()
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
            var d = ""SomeOtherString"";
            var e = ""SomeOtherString"";
            var f = ""SomeOtherString"";
        }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, "SomeOtherString"));
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
   public async Task ExclusionFileSupportsMultipleLinesAndComments()
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
            var d = ""AnotherLongString"";
            var e = ""AnotherLongString"";
            var f = ""AnotherLongString"";
        }
    }
}";

      var exclusionContent = @"# This is a comment
ConnectionStrings:Secret

# Another comment
AnotherLongString
";

      var test = new CSharpAnalyzerVerifier<DSA032Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, exclusionContent));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task EmptyExclusionFileDoesNotSuppressAnything()
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
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredStringsFileName, string.Empty));
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
   public async Task DoesNotFlagDesignerGeneratedFilesByDefault()
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

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagGeneratedCsFilesByDefault()
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
      test.TestState.Sources.Add(("MyTypes.generated.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlagCustomIgnoredFilePattern()
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
      test.TestState.Sources.Add(("MyAutoGenerated_Constants.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, "*AutoGenerated*"));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task CustomIgnoredFilePatternsAreAddedToDefaults()
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

      var customSource = @"
namespace TestApp
{
    public class CustomClass
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
      test.TestState.Sources.Add(("Resources.Designer.cs", designerSource));
      test.TestState.Sources.Add(("MyCustom_Gen.cs", customSource));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, "*Custom*"));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task IgnoredFilePatternSupportsMultipleWildcards()
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
      test.TestState.Sources.Add(("My.Auto.Generated.Data.cs", source));
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AdditionalFiles.Add((DSA032Analyzer.IgnoredFileNamesFileName, "*Auto*Data*"));

      await test.RunAsync().ConfigureAwait(false);
   }
}
