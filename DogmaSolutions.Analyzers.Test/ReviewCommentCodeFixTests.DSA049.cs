using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa049Key = DSA049Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA049 review comment

   [TestMethod]
   public async Task DSA049_ReviewComment_FieldAccessedWithAndWithoutItsLock()
   {
      var source = @"public class Worker
{
    private readonly object _gate = new object();
    private int _count;

    public void Inc()
    {
        lock (_gate)
        {
            _count++;
        }
    }

    public void Reset()
    {
        {|#0:_count|} = 0;
    }
}";

      var fixedSource = @"public class Worker
{
    private readonly object _gate = new object();
    private int _count;

    public void Inc()
    {
        lock (_gate)
        {
            _count++;
        }
    }

    public void Reset()
    {
        /* [DSA049 / QA + Code Smell]: Field accessed with and without its lock (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA049.md)
         * CWE-366: Race Condition within a Thread - https://cwe.mitre.org/data/definitions/366.html
         * CWE-567: Unsynchronized Access to Shared Data in a Multithreaded Context - https://cwe.mitre.org/data/definitions/567.html
         * CWE-820: Missing Synchronization - https://cwe.mitre.org/data/definitions/820.html
         */
        _count = 0;
    }
}";

      var message = string.Format(Resources.Culture, Resources.DSA049MessageUnlockedWrite, "_count", "_gate", "Inc", string.Empty);
      var test = new CSharpCodeFixVerifier<DSA049Analyzer, DSA049CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa049Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA049Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA049Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(21, 9, 21, 15).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
