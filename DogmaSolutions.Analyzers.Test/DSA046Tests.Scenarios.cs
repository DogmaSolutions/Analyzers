using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA046Tests
{
    private const string Counter = "private volatile int _n;\n";

    // title, types declared before the class, class header, members ([[...]] marks the expected operation, in order), settings (key=value;...), expected ("Scenario|field|operator;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the messages: one per scenario ------------------------------------------------------------------------
        ["Increment, int", "", Header, Counter + "public void Hit() { [[_n++]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["Increment, other value type", "", Header, "private volatile short _n;\npublic void Hit() { [[_n++]]; }", "", E("IncrementValue", "_n", "++")],
        ["Increment, reference type with an operator", "public class Level { public static Level operator ++(Level l) => l; }", Header, "private volatile Level _n;\npublic void Hit() { [[_n++]]; }", "", E("IncrementReference", "_n", "++")],
        ["Compound assignment, int", "", Header, Counter + "public void Hit(int v) { [[_n += v]]; }", "", E("CompoundInterlocked", "_n", "+=")],
        ["Compound assignment, other value type", "", Header, "private volatile byte _n;\npublic void Hit(byte v) { [[_n |= v]]; }", "", E("CompoundValue", "_n", "|=")],
        ["Compound assignment, delegate", "", Header, "private volatile EventHandler _h;\npublic void Add(EventHandler h) { [[_h += h]]; }", "", E("CompoundReference", "_h", "+=")],
        ["Compound assignment, string", "", Header, "private volatile string _s = \"\";\npublic void Add(string t) { [[_s += t]]; }", "", E("CompoundReference", "_s", "+=")],
        ["Assignment from itself, int", "", Header, Counter + "public void Hit() { [[_n = _n + 1]]; }", "", E("AssignmentInterlocked", "_n", "=")],
        ["Assignment from itself, bool", "", Header, "private volatile bool _on;\npublic void Toggle() { [[_on = !_on]]; }", "", E("AssignmentValue", "_on", "=")],
        ["Assignment from itself, reference", "", Header, "private volatile object _o;\npublic void Init() { [[_o = _o ?? new object()]]; }", "", E("AssignmentReference", "_o", "=")],

        // ---- the operators -----------------------------------------------------------------------------------------------
        ["Pre increment", "", Header, Counter + "public void Hit() { [[++_n]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["Post decrement", "", Header, Counter + "public void Hit() { [[_n--]]; }", "", E("IncrementInterlocked", "_n", "--")],
        ["Pre decrement", "", Header, Counter + "public void Hit() { [[--_n]]; }", "", E("IncrementInterlocked", "_n", "--")],
        ["Subtract", "", Header, Counter + "public void Hit() { [[_n -= 2]]; }", "", E("CompoundInterlocked", "_n", "-=")],
        ["Multiply", "", Header, Counter + "public void Hit() { [[_n *= 2]]; }", "", E("CompoundInterlocked", "_n", "*=")],
        ["Divide", "", Header, Counter + "public void Hit() { [[_n /= 2]]; }", "", E("CompoundInterlocked", "_n", "/=")],
        ["Modulo", "", Header, Counter + "public void Hit() { [[_n %= 2]]; }", "", E("CompoundInterlocked", "_n", "%=")],
        ["And", "", Header, Counter + "public void Hit() { [[_n &= 2]]; }", "", E("CompoundInterlocked", "_n", "&=")],
        ["Or", "", Header, Counter + "public void Hit() { [[_n |= 2]]; }", "", E("CompoundInterlocked", "_n", "|=")],
        ["Xor", "", Header, Counter + "public void Hit() { [[_n ^= 2]]; }", "", E("CompoundInterlocked", "_n", "^=")],
        ["Shift left", "", Header, Counter + "public void Hit() { [[_n <<= 2]]; }", "", E("CompoundInterlocked", "_n", "<<=")],
        ["Shift right", "", Header, Counter + "public void Hit() { [[_n >>= 2]]; }", "", E("CompoundInterlocked", "_n", ">>=")],
        ["Unsigned shift right", "", Header, Counter + "public void Hit() { [[_n >>>= 2]]; }", "", E("CompoundInterlocked", "_n", ">>>=")],
        ["Unsigned int", "", Header, "private volatile uint _u;\npublic void Hit() { [[_u++]]; }", "", E("IncrementInterlocked", "_u", "++")],
        ["Enum is another value type", "public enum Mode { Off, On }", Header, "private volatile Mode _m;\npublic void Next() { [[_m = _m == Mode.Off ? Mode.On : Mode.Off]]; }", "", E("AssignmentValue", "_m", "=")],

        // ---- how the field is reached ----------------------------------------------------------------------------------------
        ["Through this", "", Header, Counter + "public void Hit() { [[this._n++]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["Through another instance", "", Header, Counter + "public void Hit(Worker other) { [[other._n++]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["Parenthesized", "", Header, Counter + "public void Hit() { [[(_n)++]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["Static field", "", Header, "private static volatile int _n;\npublic static void Hit() { [[_n++]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["Public field", "", Header, "public volatile int N;\npublic void Hit() { [[N++]]; }", "", E("IncrementInterlocked", "N", "++")],
        ["Value of the expression is used", "", Header, Counter + "public int Hit() { return [[_n++]]; }", "", E("IncrementInterlocked", "_n", "++")],
        ["In an expression-bodied member", "", Header, Counter + "public void Hit() => [[_n++]];", "", E("IncrementInterlocked", "_n", "++")],
        ["In a lambda", "", Header, Counter + "public Action Hit() => () => { [[_n++]]; };", "", E("IncrementInterlocked", "_n", "++")],
        ["In a local function", "", Header, Counter + "public void Hit() { void Inc() { [[_n++]]; } Inc(); }", "", E("IncrementInterlocked", "_n", "++")],
        ["In a property accessor", "", Header, Counter + "public int N { set { [[_n += value]]; } }", "", E("CompoundInterlocked", "_n", "+=")],
        ["Several operations", "", Header, Counter + "public void Hit() { [[_n++]]; [[_n += 2]]; }", "", E("IncrementInterlocked", "_n", "++") + ";" + E("CompoundInterlocked", "_n", "+=")],
        ["Self assignment through this", "", Header, Counter + "public void Hit() { [[this._n = this._n + 1]]; }", "", E("AssignmentInterlocked", "_n", "=")],
        ["Self assignment, one side through this", "", Header, Counter + "public void Hit() { [[_n = this._n + 1]]; }", "", E("AssignmentInterlocked", "_n", "=")],
        ["Self assignment through another instance", "", Header, Counter + "public void Hit(Worker o) { [[o._n = o._n + 1]]; }", "", E("AssignmentInterlocked", "_n", "=")],
        ["Self assignment in a method call", "", Header, Counter + "public void Hit(int v) { [[_n = Math.Max(_n, v)]]; }", "", E("AssignmentInterlocked", "_n", "=")],

        // ---- what is not a read-modify-write ----------------------------------------------------------------------------------
        ["Plain assignment", "", Header, Counter + "public void Reset() { _n = 0; }", "", ""],
        ["Assignment of another field", "", Header, Counter + "private int _m;\npublic void Hit() { _n = _m + 1; }", "", ""],
        ["Assignment from a parameter", "", Header, Counter + "public void Hit(int n) { _n = n + 1; }", "", ""],
        ["Assignment from the same name of another object", "", Header, Counter + "public void Hit(Worker o) { _n = o._n + 1; }", "", ""],
        ["Assignment to another object from itself", "", Header, Counter + "public void Hit(Worker o) { o._n = _n + 1; }", "", ""],
        ["Assignment from a local of the same name", "", Header, "private volatile int n;\npublic void Hit() { var n = 1; this.n = n + 1; }", "", ""],
        ["Read", "", Header, Counter + "public int Get() { return _n + 1; }", "", ""],
        ["Not volatile", "", Header, "private int _n;\npublic void Hit() { _n++; }", "", ""],
        ["Property", "", Header, "public int N { get; set; }\npublic void Hit() { N++; }", "", ""],
        ["Local variable", "", Header, "public void Hit() { var n = 0; n++; n += 2; n = n + 1; }", "", ""],
        ["Element of a field", "", Header, "private volatile int[] _a = new int[1];\npublic void Hit() { _a[0]++; }", "", ""],
        ["Member of a field", "", Header, "private volatile Worker _w;\npublic int N;\npublic void Hit() { _w.N++; _w.N += 2; }", "", ""],
        ["Method call as target", "", Header, "private int _x;\nprivate ref int Get() => ref _x;\npublic void Hit() { Get()++; }", "", ""],
        ["Null coalescing assignment is another lesson", "", Header, "private volatile object _o;\npublic void Init() { _o ??= new object(); }", "", ""],

        // ---- the contexts that are exempted by default ------------------------------------------------------------------------
        ["Inside a lock", "", Header, Counter + "private readonly object _gate = new object();\npublic void Hit() { lock (_gate) { _n++; } }", "", ""],
        ["Inside a lock, option off", "", Header, Counter + "private readonly object _gate = new object();\npublic void Hit() { lock (_gate) { [[_n++]]; } }", "skip_inside_lock=false", E("IncrementInterlocked", "_n", "++")],
        ["A lambda inside a lock runs later", "", Header, Counter + "private readonly object _gate = new object();\npublic Action Hit() { lock (_gate) { return () => { [[_n++]]; }; } }", "", E("IncrementInterlocked", "_n", "++")],
        ["A lock outside the lambda of a member", "", Header, Counter + "private readonly object _gate = new object();\npublic void Hit() { lock (_gate) { void Inc() { [[_n++]]; } Inc(); } }", "", E("IncrementInterlocked", "_n", "++")],
        ["In a constructor", "", Header, Counter + "public Worker() { _n++; _n += 2; _n = _n + 1; }", "", ""],
        ["In a constructor, option off", "", Header, Counter + "public Worker() { [[_n++]]; }", "skip_constructors=false", E("IncrementInterlocked", "_n", "++")],
        ["In a static constructor", "", Header, "private static volatile int _n;\nstatic Worker() { _n++; }", "", ""],
        ["A lambda of a constructor runs later", "", Header, Counter + "public Worker() { Action a = () => { [[_n++]]; }; a(); }", "", E("IncrementInterlocked", "_n", "++")],
        ["A local function of a constructor runs later", "", Header, Counter + "public Worker() { void Inc() { [[_n++]]; } Inc(); }", "", E("IncrementInterlocked", "_n", "++")],

        // ---- the options ---------------------------------------------------------------------------------------------------
        ["Increments refused", "", Header, Counter + "public void Hit() { _n++; [[_n += 1]]; }", "report_increments=false", E("CompoundInterlocked", "_n", "+=")],
        ["Compound assignments refused", "", Header, Counter + "public void Hit() { [[_n++]]; _n += 1; }", "report_compound_assignments=false", E("IncrementInterlocked", "_n", "++")],
        ["Self assignments refused", "", Header, Counter + "public void Hit() { _n = _n + 1; }", "report_self_assignments=false", ""],
        ["Self assignments refused, increments still reported", "", Header, Counter + "public void Hit() { [[_n++]]; _n = _n + 1; }", "report_self_assignments=false", E("IncrementInterlocked", "_n", "++")],
        ["Interlocked types refused", "", Header, Counter + "public void Hit() { _n++; }", "report_interlocked_types=false", ""],
        ["Interlocked types refused, others still reported", "", Header, "private volatile bool _b;\nprivate volatile int _n;\npublic void Hit() { _n++; [[_b = !_b]]; }", "report_interlocked_types=false", E("AssignmentValue", "_b", "=")],
        ["Other value types refused", "", Header, "private volatile bool _b;\npublic void Hit() { _b = !_b; }", "report_other_value_types=false", ""],
        ["Reference types refused", "", Header, "private volatile EventHandler _h;\npublic void Add(EventHandler h) { _h += h; }", "report_reference_types=false", ""],
        ["Excluded type name", "", Header, Counter + "public void Hit() { _n++; }", "excluded_type_name_patterns=Work*", ""],
        ["Type name not excluded", "", Header, Counter + "public void Hit() { [[_n++]]; }", "excluded_type_name_patterns=Other*", E("IncrementInterlocked", "_n", "++")],
        ["Excluded namespace", "", Header, Counter + "public void Hit() { _n++; }", "excluded_namespace_patterns=TestApp", ""],
        ["Namespace not excluded", "", Header, Counter + "public void Hit() { [[_n++]]; }", "excluded_namespace_patterns=Other.*", E("IncrementInterlocked", "_n", "++")],
        ["Excluded base type", "public class BaseWorker { }", "public class Worker : BaseWorker", Counter + "public void Hit() { _n++; }", "excluded_base_types=TestApp.BaseWorker", ""],
        ["Base type not excluded", "public class BaseWorker { }", "public class Worker : BaseWorker", Counter + "public void Hit() { [[_n++]]; }", "excluded_base_types=Other.Base", E("IncrementInterlocked", "_n", "++")],
        ["Excluded field name", "", Header, Counter + "public void Hit() { _n++; }", "excluded_field_name_patterns=_n*", ""],
        ["Field name not excluded", "", Header, Counter + "public void Hit() { [[_n++]]; }", "excluded_field_name_patterns=_x*", E("IncrementInterlocked", "_n", "++")],
        ["Excluded file", "", Header, Counter + "public void Hit() { _n++; }", "excluded_file_patterns=*.cs", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenarios(string title, string pre, string header, string members, string settings, string expected)
    {
        await RunAsync(pre, header, members, settings, expected).ConfigureAwait(false);
    }
}
