using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa051Key = DSA051Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA051 review comment

   [TestMethod]
   public async Task DSA051_ReviewComment_ClassWithoutToString()
   {
      var source = @"public class {|#0:Invoice|}
{
    public int Id { get; set; }
}";

      var fixedSource = @"/* [DSA051 / QA + Best Practice]: Class without a ToString() override (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA051.md)
 * Object.ToString - https://learn.microsoft.com/dotnet/api/system.object.tostring
 * CWE-778: Insufficient Logging - https://cwe.mitre.org/data/definitions/778.html
 */
public class Invoice
{
    public int Id { get; set; }
}";

      var test = new CSharpCodeFixVerifier<DSA051Analyzer, DSA051CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa051Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(5, 14, 5, 21));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA051_ReviewComment_FixAll()
   {
      var comment = @"/* [DSA051 / QA + Best Practice]: Class without a ToString() override (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA051.md)
 * Object.ToString - https://learn.microsoft.com/dotnet/api/system.object.tostring
 * CWE-778: Insufficient Logging - https://cwe.mitre.org/data/definitions/778.html
 */
".Replace("\r\n", "\n");
      var source = "public class {|#0:First|}\n{\n    public int Id { get; set; }\n}\n\npublic class {|#1:Second|}\n{\n    public int Id { get; set; }\n}";
      var fixedSource = comment + "public class First\n{\n    public int Id { get; set; }\n}\n\n" + comment + "public class Second\n{\n    public int Id { get; set; }\n}";

      var test = new CSharpCodeFixVerifier<DSA051Analyzer, DSA051CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa051Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(1));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(5, 14, 5, 19));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(14, 14, 14, 20));
      test.NumberOfIncrementalIterations = 3;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllInProjectCheck | CodeFixTestBehaviors.SkipFixAllInSolutionCheck;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
