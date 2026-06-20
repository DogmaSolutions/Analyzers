using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSR003RefactoringTests
{
   [TestMethod]
   public async Task SplitsTwoClassesViaRefactoring()
   {
      var source = @"namespace TestApp
{
    public class [||]ClassA
    {
        public int A { get; set; }
    }

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var fixedOriginal = @"namespace TestApp
{
    public class ClassA
    {
        public int A { get; set; }
    }
}";

      var fixedNew = @"namespace TestApp
{

    public class ClassB
    {
        public int B { get; set; }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR003RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR003RefactoringProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("ClassA.cs", fixedOriginal));
      test.FixedState.Sources.Add(("/0/ClassB.cs", fixedNew));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NoRefactoringOffered_SingleType()
   {
      var source = @"namespace TestApp
{
    public class [||]ClassA
    {
        public int A { get; set; }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR003RefactoringProvider>.Test();
      test.TestCode = source;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.FixedCode = source;
      test.CodeActionEquivalenceKey = DSR003RefactoringProvider.EquivalenceKey;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsWithFileScopedNamespace()
   {
      var source = @"namespace TestApp;

public class [||]Alpha
{
    public int A { get; set; }
}

public class Beta
{
    public int B { get; set; }
}";

      var fixedAlpha = @"namespace TestApp;

public class Alpha
{
    public int A { get; set; }
}
";

      var fixedBeta = @"namespace TestApp;

public class Beta
{
    public int B { get; set; }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR003RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR003RefactoringProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Alpha.cs", fixedAlpha));
      test.FixedState.Sources.Add(("/0/Beta.cs", fixedBeta));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsInterfaceAndEnum()
   {
      var source = @"namespace TestApp
{
    public interface [||]IWidget
    {
        int Size { get; }
    }

    public enum Flavor
    {
        Sweet,
        Sour
    }
}";

      var fixedInterface = @"namespace TestApp
{
    public interface IWidget
    {
        int Size { get; }
    }
}";

      var fixedEnum = @"namespace TestApp
{

    public enum Flavor
    {
        Sweet,
        Sour
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR003RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR003RefactoringProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("IWidget.cs", fixedInterface));
      test.FixedState.Sources.Add(("/0/Flavor.cs", fixedEnum));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsWithUsingsPreserved()
   {
      var source = @"using System;

namespace TestApp
{
    public class [||]ClassA
    {
        public DateTime Start { get; set; }
    }

    public class ClassB
    {
        public DateTime End { get; set; }
    }
}";

      var fixedOriginal = @"using System;

namespace TestApp
{
    public class ClassA
    {
        public DateTime Start { get; set; }
    }
}";

      var fixedNew = @"using System;

namespace TestApp
{

    public class ClassB
    {
        public DateTime End { get; set; }
    }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR003RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR003RefactoringProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("ClassA.cs", fixedOriginal));
      test.FixedState.Sources.Add(("/0/ClassB.cs", fixedNew));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SplitsWithNoNamespace()
   {
      // Two classes with NO namespace. RemoveNodes on CompilationUnit children.
      var source = @"public class [||]Pixel
{
    public int X { get; set; }
}

public class Voxel
{
    public int Y { get; set; }
}";

      var fixedPixel = @"public class Pixel
{
    public int X { get; set; }
}
";

      var fixedVoxel = @"
public class Voxel
{
    public int Y { get; set; }
}";

      var test = new CSharpCodeRefactoringVerifier<DSR003RefactoringProvider>.Test();
      test.TestState.Sources.Add(source);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR003RefactoringProvider.EquivalenceKey;
      test.FixedState.Sources.Add(("Pixel.cs", fixedPixel));
      test.FixedState.Sources.Add(("/0/Voxel.cs", fixedVoxel));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;

      await test.RunAsync().ConfigureAwait(false);
   }
}
