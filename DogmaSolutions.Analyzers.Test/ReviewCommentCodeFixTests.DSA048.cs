using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa048Key = DSA048Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA048 review comment

   [TestMethod]
   public async Task DSA048_ReviewComment_PollingLoop()
   {
      var source = @"using System.Threading;

public class Worker
{
    private bool _ready;

    public void Wait()
    {
        {|#0:while (!_ready)|}
        {
            Thread.Sleep(50);
        }
    }
}";

      var fixedSource = @"using System.Threading;

public class Worker
{
    private bool _ready;

    public void Wait()
    {
        /* [DSA048 / QA + Code Smell]: Polling loop that waits for a change of state (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA048.md)
         * CWE-833: Deadlock - https://cwe.mitre.org/data/definitions/833.html
         * CWE-835: Loop with Unreachable Exit Condition ('Infinite Loop') - https://cwe.mitre.org/data/definitions/835.html
         * CWE-400: Uncontrolled Resource Consumption - https://cwe.mitre.org/data/definitions/400.html
         */
        while (!_ready)
        {
            Thread.Sleep(50);
        }
    }
}";

      var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA048MessageNoTimeout", Resources.Culture), "_ready", "Thread.Sleep");
      var test = new CSharpCodeFixVerifier<DSA048Analyzer, DSA048CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa048Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA048Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA048Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(14, 9, 14, 24).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
