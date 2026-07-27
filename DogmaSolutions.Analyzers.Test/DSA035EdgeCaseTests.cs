using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Edge-case tests for lambda/delegate/async/parallel/yield/LINQ patterns.
/// Each test expects ZERO diagnostics — if the analyzer fires, the test fails
/// and prints what was flagged.
/// </summary>
[TestClass]
public class DSA035EdgeCaseTests
{
    // ── FALSE POSITIVE CHECKS (should NOT flag) ────────────────────

    [TestMethod]
    public async Task LinqSelect_LambdaParamReceiver_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Linq;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<List<object>> batches)
                    {
                        foreach (var batch in batches)
                        {
                            var types = batch.Select(x => x.GetType()).ToList();
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParallelForEach_LambdaParamReceiver_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<string> targets)
                    {
                        foreach (var target in targets)
                        {
                            Parallel.ForEach(new List<object>(), item =>
                            {
                                var t = item.GetType();
                            });
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParallelInvoke_LambdaParamReceiver_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;
            namespace TestApp
            {
                public interface IWorker { void Work(); }
                public class MyClass
                {
                    public void Test(List<string> items, List<IWorker> workers)
                    {
                        foreach (var item in items)
                        {
                            Parallel.ForEach(workers, worker =>
                            {
                                Console.WriteLine(worker.GetType().Name);
                                worker.Work();
                            });
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AsyncLambdaParam_InsideOuterLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Threading;
            using System.Threading.Tasks;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public async Task Test(List<string> targets, List<object> processors)
                    {
                        foreach (var target in targets)
                        {
                            await Parallel.ForEachAsync(processors, CancellationToken.None,
                                async (processor, ct) =>
                                {
                                    await Task.Delay(1, ct);
                                    var t = processor.GetType();
                                });
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NestedLambdas_InnerParamReceiver_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<string> targets)
                    {
                        foreach (var target in targets)
                        {
                            var list = new List<object>();
                            list.ForEach(a =>
                            {
                                var inner = new List<object>();
                                inner.ForEach(b =>
                                {
                                    var t = b.GetType();
                                });
                            });
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LinqQuerySyntax_RangeVariableReceiver_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Linq;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<List<object>> batches)
                    {
                        foreach (var batch in batches)
                        {
                            var q = from x in batch
                                    where x.GetType() == typeof(string)
                                    select x;
                            var r = q.ToList();
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ActionDelegateParam_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<string> items)
                    {
                        foreach (var item in items)
                        {
                            Action<object> action = delegate(object obj)
                            {
                                var t = obj.GetType();
                            };
                            action(item);
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalFunctionParam_InsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<string> items)
                    {
                        foreach (var item in items)
                        {
                            void Process(object obj)
                            {
                                var t = obj.GetType();
                            }
                            Process(item);
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CapturedVariableChangedInsideLoop_NotFlagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private object GetNext(int i) => new object();
                    public void Test()
                    {
                        object captured = null;
                        for (int i = 0; i < 10; i++)
                        {
                            captured = GetNext(i);
                            Action a = () =>
                            {
                                var t = captured.GetType();
                            };
                            a();
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── TRUE POSITIVE CHECKS (SHOULD flag) ─────────────────────────

    [TestMethod]
    public async Task InvariantReceiver_InsideLambda_InsideLoop_Flagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, List<string> items)
                    {
                        foreach (var item in items)
                        {
                            Action a = () =>
                            {
                                var t = {|#0:obj.GetType()|};
                            };
                            a();
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvariantReceiver_InsideTaskRun_InsideLoop_Flagged()
    {
        var source = @"
            using System;
            using System.Threading.Tasks;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public async Task Test(object obj, List<string> items)
                    {
                        foreach (var item in items)
                        {
                            await Task.Run(() =>
                            {
                                var t = {|#0:obj.GetType()|};
                            });
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvariantReceiver_InsideParallelInvoke_InsideLoop_Flagged()
    {
        var source = @"
            using System;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            Parallel.Invoke(() =>
                            {
                                var t = {|#0:obj.GetType()|};
                            });
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── NULL-CONDITIONAL (obj?.GetType()) ──────────────────────────

    [TestMethod]
    public async Task NullConditional_InvariantReceiver_Flagged()
    {
        var source = @"
            using System;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(object obj)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var t = obj?{|#0:.GetType()|};
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(".GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NullConditional_VariantReceiver_NotFlagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(List<object> items)
                    {
                        foreach (var item in items)
                        {
                            var t = item?.GetType();
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── typeof(T).GetProperties() (generic type parameter) ─────────

    [TestMethod]
    public async Task TypeofGenericParam_GetProperties_Flagged()
    {
        var source = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test<T>(int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            var props = {|#0:typeof(T).GetProperties()|};
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("typeof(T).GetProperties()"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Attribute.GetCustomAttributes() ────────────────────────────

    [TestMethod]
    public async Task AttributeGetCustomAttributes_InvariantArg_Flagged()
    {
        var source = @"
            using System;
            using System.Reflection;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(MemberInfo member)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            var attrs = {|#0:Attribute.GetCustomAttributes(member)|};
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("Attribute.GetCustomAttributes(member)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Yield return with invariant receiver ───────────────────────

    [TestMethod]
    public async Task YieldReturn_InvariantReceiver_Flagged()
    {
        var source = @"
            using System;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public IEnumerable<string> Test(object obj, int[] arr)
                    {
                        for (int i = 0; i < arr.Length; i++)
                        {
                            yield return {|#0:obj.GetType()|}.Name;
                        }
                    }
                }
            }";
        var test = new CSharpAnalyzerVerifier<DSA035Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA035Analyzer>.Diagnostic(DSA035Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("obj.GetType()"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
