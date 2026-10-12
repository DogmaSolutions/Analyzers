using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// An identifier that cannot be resolved (here, a typo in the code being edited) gives no symbol to compare: two chains with the same
/// text are still the same chain.
/// </summary>
[TestClass]
public class DSA019UnresolvedIdentifierTests
{
    [TestMethod]
    public async Task RepeatedChainWithUnresolvedRoot_IsReported()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class MyService
                {
                    public void Process()
                    {
                        var a = {|#0:{|CS0103:unknown|}.First.Second.Third|};
                        var b = {|#1:{|CS0103:unknown|}.First.Second.Third|};
                        var c = {|#2:{|CS0103:unknown|}.First.Second.Third|};
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        for (var i = 0; i < 3; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                    .WithLocation(i)
                    .WithArguments("unknown.First.Second.Third", 3));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
