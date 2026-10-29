using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa056Key = DSA056Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA056 review comment

   [TestMethod]
   public async Task DSA056_ReviewComment_TestFixtureNotExcludedFromCoverage()
   {
      var source = @"public class {|#0:OrderTests|}
{
    [Xunit.Fact]
    public void Works() { }
}";

      var fixedSource = @"/* [DSA056 / QA + Code Smell]: Test fixture not excluded from the code coverage (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA056.md)
 * CWE-1164: Irrelevant Code - https://cwe.mitre.org/data/definitions/1164.html
 */
public class OrderTests
{
    [Xunit.Fact]
    public void Works() { }
}";

      var message = string.Format(Resources.Culture, Resources.DSA056Message, "OrderTests");
      var test = new CSharpCodeFixVerifier<DSA056Analyzer, DSA056CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa056Key;
      test.ReferenceAssemblies = DSA056Tests.WithFrameworks;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA056Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA056Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(4, 14, 4, 24).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
