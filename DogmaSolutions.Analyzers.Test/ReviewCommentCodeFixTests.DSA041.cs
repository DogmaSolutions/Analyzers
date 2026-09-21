using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa041Key = DSA041Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   #region DSA041 review comment

   [TestMethod]
   public async Task DSA041_ReviewComment_LargeEnumSwitch()
   {
      var source = @"using System;

public class C
{
    public int M(DayOfWeek d)
    {
        switch (d)
        {
            default: return 0;
        }
    }
}";

      var fixedSource = @"using System;

public class C
{
    public int M(DayOfWeek d)
    {
        /* [DSA041 / QA + Code Smell]: Switch driven by a large enum (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA041.md)
         * CWE-670: Always-Incorrect Control Flow Implementation - https://cwe.mitre.org/data/definitions/670.html
         * CWE-840: Business Logic Errors - https://cwe.mitre.org/data/definitions/840.html
         * CWE-478: Missing Default Case in Multiple Condition Expression - https://cwe.mitre.org/data/definitions/478.html
         * Strategy pattern - https://en.wikipedia.org/wiki/Strategy_pattern
         */
        switch (d)
        {
            default: return 0;
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA041Analyzer, DSA041CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa041Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA041Analyzer, DSA041CodeFixProvider>.Diagnostic(DSA041Analyzer.DiagnosticId)
            .WithSpan(7, 9, 7, 15)
            .WithArguments("DayOfWeek", 7, 4));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA041Analyzer, DSA041CodeFixProvider>.Diagnostic(DSA041Analyzer.DiagnosticId)
            .WithSpan(13, 9, 13, 15)
            .WithArguments("DayOfWeek", 7, 4));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   #endregion
}
