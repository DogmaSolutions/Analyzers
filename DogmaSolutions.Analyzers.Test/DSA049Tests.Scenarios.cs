using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA049Tests
{
    private const string Gate = "private readonly object _gate = new object();\n";
    private const string Counter = Gate + "private int _count;\npublic void Inc() { lock (_gate) { _count++; } }\n";

    // title, types declared before the class, class header, members ([[...]] marks the expected access, in order), settings (key=value;...), expected ("Scenario~field~guard~example~held;...")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the messages: one per scenario ------------------------------------------------------------------------
        ["Unlocked write", "", Header, Counter + "public void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Unlocked read", "", Header, Counter + "public int Count => [[_count]];", "", E("UnlockedRead", "_count", "_gate", "Inc")],
        ["Write under a different lock", "", Header, Counter + "private readonly object _other = new object();\npublic void Alt() { lock (_other) { [[_count]] = 1; } }", "", E("DifferentLocks", "_count", "_gate", "Inc", "_other")],
        ["Several unlocked accesses", "", Header, Counter + "public void Reset() { [[_count]] = 0; }\npublic int Count => [[_count]];", "", E("UnlockedWrite", "_count", "_gate", "Inc") + ";" + E("UnlockedRead", "_count", "_gate", "Inc")],

        // ---- what is a write -------------------------------------------------------------------------------------------------
        ["Compound assignment", "", Header, Counter + "public void Add() { [[_count]] += 2; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Increment", "", Header, Counter + "public void Add() { [[_count]]++; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Pre decrement", "", Header, Counter + "public void Add() { --[[_count]]; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Assignment through this", "", Header, Counter + "public void Reset() { this.[[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Parenthesized assignment", "", Header, Counter + "public void Reset() { ([[_count]]) = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Tuple assignment", "", Header, Counter + "private int _other;\npublic void Reset() { ([[_count]], _other) = (0, 1); }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Read in an argument", "", Header, Counter + "public void Show() { Console.WriteLine([[_count]]); }", "", E("UnlockedRead", "_count", "_gate", "Inc")],
        ["Read in the right side of an assignment", "", Header, Counter + "public void Copy(Worker w) { w.[[_count]] = 0; var x = [[_count]]; }", "", E("UnlockedWrite", "_count", "_gate", "Inc") + ";" + E("UnlockedRead", "_count", "_gate", "Inc")],

        // ---- how the lock is written ------------------------------------------------------------------------------------------------
        ["Lock on this", "", Header, "private int _count;\npublic void Inc() { lock (this) { _count++; } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "this", "Inc")],
        ["Lock through this", "", Header, Gate + "private int _count;\npublic void Inc() { lock (this._gate) { _count++; } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Parenthesized lock", "", Header, Gate + "private int _count;\npublic void Inc() { lock ((_gate)) { _count++; } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Lock on a property", "", Header, "private object Gate { get; } = new object();\nprivate int _count;\npublic void Inc() { lock (Gate) { _count++; } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "Gate", "Inc")],
        ["Lock on another expression", "", Header, "private int _count;\nprivate static object Get() => new object();\npublic void Inc() { lock (Get()) { _count++; } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "Get()", "Inc")],
        ["Static field and static lock", "", Header, "private static readonly object G = new object();\nprivate static int _total;\npublic static void Inc() { lock (G) { _total++; } }\npublic static void Reset() { [[_total]] = 0; }", "", E("UnlockedWrite", "_total", "G", "Inc")],
        ["Nested locks hold both", "", Header, Gate + "private readonly object _b = new object();\nprivate int _count;\npublic void Inc() { lock (_gate) { lock (_b) { _count++; } } }\npublic void Dec() { lock (_b) { lock (_gate) { _count--; } } }\npublic void Reset() { lock (_gate) { lock (_b) { _count = 0; } } }", "", ""],
        ["The most used lock guards", "", Header, Gate + "private readonly object _b = new object();\nprivate int _count;\npublic void A() { lock (_gate) { _count++; } }\npublic void B() { lock (_gate) { _count++; } }\npublic void C() { lock (_b) { [[_count]]++; } }", "", E("DifferentLocks", "_count", "_gate", "A", "_b")],
        ["The first lock in alphabetical order guards when tied", "", Header, "private readonly object _b = new object();\nprivate readonly object _a = new object();\nprivate int _count;\npublic void X() { lock (_b) { [[_count]]++; } }\npublic void Y() { lock (_a) { _count++; } }\npublic void Z() { [[_count]]++; }", "", E("DifferentLocks", "_count", "_a", "Y", "_b") + ";" + E("UnlockedWrite", "_count", "_a", "Y")],
        ["Lock in a lambda", "", Header, Gate + "private int _count;\npublic Action Inc() => () => { lock (_gate) { _count++; } };\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Lambda inside a lock", "", Header, Counter + "public void Each(Action<int> a) { lock (_gate) { Action x = () => { _count = 0; }; x(); } }", "", ""],
        ["Local function inside a lock", "", Header, Counter + "public void Each() { lock (_gate) { void Zero() { _count = 0; } Zero(); } }", "", ""],

        // ---- the members --------------------------------------------------------------------------------------------------------------
        ["Locked write in a setter", "", Header, Gate + "private int _count;\npublic int Count { get { lock (_gate) { return _count; } } set { lock (_gate) { _count = value; } } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Count")],
        ["Locked write in an indexer", "", Header, Gate + "private int _count;\npublic int this[int i] { set { lock (_gate) { _count = value; } } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "this[]")],
        ["Unlocked read in an indexer", "", Header, Counter + "public int this[int i] { get { return [[_count]]; } }", "", E("UnlockedRead", "_count", "_gate", "Inc")],
        ["Locked write in an operator", "", Header, "private static readonly object G = new object();\nprivate static int _total;\npublic static Worker operator +(Worker a, Worker b) { lock (G) { _total++; } return a; }\npublic static void Reset() { [[_total]] = 0; }", "", E("UnlockedWrite", "_total", "G", "OperatorDeclaration")],
        ["Unlocked access in a lambda", "", Header, Counter + "public Action Reset() => () => { [[_count]] = 0; };", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Unlocked access in a local function", "", Header, Counter + "public void Reset() { void Zero() { [[_count]] = 0; } Zero(); }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],

        // ---- the accesses that are not part of the protocol ---------------------------------------------------------------------------
        ["Constructor", "", Header, Counter + "public Worker() { _count = 1; }", "", ""],
        ["Static constructor", "", Header, Gate + "private static int _total;\npublic static void Inc() { lock (new object()) { _total++; } }\nstatic Worker() { _total = 1; }", "", ""],
        ["Field initializer", "", Header, Gate + "private int _count = 1;\npublic void Inc() { lock (_gate) { _count++; } }", "", ""],
        ["Initializer that reads another field", "", Header, Gate + "private static int _seed;\nprivate static int _total = _seed;\npublic static void Inc() { lock (new object()) { _seed++; } }", "", ""],
        ["Dispose", "", Header, Counter + "public void Dispose() { _count = 0; }", "", ""],
        ["Finalizer", "", Header, Counter + "~Worker() { _count = 0; }", "", ""],
        ["Dispose with a parameter", "", Header, Counter + "public void Dispose(bool disposing) { _count = 0; }", "", ""],

        // ---- the fields that are not reported --------------------------------------------------------------------------------------------
        ["Readonly field", "", Header, Gate + "private readonly int _fixed = 1;\npublic int Get() { lock (_gate) { return _fixed; } }\npublic int Get2() { return _fixed; }", "", ""],
        ["Constant", "", Header, Gate + "private const int Max = 1;\npublic int Get() { lock (_gate) { return Max; } }\npublic int Get2() { return Max; }", "", ""],
        ["Thread static field", "", Header, "[ThreadStatic] private static int _local;\nprivate static readonly object G = new object();\npublic static void Inc() { lock (G) { _local++; } }\npublic static void Reset() { _local = 0; }", "", ""],
        ["No locked write", "", Header, Gate + "private int _count;\npublic int Get() { lock (_gate) { return _count; } }\npublic void Reset() { _count = 0; }", "", ""],
        ["Only locked accesses", "", Header, Gate + "private int _count;\npublic void Inc() { lock (_gate) { _count++; } }\npublic int Get() { lock (_gate) { return _count; } }", "", ""],
        ["Only unlocked accesses", "", Header, Gate + "private int _count;\npublic void Inc() { _count++; }\npublic int Get() { lock (_gate) { return 1; } }", "", ""],
        ["Read under another lock", "", Header, Counter + "private readonly object _other = new object();\npublic int Get() { lock (_other) { return _count; } }", "", ""],
        ["Handed to Interlocked elsewhere", "", Header, Counter + "public void Hit() { Interlocked.Increment(ref _count); }\npublic void Reset() { _count = 0; }", "", ""],
        ["Handed by out elsewhere", "", Header, Counter + "public void Parse() { int.TryParse(\"1\", out _count); }\npublic void Reset() { _count = 0; }", "", ""],
        ["Volatile field, unlocked write", "", Header, Gate + "private volatile bool _stop;\npublic void Stop() { lock (_gate) { _stop = true; } }\npublic void Kill() { [[_stop]] = true; }", "", E("UnlockedWrite", "_stop", "_gate", "Stop")],
        ["Volatile field, unlocked read", "", Header, Gate + "private volatile bool _stop;\npublic void Stop() { lock (_gate) { _stop = true; } }\npublic bool IsStopped => _stop;", "", ""],
        ["A local with the name of the field", "", Header, Counter + "public int Get() { var _count = 1; return _count; }", "", ""],
        ["The field of another type", "", Header, Counter + "private class Other { public int _count; }\nprivate int Get(Other o) { return o._count; }", "", ""],
        ["A type without a lock", "", Header, "private int _count;\npublic void Inc() { _count++; }\npublic int Get() => _count;", "", ""],
        ["A struct", "", "public struct Worker", "private object _gate;\nprivate int _count;\npublic void Inc() { lock (_gate) { _count++; } }\npublic void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["A nested type is a type of its own", "", Header, Counter + "private class Inner { private int _count; public void Reset() { _count = 0; } }", "", ""],
        ["A nested type with its own locks", "", Header, "private class Inner { private readonly object _g = new object(); private int _n; public void Inc() { lock (_g) { _n++; } } public void Reset() { [[_n]] = 0; } }", "", E("UnlockedWrite", "_n", "_g", "Inc")],
        ["A generic type", "", "public class Worker<T>", Counter + "public void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["A record", "", "public record Worker", Counter + "public void Reset() { [[_count]] = 0; }", "", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["An interface is not a class", "", "public interface IWorker", "int Get();", "", ""],

        // ---- the private helpers ------------------------------------------------------------------------------------------------------------
        ["Helper called under the lock", "", Header, Gate + "private int _count;\nprivate void Bump() { _count++; }\npublic void Inc() { lock (_gate) { Bump(); } }", "", ""],
        ["Helper called through this", "", Header, Gate + "private int _count;\nprivate void Bump() { _count++; }\npublic void Inc() { lock (_gate) { this.Bump(); } }", "", ""],
        ["Helper of a helper", "", Header, Gate + "private int _count;\nprivate void Bump() { _count++; }\nprivate void Twice() { Bump(); Bump(); }\npublic void Inc() { lock (_gate) { Twice(); } }", "", ""],
        ["Helper called under the lock and without it", "", Header, Gate + "private int _count;\nprivate void Bump() { [[_count]]++; }\npublic void Inc() { lock (_gate) { Bump(); } }\npublic void Skip() { Bump(); }\npublic void Set() { lock (_gate) { _count = 1; } }", "", E("UnlockedWrite", "_count", "_gate", "Set")],
        ["Helper called under two different locks", "", Header, Gate + "private readonly object _b = new object();\nprivate int _count;\nprivate void Bump() { [[_count]]++; }\npublic void A() { lock (_gate) { Bump(); } }\npublic void B() { lock (_b) { Bump(); } }\npublic void Set() { lock (_gate) { _count = 1; } }", "", E("UnlockedWrite", "_count", "_gate", "Set")],
        ["Helper that is never called", "", Header, Gate + "private int _count;\nprivate void Bump() { [[_count]]++; }\npublic void Set() { lock (_gate) { _count = 1; } }", "", E("UnlockedWrite", "_count", "_gate", "Set")],
        ["Helper used as a method group", "", Header, Gate + "private int _count;\nprivate void Bump() { [[_count]]++; }\npublic void Inc() { lock (_gate) { Bump(); } }\npublic Action Get() => Bump;\npublic void Set() { lock (_gate) { _count = 1; } }", "", E("UnlockedWrite", "_count", "_gate", "Set")],
        ["Public helper is not inferred", "", Header, Gate + "private int _count;\npublic void Bump() { [[_count]]++; }\npublic void Inc() { lock (_gate) { Bump(); } }\npublic void Set() { lock (_gate) { _count = 1; } }", "", E("UnlockedWrite", "_count", "_gate", "Set")],
        ["Private method of the outer type", "", Header, "private static void Help() { }\npublic class Inner { private readonly object _g = new object(); private int _n; public void A() { lock (_g) { _n++; Help(); } } public void B() { [[_n]] = 0; } }", "", E("UnlockedWrite", "_n", "_g", "A")],
        ["Helper inference refused", "", Header, Gate + "private int _count;\nprivate void Bump() { [[_count]]++; }\npublic void Inc() { lock (_gate) { Bump(); } }\npublic void Set() { lock (_gate) { _count = 1; } }", "infer_locks_of_private_helpers=false", E("UnlockedWrite", "_count", "_gate", "Set")],

        // ---- the options -----------------------------------------------------------------------------------------------------------------------
        ["Unlocked writes refused", "", Header, Counter + "public void Reset() { _count = 0; }", "report_unlocked_writes=false", ""],
        ["Unlocked writes refused, reads still reported", "", Header, Counter + "public void Reset() { _count = 0; }\npublic int Get() => [[_count]];", "report_unlocked_writes=false", E("UnlockedRead", "_count", "_gate", "Inc")],
        ["Unlocked reads refused", "", Header, Counter + "public int Get() => _count;", "report_unlocked_reads=false", ""],
        ["Unlocked reads refused, writes still reported", "", Header, Counter + "public void Reset() { [[_count]] = 0; }\npublic int Get() => _count;", "report_unlocked_reads=false", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Different locks refused", "", Header, Counter + "private readonly object _other = new object();\npublic void Alt() { lock (_other) { _count = 1; } }", "report_different_locks=false", ""],
        ["Excluded type name", "", Header, Counter + "public void Reset() { _count = 0; }", "excluded_type_name_patterns=Work*", ""],
        ["Type name not excluded", "", Header, Counter + "public void Reset() { [[_count]] = 0; }", "excluded_type_name_patterns=Other*", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Excluded namespace", "", Header, Counter + "public void Reset() { _count = 0; }", "excluded_namespace_patterns=TestApp", ""],
        ["Namespace not excluded", "", Header, Counter + "public void Reset() { [[_count]] = 0; }", "excluded_namespace_patterns=Other.*", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Excluded base type", "public class BaseWorker { }", "public class Worker : BaseWorker", Counter + "public void Reset() { _count = 0; }", "excluded_base_types=TestApp.BaseWorker", ""],
        ["Base type not excluded", "public class BaseWorker { }", "public class Worker : BaseWorker", Counter + "public void Reset() { [[_count]] = 0; }", "excluded_base_types=Other.Base", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Excluded field name", "", Header, Counter + "public void Reset() { _count = 0; }", "excluded_field_name_patterns=_cou*", ""],
        ["Field name not excluded", "", Header, Counter + "public void Reset() { [[_count]] = 0; }", "excluded_field_name_patterns=_x*", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Excluded member name", "", Header, Counter + "public void Reset() { _count = 0; }", "excluded_member_name_patterns=Res*", ""],
        ["Member name not excluded", "", Header, Counter + "public void Reset() { [[_count]] = 0; }", "excluded_member_name_patterns=Other*", E("UnlockedWrite", "_count", "_gate", "Inc")],
        ["Excluded file", "", Header, Counter + "public void Reset() { _count = 0; }", "excluded_file_patterns=*.cs", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenarios(string title, string pre, string header, string members, string settings, string expected)
    {
        await RunAsync(pre, header, members, settings, expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task The_lock_and_the_access_can_be_in_different_files_of_a_partial_class()
    {
        var source = BuildSource(string.Empty, "public partial class Worker", Gate + "private int _count;\npublic void Inc() { lock (_gate) { _count++; } }", expectDiagnostics: false);
        var test = CreateTest(source, string.Empty, string.Empty);
        test.TestState.Sources.Add(("Other.cs", "namespace TestApp\n{\n    public partial class Worker\n    {\n        public void Reset() { {|#0:_count|} = 0; }\n    }\n}"));
        test.ExpectedDiagnostics.Add(new Microsoft.CodeAnalysis.Testing.DiagnosticResult(DSA049Analyzer.DiagnosticId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning).WithLocation(0).WithMessage(string.Format(
            Resources.Culture, Resources.DSA049MessageUnlockedWrite, "_count", "_gate", "Inc", string.Empty)));

        await test.RunAsync().ConfigureAwait(false);
    }
}
