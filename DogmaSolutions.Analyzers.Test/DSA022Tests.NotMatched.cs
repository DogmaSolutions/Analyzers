using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA022Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Expression uses loop variable: i * 2",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = i * 2;
                        }
                    }
                }
            }"
        ],
        [
            "Expression uses variable assigned inside loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a)
                    {
                        int x = 0;
                        for (int i = 0; i < 100; i++)
                        {
                            x = i + 1;
                            int val = x * a;
                        }
                    }
                }
            }"
        ],
        [
            "Trivial expression: single literal",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = 42;
                        }
                    }
                }
            }"
        ],
        [
            "Expression in loop condition is not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        for (int i = 0; i < a * b; i++)
                        {
                            int x = i;
                        }
                    }
                }
            }"
        ],
        [
            "Method call in expression: not flagged",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = (int)(Math.Sin(a) + i);
                        }
                    }
                }
            }"
        ],
        [
            "Variable incremented in loop: not invariant",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int y)
                    {
                        int x = 0;
                        for (int i = 0; i < 100; i++)
                        {
                            int val = x * y;
                            x++;
                        }
                    }
                }
            }"
        ],
        [
            "ForEach iteration variable in expression",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<int> list)
                    {
                        int total = 0;
                        foreach (var x in list)
                        {
                            total = x * 2;
                        }
                    }
                }
            }"
        ],
        [
            "Array element access in expression: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] data, int a)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            int val = data[0] + a;
                        }
                    }
                }
            }"
        ],
        [
            "Expression with property access: not flagged",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<int> list, int a)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            int val = list.Count + a;
                        }
                    }
                }
            }"
        ],
        [
            "Variable declared inside loop used in expression",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            int temp = i + 1;
                            int val = temp * a;
                        }
                    }
                }
            }"
        ],
        [
            "Variable decremented in loop: not invariant",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int y)
                    {
                        int x = 100;
                        for (int i = 0; i < 100; i++)
                        {
                            int val = x * y;
                            x--;
                        }
                    }
                }
            }"
        ],
        [
            "Variable passed by ref in loop body",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    private void Modify(ref int v) { v++; }
                    public void Test(int y)
                    {
                        int x = 10;
                        for (int i = 0; i < 100; i++)
                        {
                            int val = x * y;
                            Modify(ref x);
                        }
                    }
                }
            }"
        ],
        [
            "Variable passed by out in loop body",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    private void GetValue(out int v) { v = 0; }
                    public void Test(int y)
                    {
                        int x = 10;
                        for (int i = 0; i < 100; i++)
                        {
                            int val = x * y;
                            GetValue(out x);
                        }
                    }
                }
            }"
        ],
        [
            "Expression in for loop incrementor: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        for (int i = 0; i < 100; i += a * b)
                        {
                            int x = i;
                        }
                    }
                }
            }"
        ],
        [
            "Expression in for loop initializer: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        for (int i = a * b; i < 100; i++)
                        {
                            int x = i;
                        }
                    }
                }
            }"
        ],
        [
            "Object creation in expression: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int[] arr)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            var obj = new object();
                        }
                    }
                }
            }"
        ],
        [
            "Non-const field in expression: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    private int _field = 10;
                    public void Test(int a)
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            int val = _field + a;
                        }
                    }
                }
            }"
        ],
        [
            "Compound assignment makes variable non-invariant",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int y)
                    {
                        int x = 10;
                        for (int i = 0; i < 100; i++)
                        {
                            int val = x * y;
                            x += 1;
                        }
                    }
                }
            }"
        ],
        [
            "Single identifier is not a binary expression: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = a;
                        }
                    }
                }
            }"
        ],
        [
            "Binary expression not inside any loop: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        int result = a * b;
                    }
                }
            }"
        ],
        [
            "String concatenation with literal in loop: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string script)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            if (i == 0)
                                throw new System.Exception(""Error: "" + script);
                        }
                    }
                }
            }"
        ],
        [
            "String concatenation with two string variables in loop: not flagged",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string prefix, string suffix)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var msg = prefix + suffix;
                        }
                    }
                }
            }"
        ],
        [
            "Right shift expression: a >> b inside for loop is not flagged when using loop variable",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = i >> a;
                        }
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA022Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
