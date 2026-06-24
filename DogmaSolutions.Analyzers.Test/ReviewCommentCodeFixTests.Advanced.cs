using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class ReviewCommentCodeFixTests
{

   [TestMethod]
   public async Task TabIndentation()
   {
      var source = @"
using System;
namespace TestApp
{
	public class MyClass
	{
		public bool Check(string s)
		{
			return {|#0:string.IsNullOrEmpty(s)|};
		}
	}
}";

      var fixedSource = @"
using System;
namespace TestApp
{
	public class MyClass
	{
		public bool Check(string s)
		{
			/* [DSA003 / CySec + QA + Code Smell]: Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md)
			 * MITRE, CWE-20: Improper Input Validation - https://cwe.mitre.org/data/definitions/20.html
			 * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
			 */
			return {|#0:string.IsNullOrEmpty(s)|};
		}
	}
}";

      var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa003Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ExpressionBodiedMember_DSA004()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public DateTime Timestamp => {|#0:DateTime.Now|};
    }
}";

      var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        /* [DSA004 / CySec + QA + Code Smell]: Use `DateTime.UtcNow` instead of `DateTime.Now` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA004.md)
         * CWE-361: 7PK - Time and State - https://cwe.mitre.org/data/definitions/361.html
         * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
         */
        public DateTime Timestamp => {|#0:DateTime.Now|};
    }
}";

      var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = DSA004Analyzer.DiagnosticId + ReviewCommentCodeFix.EquivalenceKeySuffix;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Diagnostic(DSA004Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Diagnostic(DSA004Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task PreservesRegionDirective()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            /* [DSA003 / CySec + QA + Code Smell]: Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md)
             * MITRE, CWE-20: Improper Input Validation - https://cwe.mitre.org/data/definitions/20.html
             * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
             */
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa003Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FirstStatementInMethod_NoLeadingTrivia()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s) {
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s) {
            /* [DSA003 / CySec + QA + Code Smell]: Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md)
             * MITRE, CWE-20: Improper Input Validation - https://cwe.mitre.org/data/definitions/20.html
             * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
             */
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa003Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task MultipleStatementsOnSameMethod()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            var x = 1;
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            var x = 1;
            /* [DSA003 / CySec + QA + Code Smell]: Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md)
             * MITRE, CWE-20: Improper Input Validation - https://cwe.mitre.org/data/definitions/20.html
             * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
             */
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa003Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task Idempotency_DoesNotInsertDuplicateComment()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            /* [DSA003 / CySec + QA + Code Smell]: Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md)
             * MITRE, CWE-20: Improper Input Validation - https://cwe.mitre.org/data/definitions/20.html
             * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
             */
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source;
      test.CodeActionEquivalenceKey = Dsa003Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public void SanitizeBlockComment_NoInternalClosing_Unchanged()
   {
      var input = "/* safe comment body */";
      var result = ReviewCommentCodeFix.SanitizeBlockComment(input);
      Assert.AreEqual(input, result);
   }

   [TestMethod]
   public void SanitizeBlockComment_InternalClosing_Escaped()
   {
      var input = "/* contains */ inside */";
      var result = ReviewCommentCodeFix.SanitizeBlockComment(input);
      Assert.AreEqual("/* contains * / inside */", result);
   }

   [TestMethod]
   public void SanitizeBlockComment_NotBlockComment_Unchanged()
   {
      var input = "// line comment";
      var result = ReviewCommentCodeFix.SanitizeBlockComment(input);
      Assert.AreEqual(input, result);
   }

   [TestMethod]
   public void SanitizeBlockComment_MultipleInternalClosings()
   {
      var input = "/* a */ b */ c */";
      var result = ReviewCommentCodeFix.SanitizeBlockComment(input);
      Assert.AreEqual("/* a * / b * / c */", result);
   }

   [TestMethod]
   public async Task XmlDocComment_Preserved()
   {
      var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>Checks the string.</summary>
        public bool Check(string s)
        {
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>Checks the string.</summary>
        public bool Check(string s)
        {
            /* [DSA003 / CySec + QA + Code Smell]: Use `String.IsNullOrWhiteSpace` instead of `String.IsNullOrEmpty` (see: https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA003.md)
             * MITRE, CWE-20: Improper Input Validation - https://cwe.mitre.org/data/definitions/20.html
             * IEC 62443-3-3: System security requirements and security levels - https://webstore.iec.ch/en/publication/7033
             */
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

      var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.CodeActionEquivalenceKey = Dsa003Key;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.FixedState.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Diagnostic(DSA003Analyzer.DiagnosticId)
            .WithLocation(0));
      test.NumberOfIncrementalIterations = 1;
      test.NumberOfFixAllIterations = 1;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck | CodeFixTestBehaviors.FixOne;

      await test.RunAsync().ConfigureAwait(false);
   }
}
