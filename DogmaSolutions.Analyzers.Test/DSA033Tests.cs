using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA033Tests
{
   #region Matched (file exceeds threshold)

   [TestMethod]
   public async Task Flags_FileExceedingDefaultThreshold()
   {
      var lines = new string[502];
      lines[0] = "namespace TestApp {";
      lines[1] = "    public class MyClass {";
      for (int i = 2; i < 500; i++)
         lines[i] = $"        public int P{i} {{ get; set; }}";
      lines[500] = "    }";
      lines[501] = "}";
      var source = string.Join("\n", lines);

      var test = new CSharpAnalyzerVerifier<DSA033Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA033Analyzer>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 20)
            .WithArguments("Test0.cs", 502, 500));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Flags_FileExceedingCustomThreshold()
   {
      var source = @"
namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public int D { get; set; }
        public int E { get; set; }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA033Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 5
"));
      test.ExpectedDiagnostics.Add(
         CSharpAnalyzerVerifier<DSA033Analyzer>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 1)
            .WithArguments("Test0.cs", 12, 5));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Not matched (file under threshold)

   [TestMethod]
   public async Task DoesNotFlag_FileUnderDefaultThreshold()
   {
      var source = @"
namespace TestApp
{
    public class MyClass
    {
        public int Value { get; set; }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA033Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlag_FileExactlyAtThreshold()
   {
      var lines = new string[500];
      lines[0] = "namespace TestApp {";
      lines[1] = "    public class MyClass {";
      for (int i = 2; i < 498; i++)
         lines[i] = $"        public int P{i} {{ get; set; }}";
      lines[498] = "    }";
      lines[499] = "}";
      var source = string.Join("\n", lines);

      var test = new CSharpAnalyzerVerifier<DSA033Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlag_FileUnderCustomThreshold()
   {
      var source = @"
namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA033Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 100
"));
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotFlag_InvalidThresholdUsesDefault()
   {
      var source = @"
namespace TestApp
{
    public class MyClass
    {
        public int Value { get; set; }
    }
}";

      var test = new CSharpAnalyzerVerifier<DSA033Analyzer>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = -1
"));
      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
