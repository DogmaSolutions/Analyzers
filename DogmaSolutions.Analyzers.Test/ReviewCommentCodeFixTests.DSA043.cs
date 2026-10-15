using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa043Key = DSA043Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA043 review comment

   [TestMethod]
   public async Task DSA043_ReviewComment_ComplexClassWithoutLogger()
   {
      var source = @"namespace TestApp
{
    public class {|#0:Service|}
    {
        public int Run(int x)
        {
            if (x == 1) { x++; }
            if (x == 2) { x++; }
            try { x++; } catch (System.Exception) { x--; }
            return x;
        }
    }
}
namespace Microsoft.Extensions.Logging { public interface ILogger { } }";

      var fixedSource = @"namespace TestApp
{
    /* [DSA043 / QA + Best Practice]: Complex class without logging (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA043.md)
     * CWE-778: Insufficient Logging - https://cwe.mitre.org/data/definitions/778.html
     * OWASP Top 10 A09:2021 Security Logging and Monitoring Failures - https://owasp.org/Top10/A09_2021-Security_Logging_and_Monitoring_Failures/
     * Logging in .NET - https://learn.microsoft.com/dotnet/core/extensions/logging
     */
    public class Service
    {
        public int Run(int x)
        {
            if (x == 1) { x++; }
            if (x == 2) { x++; }
            try { x++; } catch (System.Exception) { x--; }
            return x;
        }
    }
}
namespace Microsoft.Extensions.Logging { public interface ILogger { } }";

      var test = new CSharpCodeFixVerifier<DSA043Analyzer, DSA043CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa043Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.DSA043.min_cyclomatic_complexity = 4\n"));
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", "root = true\n[*.cs]\ndotnet_diagnostic.DSA043.min_cyclomatic_complexity = 4\n"));
      test.ExpectedDiagnostics.Add(
         new DiagnosticResult(DSA043Analyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         new DiagnosticResult(DSA043Analyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithSpan(8, 18, 8, 25));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
