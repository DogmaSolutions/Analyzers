using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa046Key = DSA046Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA046 review comment

   [TestMethod]
   public async Task DSA046_ReviewComment_ReadModifyWriteOfAVolatileField()
   {
      var source = @"public class Worker
{
    private volatile bool _on;

    public void Toggle()
    {
        {|#0:_on = !_on|};
    }
}";

      var fixedSource = @"public class Worker
{
    private volatile bool _on;

    public void Toggle()
    {
        /* [DSA046 / QA + Code Smell]: Read-modify-write of a volatile field (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA046.md)
         * CWE-362: Concurrent Execution using Shared Resource with Improper Synchronization ('Race Condition') - https://cwe.mitre.org/data/definitions/362.html
         * CWE-366: Race Condition within a Thread - https://cwe.mitre.org/data/definitions/366.html
         * CWE-820: Missing Synchronization - https://cwe.mitre.org/data/definitions/820.html
         */
        _on = !_on;
    }
}";

      var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA046MessageAssignmentValue", Resources.Culture), "_on", "=", string.Format(Resources.Culture, Resources.DSA046RemedyNoOverload, "bool"));
      var test = new CSharpCodeFixVerifier<DSA046Analyzer, DSA046CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa046Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA046Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA046Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(12, 9, 12, 19).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
