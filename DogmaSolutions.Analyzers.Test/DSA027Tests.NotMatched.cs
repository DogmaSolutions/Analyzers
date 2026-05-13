using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA027Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Concatenation outside any loop",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build()
                    {
                        string result = """";
                        result += ""hello"";
                        result += "" world"";
                        return result;
                    }
                }
            }"
        ],
        [
            "Non-string compound assignment (int += int)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public int Sum(int[] numbers)
                    {
                        int total = 0;
                        for (int i = 0; i < numbers.Length; i++)
                        {
                            total += numbers[i];
                        }
                        return total;
                    }
                }
            }"
        ],
        [
            "Variable declared inside loop body (fresh each iteration)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Build(string[] items)
                    {
                        for (int i = 0; i < items.Length; i++)
                        {
                            string line = """";
                            line += items[i];
                            System.Console.WriteLine(line);
                        }
                    }
                }
            }"
        ],
        [
            "StringBuilder already used correctly",
            @"
            using System;
            using System.Text;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in items)
                        {
                            sb.Append(item);
                        }
                        return sb.ToString();
                    }
                }
            }"
        ],
        [
            "String.Join used instead of loop",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        return string.Join("","", items);
                    }
                }
            }"
        ],
        [
            "Simple assignment without self-reference",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Process(string[] items)
                    {
                        string last = """";
                        foreach (var item in items)
                        {
                            last = item;
                        }
                        System.Console.WriteLine(last);
                    }
                }
            }"
        ],
        [
            "Field concatenation in loop (not ILocalSymbol or IParameterSymbol)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    private string _result = """";
                    public void Build(string[] items)
                    {
                        foreach (var item in items)
                        {
                            _result += item;
                        }
                    }
                }
            }"
        ],
        [
            "Property concatenation in loop (not ILocalSymbol or IParameterSymbol)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Result { get; set; } = """";
                    public void Build(string[] items)
                    {
                        foreach (var item in items)
                        {
                            Result += item;
                        }
                    }
                }
            }"
        ],
        [
            "Non-string self-reference assignment (double = double + expr)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public double Sum(double[] values)
                    {
                        double total = 0;
                        for (int i = 0; i < values.Length; i++)
                        {
                            total = total + values[i];
                        }
                        return total;
                    }
                }
            }"
        ],
        [
            "String concatenation with string.Concat (no operator)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        foreach (var item in items)
                        {
                            result = string.Concat(result, item);
                        }
                        return result;
                    }
                }
            }"
        ],
        [
            "Non-string compound assignment (long += long)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public long Sum(long[] values)
                    {
                        long total = 0;
                        foreach (var v in values)
                        {
                            total += v;
                        }
                        return total;
                    }
                }
            }"
        ],
        [
            "Simple assignment where right side is not an add expression",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Process(string[] items)
                    {
                        string result = """";
                        foreach (var item in items)
                        {
                            result = item.ToUpper();
                        }
                    }
                }
            }"
        ],
        [
            "Multiplication compound assignment (not string)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public int Factorial(int n)
                    {
                        int result = 1;
                        for (int i = 1; i <= n; i++)
                        {
                            result *= i;
                        }
                        return result;
                    }
                }
            }"
        ],
        [
            "LINQ Aggregate instead of loop concatenation",
            @"
            using System;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        return items.Aggregate("""", (acc, item) => acc + item);
                    }
                }
            }"
        ],
        [
            "Conditional concatenation with break (single append only)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string FindFirst(string[] items, string prefix)
                    {
                        string result = """";
                        foreach (var item in items)
                        {
                            if (item.StartsWith(prefix))
                            {
                                result = item;
                                break;
                            }
                        }
                        return result;
                    }
                }
            }"
        ],
        [
            "Non-string compound assignment (decimal += decimal)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public decimal Sum(decimal[] values)
                    {
                        decimal total = 0;
                        foreach (var v in values)
                        {
                            total += v;
                        }
                        return total;
                    }
                }
            }"
        ],
        [
            "Interpolated self-reference (not a BinaryExpressionSyntax)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Build(string[] items)
                    {
                        string result = """";
                        foreach (var item in items)
                        {
                            result = $""{result}{item}"";
                        }
                    }
                }
            }"
        ],
        [
            "Identity assignment (no addition)",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Build(string[] items)
                    {
                        string result = """";
                        foreach (var item in items)
                        {
                            result = result;
                        }
                    }
                }
            }"
        ],
        [
            "Field self-reference with add expression",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    private string _result = """";
                    public void Build(string[] items)
                    {
                        foreach (var item in items)
                        {
                            _result = _result + item;
                        }
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatchedNoLoopAtAll()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string a, string b, string c)
                    {
                        string result = """";
                        result += a;
                        result += b;
                        result += c;
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatchedStaticFieldConcatenation()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    private static string _log = """";
                    public void Append(string[] items)
                    {
                        foreach (var item in items)
                        {
                            _log += item;
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatchedSelfReferenceOnRightSideOnly()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Process(string[] items)
                    {
                        string a = """";
                        string b = """";
                        foreach (var item in items)
                        {
                            a = b + item;
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatchedConcatenationInsideLambdaWithinLoop()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Build(string[] items)
                    {
                        string result = """";
                        foreach (var item in items)
                        {
                            Action a = () =>
                            {
                                result += item;
                            };
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatchedAssignmentToOtherVariableWithAddExpression()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Process(string[] items, string prefix)
                    {
                        string output = """";
                        foreach (var item in items)
                        {
                            output = prefix + item;
                        }
                        System.Console.WriteLine(output);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }
}
