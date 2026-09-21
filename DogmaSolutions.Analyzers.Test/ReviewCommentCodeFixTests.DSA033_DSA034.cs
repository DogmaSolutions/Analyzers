using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa033Key = DSA033Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;
   private const string Dsa034Key = DSA034Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   private const string Dsa033EditorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA033.max_lines = 5
dotnet_diagnostic.DSA033.count_blank_lines = true
";

   private const string Dsa034EditorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 5
dotnet_diagnostic.DSA034.count_blank_lines = true
";

   #region DSA033 review comment

   [TestMethod]
   public async Task DSA033_ReviewComment_MultiTypeFile()
   {
      var source = @"namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var fixedSource = @"/* [DSA033 / QA + Code Smell]: File exceeds maximum line count (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA033.md)
 * CWE-1080: Source Code File with Excessive Number of Lines of Code - https://cwe.mitre.org/data/definitions/1080.html
 */
namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa033Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa033EditorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 12, 5));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 145)
            .WithArguments("Test0.cs", 15, 5));
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa033EditorConfig));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA033_ReviewComment_SingleTypeFile()
   {
      var source = @"namespace TestApp
{
    public class MyClass
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
    }
}";

      var fixedSource = @"/* [DSA033 / QA + Code Smell]: File exceeds maximum line count (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA033.md)
 * CWE-1080: Source Code File with Excessive Number of Lines of Code - https://cwe.mitre.org/data/definitions/1080.html
 */
namespace TestApp
{
    public class MyClass
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa033Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa033EditorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 18)
            .WithArguments("Test0.cs", 9, 5));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA033Analyzer, DSA033CodeFixProvider>.Diagnostic(DSA033Analyzer.DiagnosticId)
            .WithSpan(1, 1, 1, 145)
            .WithArguments("Test0.cs", 12, 5));
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa033EditorConfig));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion

   #region DSA034 review comment

   [TestMethod]
   public async Task DSA034_ReviewComment_SingleClassFile()
   {
      var source = @"namespace TestApp
{
    public class MyClass
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
    }
}";

      // The comment now anchors on the type declaration (the diagnostic location), so it lands inside the
      // namespace, immediately before the class, indented to the type — not at file top.
      var fixedSource = @"namespace TestApp
{
    /* [DSA034 / QA + Code Smell]: Single-type file exceeds maximum line count (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA034.md)
     * CWE-1080: Source Code File with Excessive Number of Lines of Code - https://cwe.mitre.org/data/definitions/1080.html
     */
    public class MyClass
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa034Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa034EditorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(3, 18, 3, 25)
            .WithArguments("Test0.cs", 9, 5));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(6, 18, 6, 25)
            .WithArguments("Test0.cs", 12, 5));
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa034EditorConfig));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA034_ReviewComment_SingleEnumFile()
   {
      var source = @"namespace TestApp
{
    public enum Color
    {
        Red,
        Green,
        Blue,
        Yellow
    }
}";

      // The comment now anchors on the type declaration (the diagnostic location), so it lands inside the
      // namespace, immediately before the enum, indented to the type — not at file top.
      var fixedSource = @"namespace TestApp
{
    /* [DSA034 / QA + Code Smell]: Single-type file exceeds maximum line count (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA034.md)
     * CWE-1080: Source Code File with Excessive Number of Lines of Code - https://cwe.mitre.org/data/definitions/1080.html
     */
    public enum Color
    {
        Red,
        Green,
        Blue,
        Yellow
    }
}";

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa034Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa034EditorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(3, 17, 3, 22)
            .WithArguments("Test0.cs", 10, 5));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithSpan(6, 17, 6, 22)
            .WithArguments("Test0.cs", 13, 5));
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", Dsa034EditorConfig));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
