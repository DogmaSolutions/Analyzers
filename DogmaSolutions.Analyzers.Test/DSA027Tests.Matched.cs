using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA027Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Compound assignment in for loop",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        for (int i = 0; i < items.Length; i++)
                        {
                            {|#0:result += items[i]|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Compound assignment in foreach loop",
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
                            {|#0:result += item|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Compound assignment in while loop",
            @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(Queue<string> q)
                    {
                        string result = """";
                        while (q.Count > 0)
                        {
                            {|#0:result += q.Dequeue()|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Compound assignment in do-while loop",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        int i = 0;
                        do
                        {
                            {|#0:result += items[i]|};
                            i++;
                        } while (i < items.Length);
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Simple assignment with self-reference: s = s + expr",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        for (int i = 0; i < items.Length; i++)
                        {
                            {|#0:result = result + items[i]|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Compound assignment with string literal",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(int count)
                    {
                        string result = """";
                        for (int i = 0; i < count; i++)
                        {
                            {|#0:result += ""line\n""|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Compound assignment with interpolated string",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        for (int i = 0; i < items.Length; i++)
                        {
                            {|#0:result += $""{items[i]}, ""|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Compound assignment with ToString call",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(int[] numbers)
                    {
                        string result = """";
                        foreach (var n in numbers)
                        {
                            {|#0:result += n.ToString()|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Variable initialized with non-empty value",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = ""Header: "";
                        foreach (var item in items)
                        {
                            {|#0:result += item|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Method parameter concatenated in loop",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string result, string[] items)
                    {
                        foreach (var item in items)
                        {
                            {|#0:result += item|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Self-reference with chained additions: s = s + a + b",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        for (int i = 0; i < items.Length; i++)
                        {
                            {|#0:result = result + items[i] + ""\n""|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "var-declared string variable",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        var result = """";
                        foreach (var item in items)
                        {
                            {|#0:result += item|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Concatenation inside if block within loop",
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
                            if (item != null)
                            {
                                {|#0:result += item|};
                            }
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Concatenation inside try block within loop",
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
                            try
                            {
                                {|#0:result += item|};
                            }
                            catch { }
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Concatenation inside switch/case within loop",
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
                            switch (item.Length)
                            {
                                case 1:
                                    {|#0:result += item|};
                                    break;
                            }
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Loop with no braces (single-statement body)",
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
                            {|#0:result += item|};
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Verbatim string concatenation",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(int count)
                    {
                        string result = """";
                        for (int i = 0; i < count; i++)
                        {
                            {|#0:result += @""line""|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Ternary expression in concatenation",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(int count)
                    {
                        string result = """";
                        for (int i = 0; i < count; i++)
                        {
                            {|#0:result += (i % 2 == 0 ? ""even"" : ""odd"")|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Null-coalescing expression in concatenation",
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
                            {|#0:result += (item ?? ""default"")|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Method call result in concatenation",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        for (int i = 0; i < items.Length; i++)
                        {
                            {|#0:result += items[i].ToUpper()|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Char literal concatenation",
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
                            {|#0:result += ','|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Variable initialized with string.Empty",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = string.Empty;
                        foreach (var item in items)
                        {
                            {|#0:result += item|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Self-reference in middle of chain: prefix + s + suffix",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(int count)
                    {
                        string result = """";
                        for (int i = 0; i < count; i++)
                        {
                            {|#0:result = ""["" + result + ""]""|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Simple assignment with self-reference in foreach",
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
                            {|#0:result = result + item|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Simple assignment with self-reference in while",
            @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(Queue<string> q)
                    {
                        string result = """";
                        while (q.Count > 0)
                        {
                            {|#0:result = result + q.Dequeue()|};
                        }
                        return result;
                    }
                }
            }",
            "result"
        ],
        [
            "Simple assignment with self-reference in do-while",
            @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[] items)
                    {
                        string result = """";
                        int i = 0;
                        do
                        {
                            {|#0:result = result + items[i]|};
                            i++;
                        } while (i < items.Length);
                        return result;
                    }
                }
            }",
            "result"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string variableName
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(variableName));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedInnerLoopConcatenation()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[][] matrix)
                    {
                        string result = """";
                        foreach (var row in matrix)
                        {
                            foreach (var cell in row)
                            {
                                {|#0:result += cell|};
                            }
                        }
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedTwoVariablesInSameLoop()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Build(string[] items)
                    {
                        string names = """";
                        string values = """";
                        foreach (var item in items)
                        {
                            {|#0:names += item|};
                            {|#1:values += item.Length.ToString()|};
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("names"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("values"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedMultipleConcatenationsInSameLoop()
    {
        var sourceCode = @"
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
                            {|#0:result += item|};
                            {|#1:result += ""\n""|};
                        }
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedVariableDeclaredOutsideOuterLoopConcatenatedInInner()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[][] data)
                    {
                        string result = """";
                        for (int i = 0; i < data.Length; i++)
                        {
                            for (int j = 0; j < data[i].Length; j++)
                            {
                                {|#0:result += data[i][j]|};
                            }
                        }
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedAwaitForeachLoop()
    {
        var sourceCode = @"
            using System;
            using System.Collections.Generic;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyClass
                {
                    public async Task<string> Build()
                    {
                        string result = """";
                        await foreach (var item in GetAsync())
                        {
                            {|#0:result += item|};
                        }
                        return result;
                    }

                    private async IAsyncEnumerable<string> GetAsync()
                    {
                        yield return ""a"";
                        await Task.CompletedTask;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedThreeLevelNesting()
    {
        var sourceCode = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(string[][][] cube)
                    {
                        string result = """";
                        for (int i = 0; i < cube.Length; i++)
                        {
                            foreach (var row in cube[i])
                            {
                                int j = 0;
                                while (j < row.Length)
                                {
                                    {|#0:result += row[j]|};
                                    j++;
                                }
                            }
                        }
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedConcatenationInIfElseBranches()
    {
        var sourceCode = @"
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
                            if (item.Length > 3)
                            {
                                {|#0:result += item|};
                            }
                            else
                            {
                                {|#1:result += ""_""|};
                            }
                        }
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedTupleDeconstructionForeach()
    {
        var sourceCode = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public string Build(List<(string Name, string Value)> pairs)
                    {
                        string result = """";
                        foreach (var (name, value) in pairs)
                        {
                            {|#0:result += name|};
                        }
                        return result;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("result"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedDeclaredInsideOuterLoopConcatenatedInInner()
    {
        var sourceCode = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Build(string[][] data)
                    {
                        for (int i = 0; i < data.Length; i++)
                        {
                            string line = """";
                            for (int j = 0; j < data[i].Length; j++)
                            {
                                {|#0:line += data[i][j]|};
                            }
                            System.Console.WriteLine(line);
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA027Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA027Analyzer>.Diagnostic(DSA027Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("line"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
