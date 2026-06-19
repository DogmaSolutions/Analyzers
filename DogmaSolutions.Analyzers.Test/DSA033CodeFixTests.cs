using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA033CodeFixTests
{
   #region Basic split

   [TestMethod]
   public async Task SplitsFileWithTwoClassesInNamespace()
   {
      var source = @"namespace TestApp
{
    public class {|#0:ClassA|}
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var fixedOriginal = @"namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }
}";

      var fixedNew = @"namespace TestApp
{

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
"));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("ClassA.cs", fixedOriginal));
      test.FixedState.Sources.Add(("/0/ClassB.cs", fixedNew));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
"));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Multiple types

   [TestMethod]
   public async Task SplitsFileWithThreeClasses()
   {
      var source = @"namespace TestApp
{
    public class {|#0:ClassA|}
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }

    public struct StructC
    {
        public int C { get; set; }
    }
}";

      var fixedOriginal = @"namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }
}";

      var fixedClassB = @"namespace TestApp
{

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var fixedStructC = @"namespace TestApp
{

    public struct StructC
    {
        public int C { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
"));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 17, 10));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("ClassA.cs", fixedOriginal));
      test.FixedState.Sources.Add(("/0/ClassB.cs", fixedClassB));
      test.FixedState.Sources.Add(("/0/StructC.cs", fixedStructC));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
"));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region No fix offered

   [TestMethod]
   public async Task NoFixOffered_SingleTypeInFile()
   {
      var lines = new string[502];
      lines[0] = "namespace TestApp {";
      lines[1] = "    public class MyClass {";
      for (int i = 2; i < 500; i++)
         lines[i] = $"        public int P{i} {{ get; set; }}";
      lines[500] = "    }";
      lines[501] = "}";
      var source = string.Join("\n", lines);

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 20)
            .WithArguments("Test0.cs", 502, 500));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region With usings

   [TestMethod]
   public async Task SplitPreservesUsings()
   {
      var source = @"using System;
using System.Collections.Generic;

namespace TestApp
{
    public class {|#0:ClassA|}
    {
        public List<int> Items { get; set; }
    }

    public class ClassB
    {
        public DateTime Created { get; set; }
    }
}";

      var fixedOriginal = @"using System;
using System.Collections.Generic;

namespace TestApp
{
    public class ClassA
    {
        public List<int> Items { get; set; }
    }
}";

      var fixedNew = @"using System;
using System.Collections.Generic;

namespace TestApp
{

    public class ClassB
    {
        public DateTime Created { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 12
"));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 14)
            .WithArguments("Test0.cs", 15, 12));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("ClassA.cs", fixedOriginal));
      test.FixedState.Sources.Add(("/0/ClassB.cs", fixedNew));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 12
"));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
