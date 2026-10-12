using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA016RepetitionTests
{
    /// <summary>
    /// A call repeated many times in a scope is reported at every occurrence with the number of repetitions, while the same call in
    /// mutually exclusive branches (or on a different receiver) is not a repetition.
    /// </summary>
    [TestMethod]
    public async Task ManyRepetitions_WithExclusiveBranchesAndOtherReceivers()
    {
        const int repetitions = 40;
        var code = new StringBuilder();
        code.Append("using System.Linq;\nusing System.Collections.Generic;\nnamespace TestApp\n{\n    public class MyService\n    {\n        public void Process(List<int> items, List<int> others, bool flag)\n        {\n");
        for (var i = 0; i < repetitions; i++)
            code.Append("            var a" + i + " = {|#" + i + ":items.Count()|};\n");

        code.Append("            var other = others.Count();\n");
        code.Append("            if (flag)\n            {\n                var c1 = items.Any();\n            }\n            else\n            {\n                var c2 = items.Any();\n            }\n");
        code.Append("        }\n    }\n}");

        var test = new CSharpAnalyzerVerifier<DSA016Analyzer>.Test();
        test.TestCode = code.ToString();
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        for (var i = 0; i < repetitions; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<DSA016Analyzer>.Diagnostic(DSA016Analyzer.DiagnosticId)
                    .WithLocation(i)
                    .WithArguments("Count", repetitions));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
