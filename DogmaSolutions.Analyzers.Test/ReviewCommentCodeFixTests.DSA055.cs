using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa055Key = DSA055Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA055 review comment

   [TestMethod]
   public async Task DSA055_ReviewComment_KeyComparedWithZero()
   {
      var source = @"public class Worker
{
    public bool HasOwner(long ownerKey)
    {
        return {|#0:ownerKey != 0|};
    }
}";

      var fixedSource = @"public class Worker
{
    public bool HasOwner(long ownerKey)
    {
        /* [DSA055 / QA + Code Smell]: Key or identifier compared with zero by equality: a negative value is as invalid as zero (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA055.md)
         * CWE-697: Incorrect Comparison - https://cwe.mitre.org/data/definitions/697.html
         * CWE-1023: Incomplete Comparison with Missing Factors - https://cwe.mitre.org/data/definitions/1023.html
         */
        return ownerKey != 0;
    }
}";

      var message = string.Format(Resources.Culture, Resources.DSA055MessageNotEqualsSigned, "ownerKey", "long");
      var test = new CSharpCodeFixVerifier<DSA055Analyzer, DSA055CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa055Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA055Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA055Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(9, 16, 9, 29).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
