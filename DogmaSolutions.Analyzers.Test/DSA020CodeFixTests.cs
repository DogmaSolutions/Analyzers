using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA020CodeFixTests
{
    [TestMethod]
    public async Task FixesParenthesizedParam_ToDiscard()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = {|#0:async (ct) => await Task.FromResult(42)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = _ => Task.FromResult(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesSimpleParam_ToDiscard()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = {|#0:async ct => await Task.FromResult(42)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = _ => Task.FromResult(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesDiscardParam_KeepsDiscard()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = {|#0:async _ => await Task.FromResult(42)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = _ => Task.FromResult(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesNoParams_KeepsParentheses()
    {
        var source = @"
using System;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<Task<string>> func = {|#0:async () => await Task.FromResult(""hello"")|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<Task<string>> func = () => Task.FromResult(""hello"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesGenericFromResult()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = {|#0:async (ct) => await Task.FromResult<int>(42)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = _ => Task.FromResult<int>(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesBlockBody_ToExpressionBody()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = {|#0:async (ct) => { return await Task.FromResult(42); }|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = _ => Task.FromResult(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesBlockBodyNoParams_ToExpressionBody()
    {
        var source = @"
using System;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<Task<int>> func = {|#0:async () => { return await Task.FromResult(42); }|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<Task<int>> func = () => Task.FromResult(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task KeepsParamName_WhenUsedInFromResultArguments()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<bool>> func = {|#0:async ct => await Task.FromResult(ct.IsCancellationRequested)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<bool>> func = ct => Task.FromResult(ct.IsCancellationRequested);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesWithConfigureAwait()
    {
        var source = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = {|#0:async (ct) => await Task.FromResult(42).ConfigureAwait(false)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Threading;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public void Setup()
        {
            Func<CancellationToken, Task<int>> func = _ => Task.FromResult(42);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA020Analyzer, DSA020CodeFixProvider>
                .Diagnostic(DSA020Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
