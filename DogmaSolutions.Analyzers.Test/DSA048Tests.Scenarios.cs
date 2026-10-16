using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA048Tests
{
    private const string Ready = "private bool _ready;\n";
    private const string Wait = "public void Wait() { [[while (!_ready)]] { Thread.Sleep(50); } }\n";

    // title, types declared before the class, class header, members ([[...]] marks the expected loop header, in order), settings (key=value;...), expected ("Scenario~state~wait;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the messages: one per scenario ------------------------------------------------------------------------
        ["No way to give up", "", Header, Ready + Wait, "", E("NoTimeout", "_ready")],
        ["Timeout with the clock", "", Header, Ready + "public void Wait() { var start = DateTime.UtcNow; [[while (!_ready)]] { if ((DateTime.UtcNow - start).TotalSeconds > 5) break; Thread.Sleep(50); } }", "", E("WithTimeout", "_ready")],
        ["Timeout with a stopwatch", "using System.Diagnostics;", Header, Ready + "public void Wait() { var sw = Stopwatch.StartNew(); [[while (!_ready && sw.ElapsedMilliseconds < 5000)]] { Thread.Sleep(50); } }", "", E("WithTimeout", "_ready")],
        ["Timeout with the tick count", "", Header, Ready + "public void Wait() { var start = Environment.TickCount; [[while (!_ready && Environment.TickCount - start < 5000)]] { Thread.Sleep(50); } }", "", E("WithTimeout", "_ready")],
        ["Cancellation token", "", Header, Ready + "public void Wait(CancellationToken token) { [[while (!_ready && !token.IsCancellationRequested)]] { Thread.Sleep(50); } }", "", E("WithTimeout", "_ready")],
        ["Cancellation token checked in the body", "", Header, Ready + "public void Wait(CancellationToken token) { [[while (!_ready)]] { token.ThrowIfCancellationRequested(); Thread.Sleep(50); } }", "", E("WithTimeout", "_ready")],
        ["Variable called timeout", "", Header, Ready + "public void Wait(int timeoutMs) { [[while (!_ready && timeoutMs > 0)]] { Thread.Sleep(50); timeoutMs -= 50; } }", "", E("WithTimeout", "_ready")],
        ["Variable called deadline", "", Header, Ready + "public void Wait(DateTime deadline) { [[while (!_ready && DateTime.Now < deadline)]] { Thread.Sleep(50); } }", "", E("WithTimeout", "_ready")],
        ["Task.Delay", "using System.Threading.Tasks;", Header, Ready + "public async Task Wait() { [[while (!_ready)]] { await Task.Delay(50); } }", "", E("NoTimeout", "_ready", "Task.Delay")],
        ["Task.Delay waited synchronously", "using System.Threading.Tasks;", Header, Ready + "public void Wait() { [[while (!_ready)]] { Task.Delay(50).Wait(); } }", "", E("NoTimeout", "_ready", "Task.Delay")],

        // ---- the state the loop waits for ---------------------------------------------------------------------------------
        ["Field read through this", "", Header, Ready + "public void Wait() { [[while (!this._ready)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "_ready")],
        ["Static field", "", Header, "private static bool _ready;\npublic static void Wait() { [[while (!_ready)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "_ready")],
        ["Property", "", Header, "public bool IsReady { get; set; }\npublic void Wait() { [[while (!IsReady)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "IsReady")],
        ["Method of the class", "", Header, Ready + "private bool Check() => _ready;\npublic void Wait() { [[while (!Check())]] { Thread.Sleep(50); } }", "", E("NoTimeout", "Check")],
        ["Member of a field", "", Header, "private readonly Worker _other = null;\npublic bool IsReady { get; set; }\npublic void Wait() { [[while (!_other.IsReady)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "_other")],
        ["Member of a field, compared to a value", "", Header, "private int _count;\npublic void Wait() { [[while (_count == 0)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "_count")],
        ["Member of a base class", "public class Base { protected volatile bool Ready; }", "public class Worker : Base", "public void Wait() { [[while (!Ready)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "Ready")],
        ["Condition made of several members", "", Header, "private bool _a, _b;\npublic void Wait() { [[while (!_a || !_b)]] { Thread.Sleep(50); } }", "", E("NoTimeout", "_a")],
        ["Do loop", "", Header, Ready + "public void Wait() { do { Thread.Sleep(50); } [[while (!_ready)]]; }", "", E("NoTimeout", "_ready")],
        ["Sleep among other statements", "", Header, Ready + "public void Wait() { [[while (!_ready)]] { Console.WriteLine(\"waiting\"); Thread.Sleep(50); Console.WriteLine(\"again\"); } }", "", E("NoTimeout", "_ready")],
        ["Sleep in an if", "", Header, Ready + "public void Wait() { [[while (!_ready)]] { if (DateTime.Now.Ticks > 0) { Thread.Sleep(50); } } }", "", E("WithTimeout", "_ready")],
        ["Sleep without a statement block", "", Header, Ready + "public void Wait() { [[while (!_ready)]] Thread.Sleep(50); }", "", E("NoTimeout", "_ready")],
        ["Loop in a property accessor", "", Header, Ready + "public int N { get { [[while (!_ready)]] { Thread.Sleep(50); } return 1; } }", "", E("NoTimeout", "_ready")],
        ["Loop in a lambda", "", Header, Ready + "public Action Wait() => () => { [[while (!_ready)]] { Thread.Sleep(50); } };", "", E("NoTimeout", "_ready")],
        ["Two loops", "", Header, Ready + "public void Wait() { [[while (!_ready)]] { Thread.Sleep(50); } [[while (!_ready)]] { Thread.Sleep(10); } }", "", E("NoTimeout", "_ready") + ";" + E("NoTimeout", "_ready")],
        ["A nested loop is its own loop", "", Header, Ready + "private bool _other;\npublic void Wait() { while (_other) { [[while (!_ready)]] { Thread.Sleep(50); } _other = false; } }", "", E("NoTimeout", "_ready")],

        // ---- what is not waiting for the class ---------------------------------------------------------------------------------
        ["Local variable", "", Header, "public void Wait() { var ready = false; while (!ready) { Thread.Sleep(50); ready = Check(); } }\nprivate static bool Check() => true;", "", ""],
        ["Parameter", "", Header, "public void Wait(Func<bool> ready) { while (!ready()) { Thread.Sleep(50); } }", "", ""],
        ["Member of a parameter", "", Header, "public void Wait(Worker other) { while (!other.IsReady) { Thread.Sleep(50); } }\npublic bool IsReady { get; set; }", "", ""],
        ["Member of a local", "", Header, "public bool IsReady { get; set; }\npublic void Wait() { var other = new Worker(); while (!other.IsReady) { Thread.Sleep(50); } }", "", ""],
        ["Static member of another type", "", Header, "public void Wait() { while (!System.IO.File.Exists(\"x\")) { Thread.Sleep(50); } }", "", ""],
        ["Constant", "", Header, "private const bool Never = false;\npublic void Wait() { while (Never) { Thread.Sleep(50); } }", "", ""],
        ["Literal true", "", Header, "public void Run() { while (true) { Thread.Sleep(50); } }", "", ""],
        ["Readonly field alone", "", Header, "private readonly bool _fixed = true;\npublic void Run() { while (_fixed) { Thread.Sleep(50); } }", "", ""],
        ["Static member of another type, used by its simple name", "using static System.Environment;", Header, "public void Run() { while (HasShutdownStarted) { Thread.Sleep(50); } }", "", ""],
        ["No sleep", "", Header, Ready + "public void Wait() { while (!_ready) { Thread.Yield(); } }", "", ""],
        ["Sleep in a lambda of the loop", "", Header, Ready + "public void Wait() { while (!_ready) { Action a = () => Thread.Sleep(50); a(); } }", "", ""],
        ["Sleep in a local function of the loop", "", Header, Ready + "public void Wait() { while (!_ready) { void Pause() { Thread.Sleep(50); } Pause(); } }", "", ""],
        ["Another method called Sleep", "", Header, Ready + "private void Sleep(int ms) { }\npublic void Wait() { while (!_ready) { Sleep(50); } }", "", ""],
        ["A method called Delay on another type", "", Header, Ready + "public void Wait(Worker w) { while (!_ready) { w.Delay(50); } }\npublic void Delay(int ms) { }", "", ""],
        ["A call that is not a name", "", Header, Ready + "private static Action Make() => () => { };\npublic void Wait() { [[while (!_ready)]] { Make()(); Thread.Sleep(1); } }", "", E("NoTimeout", "_ready")],
        ["The loop makes progress itself", "", Header, "private int _attempt;\npublic void Retry() { while (_attempt < 3) { _attempt++; Thread.Sleep(50); } }", "", ""],
        ["The loop assigns what it waits for", "", Header, Ready + "public void Wait() { while (!_ready) { _ready = Check(); Thread.Sleep(50); } }\nprivate static bool Check() => true;", "", ""],
        ["The loop assigns a member of what it waits for", "", Header, "private int _count;\npublic void Wait() { while (_count < 3) { _count += 1; Thread.Sleep(50); } }", "", ""],
        ["The loop passes what it waits for by ref", "", Header, "private int _count;\npublic void Wait() { while (_count < 3) { Interlocked.Increment(ref _count); Thread.Sleep(50); } }", "", ""],
        ["The loop assigns what it waits for through this", "", Header, Ready + "public void Wait() { while (!this._ready) { this._ready = Check(); Thread.Sleep(50); } }\nprivate static bool Check() => true;", "", ""],
        ["The loop decrements what it waits for", "", Header, "private int _count = 3;\npublic void Wait() { while (_count > 0) { --_count; Thread.Sleep(50); } }", "", ""],
        ["The loop writes something else", "", Header, Ready + "private int _tries;\npublic void Wait() { [[while (!_ready)]] { _tries++; Thread.Sleep(50); } }", "", E("NoTimeout", "_ready")],

        // ---- the options ---------------------------------------------------------------------------------------------------------
        ["Loops with a timeout refused", "", Header, Ready + "public void Wait(CancellationToken t) { while (!_ready && !t.IsCancellationRequested) { Thread.Sleep(50); } }", "report_loops_with_timeout=false", ""],
        ["Loops with a timeout refused, others still reported", "", Header, Ready + Wait, "report_loops_with_timeout=false", E("NoTimeout", "_ready")],
        ["Task.Delay refused", "using System.Threading.Tasks;", Header, Ready + "public async Task Wait() { while (!_ready) { await Task.Delay(50); } }", "include_task_delay=false", ""],
        ["Task.Delay refused, Thread.Sleep still reported", "", Header, Ready + Wait, "include_task_delay=false", E("NoTimeout", "_ready")],
        ["Task.Delay refused, a loop with both", "using System.Threading.Tasks;", Header, Ready + "public async Task Wait() { [[while (!_ready)]] { await Task.Delay(50); Thread.Sleep(1); } }", "include_task_delay=false", E("NoTimeout", "_ready")],
        ["Do loops refused", "", Header, Ready + "public void Wait() { do { Thread.Sleep(50); } while (!_ready); }", "include_do_loops=false", ""],
        ["Do loops refused, while loops still reported", "", Header, Ready + Wait, "include_do_loops=false", E("NoTimeout", "_ready")],
        ["Excluded type name", "", Header, Ready + "public void Wait() { while (!_ready) { Thread.Sleep(50); } }", "excluded_type_name_patterns=Work*", ""],
        ["Type name not excluded", "", Header, Ready + Wait, "excluded_type_name_patterns=Other*", E("NoTimeout", "_ready")],
        ["Excluded namespace", "", Header, Ready + "public void Wait() { while (!_ready) { Thread.Sleep(50); } }", "excluded_namespace_patterns=TestApp", ""],
        ["Namespace not excluded", "", Header, Ready + Wait, "excluded_namespace_patterns=Other.*", E("NoTimeout", "_ready")],
        ["Excluded base type", "public class BaseWorker { }", "public class Worker : BaseWorker", Ready + "public void Wait() { while (!_ready) { Thread.Sleep(50); } }", "excluded_base_types=TestApp.BaseWorker", ""],
        ["Base type not excluded", "public class BaseWorker { }", "public class Worker : BaseWorker", Ready + Wait, "excluded_base_types=Other.Base", E("NoTimeout", "_ready")],
        ["Excluded member name", "", Header, Ready + "public void Wait() { while (!_ready) { Thread.Sleep(50); } }", "excluded_member_name_patterns=Wa*", ""],
        ["Member name not excluded", "", Header, Ready + Wait, "excluded_member_name_patterns=Run*", E("NoTimeout", "_ready")],
        ["Excluded file", "", Header, Ready + "public void Wait() { while (!_ready) { Thread.Sleep(50); } }", "excluded_file_patterns=*.cs", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenarios(string title, string pre, string header, string members, string settings, string expected)
    {
        await RunAsync(pre, header, members, settings, expected).ConfigureAwait(false);
    }
}
