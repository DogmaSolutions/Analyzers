using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa050Key = DSA050Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA050 review comment

   [TestMethod]
   public async Task DSA050_ReviewComment_CheckThenActOnAFlag()
   {
      var source = @"public class Worker
{
    private bool _started;

    public void Start()
    {
        {|#0:if (!_started)|}
        {
            _started = true;
        }
    }
}";

      var fixedSource = @"public class Worker
{
    private bool _started;

    public void Start()
    {
        /* [DSA050 / QA + Code Smell]: Check-then-act on a flag or a state (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA050.md)
         * CWE-367: Time-of-check Time-of-use (TOCTOU) Race Condition - https://cwe.mitre.org/data/definitions/367.html
         * CWE-362: Concurrent Execution using Shared Resource with Improper Synchronization ('Race Condition') - https://cwe.mitre.org/data/definitions/362.html
         * CWE-820: Missing Synchronization - https://cwe.mitre.org/data/definitions/820.html
         */
        if (!_started)
        {
            _started = true;
        }
    }
}";

      var message = string.Format(Resources.Culture, Resources.DSA050MessageSetOnce, "_started", "true", "false");
      var test = new CSharpCodeFixVerifier<DSA050Analyzer, DSA050CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa050Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA050Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA050Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(12, 9, 12, 23).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
