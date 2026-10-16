using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA044Tests
{
    private const string Header = "public class Service";
    private const string Run = "public void Run() { [[Console.WriteLine(\"hello\")]]; }";

    // title, types declared before the class, class header, members ([[...]] marks the expected call), settings (key=value;...), is it reported
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the logger technologies ------------------------------------------------------------------------------
        ["Microsoft ILogger<T> field", "", Header, "private readonly ILogger<Service> _l;\n" + Run, "", true],
        ["Microsoft ILogger property", "", Header, "public ILogger L { get; set; }\n" + Run, "", true],
        ["Logger property without a backing field", "", Header, "public ILogger L => null;\n" + Run, "", true],
        ["Microsoft ILoggerFactory field", "", Header, "private ILoggerFactory _f;\n" + Run, "", true],
        ["Serilog ILogger field", "", Header, "private Serilog.ILogger _l;\n" + Run, "", true],
        ["NLog Logger field (through its interface)", "", Header, "private NLog.Logger _l;\n" + Run, "", true],
        ["log4net ILog field", "", Header, "private log4net.ILog _l;\n" + Run, "", true],
        ["Castle ILogger field", "", Header, "private Castle.Core.Logging.ILogger _l;\n" + Run, "", true],
        ["Common.Logging ILog field", "", Header, "private Common.Logging.ILog _l;\n" + Run, "", true],
        ["NServiceBus ILog field", "", Header, "private NServiceBus.Logging.ILog _l;\n" + Run, "", true],
        ["Akka ILoggingAdapter field", "", Header, "private Akka.Event.ILoggingAdapter _l;\n" + Run, "", true],
        ["TraceSource field", "", Header, "private TraceSource _l;\n" + Run, "", true],
        ["No logger", "", Header, "private int _x;\n" + Run, "", false],
        ["A type that is not a logger", "", Header, "private Other.NotALogger _l;\n" + Run, "", false],
        ["Logger type added by logger_types", "", Header, "private Mine.IMyLog _l;\n" + Run, "logger_types=Mine.IMyLog", true],
        ["Logger namespace added by logger_namespaces", "", Header, "private Mine.IMyLog _l;\n" + Run, "logger_namespaces=Mine", true],
        ["Default logger types replaced", "", Header, "private Serilog.ILogger _l;\n" + Run, "logger_types=Mine.IMyLog", false],

        // ---- where the logger comes from --------------------------------------------------------------------------
        ["Inherited protected logger", "public class Base { protected ILogger Log; }", Header + " : Base", Run, "", true],
        ["Inherited logger property", "public class Base { protected ILogger Log => null; }", Header + " : Base", Run, "", true],
        ["Inherited private logger", "public class Base { private ILogger Log; }", Header + " : Base", Run, "", false],
        ["Inherited logger refused", "public class Base { protected ILogger Log; }", Header + " : Base", Run, "accept_inherited_logger=false", false],
        ["Parameter of the method", "", Header, "public void Run(ILogger logger) { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Parameter of the method refused", "", Header, "public void Run(ILogger logger) { [[Console.WriteLine(\"hello\")]]; }", "accept_logger_parameters=false", false],
        ["Parameter of the lambda", "", Header, "public void Run() { Action<ILogger> a = logger => [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Parameter of the method used in a lambda", "", Header, "public void Run(ILogger logger) { Action a = () => [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Parameter of the method used in a local function", "", Header, "public void Run(ILogger logger) { void Local() { [[Console.WriteLine(\"hello\")]]; } Local(); }", "", true],
        ["Parameter of another method", "", Header, "public void Other(ILogger logger) { }\n" + Run, "", false],
        ["Parameter of the constructor, used in the constructor", "", Header, "public Service(ILogger logger) { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Parameter of the constructor, used in another method", "", Header, "public Service(ILogger logger) { }\n" + Run, "", false],
        ["Primary constructor parameter", "", Header + "(ILogger<Service> logger)", Run, "", true],
        ["Primary constructor parameter not a logger", "", Header + "(int count)", Run, "", false],
        ["Static logger already called in the class", "", Header, "public void Other() { Serilog.Log.Information(\"x\"); }\n" + Run, "", true],
        ["Static logger refused", "", Header, "public void Other() { Serilog.Log.Information(\"x\"); }\n" + Run, "accept_static_logger_calls=false", false],
        ["Static logger types replaced", "", Header, "public void Other() { Serilog.Log.Information(\"x\"); }\n" + Run, "static_logger_types=Other.Log", false],
        ["A call that is not to a static logger", "", Header, "public void Elsewhere() { Other.Log.Information(\"x\"); }\n" + Run, "", false],
        ["Static logger in the second part of a partial class", "", "public partial class Service", "public void Other() { Serilog.Log.Information(\"x\"); }\n" + Run, "", true],

        // ---- static members ---------------------------------------------------------------------------------------
        ["Instance logger in a static method", "", Header, "private ILogger _l;\npublic static void Run() { [[Console.WriteLine(\"hello\")]]; }", "", false],
        ["Instance logger in a static method, accessibility not required", "", Header, "private ILogger _l;\npublic static void Run() { [[Console.WriteLine(\"hello\")]]; }", "require_accessible_logger=false", true],
        ["Static logger in a static method", "", Header, "private static ILogger _l;\npublic static void Run() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Primary constructor parameter in a static method", "", Header + "(ILogger logger)", "public static void Run() { [[Console.WriteLine(\"hello\")]]; }", "", false],
        ["Logger in a field initializer", "", Header, "private ILogger _l;\nprivate Action _a = () => [[Console.WriteLine(\"hello\")]];", "", true],
        ["Logger in a property accessor", "", Header, "private ILogger _l;\npublic int P { get { [[Console.WriteLine(\"hello\")]]; return 1; } }", "", true],

        // ---- which calls ------------------------------------------------------------------------------------------
        ["Console.Write", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Write(\"hello\")]]; }", "", true],
        ["Console.WriteLine with a format", "", Header, "private ILogger _l;\npublic void Run(int x) { [[Console.WriteLine(\"hello {0}\", x)]]; }", "", true],
        ["Console.WriteLine without arguments", "", Header, "private ILogger _l;\npublic void Run() { [[Console.WriteLine()]]; }", "", true],
        ["Debug.WriteLine", "", Header, "private ILogger _l;\npublic void Run() { [[Debug.WriteLine(\"hello\")]]; }", "", true],
        ["Debug.Write", "", Header, "private ILogger _l;\npublic void Run() { [[Debug.Write(\"hello\")]]; }", "", true],
        ["Debug.WriteIf", "", Header, "private ILogger _l;\npublic void Run() { [[Debug.WriteIf(true, \"hello\")]]; }", "", true],
        ["Debug.WriteLineIf", "", Header, "private ILogger _l;\npublic void Run() { [[Debug.WriteLineIf(true, \"hello\")]]; }", "", true],
        ["Console.Error.WriteLine", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Error.WriteLine(\"hello\")]]; }", "", true],
        ["Console.Out.Write", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Out.Write(\"hello\")]]; }", "", true],
        ["Console streams not included", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Error.WriteLine(\"hello\")]]; }", "include_console_streams=false", false],
        ["Console streams need Console among the flagged types", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Error.WriteLine(\"hello\")]]; }", "flagged_types=System.Diagnostics.Debug", false],
        ["A writer that is not a console stream", "", Header, "private ILogger _l;\npublic void Run(System.IO.TextWriter w) { w.WriteLine(\"hello\"); w.Write(\"x\"); }", "", false],
        ["A property of another type in the receiver", "", Header, "private ILogger _l;\npublic void Run(Holder h) { h.Out.WriteLine(\"hello\"); }\npublic class Holder { public System.IO.TextWriter Out { get; } }", "", false],
        ["A member of the console that is not a write", "", Header, "private ILogger _l;\npublic void Run() { Console.Beep(); Debug.Assert(true); Debug.Flush(); }", "", false],
        ["Using static Console", "using static System.Console;", Header, "private ILogger _l;\npublic void Run() { [[WriteLine(\"hello\")]]; }", "", true],
        ["Trace is not flagged by default", "", Header, "private ILogger _l;\npublic void Run() { Trace.WriteLine(\"hello\"); }", "", false],
        ["Trace flagged by flagged_types", "", Header, "private ILogger _l;\npublic void Run() { [[Trace.WriteLine(\"hello\")]]; }", "flagged_types=System.Console,System.Diagnostics.Trace", true],
        ["Console not flagged when flagged_types replaced", "", Header, "private ILogger _l;\npublic void Run() { Console.WriteLine(\"hello\"); }", "flagged_types=System.Diagnostics.Debug", false],
        ["Members replaced by flagged_members", "", Header, "private ILogger _l;\npublic void Run() { Console.Write(\"a\"); [[Console.WriteLine(\"b\")]]; }", "flagged_members=WriteLine", true],
        ["A call that is not named like a flagged member", "", Header, "private ILogger _l;\npublic void Run() { Console.ReadLine(); }", "", false],
        ["A call whose callee is not a name", "", Header, "private ILogger _l;\npublic void Run(Action[] a) { a[0](); }", "", false],

        // ---- exclusions -------------------------------------------------------------------------------------------
        ["Class named Program", "", "public class Program", "private ILogger _l;\npublic void Run() { Console.WriteLine(\"hello\"); }", "", false],
        ["Method named Main", "", Header, "private static ILogger _l;\npublic static void Main() { Console.WriteLine(\"hello\"); }", "", false],
        ["Type exclusion list replaced", "", "public class Program", "private ILogger _l;\npublic void Run() { [[Console.WriteLine(\"hello\")]]; }", "excluded_type_name_patterns=*Cli", true],
        ["Excluded by type name pattern", "", "public class ToolCli", "private ILogger _l;\npublic void Run() { Console.WriteLine(\"hello\"); }", "excluded_type_name_patterns=*Cli", false],
        ["Excluded by member name pattern", "", Header, "private ILogger _l;\npublic void PrintReport() { Console.WriteLine(\"hello\"); }", "excluded_member_name_patterns=Print*", false],
        ["Excluded by namespace pattern", "", Header, "private ILogger _l;\npublic void Run() { Console.WriteLine(\"hello\"); }", "excluded_namespace_patterns=TestApp", false],
        ["Namespace pattern not matching", "", Header, "private ILogger _l;\npublic void Run() { [[Console.WriteLine(\"hello\")]]; }", "excluded_namespace_patterns=*.Other", true],
        ["Interactive member", "", Header, "private ILogger _l;\npublic void Run() { Console.Write(\"name? \"); var name = Console.ReadLine(); }", "", false],
        ["Interactive member refused", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Write(\"name? \")]]; var name = Console.ReadLine(); }", "skip_interactive_members=false", true],
        ["Console.Read in another member", "", Header, "private ILogger _l;\npublic void Ask() { Console.ReadLine(); }\npublic void Run() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["A read that is not of the console", "", Header, "private ILogger _l;\npublic void Run(System.IO.TextReader r) { r.ReadLine(); [[Console.WriteLine(\"hello\")]]; }", "", true],

        // ---- the code decided what to do without a logger -----------------------------------------------------------------------
        ["Else of a logger != null", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) logger.LogError(\"Failed {Name}\", \"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of a logger != null, with braces", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { logger.LogError(\"x\"); } else { Console.WriteLine(e.Message); } }", "", false],
        ["Then of a logger == null", "", Header, "public void Run(ILogger logger, Exception e) { if (logger == null) Console.WriteLine(e.Message); else logger.LogError(\"x\"); }", "", false],
        ["Then of a logger == null, no else", "", Header, "public void Run(ILogger logger, Exception e) { if (logger == null) { Console.WriteLine(e.Message); return; } logger.LogError(\"x\"); }", "", false],
        ["Then of null == logger", "", Header, "public void Run(ILogger logger, Exception e) { if (null == logger) Console.WriteLine(e.Message); }", "", false],
        ["Then of logger is null", "", Header, "public void Run(ILogger logger, Exception e) { if (logger is null) Console.WriteLine(e.Message); }", "", false],
        ["Else of logger is not null", "", Header, "public void Run(ILogger logger, Exception e) { if (logger is not null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of logger is a type", "", Header, "public void Run(ILogger logger, Exception e) { if (logger is ILogger) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of logger is a declaration", "", Header, "public void Run(ILogger logger, Exception e) { if (logger is ILogger l) l.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of logger is a property pattern", "", Header, "public void Run(ILogger logger, Exception e) { if (logger is {}) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of a negated null test", "", Header, "public void Run(ILogger logger, Exception e) { if (!(logger == null)) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Then of a negated not-null test", "", Header, "public void Run(ILogger logger, Exception e) { if (!(logger != null)) Console.WriteLine(e.Message); }", "", false],
        ["Else of a test with and", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null && e != null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Then of a test with or", "", Header, "public void Run(ILogger logger, Exception e) { if (logger == null || e == null) Console.WriteLine(\"x\"); }", "", false],
        ["Else of the conditional access of a logger", "", Header, "public void Run(ILogger logger, Exception e) { if (logger?.ToString() != null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of the conditional access of a logger, pattern", "", Header, "public void Run(ILogger logger, Exception e) { if (logger?.ToString() is not null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Else of a negated conditional access", "", Header, "public void Run(ILogger logger, Exception e) { if (!(logger?.ToString() == null)) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["After a guard that returns", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { logger.LogError(\"x\"); return; } Console.WriteLine(e.Message); }", "", false],
        ["After a guard that returns, without braces", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) return; Console.WriteLine(e.Message); }", "", false],
        ["After a guard that throws", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) throw new InvalidOperationException(); Console.WriteLine(e.Message); }", "", false],
        ["After a guard that continues", "", Header, "public void Run(ILogger logger, Exception e) { foreach (var i in new[] { 1 }) { if (logger != null) { logger.LogError(\"x\"); continue; } Console.WriteLine(e.Message); } }", "", false],
        ["After a guard that breaks", "", Header, "public void Run(ILogger logger, Exception e) { foreach (var i in new[] { 1 }) { if (logger != null) break; Console.WriteLine(e.Message); } }", "", false],
        ["Deeper than the guard", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) return; try { Console.WriteLine(e.Message); } catch (Exception) { } }", "", false],
        ["In an else if chain", "", Header, "public void Run(ILogger logger, Exception e) { if (e == null) { } else if (logger != null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["In the false part of a conditional expression", "", Header, "public void Run(ILogger logger, Exception e) { Action a = logger != null ? () => logger.LogError(\"x\") : () => Console.WriteLine(\"x\"); }", "", false],
        ["In the true part of a negated conditional expression", "", Header, "public void Run(ILogger logger, Exception e) { Action a = logger == null ? () => Console.WriteLine(\"x\") : () => logger.LogError(\"x\"); }", "", false],
        ["Field logger, through this", "", Header, "private ILogger _l;\npublic void Run(Exception e) { if (this._l != null) _l.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Typed logger field", "", Header, "private ILogger<Service> _l;\npublic void Run(Exception e) { if (_l == null) Console.WriteLine(e.Message); }", "", false],
        ["Debug write in the else", "", Header, "private ILogger _l;\npublic void Run(Exception e) { if (_l != null) _l.LogError(\"x\"); else Debug.WriteLine(e.Message); }", "", false],
        ["Console.Error in the else", "", Header, "private ILogger _l;\npublic void Run(Exception e) { if (_l != null) _l.LogError(\"x\"); else Console.Error.WriteLine(e.Message); }", "", false],
        ["Local function in the else", "", Header, "private ILogger _l;\npublic void Run(Exception e) { if (_l != null) _l.LogError(\"x\"); else Show(); void Show() { [[Console.WriteLine(e.Message)]]; } }", "", true],
        // the decision does not make a write deliberate when it is not about the logger or when it is not the branch without a logger
        ["In the branch with the logger", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { logger.LogError(\"x\"); [[Console.WriteLine(e.Message)]]; } }", "", true],
        ["In the else of a null test", "", Header, "public void Run(ILogger logger, Exception e) { if (logger == null) logger.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["In the then of a not-null test", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of something else than the logger", "", Header, "public void Run(ILogger logger, Exception e) { if (e != null) logger.LogError(\"x\"); else [[Console.WriteLine(\"x\")]]; }", "", true],
        ["After a null guard that returns", "", Header, "public void Run(ILogger logger, Exception e) { if (logger == null) return; [[Console.WriteLine(e.Message)]]; }", "", true],
        ["After a guard that does not exit", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { logger.LogError(\"x\"); } [[Console.WriteLine(e.Message)]]; }", "", true],
        ["After a guard that has an else", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { return; } else { logger.LogError(\"x\"); } [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Before the guard", "", Header, "public void Run(ILogger logger, Exception e) { [[Console.WriteLine(e.Message)]]; if (logger != null) { logger.LogError(\"x\"); return; } }", "", true],
        ["Guard of another member", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { return; } }\npublic void Other(ILogger logger, Exception e) { [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Empty guard", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { } [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of a logger returned by a call", "", Header, "public void Run(ILogger logger, Exception e) { Func<ILogger> other = () => null; if (other() != null) logger.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of the logger of another object", "", Header, "private ILogger _l;\npublic void Run(Service other, Exception e) { if (other._l != null) other._l.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Own logger tested, private logger of the base class ignored", "public class Base { private ILogger L; public void Use() { if (L != null) L.LogError(\"x\"); } }", "public class Service : Base", "private ILogger _l;\npublic void Run(Exception e) { if (_l != null) _l.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Test of a local variable that holds nothing", "", Header, "public void Run(ILogger logger, Exception e) { ILogger local = null; if (local != null) local.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of a local variable that copies the logger", "", Header, "public void Run(ILogger logger, Exception e) { var copy = logger; if (copy != null) copy.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Test of a local variable that copies a field", "", Header, "private ILogger _l;\npublic void Run(Exception e) { ILogger copy = this._l; if (copy == null) Console.WriteLine(e.Message); }", "", false],
        ["Test of a copy of a copy", "", Header, "public void Run(ILogger logger, Exception e) { var a = logger; var b = (a); if (b != null) b.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Test of a copy that is too far", "", Header, "public void Run(ILogger logger, Exception e) { var a = logger; var b = a; var c = b; var d = c; if (d != null) d.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of a copy of something else", "", Header, "public void Run(ILogger logger, Exception e) { var copy = Get(); if (copy != null) copy.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }\nprivate static ILogger Get() => null;", "", true],
        ["Test of a local without an initializer", "", Header, "public void Run(ILogger logger, Exception e) { ILogger copy; copy = logger; if (copy != null) copy.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of another available logger parameter", "", Header, "public void Run(ILogger a, ILogger b, Exception e) { if (b != null) b.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Test of an inherited logger", "public class Base { protected ILogger L; }", "public class Service : Base", "public void Run(Exception e) { if (L != null) L.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Test of an inherited property", "public class Base { protected ILogger L { get; set; } }", "public class Service : Base", "public void Run(Exception e) { if (L == null) Console.WriteLine(e.Message); }", "", false],
        ["Instance logger in a static member: not available, not reported", "", Header, "private ILogger _l;\npublic static void Run(Exception e) { [[Console.WriteLine(e.Message)]]; }", "", false],
        ["Test of a static logger in a static member", "", Header, "private static ILogger _l;\npublic static void Run(Exception e) { if (_l != null) _l.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Test of a parameter of a lambda", "", Header, "public void Run(Exception e) { Action<ILogger> a = l => { if (l != null) l.LogError(\"x\"); else Console.WriteLine(e.Message); }; }", "", false],
        ["Test of a parameter of the method from a lambda", "", Header, "public void Run(ILogger logger, Exception e) { Action a = () => { if (logger != null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }; }", "", false],
        ["Test of a parameter of a local function", "", Header, "public void Run(Exception e) { void Show(ILogger l) { if (l != null) l.LogError(\"x\"); else Console.WriteLine(e.Message); } }", "", false],
        ["Test of a parameter that is not a logger", "", Header, "private ILogger _l;\npublic void Run(object o, Exception e) { if (o != null) _l.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Test of a logger parameter that is not accepted", "", Header, "private ILogger _l;\npublic void Run(ILogger p, Exception e) { if (p != null) p.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "accept_logger_parameters=false", true],
        ["Test of the logger of the primary constructor", "", "public class Service(ILogger logger)", "public void Run(Exception e) { if (logger != null) logger.LogError(\"x\"); else Console.WriteLine(e.Message); }", "", false],
        ["Primary constructor in a static member: not available, not reported", "", "public class Service(ILogger logger)", "public static void Run(Exception e) { [[Console.WriteLine(e.Message)]]; }", "", false],
        ["Test of a method group", "", Header, "private ILogger _l;\npublic void Run(Exception e) { Action a = Run2; if (a != null) _l.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }\npublic void Run2() { }", "", true],
        ["Test of a cast of the logger", "", Header, "private ILogger _l;\npublic void Run(Exception e) { if ((object)_l != null) _l.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "", true],
        ["Guard that does not exit, in another method", "", Header, "private ILogger _l;\npublic void Run(Exception e) { if (_l != null) _l.LogError(\"x\"); else Console.WriteLine(e.Message); }\npublic void Other(ILogger logger, Exception e) { if (_l != null) { } [[Console.WriteLine(e.Message)]]; }", "", true],
        ["In the condition itself", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null && [[Show(e)]]) { } }\nprivate static bool Show(Exception e) { Console.WriteLine(e.Message); return true; }", "", false],
        ["Explicit checks not skipped", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) logger.LogError(\"x\"); else [[Console.WriteLine(e.Message)]]; }", "skip_explicit_logger_checks=false", true],
        ["Explicit guard not skipped", "", Header, "public void Run(ILogger logger, Exception e) { if (logger != null) { logger.LogError(\"x\"); return; } [[Console.WriteLine(e.Message)]]; }", "skip_explicit_logger_checks=false", true],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string pre, string header, string members, string settings, bool matched)
    {
        var test = CreateTest(BuildClass(pre, header, members, matched), settings);
        if (matched)
            ExpectDiagnostic(test);

        await test.RunAsync().ConfigureAwait(false);
    }
}
