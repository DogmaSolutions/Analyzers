using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA026Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Lambda captures outer method parameter — passed as argument",
            @"
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
                            await Task.Delay(1, {|#0:ct|});
                        };
                        await work(ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Lambda captures outer — ThrowIfCancellationRequested",
            @"
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
                            {|#0:ct|}.ThrowIfCancellationRequested();
                            await Task.Delay(1, innerCt);
                        };
                        await work(ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Lambda captures outer — IsCancellationRequested",
            @"
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
                            if ({|#0:ct|}.IsCancellationRequested) return;
                            await Task.Delay(1, innerCt);
                        };
                        await work(ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Lambda captures outer — Register",
            @"
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
                            {|#0:ct|}.Register(() => { });
                            await Task.CompletedTask;
                        };
                        await work(ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Lambda captures outer — WaitHandle",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(CancellationToken ct)
                    {
                        Action<CancellationToken> work = (innerCt) =>
                        {
                            {|#0:ct|}.WaitHandle.WaitOne();
                        };
                        work(ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Local function uses outer CancellationToken parameter",
            @"
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
            }",
            "innerCt",
            "ct"
        ],
        [
            "Anonymous delegate uses outer CancellationToken parameter",
            @"
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
            }",
            "innerCt",
            "ct"
        ],
        [
            "Var-assigned lambda captures outer",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        var work = async (string item, CancellationToken innerCt) =>
                        {
                            await Task.Delay(1, {|#0:ct|});
                        };
                        await work(""x"", ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Action<string, CancellationToken> lambda captures outer",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(CancellationToken ct)
                    {
                        Action<string, CancellationToken> work = (item, innerCt) =>
                        {
                            {|#0:ct|}.ThrowIfCancellationRequested();
                        };
                        work(""x"", ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Func<string, CancellationToken, Task> lambda captures outer",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process(CancellationToken ct)
                    {
                        Func<string, CancellationToken, Task> work = async (item, innerCt) =>
                        {
                            await Task.Delay(1, {|#0:ct|});
                        };
                        await work(""x"", ct);
                    }
                }
            }",
            "innerCt",
            "ct"
        ],
        [
            "Outer local variable of type CancellationToken captured",
            @"
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
                            await Task.Delay(1, {|#0:outerToken|});
                        };
                        await work(ct);
                    }
                }
            }",
            "innerCt",
            "outerToken"
        ],
        [
            "SimpleLambdaExpression — single CancellationToken parameter",
            @"
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
            }",
            "innerCt",
            "ct"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string nearestName,
        string outerName
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(nearestName, outerName));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedThreeLevelsInnermostUsesOutermost()
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
                        Func<CancellationToken, Task> outer = async (ct2) =>
                        {
                            Func<CancellationToken, Task> inner = async (ct3) =>
                            {
                                await Task.Delay(1, {|#0:ct|});
                            };
                            await inner(ct2);
                        };
                        await outer(ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("ct3", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedThreeLevelsInnermostUsesMiddle()
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
                        Func<CancellationToken, Task> outer = async (ct2) =>
                        {
                            Func<CancellationToken, Task> inner = async (ct3) =>
                            {
                                await Task.Delay(1, {|#0:ct2|});
                            };
                            await inner(ct2);
                        };
                        await outer(ct);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA026Analyzer>.Diagnostic(DSA026Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("ct3", "ct2"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedMultipleUsagesInSameLambda()
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
                            {|#0:ct|}.ThrowIfCancellationRequested();
                            await Task.Delay(1, {|#1:ct|});
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
                .WithLocation(1).WithArguments("innerCt", "ct"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MatchedOuterCancellationTokenSourceToken()
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
                        var cts = new CancellationTokenSource();
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
}
