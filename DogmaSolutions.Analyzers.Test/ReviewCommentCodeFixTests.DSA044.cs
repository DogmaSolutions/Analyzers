using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa044Key = DSA044Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA044 review comment

   [TestMethod]
   public async Task DSA044_ReviewComment_ConsoleWriteWhereALoggerIsAvailable()
   {
      var source = @"namespace TestApp
{
    public class Service
    {
        private Serilog.ILogger _logger;

        public void Run()
        {
            {|#0:System.Console.WriteLine(""hello"")|};
        }
    }
}";

      var fixedSource = @"namespace TestApp
{
    public class Service
    {
        private Serilog.ILogger _logger;

        public void Run()
        {
            /* [DSA044 / QA + Best Practice]: Console or debug output where a logger is available (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA044.md)
             * CWE-778: Insufficient Logging - https://cwe.mitre.org/data/definitions/778.html
             * Logging in .NET - https://learn.microsoft.com/dotnet/core/extensions/logging
             */
            System.Console.WriteLine(""hello"");
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA044Analyzer, DSA044CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa044Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.TestState.Sources.Add(("Fakes.cs", DSA044TestSupport.FakeLoggersSource));
      test.FixedState.Sources.Add(("Fakes.cs", DSA044TestSupport.FakeLoggersSource));
      test.ExpectedDiagnostics.Add(
         new DiagnosticResult(DSA044Analyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         new DiagnosticResult(DSA044Analyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Info).WithSpan(13, 13, 13, 46));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
