using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA035CodeFixTests
{
    [TestMethod]
    public async Task HoistsGetTypeFromForLoop()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var t = {|#0:obj.GetType()|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, int[] arr)
                    {
                        var hoisted_obj_GetType = obj.GetType();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var t = hoisted_obj_GetType;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsGetPropertiesFromForEachLoop()
    {
        var source = @"
            using System;
            using System.Reflection;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type, List<string> names)
                    {
                        foreach (var name in names)
                        {
                            var props = {|#0:type.GetProperties()|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            using System;
            using System.Reflection;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type, List<string> names)
                    {
                        var hoisted_type_GetProperties = type.GetProperties();
                        foreach (var name in names)
                        {
                            var props = hoisted_type_GetProperties;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("type.GetProperties()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsFromWhileLoop()
    {
        var source = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type)
                    {
                        int i = 0;
                        while (i < 100)
                        {
                            var prop = {|#0:type.GetProperty(""Name"")|};
                            i++;
                        }
                    }
                }
            }";

        var fixedSource = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type)
                    {
                        int i = 0;
                        var hoisted_type_GetProperty = type.GetProperty(""Name"");
                        while (i < 100)
                        {
                            var prop = hoisted_type_GetProperty;
                            i++;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments(@"type.GetProperty(""Name"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsFromDoWhileLoop()
    {
        var source = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type)
                    {
                        int i = 0;
                        do
                        {
                            var method = {|#0:type.GetMethod(""Execute"")|};
                            i++;
                        } while (i < 100);
                    }
                }
            }";

        var fixedSource = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type)
                    {
                        int i = 0;
                        var hoisted_type_GetMethod = type.GetMethod(""Execute"");
                        do
                        {
                            var method = hoisted_type_GetMethod;
                            i++;
                        } while (i < 100);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments(@"type.GetMethod(""Execute"")"));
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
                    public void Test(object obj, int[] arr)
                    {
                        var hoisted_obj_GetType = 999;
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var t = {|#0:obj.GetType()|};
                            var x = hoisted_obj_GetType;
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, int[] arr)
                    {
                        var hoisted_obj_GetType = 999;
                        var hoisted_obj_GetType1 = obj.GetType();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var t = hoisted_obj_GetType1;
                            var x = hoisted_obj_GetType;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsMultipleOccurrencesOfSameReflectionCall()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var t1 = {|#0:obj.GetType()|};
                            var t2 = {|#1:obj.GetType()|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, int[] arr)
                    {
                        var hoisted_obj_GetType = obj.GetType();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var t1 = hoisted_obj_GetType;
                            var t2 = hoisted_obj_GetType;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("obj.GetType()"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(1).WithArguments("obj.GetType()"));
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
                    public void Test(object obj)
                    {
                        for (int y = 0; y < 10; y++)
                        {
                            for (int x = 0; x < 10; x++)
                            {
                                var t = {|#0:obj.GetType()|};
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
                    public void Test(object obj)
                    {
                        var hoisted_obj_GetType1 = obj.GetType();
                        for (int y = 0; y < 10; y++)
                        {
                            var hoisted_obj_GetType = hoisted_obj_GetType1;
                            for (int x = 0; x < 10; x++)
                            {
                                var t = hoisted_obj_GetType;
                            }
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.NumberOfIncrementalIterations = 2;
        test.NumberOfFixAllIterations = 2;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsGetFieldsFromForLoop()
    {
        var source = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var fields = {|#0:type.GetFields()|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(Type type, int[] arr)
                    {
                        var hoisted_type_GetFields = type.GetFields();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var fields = hoisted_type_GetFields;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("type.GetFields()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task HoistsTypeofGetPropertiesFromForLoop()
    {
        var source = @"
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var props = {|#0:typeof(string).GetProperties()|};
                        }
                    }
                }
            }";

        var fixedSource = @"
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        var hoisted_GetProperties = typeof(string).GetProperties();
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var props = hoisted_GetProperties;
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>
                .Diagnostic(DSA035Analyzer.DiagnosticId).WithLocation(0).WithArguments("typeof(string).GetProperties()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DoesNotFlagIterationVariableReceiver()
    {
        var source = @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<object> items)
                    {
                        foreach (var item in items)
                        {
                            var t = item.GetType();
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA035Analyzer, DSA035CodeFixProvider>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }
}
