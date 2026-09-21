using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa042Key = DSA042Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA042 review comment

   [TestMethod]
   public async Task DSA042_ReviewComment_RepeatedSegment()
   {
      var source = @"namespace My.Api.Api
{
    public class C { }
}";

      var fixedSource = @"/* [DSA042 / QA + Code Smell]: Namespace repeats a segment (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA042.md)
 * CWE-1078: Inappropriate Source Code Style or Formatting - https://cwe.mitre.org/data/definitions/1078.html
 */
namespace My.Api.Api
{
    public class C { }
}";

      var test = new CSharpCodeFixVerifier<DSA042Analyzer, DSA042CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa042Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA042Analyzer, DSA042CodeFixProvider>.Diagnostic(DSA042Analyzer.DiagnosticId)
            .WithSpan(1, 11, 1, 21)
            .WithArguments("My.Api.Api", "Api"));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA042Analyzer, DSA042CodeFixProvider>.Diagnostic(DSA042Analyzer.DiagnosticId)
            .WithSpan(4, 11, 4, 21)
            .WithArguments("My.Api.Api", "Api"));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
