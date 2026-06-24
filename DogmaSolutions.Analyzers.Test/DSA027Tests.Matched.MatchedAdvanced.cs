using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA027Tests
{

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
