using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA026Tests
{

    [TestMethod]
    public async Task MatchedLocalFunctionNestedInsideLambda()
    {
        var sourceCode = @"
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
                            async Task Nested(CancellationToken deepCt)
                            {
                                await Task.Delay(1, {|#0:ct|});
                            }
                            await Nested(innerCt);
                        };
                        await work(ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("deepCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedParallelForEachAsync()
    {
        var sourceCode = @"
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

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedExpressionBodiedLocalFunction()
    {
        var sourceCode = @"
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

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedLambdaInsideLocalFunction()
    {
        var sourceCode = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        async Task Outer(CancellationToken outerCt)
                        {
                            Func<CancellationToken, Task> inner = async (innerCt) =>
                            {
                                await Task.Delay(1, {|#0:ct|});
                            };
                            await inner(outerCt);
                        }
                        await Outer(ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedOuterTokenInTernaryExpression()
    {
        var sourceCode = @"
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
                            await Task.Delay(1, true ? {|#0:ct|} : CancellationToken.None);
                        };
                        await work(ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedOuterTokenPassedToConstructor()
    {
        var sourceCode = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class Processor
                {
                    public Processor(CancellationToken ct) { }
                }
                public class MyService
                {
                    public void Process(CancellationToken ct)
                    {
                        Action<CancellationToken> work = (innerCt) =>
                        {
                            var p = new Processor({|#0:ct|});
                        };
                        work(ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedOuterTokenPassedAsNamedArgument()
    {
        var sourceCode = @"
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

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedMultipleCtParametersInInnerScope()
    {
        var sourceCode = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        Func<CancellationToken, CancellationToken, Task> work = async (ct1, ct2) =>
                        {
                            await Task.Delay(1, {|#0:ct|});
                        };
                        await work(ct, ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("ct1", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedCancellationTokenSourceAsMethodParameter()
    {
        var sourceCode = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationTokenSource cts)
                    {
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, {|#0:cts.Token|});
                        };
                        await work(cts.Token);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "cts.Token"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedTwoDistinctOuterTokensInSameLambda()
    {
        var sourceCode = @"
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

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("innerCt", "outerToken"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedMiddleLevelLambdaWithoutCtParamDoesNotBlock()
    {
        var sourceCode = @"
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

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
