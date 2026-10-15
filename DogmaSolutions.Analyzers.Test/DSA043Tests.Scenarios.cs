using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA043Tests
{
    private const string Catch = "try { x++; } catch (System.Exception) { x--; }";
    private const string Quiet = "x++;";

    // title, types declared before the class, class header, members, activity, settings (key=value;...), is the class reported
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the class has a logger -------------------------------------------------------------------------------
        ["Matched: complex, catches and has no logger", "", "public class [[Service]]", "", Catch, "", true],
        ["Logger field", "", "public class [[Service]]", "private readonly ILogger<Service> _logger;", Catch, "", false],
        ["Logger property", "", "public class [[Service]]", "public ILogger Logger { get; set; }", Catch, "", false],
        ["Logger factory in a constructor", "", "public class [[Service]]", "public Service(ILoggerFactory factory) { }", Catch, "", false],
        ["Logger in the primary constructor", "", "public class [[Service]](ILogger<Service> logger)", "", Catch, "", false],
        ["Inherited protected logger", "public class Base { protected ILogger Log; }", "public class [[Service]] : Base", "", Catch, "", false],
        ["Inherited public logger property of a distant base", "public class Base { public ILogger Log { get; } } public class Middle : Base { }", "public class [[Service]] : Middle", "", Catch, "", false],
        ["Inherited private logger does not count", "public class Base { private ILogger Log; }", "public class [[Service]] : Base", "", Catch, "", true],
        ["Inherited logger refused by accept_inherited_logger=false", "public class Base { protected ILogger Log; }", "public class [[Service]] : Base", "", Catch, "accept_inherited_logger=false", true],
        ["Inherited member that is not a logger", "public class Base { protected int Count; public string Name { get; } public void Do() { } }", "public class [[Service]] : Base", "", Catch, "", true],
        ["Logger parameter of a method", "", "public class [[Service]]", "public void Do(ILogger logger) { }", Catch, "", false],
        ["Logger parameter refused by accept_logger_parameters=false", "", "public class [[Service]]", "public void Do(ILogger logger) { }", Catch, "accept_logger_parameters=false", true],
        ["Logger parameter of a lambda", "", "public class [[Service]]", "private readonly Action<ILogger> _a = (ILogger l) => { };", Catch, "", false],
        ["Logger local variable", "", "public class [[Service]]", "public void Do() { ILogger l = null; }", Catch, "", false],
        ["LoggerMessage.Define call", "", "public class [[Service]]", "public void Do() { LoggerMessage.Define(\"x\"); }", Catch, "", false],
        ["Logging extension call", "", "public class [[Service]]", "public void Do(object o) { ((ILogger)o).LogInformation(\"x\"); }", Catch, "accept_logger_parameters=false", false],
        ["LoggerMessage attribute", "", "public class [[Service]]", "[LoggerMessage] public void Do() { }", Catch, "", false],
        ["Logging object created", "", "public class [[Service]]", "public object Do() => new Microsoft.Extensions.Logging.Abstractions.NullLogger<Service>();", Catch, "", false],
        ["Logger resolved from a service provider", "", "public class [[Service]]", "public void Do() { Fakes.Services.Get<ILogger<Service>>(null); }", Catch, "", false],
        ["Type implementing the logger interface", "public class MyLogger : ILogger { }", "public class [[Service]]", "private MyLogger _l;", Catch, "", false],
        ["Type deriving from a logging class", "public class MyBase : Microsoft.Extensions.Logging.Abstractions.NullLogger<int> { }", "public class [[Service]]", "private MyBase _l;", Catch, "", false],
        ["Not a logging type: array of loggers", "", "public class [[Service]]", "private ILogger[] _l;", Catch, "", true],
        ["Custom logger type configured", "", "public class [[Service]]", "private MyCompany.Diagnostics.IAppLogger _l;", Catch, "logger_types=MyCompany.Diagnostics.IAppLogger", false],
        ["Custom logger type not configured", "", "public class [[Service]]", "private MyCompany.Diagnostics.IAppLogger _l;", Catch, "", true],
        ["Other logging library configured", "", "public class [[Service]]", "private Serilog.ILog _l;", Catch, "logger_namespaces=Microsoft.Extensions.Logging,Serilog", false],
        ["Other logging library static call configured", "", "public class [[Service]]", "public void Do() { Serilog.Log.Information(\"x\"); }", Catch, "logger_namespaces=Serilog", false],
        ["Other logging library not configured", "", "public class [[Service]]", "private Serilog.ILog _l;", Catch, "", true],
        ["Namespace that only starts like a logger namespace", "namespace Microsoft.Extensions.LoggingX { public interface IThing { } }", "public class [[Service]]", "private Microsoft.Extensions.LoggingX.IThing _l;", Catch, "", true],

        // ---- how complex ------------------------------------------------------------------------------------------
        ["Below every threshold", "", "public class [[Service]]", "", Catch, "min_cyclomatic_complexity=100", false],
        ["Method complexity alone", "", "public class [[Service]]", "", Catch, "min_cyclomatic_complexity=0;min_method_complexity=6", true],
        ["Method complexity not reached", "", "public class [[Service]]", "", Catch, "min_cyclomatic_complexity=0;min_method_complexity=7", false],
        ["Lines alone", "", "public class [[Service]]", "", Catch, "min_cyclomatic_complexity=0;min_lines=10", true],
        ["Lines not reached", "", "public class [[Service]]", "", Catch, "min_cyclomatic_complexity=0;min_lines=100", false],
        ["Blank lines counted", "", "public class [[Service]]", "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", Catch, "min_cyclomatic_complexity=0;min_lines=30;count_blank_lines=true", true],
        ["Blank lines not counted", "", "public class [[Service]]", "\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n", Catch, "min_cyclomatic_complexity=0;min_lines=30", false],
        ["Methods alone", "", "public class [[Service]]", "public void A() { } public void B() { }", Catch, "min_cyclomatic_complexity=0;min_methods=3", true],
        ["Methods not reached", "", "public class [[Service]]", "public void A() { } public void B() { }", Catch, "min_cyclomatic_complexity=0;min_methods=4", false],
        ["Constructor parameters alone", "", "public class [[Service]]", "public Service(int a, int b, int c) { }", Catch, "min_cyclomatic_complexity=0;min_constructor_parameters=3", true],
        ["Constructor parameters not reached", "", "public class [[Service]]", "public Service(int a, int b) { }", Catch, "min_cyclomatic_complexity=0;min_constructor_parameters=3", false],
        ["All thresholds required and met", "", "public class [[Service]]", "", Catch, "require_all_thresholds=true;min_method_complexity=6", true],
        ["All thresholds required and one missed", "", "public class [[Service]]", "", Catch, "require_all_thresholds=true;min_method_complexity=7", false],
        ["Every threshold disabled", "", "public class [[Service]]", "", Catch, "min_cyclomatic_complexity=0", false],

        // ---- does it do something worth logging about --------------------------------------------------------------
        ["No activity", "", "public class [[Service]]", "", Quiet, "", false],
        ["No activity needed", "", "public class [[Service]]", "", Quiet, "require_activity=false", true],
        ["Await", "", "public class [[Service]]", "public async System.Threading.Tasks.Task Go() { await System.Threading.Tasks.Task.Delay(1); }", Quiet, "", true],
        ["Await not among the activity kinds", "", "public class [[Service]]", "public async System.Threading.Tasks.Task Go() { await System.Threading.Tasks.Task.Delay(1); }", Quiet, "activity_kinds=catch", false],
        ["Throw among the activity kinds", "", "public class [[Service]]", "", "if (x < 0) { throw new System.ArgumentException(); }", "activity_kinds=throw", true],
        ["Throw not among the activity kinds", "", "public class [[Service]]", "", "if (x < 0) { throw new System.ArgumentException(); }", "", false],
        ["Call into HTTP", "", "public class [[Service]]", "", "new System.Net.Http.HttpClient().GetStringAsync(\"u\");", "", true],
        ["Creation of an HTTP client", "", "public class [[Service]]", "private System.Net.Http.HttpClient _c = new System.Net.Http.HttpClient();", Quiet, "", true],
        ["Call into a namespace that is not an activity", "", "public class [[Service]]", "", "new System.Net.Http.HttpClient().GetStringAsync(\"u\");", "activity_namespaces=My.Io", false],
        ["Call into a child of an activity namespace", "", "public class [[Service]]", "", "new System.Net.Http.Inner.Thing().Go();", "activity_namespaces=System.Net.Http", true],
        ["Call kind disabled", "", "public class [[Service]]", "", "new System.Net.Http.HttpClient().GetStringAsync(\"u\");", "activity_kinds=catch", false],
        ["Call without a symbol", "", "public class [[Service]]", "", "var name = nameof(x);", "activity_kinds=call", false],

        // ---- which classes are looked at ---------------------------------------------------------------------------
        ["Static class is skipped", "", "public static class [[Service]]", "", Catch, "", false],
        ["Static class included", "", "public static class [[Service]]", "", Catch, "include_static_classes=true", true],
        ["Abstract class is skipped", "", "public abstract class [[Service]]", "", Catch, "", false],
        ["Abstract class included", "", "public abstract class [[Service]]", "", Catch, "include_abstract_classes=true", true],
        ["Struct is skipped", "", "public struct [[Service]]", "", Catch, "", false],
        ["Struct included", "", "public struct [[Service]]", "", Catch, "included_type_kinds=struct", true],
        ["Record is skipped", "", "public record [[Service]]", "", Catch, "", false],
        ["Record included", "", "public record [[Service]]", "", Catch, "included_type_kinds=record", true],
        ["Record struct included", "", "public record struct [[Service]]", "", Catch, "included_type_kinds=record struct", true],
        ["Class not included when only records are", "", "public class [[Service]]", "", Catch, "included_type_kinds=record", false],
        ["Excluded by name", "", "public class [[OrderDto]]", "", Catch, "", false],
        ["Excluded by name, case-insensitive", "", "public class [[Ordersoptions]]", "", Catch, "", false],
        ["Name exclusion list replaced", "", "public class [[OrderDto]]", "", Catch, "excluded_type_name_patterns=*Foo", true],
        ["Excluded by namespace", "", "public class [[Service]]", "", Catch, "excluded_namespace_patterns=TestApp", false],
        ["Namespace exclusion not matching", "", "public class [[Service]]", "", Catch, "excluded_namespace_patterns=*.Other", true],
        ["Excluded by base type", "", "public class [[Service]] : System.Exception", "", Catch, "", false],
        ["Excluded by distant base type", "public class MyBase : System.Attribute { }", "public class [[Service]] : MyBase", "", Catch, "", false],
        ["Base type exclusion list replaced", "", "public class [[Service]] : System.Exception", "", Catch, "excluded_base_types=My.Other", true],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string pre, string header, string members, string activity, string settings, bool matched)
    {
        var test = CreateTest(BuildClass(pre, header, members, activity, matched), settings);
        if (matched)
            ExpectDiagnostic(test);

        await test.RunAsync().ConfigureAwait(false);
    }
}
