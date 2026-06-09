using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSR002RefactoringTests
{
   // ──────────────────────────────────────────────────────────────────────────
   //  Local constant extraction
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task SingleOccurrence_ExtractsToLocalConst()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string HelloWorld = ""hello world"";
            var a = HelloWorld;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task MultipleOccurrences_ExtractsToLocalConst()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]hello world"";
            var b = ""hello world"";
            var c = ""hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string HelloWorld = ""hello world"";
            var a = HelloWorld;
            var b = HelloWorld;
            var c = HelloWorld;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Class field extraction
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task SingleOccurrence_ExtractsToClassField()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string HelloWorld = ""hello world"";
        public void Process()
        {
            var a = HelloWorld;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.ClassFieldEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task MultipleOccurrences_ExtractsToClassField()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]hello world"";
            var b = ""hello world"";
            var c = ""hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string HelloWorld = ""hello world"";
        public void Process()
        {
            var a = HelloWorld;
            var b = HelloWorld;
            var c = HelloWorld;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.ClassFieldEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Name conflict resolution
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task LocalConst_NameConflict()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var HelloWorld = ""other"";
            var a = ""[||]hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var HelloWorld = ""other"";
            const string HelloWorld1 = ""hello world"";
            var a = HelloWorld1;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ClassField_NameConflict()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        private const string HelloWorld = ""other"";
        public void Process()
        {
            var a = ""[||]hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string HelloWorld = ""other"";
        private const string HelloWorld1 = ""hello world"";
        public void Process()
        {
            var a = HelloWorld1;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.ClassFieldEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Short strings / edge cases
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ShortString_StillOffered()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]hi"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string Hi = ""hi"";
            var a = Hi;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task EmptyString_NoRefactoring()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]"";
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Constructor
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task StringInConstructor_LocalConst()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public MyService()
        {
            var a = ""[||]connection string"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public MyService()
        {
            const string ConnectionString = ""connection string"";
            var a = ConnectionString;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Expression-bodied member (only class field)
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ExpressionBodiedMember_ClassFieldOnly()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public string GetValue() => ""[||]hello world"";
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        private const string HelloWorld = ""hello world"";
        public string GetValue() => HelloWorld;
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.ClassFieldEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Nested class
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task NestedClass_FieldInCorrectType()
   {
      var source = @"
namespace TestApp
{
    public class Outer
    {
        public class Inner
        {
            public void Process()
            {
                var a = ""[||]nested value"";
            }
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class Outer
    {
        public class Inner
        {
            private const string NestedValue = ""nested value"";
            public void Process()
            {
                var a = NestedValue;
            }
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.ClassFieldEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Name generation
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task SpecialChars_CorrectPascalCase()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]connection-string:default"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string ConnectionStringDefault = ""connection-string:default"";
            var a = ConnectionStringDefault;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DigitLeading_FallbackName()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = ""[||]123abc"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string StringConstant = ""123abc"";
            var a = StringConstant;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Scope isolation
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task DeepNesting_CorrectIndentation()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            if (true)
            {
                var a = ""[||]nested string"";
            }
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string NestedString = ""nested string"";
            if (true)
            {
                var a = NestedString;
            }
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SameStringTwoMethods_OnlyCurrentMethodReplaced()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void MethodA()
        {
            var a = ""[||]hello world"";
        }

        public void MethodB()
        {
            var b = ""hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void MethodA()
        {
            const string HelloWorld = ""hello world"";
            var a = HelloWorld;
        }

        public void MethodB()
        {
            var b = ""hello world"";
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Replacements file
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task ReplacementsFile_ReplaceOffered()
   {
      var source = @"
namespace TestApp
{
    public static class Constants
    {
        public const string HelloWorld = ""hello world"";
    }

    public class MyService
    {
        public void Process()
        {
            var a = ""[||]hello world"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public static class Constants
    {
        public const string HelloWorld = ""hello world"";
    }

    public class MyService
    {
        public void Process()
        {
            var a = Constants.HelloWorld;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.UseReplacementEquivalenceKey + ":Constants.HelloWorld";
      test.TestState.AdditionalFiles.Add(("DSA032_StringReplacements.txt", "`hello world` -> Constants.HelloWorld"));
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Negative: no refactoring expected
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task InterpolatedString_NoRefactoring()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var name = ""world"";
            var a = $""hello {[||]name}"";
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NumericLiteral_NoRefactoring()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var a = [||]42;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Method arguments and multiple statements
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task StringInMethodArguments_LocalConst()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            System.Console.WriteLine(""[||]repeated value"");
            System.Console.WriteLine(""repeated value"");
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            const string RepeatedValue = ""repeated value"";
            System.Console.WriteLine(RepeatedValue);
            System.Console.WriteLine(RepeatedValue);
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ConstInsertedBeforeFirstUsage()
   {
      var source = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var x = 1;
            var a = ""[||]my value"";
            var b = ""my value"";
        }
    }
}";

      var fixedSource = @"
namespace TestApp
{
    public class MyService
    {
        public void Process()
        {
            var x = 1;
            const string MyValue = ""my value"";
            var a = MyValue;
            var b = MyValue;
        }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR002RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR002RefactoringProvider.LocalConstEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }
}
