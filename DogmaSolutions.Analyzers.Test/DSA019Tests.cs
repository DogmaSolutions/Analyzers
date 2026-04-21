using System.Reflection;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA019Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;
        await CSharpAnalyzerVerifier<DSA019Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        #pragma warning disable CA1062
        return (string)data[0];
        #pragma warning restore CA1062
    }

    [TestMethod]
    public async Task IndexerInMiddleOfChain_FlagsBothMemberAccessAndElementAccess()
    {
        var source = @"
            namespace TestApp
            {
                public class Detail { public string Name; public decimal Price; }
                public class Section { public Detail[] Details; }
                public class Document { public Section[] Sections; }
                public class MyService
                {
                    public void Process(Document doc, int s, int d)
                    {
                        var name = {|#0:doc.Sections[s].Details|}[d].Name;
                        var price = {|#1:doc.Sections[s].Details|}[d].Price;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = Microsoft.CodeAnalysis.Testing.ReferenceAssemblies.Net.Net80;

        // MemberAccess diagnostics: doc.Sections[s].Details (depth 3)
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("doc.Sections[s].Details"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("doc.Sections[s].Details"));

        // ElementAccess diagnostics: doc.Sections[s].Details[d] (depth 4)
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(11, 36, 11, 62).WithArguments("doc.Sections[s].Details[d]"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(12, 37, 12, 63).WithArguments("doc.Sections[s].Details[d]"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
