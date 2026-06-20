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

   #region Type variety

   [TestMethod]
   public async Task SplitsFileWithInterfaceAndEnum()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
";
      var source = @"namespace TestApp
{
    public interface {|#0:IWidget|}
    {
        int Size { get; }
    }

    public enum Flavor
    {
        Sweet,
        Sour
    }
}";

      var fixedInterface = @"namespace TestApp
{
    public interface IWidget
    {
        int Size { get; }
    }
}";

      var fixedEnum = @"namespace TestApp
{

    public enum Flavor
    {
        Sweet,
        Sour
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 13, 10));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("IWidget.cs", fixedInterface));
      test.FixedState.Sources.Add(("/0/Flavor.cs", fixedEnum));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsFileWithRecordTypes()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 5
";
      var source = @"namespace TestApp
{
    public record {|#0:PointRecord|}(int X, int Y);

    public record struct MeasureRecord(double Value, string Unit);
}";

      var fixedRecord = @"namespace TestApp
{
    public record PointRecord(int X, int Y);
}";

      var fixedRecordStruct = @"namespace TestApp
{

    public record struct MeasureRecord(double Value, string Unit);
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 6, 5));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("PointRecord.cs", fixedRecord));
      test.FixedState.Sources.Add(("/0/MeasureRecord.cs", fixedRecordStruct));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsFileWithGenericTypes()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
";
      var source = @"namespace TestApp
{
    public class {|#0:Repository<T>|}
    {
        public T Find(int id) => default;
    }

    public class Mapper<TIn, TOut>
    {
        public TOut Map(TIn input) => default;
    }
}";

      var fixedRepo = @"namespace TestApp
{
    public class Repository<T>
    {
        public T Find(int id) => default;
    }
}";

      var fixedMapper = @"namespace TestApp
{

    public class Mapper<TIn, TOut>
    {
        public TOut Map(TIn input) => default;
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 12, 10));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Repository.cs", fixedRepo));
      test.FixedState.Sources.Add(("/0/Mapper.cs", fixedMapper));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Namespace variations

   [TestMethod]
   public async Task SplitsFileWithFileScopedNamespace()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 7
";
      var source = @"namespace TestApp;

public class {|#0:Alpha|}
{
    public int A { get; set; }
}

public class Beta
{
    public int B { get; set; }
}";

      var fixedAlpha = @"namespace TestApp;

public class Alpha
{
    public int A { get; set; }
}
";

      var fixedBeta = @"namespace TestApp;

public class Beta
{
    public int B { get; set; }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 19)
            .WithArguments("Test0.cs", 11, 7));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Alpha.cs", fixedAlpha));
      test.FixedState.Sources.Add(("/0/Beta.cs", fixedBeta));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsFileWithNoNamespace()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 5
";
      var source = @"public class {|#0:Alpha|}
{
    public int A { get; set; }
}

public class Beta
{
    public int B { get; set; }
}";

      var fixedAlpha = @"public class Alpha
{
    public int A { get; set; }
}
";

      var fixedBeta = @"
public class Beta
{
    public int B { get; set; }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 19)
            .WithArguments("Test0.cs", 9, 5));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Alpha.cs", fixedAlpha));
      test.FixedState.Sources.Add(("/0/Beta.cs", fixedBeta));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region Edge cases

   [TestMethod]
   public async Task SplitsFilePreservesBaseTypes()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
";
      var source = @"using System;

namespace TestApp
{
    public class {|#0:Widget|} : IDisposable
    {
        public void Dispose() { }
    }

    public class Gadget : Widget
    {
        public int Power { get; set; }
    }
}";

      var fixedWidget = @"using System;

namespace TestApp
{
    public class Widget : IDisposable
    {
        public void Dispose() { }
    }
}";

      var fixedGadget = @"using System;

namespace TestApp
{

    public class Gadget : Widget
    {
        public int Power { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 14)
            .WithArguments("Test0.cs", 14, 10));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Widget.cs", fixedWidget));
      test.FixedState.Sources.Add(("/0/Gadget.cs", fixedGadget));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DoesNotExtractNestedTypes()
   {
      var editorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 10
";
      var source = @"namespace TestApp
{
    public class {|#0:Outer|}
    {
        public int A { get; set; }

        public class Inner
        {
            public int B { get; set; }
        }
    }

    public class Sibling
    {
        public int C { get; set; }
    }
}";

      var fixedOuter = @"namespace TestApp
{
    public class Outer
    {
        public int A { get; set; }

        public class Inner
        {
            public int B { get; set; }
        }
    }
}";

      var fixedSibling = @"namespace TestApp
{

    public class Sibling
    {
        public int C { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 17, 10));
      test.CodeActionEquivalenceKey = DSA033CodeFixProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Outer.cs", fixedOuter));
      test.FixedState.Sources.Add(("/0/Sibling.cs", fixedSibling));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", editorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
