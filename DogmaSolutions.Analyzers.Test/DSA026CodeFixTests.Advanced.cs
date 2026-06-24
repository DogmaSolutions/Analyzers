using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA026CodeFixTests
{

    [TestMethod]
    public async Task ReplacesInLocalFunction()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        async Task Inner(CancellationToken innerCt)
                        {
                            await Task.Delay(1, {|#0:ct|});
                        }
                        await Inner(ct);
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
                    public async Task Process(CancellationToken ct)
                    {
                        async Task Inner(CancellationToken innerCt)
                        {
                            await Task.Delay(1, innerCt);
                        }
                        await Inner(ct);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesInAnonymousDelegate()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(CancellationToken ct)
                    {
                        Action<CancellationToken> work = delegate(CancellationToken innerCt)
                        {
                            {|#0:ct|}.ThrowIfCancellationRequested();
                        };
                        work(ct);
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
                    public void Process(CancellationToken ct)
                    {
                        Action<CancellationToken> work = delegate(CancellationToken innerCt)
                        {
                            innerCt.ThrowIfCancellationRequested();
                        };
                        work(ct);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesInSimpleLambdaExpression()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        Func<CancellationToken, Task> work = async innerCt =>
                        {
                            await Task.Delay(1, {|#0:ct|});
                        };
                        await work(ct);
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
                    public async Task Process(CancellationToken ct)
                    {
                        Func<CancellationToken, Task> work = async innerCt =>
                        {
                            await Task.Delay(1, innerCt);
                        };
                        await work(ct);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesInMiddleLevelPassThrough()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        Func<Task> middle = async () =>
                        {
                            Func<CancellationToken, Task> inner = async (innerCt) =>
                            {
                                await Task.Delay(1, {|#0:ct|});
                            };
                            await inner(ct);
                        };
                        await middle();
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
                    public async Task Process(CancellationToken ct)
                    {
                        Func<Task> middle = async () =>
                        {
                            Func<CancellationToken, Task> inner = async (innerCt) =>
                            {
                                await Task.Delay(1, innerCt);
                            };
                            await inner(ct);
                        };
                        await middle();
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesExpressionBodiedLocalFunction()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        Task Inner(CancellationToken innerCt) => Task.Delay(1, {|#0:ct|});
                        await Inner(ct);
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
                    public async Task Process(CancellationToken ct)
                    {
                        Task Inner(CancellationToken innerCt) => Task.Delay(1, innerCt);
                        await Inner(ct);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesInParallelForEachAsync()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        var items = new[] { 1, 2, 3 };
                        await Parallel.ForEachAsync(items, ct, async (item, innerCt) =>
                        {
                            await Task.Delay(item, {|#0:ct|});
                        });
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
                    public async Task Process(CancellationToken ct)
                    {
                        var items = new[] { 1, 2, 3 };
                        await Parallel.ForEachAsync(items, ct, async (item, innerCt) =>
                        {
                            await Task.Delay(item, innerCt);
                        });
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesTwoDistinctOuterTokens()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        var outerToken = ct;
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, {|#0:ct|});
                            {|#1:outerToken|}.ThrowIfCancellationRequested();
                        };
                        await work(ct);
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
                    public async Task Process(CancellationToken ct)
                    {
                        var outerToken = ct;
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, innerCt);
                            innerCt.ThrowIfCancellationRequested();
                        };
                        await work(ct);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(1).WithArguments("innerCt", "outerToken"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNamedArgumentPreservesLabelReplacesValue()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(millisecondsDelay: 1, cancellationToken: {|#0:ct|});
                        };
                        await work(ct);
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
                    public async Task Process(CancellationToken ct)
                    {
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(millisecondsDelay: 1, cancellationToken: innerCt);
                        };
                        await work(ct);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA026Analyzer, DSA026CodeFixProvider>
                .Diagnostic(DSA026Analyzer.DiagnosticId).WithLocation(0).WithArguments("innerCt", "ct"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
