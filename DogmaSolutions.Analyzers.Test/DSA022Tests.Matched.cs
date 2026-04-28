using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA022Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Simple invariant multiplication: a * b inside for loop",
            @"
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
            }",
            "a * b"
        ],
        [
            "Nested loop: outer variables multiplied in inner loop",
            @"
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
            }",
            "y * width"
        ],
        [
            "While loop: invariant addition",
            @"
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
            }",
            "a + b"
        ],
        [
            "Do-while loop: invariant multiply",
            @"
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
            }",
            "a * b"
        ],
        [
            "Complex invariant: a * b + c where all are parameters",
            @"
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
            }",
            "a * b + c"
        ],
        [
            "Outer loop variable invariant in inner loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int outH, int outW, int[] output)
                    {
                        for (int py = 0; py < outH; py++)
                        {
                            for (int px = 0; px < outW; px++)
                            {
                                output[{|#0:py * outW|} + px] = 0;
                            }
                        }
                    }
                }
            }",
            "py * outW"
        ],
        [
            "Triple nested: oc * outH + py invariant in innermost px loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int outC, int outH, int outW, int[] output)
                    {
                        for (int oc = 0; oc < outC; oc++)
                        {
                            for (int py = 0; py < outH; py++)
                            {
                                for (int px = 0; px < outW; px++)
                                {
                                    output[{|#0:oc * outH + py|} + px] = 0;
                                }
                            }
                        }
                    }
                }
            }",
            "oc * outH + py"
        ],
        [
            "Subtraction: a - b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a - b|} + i;
                        }
                    }
                }
            }",
            "a - b"
        ],
        [
            "Division: a / b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a / b|} + i;
                        }
                    }
                }
            }",
            "a / b"
        ],
        [
            "Modulo: a % b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a % b|} + i;
                        }
                    }
                }
            }",
            "a % b"
        ],
        [
            "Left shift: a << b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a << b|};
                        }
                    }
                }
            }",
            "a << b"
        ],
        [
            "Right shift: a >> b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a >> b|};
                        }
                    }
                }
            }",
            "a >> b"
        ],
        [
            "Bitwise AND: a & b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a & b|};
                        }
                    }
                }
            }",
            "a & b"
        ],
        [
            "Bitwise OR: a | b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a | b|};
                        }
                    }
                }
            }",
            "a | b"
        ],
        [
            "Bitwise XOR: a ^ b inside for loop",
            @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            arr[i] = {|#0:a ^ b|};
                        }
                    }
                }
            }",
            "a ^ b"
        ],
        [
            "ForEach loop with outer-scope invariant expression",
            @"
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
            }",
            "a * b"
        ],
        [
            "Both operands are literals: 10 * 20 inside loop",
            @"
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
            }",
            "10 * 20"
        ],
        [
            "Const field used unqualified in expression",
            @"
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
            }",
            "Scale * a"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(string title, string sourceCode, string expectedExpression)
    {
        var test = new CSharpAnalyzerVerifier<DSA022Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA022Analyzer>.Diagnostic(DSA022Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(expectedExpression));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MultipleSeparateInvariantExpressions_BothFlagged()
    {
        var source = @"
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(int a, int b, int c, int d, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            int x = {|#0:a * b|};
                            int y = {|#1:c + d|};
                            arr[i] = x + y + i;
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA022Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA022Analyzer>.Diagnostic(DSA022Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("a * b"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA022Analyzer>.Diagnostic(DSA022Analyzer.DiagnosticId)
                .WithLocation(1)
                .WithArguments("c + d"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
