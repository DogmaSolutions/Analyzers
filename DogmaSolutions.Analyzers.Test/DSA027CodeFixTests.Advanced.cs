using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA027CodeFixTests
{

    [TestMethod]
    public async Task FixInnerLoopConcatenation()
    {
        var source = @"
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

        var fixedSource = @"
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
                var resultBuilder = new System.Text.StringBuilder(result);
                foreach (var cell in row)
                {
                    resultBuilder.Append(cell);
                }
                result = resultBuilder.ToString();
            }
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("result"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDeclaredInsideOuterLoopConcatenatedInInner()
    {
        var source = @"
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

        var fixedSource = @"
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
                var lineBuilder = new System.Text.StringBuilder(line);
                for (int j = 0; j < data[i].Length; j++)
                {
                    lineBuilder.Append(data[i][j]);
                }
                line = lineBuilder.ToString();
                System.Console.WriteLine(line);
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("line"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixInterpolatedStringConcatenation()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public string Build(string[] items)
        {
            string result = """";
            var resultBuilder = new System.Text.StringBuilder(result);
            for (int i = 0; i < items.Length; i++)
            {
                resultBuilder.Append($""{items[i]}, "");
            }
            result = resultBuilder.ToString();
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("result"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixBuilderNameCollisionInsideLoop()
    {
        var source = @"
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
                var resultBuilder = item.Length;
                {|#0:result += item|};
            }
            return result;
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public string Build(string[] items)
        {
            string result = """";
            var resultBuilder1 = new System.Text.StringBuilder(result);
            foreach (var item in items)
            {
                var resultBuilder = item.Length;
                resultBuilder1.Append(item);
            }
            result = resultBuilder1.ToString();
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("result"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNonEmptyInitialValue()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public string Build(string[] items)
        {
            string result = ""Header: "";
            var resultBuilder = new System.Text.StringBuilder(result);
            foreach (var item in items)
            {
                resultBuilder.Append(item);
            }
            result = resultBuilder.ToString();
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("result"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixVarDeclaredVariable()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public string Build(string[] items)
        {
            var result = """";
            var resultBuilder = new System.Text.StringBuilder(result);
            foreach (var item in items)
            {
                resultBuilder.Append(item);
            }
            result = resultBuilder.ToString();
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("result"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixBracelessLoop()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public string Build(string[] items)
        {
            string result = """";
            var resultBuilder = new System.Text.StringBuilder(result);
            foreach (var item in items)
                resultBuilder.Append(item);
            result = resultBuilder.ToString();
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("result"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixTwoVariablesInSameLoop()
    {
        var source = @"
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

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Build(string[] items)
        {
            string names = """";
            string values = """";
            var namesBuilder = new System.Text.StringBuilder(names);
            var valuesBuilder = new System.Text.StringBuilder(values);
            foreach (var item in items)
            {
                namesBuilder.Append(item);
                valuesBuilder.Append(item.Length.ToString());
            }
            values = valuesBuilder.ToString();
            names = namesBuilder.ToString();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.NumberOfFixAllIterations = 2;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(0).WithArguments("names"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA027Analyzer, DSA027CodeFixProvider>
                .Diagnostic(DSA027Analyzer.DiagnosticId).WithLocation(1).WithArguments("values"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
