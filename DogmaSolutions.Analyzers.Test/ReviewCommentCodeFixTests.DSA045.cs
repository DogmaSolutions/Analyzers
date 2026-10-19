using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa045Key = DSA045Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA045 review comment

   [TestMethod]
   public async Task DSA045_ReviewComment_PureDataCarrier()
   {
      var source = @"public class {|#0:Dto|}
{
    public int A { get; set; }
}";

      var fixedSource = @"/* [DSA045 / QA + Best Practice]: Pure data carrier not excluded from the code coverage (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA045.md)
 * ExcludeFromCodeCoverageAttribute - https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.excludefromcodecoverageattribute
 * Code coverage - https://en.wikipedia.org/wiki/Code_coverage
 */
public class Dto
{
    public int A { get; set; }
}";

      var test = new CSharpCodeFixVerifier<DSA045Analyzer, DSA045CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa045Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA045Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA045Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(5, 14, 5, 17));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA045_ReviewComment_TypeWithLogicExcluded()
   {
      var source = @"using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class {|#0:Report|}
{
    public int Compute() => 1;
}";

      var fixedSource = @"using System.Diagnostics.CodeAnalysis;

/* [DSA045 / QA + Best Practice]: Type with logic excluded from the code coverage (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA045.md)
 * ExcludeFromCodeCoverageAttribute - https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.excludefromcodecoverageattribute
 * Code coverage - https://en.wikipedia.org/wiki/Code_coverage
 */
[ExcludeFromCodeCoverage]
public class Report
{
    public int Compute() => 1;
}";

      var test = new CSharpCodeFixVerifier<DSA045Analyzer, DSA045CodeFixProvider>.Test();
      test.TestCode = source.Replace("\r\n", "\n");
      test.FixedCode = fixedSource.Replace("\r\n", "\n");
      test.CodeActionEquivalenceKey = Dsa045Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA045Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(new DiagnosticResult(DSA045Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithSpan(8, 14, 8, 20));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
