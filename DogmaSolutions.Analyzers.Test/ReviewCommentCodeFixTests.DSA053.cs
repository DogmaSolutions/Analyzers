using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa053Key = DSA053Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA053 review comment

   [TestMethod]
   public async Task DSA053_ReviewComment()
   {
      const string Header = "using System.Collections.Generic;\nusing System.Linq;\n\npublic class Repository\n{\n    public bool Has(IEnumerable<Repository> items)\n    {\n";
      var comment = new[]
      {
         "        /* [DSA053 / QA + Performance]: Count used only to test for existence (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA053.md)",
         "         * CWE-1049: Excessive Data Query Operations in a Large Data Table - https://cwe.mitre.org/data/definitions/1049.html",
         "         * CWE-405: Asymmetric Resource Consumption (Amplification) - https://cwe.mitre.org/data/definitions/405.html",
         "         */",
      };
      var source = Header + "        var n = {|#0:items.Count()|};\n        return n > 0;\n    }\n}";
      var fixedSource = Header + string.Join("\n", comment) + "\n        var n = items.Count();\n        return n > 0;\n    }\n}";

      var test = new CSharpCodeFixVerifier<DSA053Analyzer, DSA053CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa053Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA053Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA053Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(12, 17, 12, 30));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
