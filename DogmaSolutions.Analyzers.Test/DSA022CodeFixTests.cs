using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA022CodeFixTests
{
    [TestMethod]
    public async Task HoistsSimpleInvariantFromForLoop()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a * b|} + i;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        var hoisted_a_b = a * b;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = hoisted_a_b + i;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a * b"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsFromNestedLoop()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int width, int[] output, int[] input)
                    {
                        for (int y = 0; y < 10; y++)
                        {
                            for (int x = 0; x < 10; x++)
                            {
                                output[{|#0:y * width|}] = input[x];
                            }
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int width, int[] output, int[] input)
                    {
                        for (int y = 0; y < 10; y++)
                        {
                            var hoisted_y_width = y * width;
                            for (int x = 0; x < 10; x++)
                            {
                                output[hoisted_y_width] = input[x];
                            }
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("y * width"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsFromWhileLoop()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        int i = 0;
                        int result = 0;
                        while (i < 100)
                        {
                            result = {|#0:a + b|};
                            i++;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        int i = 0;
                        int result = 0;
                        var hoisted_a_b = a + b;
                        while (i < 100)
                        {
                            result = hoisted_a_b;
                            i++;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a + b"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsFromDoWhileLoop()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        int i = 0;
                        int result = 0;
                        do
                        {
                            result = {|#0:a * b|};
                            i++;
                        } while (i < 100);
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        int i = 0;
                        int result = 0;
                        var hoisted_a_b = a * b;
                        do
                        {
                            result = hoisted_a_b;
                            i++;
                        } while (i < 100);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a * b"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsFromForEachLoop()
    {
        var source = @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<int> list, int a, int b)
                    {
                        int result = 0;
                        foreach (var item in list)
                        {
                            result = {|#0:a * b|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<int> list, int a, int b)
                    {
                        int result = 0;
                        var hoisted_a_b = a * b;
                        foreach (var item in list)
                        {
                            result = hoisted_a_b;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a * b"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsComplexExpression()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int c, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a * b + c|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int c, int[] arr)
                    {
                        var hoisted_a_b = a * b + c;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = hoisted_a_b;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a * b + c"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsWithNameConflict()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        var hoisted_a_b = 999;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a * b|} + i + hoisted_a_b;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        var hoisted_a_b = 999;
                        var hoisted_a_b1 = a * b;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = hoisted_a_b1 + i + hoisted_a_b;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a * b"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsMultipleOccurrencesOfSameExpression()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr, int[] arr2)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a * b|} + i;
                            arr2[i] = {|#1:a * b|} - i;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr, int[] arr2)
                    {
                        var hoisted_a_b = a * b;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = hoisted_a_b + i;
                            arr2[i] = hoisted_a_b - i;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("a * b"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(1).WithArguments("a * b"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsLiteralOnlyExpression()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:10 * 20|} + i;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        var hoisted = 10 * 20;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = hoisted + i;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("10 * 20"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsConstFieldExpression()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    private const int Scale = 10;
                    public void Test(int a, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:Scale * a|} + i;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    private const int Scale = 10;
                    public void Test(int a, int[] arr)
                    {
                        var hoisted_Scale_a = Scale * a;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = hoisted_Scale_a + i;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA022Analyzer, DSA022CodeFixProvider>
                .Diagnostic(DSA022Analyzer.DiagnosticId).WithLocation(0).WithArguments("Scale * a"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
