using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{
   private const string Dsa037Key = DSA037Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;
   private const string Dsa038Key = DSA038Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;
   private const string Dsa039Key = DSA039Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;
   private const string Dsa040Key = DSA040Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;

   [TestMethod]
   public async Task DSA037_ReviewComment()
   {
      var source = @"using System;

namespace TestApp
{
    public class MyType
    {
        [ThreadStatic]
        private static Random _rnd = new Random();
    }
}";

      var fixedSource = @"using System;

namespace TestApp
{
    public class MyType
    {
        /* [DSA037 / QA + Bug]: A [ThreadStatic] Random field must not have a field initializer (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA037.md)
         * A [ThreadStatic] field initializer runs only on the thread that first triggers static construction; every other thread observes the default (null). Assign the Random per-thread on first use, or use Random.Shared.
         * MITRE, CWE-457: Use of Uninitialized Variable - https://cwe.mitre.org/data/definitions/457.html
         * ThreadStaticAttribute - https://learn.microsoft.com/en-us/dotnet/api/system.threadstaticattribute
         */
        [ThreadStatic]
        private static Random _rnd = new Random();
    }
}";

      var test = new CSharpCodeFixVerifier<DSA037Analyzer, DSA037CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa037Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA037Analyzer, DSA037CodeFixProvider>.Diagnostic(DSA037Analyzer.DiagnosticId)
            .WithSpan(8, 36, 8, 50).WithArguments("_rnd"));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA037Analyzer, DSA037CodeFixProvider>.Diagnostic(DSA037Analyzer.DiagnosticId)
            .WithSpan(13, 36, 13, 50).WithArguments("_rnd"));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA038_ReviewComment()
   {
      var source = @"using System;

namespace TestApp
{
    public class MyType
    {
        public Random Make() => new Random(Environment.TickCount);
    }
}";

      var fixedSource = @"using System;

namespace TestApp
{
    public class MyType
    {
        /* [DSA038 / CySec + QA + Bug]: Avoid seeding Random with a time-based or constant value (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA038.md)
         * A time-based seed collides within a clock tick (identical sequences); a constant seed is fully predictable. Prefer the parameterless 'new Random()' or Random.Shared.
         * MITRE, CWE-337: Predictable Seed in Pseudo-Random Number Generator - https://cwe.mitre.org/data/definitions/337.html
         * System.Random - https://learn.microsoft.com/en-us/dotnet/api/system.random
         */
        public Random Make() => new Random(Environment.TickCount);
    }
}";

      var test = new CSharpCodeFixVerifier<DSA038Analyzer, DSA038CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa038Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA038Analyzer, DSA038CodeFixProvider>.Diagnostic(DSA038Analyzer.DiagnosticId)
            .WithSpan(7, 33, 7, 66)
            .WithArguments("a time-based value, which is redundant with the parameterless 'new Random()' and makes instances created within the same clock tick share a sequence"));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA038Analyzer, DSA038CodeFixProvider>.Diagnostic(DSA038Analyzer.DiagnosticId)
            .WithSpan(12, 33, 12, 66)
            .WithArguments("a time-based value, which is redundant with the parameterless 'new Random()' and makes instances created within the same clock tick share a sequence"));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA039_ReviewComment()
   {
      var source = @"using System;
using System.Security.Cryptography;

namespace TestApp
{
    public class MyType
    {
        public int Roll(int n) => BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4), 0) % n;
    }
}";

      var fixedSource = @"using System;
using System.Security.Cryptography;

namespace TestApp
{
    public class MyType
    {
        /* [DSA039 / CySec + QA + Security]: Avoid modulo bias when reducing cryptographic random bytes into a range (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA039.md)
         * A naive % reduction skews the distribution and undermines the cryptographic guarantee. Use RandomNumberGenerator.GetInt32(fromInclusive, toExclusive), which rejection-samples an unbiased value.
         * MITRE, CWE-1241: Use of Predictable Algorithm in Random Number Generator - https://cwe.mitre.org/data/definitions/1241.html
         * MITRE, CWE-338: Use of Cryptographically Weak Pseudo-Random Number Generator (PRNG) - https://cwe.mitre.org/data/definitions/338.html
         * RandomNumberGenerator.GetInt32 - https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator.getint32
         */
        public int Roll(int n) => BitConverter.ToInt32(RandomNumberGenerator.GetBytes(4), 0) % n;
    }
}";

      var test = new CSharpCodeFixVerifier<DSA039Analyzer, DSA039CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa039Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA039Analyzer, DSA039CodeFixProvider>.Diagnostic(DSA039Analyzer.DiagnosticId)
            .WithSpan(8, 35, 8, 97));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA039Analyzer, DSA039CodeFixProvider>.Diagnostic(DSA039Analyzer.DiagnosticId)
            .WithSpan(14, 35, 14, 97));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DSA040_ReviewComment()
   {
      var source = @"using System;

namespace TestApp
{
    public class MyType
    {
        public void M()
        {
            var apiKey = new Random().Next();
        }
    }
}";

      var fixedSource = @"using System;

namespace TestApp
{
    public class MyType
    {
        public void M()
        {
            /* [DSA040 / CySec + QA + Security]: Do not use System.Random for security-sensitive values (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA040.md)
             * System.Random is fast but predictable; an attacker who reconstructs its state can forge tokens, salts, nonces, keys or OTPs. Use System.Security.Cryptography.RandomNumberGenerator (GetBytes / GetInt32).
             * MITRE, CWE-338: Use of Cryptographically Weak Pseudo-Random Number Generator (PRNG) - https://cwe.mitre.org/data/definitions/338.html
             * OWASP: Insecure Randomness - https://owasp.org/www-community/vulnerabilities/Insecure_Randomness
             * RandomNumberGenerator - https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.randomnumbergenerator
             */
            var apiKey = new Random().Next();
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA040Analyzer, DSA040CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa040Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA040Analyzer, DSA040CodeFixProvider>.Diagnostic(DSA040Analyzer.DiagnosticId)
            .WithSpan(9, 26, 9, 45));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA040Analyzer, DSA040CodeFixProvider>.Diagnostic(DSA040Analyzer.DiagnosticId)
            .WithSpan(15, 26, 15, 45));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;
      await test.RunAsync().ConfigureAwait(false);
   }
}
