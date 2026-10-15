using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA010Tests
{
    private const string BoolProperty = "private bool _flag;\npublic bool Flag\n{\n    get { [[lock (_gate)]] { return _flag; } }\n    set { [[lock (_gate)]] { _flag = value; } }\n}\n";
    private const string OtherSection = "private int _a, _b;\npublic void Work() { lock (_gate) { _a++; _b++; } }\n";

    // title, types declared before the class, class header, members ([[...]] marks the expected lock, in order), settings (key=value;...), expected ("Scenario|field|lock;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the messages: one per scenario ------------------------------------------------------------------------
        ["Read, dedicated lock", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Write, dedicated lock", "", Header, Gate + "private bool _flag;\npublic bool Flag { set { [[lock (_gate)]] { _flag = value; } } }", "", E("WriteDedicated", "_flag", "_gate")],
        ["Property with both accessors", "", Header, Gate + BoolProperty, "", E("ReadDedicated", "_flag", "_gate") + ";" + E("WriteDedicated", "_flag", "_gate")],
        ["Read of a tearable field, dedicated lock", "", Header, Gate + "private double _x;\npublic double X { get { [[lock (_gate)]] { return _x; } } }", "", E("ReadDedicatedTearable", "_x", "_gate")],
        ["Write of a tearable field, dedicated lock", "", Header, Gate + "private double _x;\npublic double X { set { [[lock (_gate)]] { _x = value; } } }", "", E("WriteDedicatedTearable", "_x", "_gate")],
        ["Read, shared lock", "", Header, Gate + OtherSection + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadShared", "_flag", "_gate")],
        ["Write, shared lock", "", Header, Gate + OtherSection + "private bool _flag;\npublic bool Flag { set { [[lock (_gate)]] { _flag = value; } } }", "", E("WriteShared", "_flag", "_gate")],
        ["Read of a tearable field, shared lock", "", Header, Gate + OtherSection + "private double _x;\npublic double X { get { [[lock (_gate)]] { return _x; } } }", "", E("ReadSharedTearable", "_x", "_gate")],
        ["Write of a tearable field, shared lock", "", Header, Gate + OtherSection + "private double _x;\npublic double X { set { [[lock (_gate)]] { _x = value; } } }", "", E("WriteSharedTearable", "_x", "_gate")],
        ["Read, lock on this", "", Header, "private bool _flag;\npublic bool Flag { get { [[lock (this)]] { return _flag; } } }", "report_lock_on_this=true", E("ReadThis", "_flag", "this")],
        ["Write, lock on this", "", Header, "private bool _flag;\npublic bool Flag { set { [[lock (this)]] { _flag = value; } } }", "report_lock_on_this=true", E("WriteThis", "_flag", "this")],
        ["Read of a tearable field, lock on this", "", Header, "private double _x;\npublic double X { get { [[lock (this)]] { return _x; } } }", "report_lock_on_this=true", E("ReadThisTearable", "_x", "this")],
        ["Write of a tearable field, lock on this", "", Header, "private double _x;\npublic double X { set { [[lock (this)]] { _x = value; } } }", "report_lock_on_this=true", E("WriteThisTearable", "_x", "this")],
        ["Read of a volatile field", "", Header, Gate + "private volatile bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("VolatileRead", "_flag", "_gate")],
        ["Write of a volatile field", "", Header, Gate + "private volatile bool _flag;\npublic bool Flag { set { [[lock (_gate)]] { _flag = value; } } }", "", E("VolatileWrite", "_flag", "_gate")],
        ["Read of a readonly field", "", Header, Gate + "private readonly bool _flag = true;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadonlyField", "_flag", "_gate")],
        ["Volatile beats the lock kind", "", Header, Gate + OtherSection + "private volatile bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("VolatileRead", "_flag", "_gate")],
        ["Readonly beats tearable", "", Header, Gate + "private readonly double _x = 1;\npublic double X { get { [[lock (_gate)]] { return _x; } } }", "", E("ReadonlyField", "_x", "_gate")],

        // ---- the options of the kinds of access and field ---------------------------------------------------------------
        ["Reads refused", "", Header, Gate + BoolProperty.Replace("[[lock (_gate)]] { return", "lock (_gate) { return"), "report_reads=false", E("WriteDedicated", "_flag", "_gate")],
        ["Writes refused", "", Header, Gate + BoolProperty.Replace("[[lock (_gate)]] { _flag", "lock (_gate) { _flag"), "report_writes=false", E("ReadDedicated", "_flag", "_gate")],
        ["Reads and writes refused", "", Header, Gate + BoolProperty.Replace("[[", string.Empty).Replace("]]", string.Empty), "report_reads=false;report_writes=false", ""],
        ["Shared locks refused", "", Header, Gate + OtherSection + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "report_shared_locks=false", ""],
        ["Shared locks refused, dedicated ones still reported", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "report_shared_locks=false", E("ReadDedicated", "_flag", "_gate")],
        ["Lock on this is not reported by default", "", Header, "private bool _flag;\npublic bool Flag { get { lock (this) { return _flag; } } }", "", ""],
        ["Tearable types refused", "", Header, Gate + "private double _x;\npublic double X { get { lock (_gate) { return _x; } } }", "report_tearable_types=false", ""],
        ["Tearable types refused, plain ones still reported", "", Header, Gate + "private int _x;\npublic int X { get { [[lock (_gate)]] { return _x; } } }", "report_tearable_types=false", E("ReadDedicated", "_x", "_gate")],
        ["Volatile fields refused", "", Header, Gate + "private volatile bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "report_volatile_fields=false", ""],
        ["Readonly fields refused", "", Header, Gate + "private readonly bool _flag = true;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "report_readonly_fields=false", ""],
        ["Write of a readonly field is not a scenario", "", Header, Gate + "private readonly bool _flag;\npublic Worker() { lock (_gate) { _flag = true; } }", "member_kinds=constructor", ""],

        // ---- the lock object ------------------------------------------------------------------------------------------
        ["Static lock of a static field", "", Header, "private static readonly object Gate2 = new object();\nprivate static int _count;\npublic static int Count { get { [[lock (Gate2)]] { return _count; } } }", "", E("ReadDedicated", "_count", "Gate2")],
        ["Fields reached through this", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (this._gate)]] { return this._flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Parenthesized field", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return (_flag); } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Critical section without braces", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] return _flag; } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Lock that is not readonly but never reassigned", "", Header, "private object _gate = new object();\nprivate bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Lock assigned in the constructor", "", Header, "private object _gate;\nprivate bool _flag;\npublic Worker() { _gate = new object(); }\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Lock assigned through this in the constructor", "", Header, "private object _gate;\nprivate bool _flag;\npublic Worker() { this._gate = new object(); }\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Lock reassigned outside the constructor", "", Header, "private object _gate = new object();\nprivate bool _flag;\npublic void Reset() { _gate = new object(); }\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Lock handed to another method", "", Header, Gate + "private bool _flag;\npublic void Share() { Use(_gate); }\nprivate static void Use(object o) { }\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Lock returned", "", Header, Gate + "private bool _flag;\npublic object Expose() => _gate;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Lock compared", "", Header, Gate + "private bool _flag;\npublic bool Has() => _gate != null;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Lock used in a monitor call that is not Monitor", "public static class Helper { public static void Pulse(object o) { } }", Header, Gate + "private bool _flag;\npublic void Foo() { Helper.Pulse(_gate); }\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Lock signaled with Pulse", "", Header, Gate + "private bool _flag;\npublic void Signal() { lock (_gate) { Monitor.Pulse(_gate); } }\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Lock signaled with PulseAll", "", Header, Gate + "private bool _flag;\npublic void Signal() { lock (_gate) { Monitor.PulseAll(_gate); } }\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "", ""],
        ["Signaled lock reported when the option says so", "", Header, Gate + "private bool _flag;\npublic void Signal() { lock (_gate) { Monitor.PulseAll(_gate); } }\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "skip_signaled_locks=false", E("ReadShared", "_flag", "_gate")],
        ["Monitor.Wait makes the lock shared", "", Header, Gate + "private bool _flag;\npublic void Sleep() { Monitor.Enter(_gate); Monitor.Wait(_gate, 10); Monitor.Exit(_gate); }\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadShared", "_flag", "_gate")],
        ["Lock shared with a longer critical section of another part of the class", "public partial class Worker { private int _a, _b; public void Work() { lock (_gate) { _a++; _b++; } } }", "public partial class Worker", Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadShared", "_flag", "_gate")],
        ["Lock shared with a longer critical section of a nested class", "", Header, Gate + "private bool _flag;\npublic class Inner { public void Work(Worker w) { } }\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Public lock is not private", "", Header, "public readonly object Gate2 = new object();\nprivate bool _flag;\npublic bool Flag { get { lock (Gate2) { return _flag; } } }", "", ""],
        ["Internal lock is not private", "", Header, "internal readonly object Gate2 = new object();\nprivate bool _flag;\npublic bool Flag { get { lock (Gate2) { return _flag; } } }", "", ""],
        ["Protected lock is not private", "", Header, "protected readonly object Gate2 = new object();\nprivate bool _flag;\npublic bool Flag { get { lock (Gate2) { return _flag; } } }", "", ""],
        ["Lock of the base class", "public class Base { protected readonly object Gate2 = new object(); }", "public class Worker : Base", "private bool _flag;\npublic bool Flag { get { lock (Gate2) { return _flag; } } }", "", ""],
        ["Lock that is a property", "", Header, "private object Gate2 => this;\nprivate bool _flag;\npublic bool Flag { get { lock (Gate2) { return _flag; } } }", "", ""],
        ["Lock that is a local variable", "", Header, "private bool _flag;\npublic bool Get() { var g = new object(); lock (g) { return _flag; } }", "body_shape=any_statement", ""],
        ["Lock of another class", "public static class Locks { public static readonly object Gate2 = new object(); }", Header, "private bool _flag;\npublic bool Flag { get { lock (Locks.Gate2) { return _flag; } } }", "", ""],
        ["Lock of the outer class used by a nested class", "", "public class Worker", Gate + "public class Inner { private bool _flag; public bool Flag { get { lock (new Worker()._gate) { return _flag; } } } }", "", ""],
        ["Lock field of another type reached by name", "", "public class Worker", "private bool _flag;\npublic bool Flag { get { lock (typeof(Worker)) { return _flag; } } }", "", ""],
        ["Lock of a field with the lock field excluded by name", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_lock_name_patterns=_ga*", ""],

        // ---- what the critical section does ---------------------------------------------------------------------------------
        ["Literal assigned", "", Header, Gate + "private bool _flag;\npublic void Stop() { [[lock (_gate)]] { _flag = false; } }", "", E("WriteDedicated", "_flag", "_gate")],
        ["Default assigned", "", Header, Gate + "private bool _flag;\npublic void Stop() { [[lock (_gate)]] { _flag = default; } }", "", E("WriteDedicated", "_flag", "_gate")],
        ["Default of a type assigned", "", Header, Gate + "private bool _flag;\npublic void Stop() { [[lock (_gate)]] { _flag = default(bool); } }", "", E("WriteDedicated", "_flag", "_gate")],
        ["Null assigned", "", Header, Gate + "private string _name;\npublic void Clear() { [[lock (_gate)]] { _name = null; } }", "", E("WriteDedicated", "_name", "_gate")],
        ["Parenthesized value assigned", "", Header, Gate + "private bool _flag;\npublic bool Flag { set { [[lock (_gate)]] { _flag = (value); } } }", "", E("WriteDedicated", "_flag", "_gate")],
        ["Negated parameter assigned", "", Header, Gate + "private bool _flag;\npublic void Set(bool v) { [[lock (_gate)]] { _flag = !v; } }", "", E("WriteDedicated", "_flag", "_gate")],
        ["Negative parameter assigned", "", Header, Gate + "private int _n;\npublic void Set(int v) { [[lock (_gate)]] { _n = -v; } }", "", E("WriteDedicated", "_n", "_gate")],
        ["Plus parameter assigned", "", Header, Gate + "private int _n;\npublic void Set(int v) { [[lock (_gate)]] { _n = +v; } }", "", E("WriteDedicated", "_n", "_gate")],
        ["Complemented parameter assigned", "", Header, Gate + "private int _n;\npublic void Set(int v) { [[lock (_gate)]] { _n = ~v; } }", "", E("WriteDedicated", "_n", "_gate")],
        ["Pre-increment is not inert", "", Header, Gate + "private int _n;\npublic void Set(int v) { lock (_gate) { _n = ++v; } }", "", ""],
        ["Constant assigned", "", Header, Gate + "private const int Limit = 3;\nprivate int _n;\npublic void Reset() { [[lock (_gate)]] { _n = Limit; } }", "", E("WriteDedicated", "_n", "_gate")],
        ["Enum member assigned", "public enum Mode { Off, On }", Header, Gate + "private Mode _mode;\npublic void Enable() { [[lock (_gate)]] { _mode = Mode.On; } }", "", E("WriteDedicated", "_mode", "_gate")],
        ["Local assigned", "", Header, Gate + "private bool _flag;\npublic void Set() { var v = true; [[lock (_gate)]] { _flag = v; } }", "body_shape=any_statement", E("WriteDedicated", "_flag", "_gate")],
        ["Another field assigned", "", Header, Gate + "private bool _flag, _other;\npublic void Copy() { lock (_gate) { _flag = _other; } }", "", ""],
        ["Static field of another class assigned", "public static class Cfg { public static bool On; }", Header, Gate + "private bool _flag;\npublic void Copy() { lock (_gate) { _flag = Cfg.On; } }", "", ""],
        ["Method result assigned", "", Header, Gate + "private bool _flag;\nprivate static bool Compute() => true;\npublic void Compute2() { lock (_gate) { _flag = Compute(); } }", "", ""],
        ["Expression assigned", "", Header, Gate + "private bool _flag;\npublic void Set(bool v, bool w) { lock (_gate) { _flag = v && w; } }", "", ""],
        ["Compound assignment", "", Header, Gate + "private int _n;\npublic void Add(int v) { lock (_gate) { _n += v; } }", "", ""],
        ["Increment", "", Header, Gate + "private int _n;\npublic void Add() { lock (_gate) { _n++; } }", "", ""],
        ["Two statements", "", Header, Gate + "private int _a, _b;\npublic void Set(int v) { lock (_gate) { _a = v; _b = v; } }", "", ""],
        ["Empty critical section", "", Header, Gate + "public void Set() { lock (_gate) { } }", "", ""],
        ["Return without a value", "", Header, Gate + "public void Set() { lock (_gate) { return; } }", "", ""],
        ["Return of a computation", "", Header, Gate + "private int _a, _b;\npublic int Sum() { lock (_gate) { return _a + _b; } }", "", ""],
        ["Return of a method call", "", Header, Gate + "private int _a;\npublic int Get() { lock (_gate) { return _a.GetHashCode(); } }", "", ""],
        ["Return of a property", "", Header, Gate + "private int A { get; set; }\npublic int Get() { lock (_gate) { return A; } }", "", ""],
        ["Return of a constant", "", Header, Gate + "private const int Limit = 3;\npublic int Get() { lock (_gate) { return Limit; } }", "", ""],
        ["Return of a field of another object", "", Header, Gate + "private Worker _next;\npublic bool _x;\npublic bool Get() { lock (_gate) { return _next._x; } }", "", ""],
        ["Return of a local", "", Header, Gate + "public int Get() { var v = 1; lock (_gate) { return v; } }", "body_shape=any_statement", ""],
        ["Collection call", "", Header, Gate + "private System.Collections.Generic.List<int> _items = new System.Collections.Generic.List<int>();\npublic void Add(int v) { lock (_gate) { _items.Add(v); } }", "", ""],
        ["Assignment to a property", "", Header, Gate + "private int A { get; set; }\npublic void Set(int v) { lock (_gate) { A = v; } }", "", ""],
        ["Assignment to an element", "", Header, Gate + "private int[] _a = new int[1];\npublic void Set(int v) { lock (_gate) { _a[0] = v; } }", "", ""],
        ["Reference field returned through this", "", Header, Gate + "private Worker _next;\npublic Worker Get() { [[lock (_gate)]] { return this._next; } }", "", E("ReadDedicated", "_next", "_gate")],

        // ---- where the critical section is ------------------------------------------------------------------------------------
        ["Method", "", Header, Gate + "private bool _flag;\npublic bool Get() { [[lock (_gate)]] { return _flag; } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Method not looked at", "", Header, Gate + "private bool _flag;\npublic bool Get() { lock (_gate) { return _flag; } }", "member_kinds=accessor", ""],
        ["Accessor not looked at", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "member_kinds=method", ""],
        ["Operator", "", Header, Gate + "private static readonly object G2 = new object();\nprivate static bool _flag;\npublic static bool operator !(Worker w) { [[lock (G2)]] { return _flag; } }", "", E("ReadDedicated", "_flag", "G2")],
        ["Conversion operator", "", Header, "private static readonly object G2 = new object();\nprivate static int _n;\npublic static implicit operator int(Worker w) { [[lock (G2)]] { return _n; } }", "", E("ReadDedicated", "_n", "G2")],
        ["Local function", "", Header, Gate + "private bool _flag;\npublic bool Get() { bool Local() { [[lock (_gate)]] { return _flag; } } return Local(); }", "member_kinds=local_function", E("ReadDedicated", "_flag", "_gate")],
        ["Local function not looked at", "", Header, Gate + "private bool _flag;\npublic bool Get() { bool Local() { lock (_gate) { return _flag; } } return Local(); }", "", ""],
        ["Lambda", "", Header, Gate + "private bool _flag;\npublic Func<bool> F => () => { [[lock (_gate)]] { return _flag; } };", "member_kinds=lambda", E("ReadDedicated", "_flag", "_gate")],
        ["Anonymous method", "", Header, Gate + "private bool _flag;\npublic Func<bool> F => delegate { [[lock (_gate)]] { return _flag; } };", "member_kinds=lambda", E("ReadDedicated", "_flag", "_gate")],
        ["Lambda not looked at", "", Header, Gate + "private bool _flag;\npublic Func<bool> F => () => { lock (_gate) { return _flag; } };", "", ""],
        ["Constructor", "", Header, Gate + "private bool _flag;\npublic Worker() { [[lock (_gate)]] { _flag = true; } }", "member_kinds=constructor", E("WriteDedicated", "_flag", "_gate")],
        ["Finalizer", "", Header, Gate + "private bool _flag;\n~Worker() { [[lock (_gate)]] { _flag = true; } }", "member_kinds=constructor", E("WriteDedicated", "_flag", "_gate")],
        ["Constructor not looked at", "", Header, Gate + "private bool _flag;\npublic Worker() { lock (_gate) { _flag = true; } }", "", ""],
        ["Nested statement", "", Header, Gate + "private bool _flag;\npublic void Set(bool x) { if (x) { [[lock (_gate)]] { _flag = true; } } }", "member_kinds=other", E("WriteDedicated", "_flag", "_gate")],
        ["Nested statement not looked at", "", Header, Gate + "private bool _flag;\npublic void Set(bool x) { if (x) { lock (_gate) { _flag = true; } } }", "", ""],
        ["Several kinds", "", Header, Gate + "private bool _flag;\npublic bool Get() { [[lock (_gate)]] { return _flag; } }\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "member_kinds=accessor, method", E("ReadDedicated", "_flag", "_gate") + ";" + E("ReadDedicated", "_flag", "_gate")],
        ["Unknown kinds fall back to the default", "", Header, Gate + "private bool _flag;\npublic bool Get() { [[lock (_gate)]] { return _flag; } }", "member_kinds=nonsense", E("ReadDedicated", "_flag", "_gate")],
        ["Critical section among other statements", "", Header, Gate + "private bool _flag;\npublic bool Get() { var x = 1; lock (_gate) { return _flag; } }", "", ""],
        ["Critical section among other statements, any statement", "", Header, Gate + "private bool _flag;\npublic bool Get() { var x = 1; [[lock (_gate)]] { return _flag; } }", "body_shape=any_statement", E("ReadDedicated", "_flag", "_gate")],
        ["Any statement of an accessor", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { var x = 1; [[lock (_gate)]] { return _flag; } } }", "body_shape=any_statement", E("ReadDedicated", "_flag", "_gate")],
        ["Any statement, case-insensitive value", "", Header, Gate + "private bool _flag;\npublic bool Get() { var x = 1; [[lock (_gate)]] { return _flag; } }", "body_shape=ANY_STATEMENT", E("ReadDedicated", "_flag", "_gate")],
        ["Any statement without a member", "", Header, Gate + "private bool _flag;\nprivate static readonly bool _x = Compute();\nprivate static bool Compute() => true;", "body_shape=any_statement;member_kinds=other", ""],
        ["Unknown body shape falls back to the default", "", Header, Gate + "private bool _flag;\npublic bool Get() { var x = 1; lock (_gate) { return _flag; } }", "body_shape=nonsense", ""],

        // ---- the exclusions ---------------------------------------------------------------------------------------------------
        ["Excluded type name", "", Header, Gate + BoolProperty.Replace("[[", string.Empty).Replace("]]", string.Empty), "excluded_type_name_patterns=Wor*", ""],
        ["Type name exclusion not matching", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "excluded_type_name_patterns=*Dto", E("ReadDedicated", "_flag", "_gate")],
        ["Excluded namespace", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_namespace_patterns=Test*", ""],
        ["Namespace exclusion not matching", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "excluded_namespace_patterns=Other*", E("ReadDedicated", "_flag", "_gate")],
        ["Excluded base type", "public class Base { }", "public class Worker : Base", Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_base_types=TestApp.Base", ""],
        ["Base type exclusion not matching", "public class Base { }", "public class Worker : Base", Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "excluded_base_types=TestApp.Other", E("ReadDedicated", "_flag", "_gate")],
        ["Excluded property name", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_member_name_patterns=Fl*", ""],
        ["Excluded method name", "", Header, Gate + "private bool _flag;\npublic bool Get() { lock (_gate) { return _flag; } }", "excluded_member_name_patterns=Get", ""],
        ["Excluded local function name", "", Header, Gate + "private bool _flag;\npublic bool Run() { bool Local() { lock (_gate) { return _flag; } } return Local(); }", "member_kinds=local_function;excluded_member_name_patterns=Loc*", ""],
        ["Excluded event name", "", Header, Gate + "private EventHandler _h;\npublic event EventHandler E { add { lock (_gate) { _h = value; } } remove { lock (_gate) { _h = value; } } }", "excluded_member_name_patterns=E", ""],
        ["Event accessors", "", Header, Gate + "private EventHandler _h;\npublic event EventHandler E { add { [[lock (_gate)]] { _h = value; } } remove { [[lock (_gate)]] { _h = value; } } }", "", E("WriteDedicated", "_h", "_gate") + ";" + E("WriteDedicated", "_h", "_gate")],
        ["Indexer", "", Header, Gate + "private bool _flag;\npublic bool this[int i] { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
        ["Excluded indexer", "", Header, Gate + "private bool _flag;\npublic bool this[int i] { get { lock (_gate) { return _flag; } } }", "excluded_member_name_patterns=this[]", ""],
        ["Lambda has no name to exclude", "", Header, Gate + "private bool _flag;\npublic Func<bool> F => () => { [[lock (_gate)]] { return _flag; } };", "member_kinds=lambda;excluded_member_name_patterns=*", E("ReadDedicated", "_flag", "_gate")],
        ["Excluded field name", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_field_name_patterns=_fl*", ""],
        ["Excluded field type", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_field_types=bool", ""],
        ["Excluded field type, another one reported", "", Header, Gate + "private int _flag;\npublic int Flag { get { [[lock (_gate)]] { return _flag; } } }", "excluded_field_types=bool", E("ReadDedicated", "_flag", "_gate")],
        ["Excluded file", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { lock (_gate) { return _flag; } } }", "excluded_file_patterns=Test0.cs", ""],
        ["File exclusion not matching", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "excluded_file_patterns=Other.cs", E("ReadDedicated", "_flag", "_gate")],
        ["Lists accept semicolons are not separators of the editorconfig", "", Header, Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "excluded_field_name_patterns=", E("ReadDedicated", "_flag", "_gate")],

        // ---- the shape of the type -------------------------------------------------------------------------------------------------
        ["Generic class, unconstrained field", "", "public class Worker<T>", Gate + "private T _v;\npublic T V { get { [[lock (_gate)]] { return _v; } } }", "", E("ReadDedicatedTearable", "_v", "_gate")],
        ["Generic class, class-constrained field", "", "public class Worker<T> where T : class", Gate + "private T _v;\npublic T V { get { [[lock (_gate)]] { return _v; } } }", "", E("ReadDedicated", "_v", "_gate")],
        ["Struct", "", "public struct Worker", "private static readonly object G2 = new object();\nprivate static bool _flag;\npublic bool Flag { get { [[lock (G2)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "G2")],
        ["Record", "", "public record Worker", Gate + "private bool _flag;\npublic bool Flag { get { [[lock (_gate)]] { return _flag; } } }", "", E("ReadDedicated", "_flag", "_gate")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string pre, string header, string members, string settings, string expected)
    {
        await RunAsync(pre, header, members, settings, expected).ConfigureAwait(false);
    }

    // ---- the types of the field ----------------------------------------------------------------------------------------------

    private static IEnumerable<object[]> GetFieldTypes =>
    [
        ["bool", false], ["byte", false], ["sbyte", false], ["short", false], ["ushort", false], ["int", false], ["uint", false], ["char", false], ["float", false],
        ["string", false], ["object", false], ["int[]", false], ["Action", false], ["System.IntPtr", false], ["System.UIntPtr", false], ["Kind", false], ["Worker", false],
        ["long", true], ["ulong", true], ["double", true], ["decimal", true], ["DateTime", true], ["TimeSpan", true], ["Guid", true], ["Pair", true], ["int?", true],
        ["(int, int)", true], ["LongKind", true],
    ];

    [TestMethod]
    [DynamicData(nameof(GetFieldTypes), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Field_type(string type, bool tearable)
    {
        var pre = "public enum Kind { A }\npublic enum LongKind : long { A }\npublic struct Pair { public int X, Y; }";
        var members = Gate + "private " + type + " _f;\npublic " + type + " F { get { [[lock (_gate)]] { return _f; } } }";

        await RunAsync(pre, Header, members, string.Empty, E(tearable ? "ReadDedicatedTearable" : "ReadDedicated", "_f", "_gate")).ConfigureAwait(false);
    }
}
