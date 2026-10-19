using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa052Key = DSA052Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA052 review comment

   [TestMethod]
   public async Task DSA052_ReviewComment()
   {
      const string Header = "using System.Collections.Generic;\nusing System.Linq;\n\npublic class Repository\n{\n    public bool Has(IEnumerable<Repository> items)\n    {\n";
      var comment = new[]
      {
         "        /* [DSA052 / QA + Performance]: FirstOrDefault used only to test for existence (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA052.md)",
         "         * CWE-1049: Excessive Data Query Operations in a Large Data Table - https://cwe.mitre.org/data/definitions/1049.html",
         "         * CWE-405: Asymmetric Resource Consumption (Amplification) - https://cwe.mitre.org/data/definitions/405.html",
         "         */",
      };
      var source = Header + "        return {|#0:items.FirstOrDefault()|} != null;\n    }\n}";
      var fixedSource = Header + string.Join("\n", comment) + "\n        return items.FirstOrDefault() != null;\n    }\n}";

      var test = new CSharpCodeFixVerifier<DSA052Analyzer, DSA052CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa052Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA052Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA052Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(12, 16, 12, 38));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
