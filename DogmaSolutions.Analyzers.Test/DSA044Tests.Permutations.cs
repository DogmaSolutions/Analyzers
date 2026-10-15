using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA044Tests
{
    // title, types declared before the class, class header, members ([[...]] marks the expected call), settings (key=value;...), is it reported
    private static IEnumerable<object[]> GetPermutationScenarios =>
    [
        // ---- the way the console is named -------------------------------------------------------------------------
        ["Fully qualified console", "", Header, "private ILogger _l;\npublic void Run() { [[System.Console.WriteLine(\"hello\")]]; }", "", true],
        ["Global qualified console", "", Header, "private ILogger _l;\npublic void Run() { [[global::System.Console.WriteLine(\"hello\")]]; }", "", true],
        ["Aliased console", "", Header, "private ILogger _l;\npublic void Run() { [[C.WriteLine(\"hello\")]]; }", "", true],
        ["Aliased debug", "", Header, "private ILogger _l;\npublic void Run() { [[D.WriteLine(\"hello\")]]; }", "", true],
        ["Console.Out stored in a local is not followed", "", Header, "private ILogger _l;\npublic void Run() { var o = Console.Out; o.WriteLine(\"hello\"); }", "", false],
        ["Console.Error.Write", "", Header, "private ILogger _l;\npublic void Run() { [[Console.Error.Write(\"hello\")]]; }", "", true],
        ["Console.Error.Write refused by the option", "", Header, "private ILogger _l;\npublic void Run() { Console.Error.Write(\"hello\"); }", "include_console_streams=false", false],
        ["Console.WriteLine without arguments", "", Header, "private ILogger _l;\npublic void Run() { [[Console.WriteLine()]]; }", "", true],

        // ---- the place of the call --------------------------------------------------------------------------------
        ["In a lambda", "", Header, "private ILogger _l;\npublic void Run() { System.Action a = () => [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["In a local function", "", Header, "private ILogger _l;\npublic void Run() { void Local() { [[Console.WriteLine(\"hello\")]]; } Local(); }", "", true],
        ["In a property getter", "", Header, "private ILogger _l;\npublic int P { get { [[Console.WriteLine(\"hello\")]]; return 1; } }", "", true],
        ["In a constructor", "", Header, "private ILogger _l;\npublic Service() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["In a static constructor", "", Header, "private static ILogger _l;\nstatic Service() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Static method with an instance logger", "", Header, "private ILogger _l;\npublic static void Run() { Console.WriteLine(\"hello\"); }", "", false],
        ["Static method with a static logger", "", Header, "private static ILogger _l;\npublic static void Run() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["In a nested class of a class with a logger", "", Header, "private ILogger _l;\npublic class Inner { public void Run() { Console.WriteLine(\"hello\"); } }", "", false],
        ["In a field initializer", "", Header, "private ILogger _l;\nprivate int _x = Compute();\nprivate static int Compute() { return 1; }", "", false],

        // ---- the logger, inherited --------------------------------------------------------------------------------
        ["Protected logger of the base class", "public class Base { protected ILogger L; }", "public class Service : Base", "public void Run() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Private logger of the base class", "public class Base { private ILogger L; }", "public class Service : Base", "public void Run() { Console.WriteLine(\"hello\"); }", "", false],
        ["Private protected logger of the base class", "public class Base { private protected ILogger L; }", "public class Service : Base", "public void Run() { [[Console.WriteLine(\"hello\")]]; }", "", true],
        ["Internal logger of the base class", "public class Base { internal ILogger L; }", "public class Service : Base", "public void Run() { [[Console.WriteLine(\"hello\")]]; }", "", true],

        // ---- the interactive members ------------------------------------------------------------------------------
        ["Read through Console.In", "", Header, "private ILogger _l;\npublic void Run() { Console.Write(\"name? \"); var name = Console.In.ReadLine(); }", "", false],
        ["ReadKey", "", Header, "private ILogger _l;\npublic void Run() { Console.Write(\"press\"); Console.ReadKey(); }", "", false],
        ["Read in a lambda of the member", "", Header, "private ILogger _l;\npublic void Run() { Console.Write(\"q\"); System.Func<string> f = () => Console.ReadLine(); }", "", false],
    ];

    [TestMethod]
    [DynamicData(nameof(GetPermutationScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Permutation_scenario(string title, string pre, string header, string members, string settings, bool matched)
    {
        var test = CreateTest("using C = System.Console;\nusing D = System.Diagnostics.Debug;\n" + BuildClass(pre, header, members, matched), settings);
        if (matched)
            ExpectDiagnostic(test);

        await test.RunAsync().ConfigureAwait(false);
    }
}
