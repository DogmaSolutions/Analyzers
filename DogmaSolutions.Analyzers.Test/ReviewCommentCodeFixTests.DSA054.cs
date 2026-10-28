using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa054Key = DSA054Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA054 review comment

   [TestMethod]
   public async Task DSA054_ReviewComment_AssignedAndReadInTheSameMember()
   {
      var source = @"public class Worker
{
    private int _total;

    public int Sum(int a, int b)
    {
        {|#0:_total = a + b|};
        return _total * 2;
    }
}";

      var fixedSource = @"public class Worker
{
    private int _total;

    public int Sum(int a, int b)
    {
        /* [DSA054 / QA + Code Smell]: Field assigned and then read only inside the same member: a missing memoization or a leftover field (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA054.md)
         * CWE-1164: Irrelevant Code - https://cwe.mitre.org/data/definitions/1164.html
         * CWE-563: Assignment to Variable without Use - https://cwe.mitre.org/data/definitions/563.html
         */
        _total = a + b;
        return _total * 2;
    }
}";

      var message = string.Format(Resources.Culture, Resources.DSA054MessageLeftover, "_total", "Sum");
      var test = new CSharpCodeFixVerifier<DSA054Analyzer, DSA054CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa054Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA054Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA054Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(11, 9, 11, 23).WithMessage(message));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
