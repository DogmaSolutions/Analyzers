using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA050Tests
{
    private const string Run = "private void Run() { }\n";
    private const string Mode = "public enum Mode { Idle, Running }";

    // title, types declared before the class, class header, members ([[...]] marks the expected if header, in order), settings (key=value;...), expected ("Scenario~field~assigned~observed~V;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the messages: one per scenario ------------------------------------------------------------------------
        ["Set once", "", Header, Run + "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false")],
        ["Consume", "", Header, Run + "private bool _pending;\npublic void Flush() { [[if (_pending)]] { _pending = false; Run(); } }", "", E("Consume", "_pending", "false", "true")],
        ["Transition", Mode, Header, Run + "private Mode _mode;\npublic void Start() { [[if (_mode == Mode.Idle)]] { _mode = Mode.Running; Run(); } }", "", E("Transition", "_mode", "Mode.Running", "Mode.Idle")],
        ["Set once, volatile", "", Header, Run + "private volatile bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false", true)],
        ["Consume, volatile", "", Header, Run + "private volatile bool _pending;\npublic void Flush() { [[if (_pending)]] { _pending = false; Run(); } }", "", E("Consume", "_pending", "false", "true", true)],
        ["Transition, volatile", Mode, Header, Run + "private volatile int _state;\npublic void Start() { [[if (_state == 0)]] { _state = 1; Run(); } }", "", E("Transition", "_state", "1", "0", true)],

        // ---- the shapes of the condition ----------------------------------------------------------------------------------
        ["Compared to false", "", Header, Run + "private bool _started;\npublic void Start() { [[if (_started == false)]] { _started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false")],
        ["Compared to true", "", Header, Run + "private bool _pending;\npublic void Flush() { [[if (_pending == true)]] { _pending = false; Run(); } }", "", E("Consume", "_pending", "false", "true")],
        ["Constant on the left", "", Header, Run + "private bool _started;\npublic void Start() { [[if (false == _started)]] { _started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false")],
        ["Not equal to true", "", Header, Run + "private bool _started;\npublic void Start() { [[if (_started != true)]] { _started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false")],
        ["Not equal to false", "", Header, Run + "private bool _pending;\npublic void Flush() { [[if (_pending != false)]] { _pending = false; Run(); } }", "", E("Consume", "_pending", "false", "true")],
        ["Parenthesized", "", Header, Run + "private bool _started;\npublic void Start() { [[if ((!(_started)))]] { _started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false")],
        ["Through this", "", Header, Run + "private bool _started;\npublic void Start() { [[if (!this._started)]] { this._started = true; Run(); } }", "", E("SetOnce", "_started", "true", "false")],
        ["Set in the else branch", "", Header, Run + "private bool _started;\npublic void Start() { [[if (_started)]] { Run(); } else { _started = true; } }", "", E("SetOnce", "_started", "true", "false")],
        ["Cleared in the else branch", "", Header, Run + "private bool _pending;\npublic void Flush() { [[if (!_pending)]] { Run(); } else { _pending = false; } }", "", E("Consume", "_pending", "false", "true")],
        ["Not equal, set in the else branch", Mode, Header, Run + "private Mode _mode;\npublic void Start() { [[if (_mode != Mode.Idle)]] { Run(); } else { _mode = Mode.Running; } }", "", E("Transition", "_mode", "Mode.Running", "Mode.Idle")],
        ["Constant on the left of an enum", Mode, Header, Run + "private Mode _mode;\npublic void Start() { [[if (Mode.Idle == _mode)]] { _mode = Mode.Running; Run(); } }", "", E("Transition", "_mode", "Mode.Running", "Mode.Idle")],
        ["Constant name", "", Header, Run + "private const int Idle = 0;\nprivate int _state;\npublic void Start() { [[if (_state == Idle)]] { _state = 2; Run(); } }", "", E("Transition", "_state", "2", "Idle")],
        ["Assignment deeper in the branch", "", Header, Run + "private bool _started;\npublic void Start() { [[if (!_started)]] { Run(); try { _started = true; } finally { } } }", "", E("SetOnce", "_started", "true", "false")],
        ["Assignment in a nested if", "", Header, Run + "private bool _started;\npublic void Start(bool b) { [[if (!_started)]] { if (b) { _started = true; } } }", "", E("SetOnce", "_started", "true", "false")],
        ["Another assignment first", "", Header, Run + "private bool _started;\nprivate int _n;\npublic void Start() { [[if (!_started)]] { _n = 1; _started = true; } }", "", E("SetOnce", "_started", "true", "false")],
        ["Chain of ifs", "", Header, Run + "private bool _a, _b;\npublic void Start() { [[if (!_a)]] { _a = true; } else [[if (!_b)]] { _b = true; } }", "", E("SetOnce", "_a", "true", "false") + ";" + E("SetOnce", "_b", "true", "false")],
        ["Static field", "", Header, Run + "private static bool _started;\npublic static void Start() { [[if (!_started)]] { _started = true; } }", "", E("SetOnce", "_started", "true", "false")],
        ["Property accessor", "", Header, Run + "private bool _started;\npublic int Start { get { [[if (!_started)]] { _started = true; } return 1; } }", "", E("SetOnce", "_started", "true", "false")],
        ["Lambda", "", Header, Run + "private bool _started;\npublic Action Start() => () => { [[if (!_started)]] { _started = true; } };", "", E("SetOnce", "_started", "true", "false")],
        ["Operator", "", Header, "private static bool _started;\npublic static Worker operator +(Worker a, Worker b) { [[if (!_started)]] { _started = true; } return a; }", "", E("SetOnce", "_started", "true", "false")],

        // ---- the types of state ----------------------------------------------------------------------------------------------------------
        ["Int", "", Header, "private int _s;\npublic void Go() { [[if (_s == 0)]] { _s = 1; } }", "", E("Transition", "_s", "1", "0")],
        ["Long", "", Header, "private long _s;\npublic void Go() { [[if (_s == 0L)]] { _s = 1L; } }", "", E("Transition", "_s", "1L", "0L")],
        ["Short", "", Header, "private short _s;\npublic void Go() { [[if (_s == 0)]] { _s = 1; } }", "", E("Transition", "_s", "1", "0")],
        ["Byte", "", Header, "private byte _s;\npublic void Go() { [[if (_s == 0)]] { _s = 1; } }", "", E("Transition", "_s", "1", "0")],
        ["Unsigned int", "", Header, "private uint _s;\npublic void Go() { [[if (_s == 0)]] { _s = 1; } }", "", E("Transition", "_s", "1", "0")],
        ["Unsigned short", "", Header, "private ushort _s;\npublic void Go() { [[if (_s == 0)]] { _s = 1; } }", "", E("Transition", "_s", "1", "0")],
        ["String is not a state", "", Header, "private string _s = \"a\";\npublic void Go() { if (_s == \"a\") { _s = \"b\"; } }", "", ""],
        ["Reference null check is left to DSA007", "", Header, "private object _o;\npublic void Go() { if (_o == null) { _o = new object(); } }", "", ""],
        ["Double is not a state", "", Header, "private double _s;\npublic void Go() { if (_s == 0.0) { _s = 1.0; } }", "", ""],

        // ---- what is not a claim ------------------------------------------------------------------------------------------------------------
        ["Same value", "", Header, "private bool _flag;\npublic void Go() { if (_flag) { _flag = true; } }", "", ""],
        ["Value that is not constant", "", Header, "private bool _flag;\nprivate static bool Compute() => true;\npublic void Go() { if (_flag) { _flag = Compute(); } }", "", ""],
        ["Another field assigned", "", Header, "private bool _flag, _other;\npublic void Go() { if (_flag) { _other = false; } }", "", ""],
        ["The field of another object assigned", "", Header, "private bool _flag;\npublic void Go(Worker w) { if (_flag) { w._flag = false; } }", "", ""],
        ["Assigned in a lambda", "", Header, "private bool _flag;\npublic Action Go() { if (!_flag) { return () => { _flag = true; }; } return null; }", "", ""],
        ["Assigned in a local function", "", Header, "private bool _flag;\npublic void Go() { if (!_flag) { void Set() { _flag = true; } Set(); } }", "", ""],
        ["Assigned by a compound operator", "", Header, "private int _n;\npublic void Go() { if (_n == 0) { _n += 1; } }", "", ""],
        ["Not equal, then branch does not say", "", Header, "private int _n;\npublic void Go() { if (_n != 0) { _n = 0; } }", "", ""],
        ["Equal, else branch does not say", "", Header, "private int _n;\npublic void Go() { if (_n == 0) { } else { _n = 1; } }", "", ""],
        ["Compared to a variable", "", Header, "private int _n;\npublic void Go(int v) { if (_n == v) { _n = 1; } }", "", ""],
        ["Compared to a variable, not equal", "", Header, "private bool _b;\npublic void Go(bool v) { if (_b != v) { _b = true; } }", "", ""],
        ["Condition with two operands", "", Header, "private bool _a, _b;\npublic void Go() { if (_a && _b) { _a = false; } }", "", ""],
        ["Condition that is a call", "", Header, "private bool _a;\nprivate bool Check() => true;\npublic void Go() { if (Check()) { _a = false; } }", "", ""],
        ["Condition that is another operator", "", Header, "private int _n;\npublic void Go() { if (_n > 0) { _n = 0; } }", "", ""],
        ["Negated call", "", Header, "private bool _a;\nprivate bool Check() => true;\npublic void Go() { if (!Check()) { _a = true; } }", "", ""],
        ["Property", "", Header, "public bool Started { get; set; }\npublic void Go() { if (!Started) { Started = true; } }", "", ""],
        ["Local variable", "", Header, "public void Go() { var started = false; if (!started) { started = true; } }", "", ""],
        ["Field of another object", "", Header, "public bool Started;\npublic void Go(Worker w) { if (!w.Started) { w.Started = true; } }", "", ""],
        ["Assignment to a name that is not the tested field", "", Header, "private bool _a;\npublic void Go() { if (!_a) { var _a = true; } }", "", ""],
        ["Else with nothing", "", Header, "private bool _a;\npublic void Go() { if (!_a) { Run(); } else { } }\n" + Run, "", ""],
        ["No else", "", Header, "private bool _a;\npublic void Go() { if (!_a) { Run(); } }\n" + Run, "", ""],

        // ---- the contexts ------------------------------------------------------------------------------------------------------------------------
        ["Inside a lock", "", Header, "private bool _started;\nprivate readonly object _g = new object();\npublic void Start() { lock (_g) { if (!_started) { _started = true; } } }", "", ""],
        ["Inside a lock, option off", "", Header, "private bool _started;\nprivate readonly object _g = new object();\npublic void Start() { lock (_g) { [[if (!_started)]] { _started = true; } } }", "skip_inside_lock=false", E("SetOnce", "_started", "true", "false")],
        ["A lambda inside a lock runs later", "", Header, "private bool _started;\nprivate readonly object _g = new object();\npublic Action Start() { lock (_g) { return () => { [[if (!_started)]] { _started = true; } }; } }", "", E("SetOnce", "_started", "true", "false")],
        ["In a constructor", "", Header, "private bool _started;\npublic Worker() { if (!_started) { _started = true; } }", "", ""],
        ["In a constructor, option off", "", Header, "private bool _started;\npublic Worker() { [[if (!_started)]] { _started = true; } }", "skip_constructors=false", E("SetOnce", "_started", "true", "false")],
        ["A lambda in a constructor runs later", "", Header, "private bool _started;\npublic Worker() { Action a = () => { [[if (!_started)]] { _started = true; } }; a(); }", "", E("SetOnce", "_started", "true", "false")],

        // ---- the members that can be called by any thread ------------------------------------------------------------------------------------------------
        ["Private member, field used by it alone", "", Header, "private bool _started;\nprivate void Go() { if (!_started) { _started = true; } }", "", ""],
        ["Private member, field used by another member", "", Header, "private bool _started;\nprivate void Go() { [[if (!_started)]] { _started = true; } }\npublic void Stop() { _started = false; }", "", E("SetOnce", "_started", "true", "false")],
        ["Private member, field used by a constructor only", "", Header, "private bool _started;\nprivate void Go() { if (!_started) { _started = true; } }\npublic Worker() { _started = false; }", "", ""],
        ["Private member, field used by an initializer only", "", Header, "private bool _started = true;\nprivate void Go() { if (!_started) { _started = true; } }", "", ""],
        ["Private member, static field", "", Header, "private static bool _started;\nprivate static void Go() { [[if (!_started)]] { _started = true; } }", "", E("SetOnce", "_started", "true", "false")],
        ["Private member, volatile field", "", Header, "private volatile bool _started;\nprivate void Go() { [[if (!_started)]] { _started = true; } }", "", E("SetOnce", "_started", "true", "false", true)],
        ["Private member, sharing not required", "", Header, "private bool _started;\nprivate void Go() { [[if (!_started)]] { _started = true; } }", "require_shared_access=false", E("SetOnce", "_started", "true", "false")],
        ["Private member, a name that is not the field elsewhere", "", Header, "private bool _started;\nprivate void Go() { if (!_started) { _started = true; } }\npublic void Other() { var _started = false; _started = true; }", "", ""],
        ["Internal member", "", Header, "private bool _started;\ninternal void Go() { [[if (!_started)]] { _started = true; } }", "", E("SetOnce", "_started", "true", "false")],
        ["Protected member", "", Header, "private bool _started;\nprotected void Go() { [[if (!_started)]] { _started = true; } }", "", E("SetOnce", "_started", "true", "false")],

        // ---- the options ------------------------------------------------------------------------------------------------------------------------------------------
        ["Set once refused", "", Header, "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "report_set_once=false", ""],
        ["Set once refused, consume still reported", "", Header, "private bool _p;\npublic void Flush() { [[if (_p)]] { _p = false; } }", "report_set_once=false", E("Consume", "_p", "false", "true")],
        ["Consume refused", "", Header, "private bool _p;\npublic void Flush() { if (_p) { _p = false; } }", "report_consume=false", ""],
        ["Transitions refused", "", Header, "private int _s;\npublic void Go() { if (_s == 0) { _s = 1; } }", "report_state_transitions=false", ""],
        ["Volatile fields refused", "", Header, "private volatile bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "report_volatile_fields=false", ""],
        ["Volatile fields refused, plain ones still reported", "", Header, "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; } }", "report_volatile_fields=false", E("SetOnce", "_started", "true", "false")],
        ["Excluded type name", "", Header, "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "excluded_type_name_patterns=Work*", ""],
        ["Type name not excluded", "", Header, "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; } }", "excluded_type_name_patterns=Other*", E("SetOnce", "_started", "true", "false")],
        ["Excluded namespace", "", Header, "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "excluded_namespace_patterns=TestApp", ""],
        ["Namespace not excluded", "", Header, "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; } }", "excluded_namespace_patterns=Other.*", E("SetOnce", "_started", "true", "false")],
        ["Excluded base type", "public class BaseWorker { }", "public class Worker : BaseWorker", "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "excluded_base_types=TestApp.BaseWorker", ""],
        ["Base type not excluded", "public class BaseWorker { }", "public class Worker : BaseWorker", "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; } }", "excluded_base_types=Other.Base", E("SetOnce", "_started", "true", "false")],
        ["Excluded field name", "", Header, "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "excluded_field_name_patterns=_sta*", ""],
        ["Field name not excluded", "", Header, "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; } }", "excluded_field_name_patterns=_x*", E("SetOnce", "_started", "true", "false")],
        ["Excluded member name", "", Header, "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "excluded_member_name_patterns=Sta*", ""],
        ["Member name not excluded", "", Header, "private bool _started;\npublic void Start() { [[if (!_started)]] { _started = true; } }", "excluded_member_name_patterns=Run*", E("SetOnce", "_started", "true", "false")],
        ["Excluded file", "", Header, "private bool _started;\npublic void Start() { if (!_started) { _started = true; } }", "excluded_file_patterns=*.cs", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenarios(string title, string pre, string header, string members, string settings, string expected)
    {
        await RunAsync(pre, header, members, settings, expected).ConfigureAwait(false);
    }
}
