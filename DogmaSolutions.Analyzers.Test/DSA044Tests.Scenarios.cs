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
