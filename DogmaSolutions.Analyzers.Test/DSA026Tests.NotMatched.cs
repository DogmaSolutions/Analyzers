using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA026Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "No inner CancellationToken parameter — outer is the only one",
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
                        Func<Task> work = async () =>
                        {
                            await Task.Delay(1, ct);
                        };
                        await work();
                    }
                }
            }"
        ],
        [
            "Correct usage — inner parameter used",
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
                            await Task.Delay(1, innerCt);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "CreateLinkedTokenSource — intentional linking",
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
                            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, innerCt);
                            await Task.Delay(1, linked.Token);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "CancellationToken.None — deliberate opt-out",
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
                            await Task.Delay(1, CancellationToken.None);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "default(CancellationToken)",
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
                            await Task.Delay(1, default(CancellationToken));
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "default literal in CancellationToken context",
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
                            await Task.Delay(1, default);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "Field CancellationToken — ambiguous intent",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    private CancellationToken _appShutdown;
                    public async Task Process()
                    {
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, _appShutdown);
                        };
                        await work(CancellationToken.None);
                    }
                }
            }"
        ],
        [
            "Property CancellationToken — ambiguous intent",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public CancellationToken AppToken { get; set; }
                    public async Task Process()
                    {
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, AppToken);
                        };
                        await work(CancellationToken.None);
                    }
                }
            }"
        ],
        [
            "Task constructor CancellationToken — not a lambda parameter",
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
                        var task = new Task(() =>
                        {
                            ct.ThrowIfCancellationRequested();
                        }, ct);
                    }
                }
            }"
        ],
        [
            "Locally created CancellationTokenSource.Token inside lambda",
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
                            using var localCts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                            await Task.Delay(1, localCts.Token);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "Local CancellationToken variable inside lambda — not from outer scope",
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
                            var localToken = innerCt;
                            await Task.Delay(1, localToken);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "No CancellationToken in any scope",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task Process()
                    {
                        Func<Task> work = async () =>
                        {
                            await Task.Delay(1);
                        };
                        await work();
                    }
                }
            }"
        ],
        [
            "Shadowed parameter name — inner ct shadows outer ct",
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
                        Func<CancellationToken, Task> work = async (ct) =>
                        {
                            await Task.Delay(1, ct);
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "Nested lambda without CancellationToken passes through correctly",
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
                            Func<Task> nested = async () =>
                            {
                                await Task.Delay(1, innerCt);
                            };
                            await nested();
                        };
                        await work(ct);
                    }
                }
            }"
        ],
        [
            "Correct usage in local function",
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
                            await Task.Delay(1, innerCt);
                        }
                        await Inner(ct);
                    }
                }
            }"
        ],
        [
            "Correct usage in Parallel.ForEachAsync",
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
                        var items = new[] { 1, 2, 3 };
                        await Parallel.ForEachAsync(items, ct, async (item, innerCt) =>
                        {
                            await Task.Delay(item, innerCt);
                        });
                    }
                }
            }"
        ],
        [
            "Outer token used outside lambda — not inside any scope",
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
                        ct.ThrowIfCancellationRequested();
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, innerCt);
                        };
                        await work(ct);
                        await Task.Delay(1, ct);
                    }
                }
            }"
        ],
        [
            "Anonymous delegate without parameter list — no CT to compare",
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
                        Action work = delegate
                        {
                            ct.ThrowIfCancellationRequested();
                        };
                        work();
                    }
                }
            }"
        ],
        [
            "Token in LINQ lambda without CancellationToken parameter",
            @"
            using System;
            using System.Linq;
            using System.Threading;
            namespace TestApp
            {
                public class MyService
                {
                    public int[] Process(int[] items, CancellationToken ct)
                    {
                        return items.Select(x =>
                        {
                            ct.ThrowIfCancellationRequested();
                            return x * 2;
                        }).ToArray();
                    }
                }
            }"
        ],
        [
            "Field CancellationTokenSource.Token inside lambda — not local or parameter",
            @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly CancellationTokenSource _cts = new();
                    public async Task Process()
                    {
                        Func<CancellationToken, Task> work = async (innerCt) =>
                        {
                            await Task.Delay(1, _cts.Token);
                        };
                        await work(_cts.Token);
                    }
                }
            }"
        ],
        [
            "Static local function — both CT parameters are in own scope",
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
                        static async Task Inner(CancellationToken innerCt, CancellationToken extra)
                        {
                            await Task.Delay(1, extra);
                        }
                        await Inner(ct, ct);
                    }
                }
            }"
        ],
        [
            "Named argument label 'cancellationToken:' is not a usage of the outer token",
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
                            await Task.Delay(millisecondsDelay: 1, cancellationToken: innerCt);
                        };
                        await work(ct);
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
        var test = new CSharpAnalyzerVerifier<DSA026Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
