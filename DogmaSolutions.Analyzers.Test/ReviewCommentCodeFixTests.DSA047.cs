using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa047Key = DSA047Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA047 review comment

   [TestMethod]
   public async Task DSA047_ReviewComment_ExposedLockObject()
   {
      var source = @"public class Worker
{
    public readonly object Gate = new object();

    public void Work()
    {
        {|#0:lock (Gate)|}
        {
        }
    }
}";

      var fixedSource = @"public class Worker
{
    public readonly object Gate = new object();

    public void Work()
    {
        /* [DSA047 / QA + Code Smell]: Lock object that is swapped or exposed (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA047.md)
         * CWE-667: Improper Locking - https://cwe.mitre.org/data/definitions/667.html
         * CWE-413: Improper Resource Locking - https://cwe.mitre.org/data/definitions/413.html
         * CWE-820: Missing Synchronization - https://cwe.mitre.org/data/definitions/820.html
         */
        lock (Gate)
        {
        }
    }
}";

      var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA047MessageFieldExposed", Resources.Culture), "Gate", Resources.DSA047ReasonField);
      var test = new CSharpCodeFixVerifier<DSA047Analyzer, DSA047CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa047Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA047Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA047Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(12, 9, 12, 20).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
