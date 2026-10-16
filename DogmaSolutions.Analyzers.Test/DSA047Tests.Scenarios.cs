using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA047Tests
{
    private const string Gate = "private readonly object _gate = new object();\n";
    private const string Swapped = "private object _gate = new object();\npublic void Replace(object o) { _gate = o; }\n";
    private const string Work = "public void Work() { [[lock (_gate)]] { } }\n";

    // title, types declared before the class, class header, members ([[...]] marks the expected lock, in order), settings (key=value;...), expected ("Scenario~lock~reason;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the messages: one per scenario ------------------------------------------------------------------------
        ["Field, swapped", "", Header, Swapped + Work, "", E("FieldSwapped", "_gate")],
        ["Field, exposed (public)", "", Header, "public readonly object Gate = new object();\npublic void Work() { [[lock (Gate)]] { } }", "", E("FieldExposed", "Gate")],
        ["Field, exposed (returned by a property)", "", Header, Gate + "public object Shared => _gate;\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Field, swapped and exposed", "", Header, "private object _gate = new object();\npublic object Shared { get { return _gate; } set { _gate = value; } }\n" + Work, "", E("FieldSwappedAndExposed", "_gate", "R:Shared")],
        ["Field, swapped and exposed (public)", "", Header, "public object Gate = new object();\npublic void Reset() { Gate = new object(); }\npublic void Work() { [[lock (Gate)]] { } }", "", E("FieldSwappedAndExposed", "Gate")],
        ["Property, swapped", "", Header, "private object Gate { get; set; } = new object();\npublic void Work() { [[lock (Gate)]] { } }", "", E("PropertySwapped", "Gate", "P")],
        ["Property, exposed", "", Header, "public object Gate { get; } = new object();\npublic void Work() { [[lock (Gate)]] { } }", "", E("PropertyExposed", "Gate", "P")],
        ["Property, swapped and exposed", "", Header, "public object Gate { get; set; } = new object();\npublic void Work() { [[lock (Gate)]] { } }", "", E("PropertySwappedAndExposed", "Gate", "P")],

        // ---- how the lock is written ---------------------------------------------------------------------------------------
        ["Through this", "", Header, Swapped + "public void Work() { [[lock (this._gate)]] { } }", "", E("FieldSwapped", "_gate")],
        ["Parenthesized", "", Header, Swapped + "public void Work() { [[lock ((_gate))]] { } }", "", E("FieldSwapped", "_gate")],
        ["Static field", "", Header, "private static object _gate = new object();\npublic static void Replace(object o) { _gate = o; }\npublic static void Work() { [[lock (_gate)]] { } }", "", E("FieldSwapped", "_gate")],
        ["Several locks", "", Header, Swapped + "public void Work() { [[lock (_gate)]] { } [[lock (_gate)]] { } }", "", E("FieldSwapped", "_gate") + ";" + E("FieldSwapped", "_gate")],
        ["Lock in a property accessor", "", Header, Swapped + "public int N { get { [[lock (_gate)]] { return 1; } } }", "", E("FieldSwapped", "_gate")],
        ["Lock in a lambda", "", Header, Swapped + "public Action Work() => () => { [[lock (_gate)]] { } };", "", E("FieldSwapped", "_gate")],

        // ---- the ways a field is reassigned ------------------------------------------------------------------------------------
        ["Reassigned by a method", "", Header, "private object _gate = new object();\npublic void Reset() { _gate = new object(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned through this", "", Header, "private object _gate = new object();\npublic void Reset() { this._gate = new object(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a setter", "", Header, "private object _gate = new object();\nprivate object Gate { set { _gate = value; } }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by Interlocked", "", Header, "private object _gate = new object();\npublic void Reset() { System.Threading.Interlocked.Exchange(ref _gate, new object()); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by an out argument", "", Header, "private object _gate = new object();\nprivate static void Make(out object o) { o = new object(); }\npublic void Reset() { Make(out _gate); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a tuple", "", Header, "private object _gate = new object();\npublic void Reset() { (_gate, _) = (new object(), 1); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a parenthesized target", "", Header, "private object _gate = new object();\npublic void Reset() { (_gate) = new object(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by ref local", "", Header, "private object _gate = new object();\npublic void Reset() { ref object r = ref _gate; r = new object(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned through another instance", "", Header, "private object _gate = new object();\npublic void Reset(Worker w) { w._gate = new object(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by an object initializer", "", Header, "private object _gate = new object();\npublic static Worker Make() => new Worker { _gate = new object() };\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a nested type", "", Header, "private object _gate = new object();\nprivate class Inner { public void Reset(Worker w) { w._gate = new object(); } }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a lambda of the constructor", "", Header, "private object _gate;\npublic Worker() { _gate = new object(); Action a = () => _gate = new object(); a(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a local function of the constructor", "", Header, "private object _gate;\npublic Worker() { void Make() { _gate = new object(); } Make(); }\n" + Work, "", E("FieldSwapped", "_gate")],
        ["Reassigned by a static field in an instance constructor", "", Header, "private static object _gate = new object();\npublic Worker() { _gate = new object(); }\npublic void Work() { [[lock (_gate)]] { } }", "", E("FieldSwapped", "_gate")],
        ["Reassigned by the increment of a value field is not a lock", "", Header, "private int _n;\npublic void Hit() { _n++; lock (_gate) { } }\n" + Gate, "", ""],

        // ---- the fields that are fine --------------------------------------------------------------------------------------------
        ["Private readonly", "", Header, Gate + "public void Work() { lock (_gate) { } }", "", ""],
        ["Assigned by the constructor", "", Header, "private object _gate;\npublic Worker() { _gate = new object(); }\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Assigned by the constructor through this", "", Header, "private object _gate;\npublic Worker() { this._gate = new object(); }\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Assigned by several constructors", "", Header, "private object _gate;\npublic Worker() { _gate = new object(); }\npublic Worker(int n) { _gate = new object(); }\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Assigned by a static constructor", "", Header, "private static object _gate;\nstatic Worker() { _gate = new object(); }\npublic static void Work() { lock (_gate) { } }", "", ""],
        ["Private field returned by a private member", "", Header, Gate + "private object Shared => _gate;\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Private field returned by a lambda", "", Header, Gate + "public Func<object> Shared() => () => _gate;\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Private field returned by a local function", "", Header, Gate + "public void Shared() { object Get() { return _gate; } Get(); }\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Private field only read", "", Header, Gate + "public bool Same(object o) => o == _gate;\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Private field only passed on", "", Header, Gate + "public void Pass() { Console.WriteLine(_gate); }\npublic void Work() { lock (_gate) { } }", "", ""],
        ["Constant", "", Header, "private const string Key = \"k\";\npublic void Work() { lock (Key) { } }", "", ""],
        ["Private get-only property", "", Header, "private object Gate { get; } = new object();\npublic void Work() { lock (Gate) { } }", "", ""],
        ["Private init-only property", "", Header, "private object Gate { get; init; } = new object();\npublic void Work() { lock (Gate) { } }", "", ""],
        ["Private expression-bodied property", "", Header, Gate + "private object Gate => _gate;\npublic void Work() { lock (Gate) { } }", "", ""],
        ["Private computed property", "", Header, "private object Gate { get { return new object(); } }\npublic void Work() { lock (Gate) { } }", "", ""],
        ["Public computed property is not looked at", "", Header, Gate + "public object Gate { get { return _gate; } }\npublic void Work() { lock (Gate) { } }", "", ""],
        ["Local variable", "", Header, "public void Work() { var gate = new object(); lock (gate) { } }", "", ""],
        ["Parameter", "", Header, "public void Work(object gate) { lock (gate) { } }", "", ""],
        ["Lock on this", "", Header, "public void Work() { lock (this) { } }", "", ""],
        ["Lock on a method result", "", Header, "private static object Get() => new object();\npublic void Work() { lock (Get()) { } }", "", ""],
        ["Lock on a field of another object", "", Header, "public object Gate = new object();\npublic void Work(Worker o) { lock (o.Gate) { } }", "", ""],
        ["Lock on a field of another assembly", "using static System.String;", Header, "public void Work() { lock (Empty) { } }", "", ""],
        ["Lock on a property of another assembly", "using static System.Environment;", Header, "public void Work() { lock (NewLine) { } }", "", ""],

        // ---- the reach of the members --------------------------------------------------------------------------------------------
        ["Public field that nobody reassigns is exposed only", "", Header, "public object Gate = new object();\npublic void Work() { [[lock (Gate)]] { } }", "", E("FieldExposed", "Gate")],
        ["Protected field", "", Header, "protected readonly object _gate = new object();\n" + Work, "", E("FieldExposed", "_gate")],
        ["Protected internal field", "", Header, "protected internal readonly object _gate = new object();\n" + Work, "", E("FieldExposed", "_gate")],
        ["Internal field is not looked at by default", "", Header, "internal readonly object _gate = new object();\n" + Work.Replace("[[", string.Empty).Replace("]]", string.Empty), "", ""],
        ["Internal field", "", Header, "internal readonly object _gate = new object();\n" + Work, "report_internal_locks=true", E("FieldExposed", "_gate")],
        ["Private protected field", "", Header, "private protected readonly object _gate = new object();\n" + Work, "report_internal_locks=true", E("FieldExposed", "_gate")],
        ["Private protected field is not looked at by default", "", Header, "private protected readonly object _gate = new object();\n" + Work.Replace("[[", string.Empty).Replace("]]", string.Empty), "", ""],
        ["Returned by an internal member", "", Header, Gate + "internal object Shared => _gate;\n" + Work, "report_internal_locks=true", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned by an internal member is not looked at by default", "", Header, Gate + "internal object Shared => _gate;\n" + Work.Replace("[[", string.Empty).Replace("]]", string.Empty), "", ""],
        ["Returned by a method", "", Header, Gate + "public object GetGate() { return _gate; }\n" + Work, "", E("FieldExposed", "_gate", "R:GetGate")],
        ["Returned by an expression-bodied method", "", Header, Gate + "public object GetGate() => _gate;\n" + Work, "", E("FieldExposed", "_gate", "R:GetGate")],
        ["Returned by a getter with an arrow", "", Header, Gate + "public object Shared { get => _gate; }\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned through a cast", "", Header, Gate + "public object Shared => (object)_gate;\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned in parentheses", "", Header, Gate + "public object Shared => (_gate);\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned through this", "", Header, Gate + "public object Shared => this._gate;\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned by an indexer", "", Header, Gate + "public object this[int i] => _gate;\n" + Work, "", E("FieldExposed", "_gate", "R:this[]")],
        ["Returned by a private and a public member", "", Header, Gate + "private object Own => _gate;\npublic object Shared => _gate;\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned by an internal and a public member", "", Header, Gate + "internal object Own => _gate;\npublic object Shared => _gate;\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Returned by a public and an internal member", "", Header, Gate + "public object Shared => _gate;\ninternal object Own => _gate;\n" + Work, "", E("FieldExposed", "_gate", "R:Shared")],
        ["Public field returned by a public member", "", Header, "public readonly object Gate = new object();\npublic object Shared => Gate;\npublic void Work() { [[lock (Gate)]] { } }", "", E("FieldExposed", "Gate")],
        ["Internal property", "", Header, "internal object Gate { get; } = new object();\npublic void Work() { [[lock (Gate)]] { } }", "report_internal_locks=true", E("PropertyExposed", "Gate", "P")],
        ["Property with a private getter and a public setter", "", Header, "public object Gate { private get; set; }\npublic void Work() { [[lock (Gate)]] { } }", "", E("PropertySwapped", "Gate", "P")],
        ["Property with a private setter", "", Header, "private object Gate { get; set; } = new object();\npublic void Work() { [[lock (Gate)]] { } }", "", E("PropertySwapped", "Gate", "P")],

        // ---- the options ---------------------------------------------------------------------------------------------------------
        ["Swapped locks refused", "", Header, Swapped + "public void Work() { lock (_gate) { } }", "report_swapped_locks=false", ""],
        ["Swapped locks refused, exposed ones still reported", "", Header, "public object Gate = new object();\npublic void Reset() { Gate = new object(); }\npublic void Work() { [[lock (Gate)]] { } }", "report_swapped_locks=false", E("FieldExposed", "Gate")],
        ["Exposed locks refused", "", Header, "public readonly object Gate = new object();\npublic void Work() { lock (Gate) { } }", "report_exposed_locks=false", ""],
        ["Exposed locks refused, swapped ones still reported", "", Header, "public object Gate = new object();\npublic void Reset() { Gate = new object(); }\npublic void Work() { [[lock (Gate)]] { } }", "report_exposed_locks=false", E("FieldSwapped", "Gate")],
        ["Fields refused", "", Header, Swapped + "public void Work() { lock (_gate) { } }", "report_fields=false", ""],
        ["Fields refused, properties still reported", "", Header, "public object Gate { get; } = new object();\npublic void Work() { [[lock (Gate)]] { } }", "report_fields=false", E("PropertyExposed", "Gate", "P")],
        ["Properties refused", "", Header, "public object Gate { get; } = new object();\npublic void Work() { lock (Gate) { } }", "report_properties=false", ""],
        ["Properties refused, fields still reported", "", Header, Swapped + Work, "report_properties=false", E("FieldSwapped", "_gate")],
        ["Excluded type name", "", Header, Swapped + "public void Work() { lock (_gate) { } }", "excluded_type_name_patterns=Work*", ""],
        ["Type name not excluded", "", Header, Swapped + Work, "excluded_type_name_patterns=Other*", E("FieldSwapped", "_gate")],
        ["Excluded namespace", "", Header, Swapped + "public void Work() { lock (_gate) { } }", "excluded_namespace_patterns=TestApp", ""],
        ["Namespace not excluded", "", Header, Swapped + Work, "excluded_namespace_patterns=Other.*", E("FieldSwapped", "_gate")],
        ["Excluded base type", "public class BaseWorker { }", "public class Worker : BaseWorker", Swapped + "public void Work() { lock (_gate) { } }", "excluded_base_types=TestApp.BaseWorker", ""],
        ["Base type not excluded", "public class BaseWorker { }", "public class Worker : BaseWorker", Swapped + Work, "excluded_base_types=Other.Base", E("FieldSwapped", "_gate")],
        ["Excluded lock name", "", Header, Swapped + "public void Work() { lock (_gate) { } }", "excluded_lock_name_patterns=_ga*", ""],
        ["Lock name not excluded", "", Header, Swapped + Work, "excluded_lock_name_patterns=_x*", E("FieldSwapped", "_gate")],
        ["Excluded file", "", Header, Swapped + "public void Work() { lock (_gate) { } }", "excluded_file_patterns=*.cs", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenarios(string title, string pre, string header, string members, string settings, string expected)
    {
        await RunAsync(pre, header, members, settings, expected).ConfigureAwait(false);
    }
}
