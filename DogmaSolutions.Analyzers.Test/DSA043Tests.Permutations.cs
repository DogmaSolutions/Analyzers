using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA043Tests
{
    // title, types declared before the class, class header, members, activity, settings (key=value;...), is the class reported
    private static IEnumerable<object[]> GetPermutationScenarios =>
    [
        // ---- the ways a logger is held or reached -------------------------------------------------------------------
        ["Static logger field", "", "public class [[Service]]", "private static readonly ILogger _logger;", Catch, "", false],
        ["Logger in a static class", "", "public static class [[Service]]", "private static ILogger _logger;", Catch, "include_static_classes=true", false],
        ["NullLogger.Instance", "", "public class [[Service]]", "public object Do() => Microsoft.Extensions.Logging.Abstractions.NullLogger<Service>.Instance;", Catch, "", false],
        ["Static logger property of a logging type", "", "public class [[Service]]", "public object Do() => Microsoft.Extensions.Logging.Abstractions.NullLogger<Service>.Shared;", Catch, "", false],
        ["Logger created in a lambda", "", "public class [[Service]]", "public System.Func<object> F = () => new Microsoft.Extensions.Logging.Abstractions.NullLogger<Service>();", Catch, "", false],
        ["Logger of a local function", "", "public class [[Service]]", "public void Do() { void Local(ILogger l) { } }", Catch, "", false],
        ["Logger as a generic argument", "", "public class [[Service]]", "private System.Collections.Generic.List<ILogger> _l;", Catch, "", true],
        ["Nullable logger", "", "public class [[Service]]", "#nullable enable\nprivate ILogger? _l;\n#nullable restore", Catch, "", false],
        ["Logger as an out parameter", "", "public class [[Service]]", "public void Do(out ILogger l) { l = null; }", Catch, "", false],
        ["Logger of an event handler", "", "public class [[Service]]", "public event System.Action<ILogger> Raised;", Catch, "", true],
        ["Logger in a record parameter list", "", "public record [[Service]](ILogger Logger)", "", Catch, "included_type_kinds=record", false],

        // ---- the shape of the type ----------------------------------------------------------------------------------
        ["Sealed class", "", "public sealed class [[Service]]", "", Catch, "", true],
        ["Generic class", "", "public class [[Service]]<T>", "", Catch, "", true],
        ["Internal class", "", "internal class [[Service]]", "", Catch, "", true],
        ["Nested class is a type of its own", "public class Outer { }", "public class [[Service]]", "public class Inner { private ILogger _l; }", Catch, "", true],
        ["Partial class, the logger in the other part", "public partial class Service { private ILogger _l; }", "public partial class [[Service]]", "", Catch, "", false],
        ["Interface is never reported", "", "public interface [[Service]]", "", Quiet, "", false],
        ["Delegate-only file", "public delegate void Handler(); ", "public class [[Service]]", "", Quiet, "require_activity=false;min_cyclomatic_complexity=0;min_methods=1", true],

        // ---- the combination of the options -------------------------------------------------------------------------
        ["Static class, activity not needed, no thresholds", "", "public static class [[Service]]", "", Quiet, "include_static_classes=true;require_activity=false;min_cyclomatic_complexity=0;min_methods=1", true],
        ["Excluded by namespace beats a match", "", "public class [[Service]]", "", Catch, "excluded_namespace_patterns=TestApp;require_activity=false", false],
        ["Inherited logger and parameters both refused", "public class Base { protected ILogger Log; }", "public class [[Service]] : Base", "public void Do(ILogger l) { }", Catch, "accept_inherited_logger=false;accept_logger_parameters=false", true],
        ["Inherited logger refused, own logger accepted", "public class Base { protected ILogger Log; }", "public class [[Service]] : Base", "private ILogger _l;", Catch, "accept_inherited_logger=false", false],
        ["All thresholds required, activity missing", "", "public class [[Service]]", "", Quiet, "require_all_thresholds=true;min_cyclomatic_complexity=1", false],
        ["Different activity kinds combined", "", "public class [[Service]]", "", "if (x < 0) { throw new System.ArgumentException(); }", "activity_kinds=throw,catch", true],
    ];

    [TestMethod]
    [DynamicData(nameof(GetPermutationScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Permutation_scenario(string title, string pre, string header, string members, string activity, string settings, bool matched)
    {
        var test = CreateTest(BuildClass(pre, header, members, activity, matched), settings);
        if (matched)
            ExpectDiagnostic(test);

        await test.RunAsync().ConfigureAwait(false);
    }
}
