using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa010Key = DSA010Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA010 review comment

   [TestMethod]
   public async Task DSA010_ReviewComment_LockAroundASingleAccess()
   {
      var source = @"public class Worker
{
    private readonly object _gate = new object();
    private bool _flag;

    public bool Flag
    {
        get
        {
            {|#0:lock (_gate)|}
            {
                return _flag;
            }
        }
    }
}";

      var fixedSource = @"public class Worker
{
    private readonly object _gate = new object();
    private bool _flag;

    public bool Flag
    {
        get
        {
            /* [DSA010 / QA + Code Smell]: Lock around a single field access (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA010.md)
             * CWE-667: Improper Locking - https://cwe.mitre.org/data/definitions/667.html
             * CWE-362: Concurrent Execution using Shared Resource with Improper Synchronization ('Race Condition') - https://cwe.mitre.org/data/definitions/362.html
             * CWE-820: Missing Synchronization - https://cwe.mitre.org/data/definitions/820.html
             */
            lock (_gate)
            {
                return _flag;
            }
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA010Analyzer, DSA010CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa010Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA010Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(ExpectedMessage()));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA010Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(15, 13, 15, 25).WithMessage(ExpectedMessage()));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   private static string ExpectedMessage() =>
      string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA010MessageReadDedicated", Resources.Culture), "_flag", "_gate");

   #endregion
}
