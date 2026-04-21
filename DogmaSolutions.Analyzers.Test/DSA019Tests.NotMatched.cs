using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA019Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Chain depth below threshold (depth 2, threshold 3)",
            @"
            namespace TestApp
            {
                public class Inner { public int Value; }
                public class Outer { public Inner Inner; }
                public class MyService
                {
                    public void Process(Outer outer)
                    {
                        var a = outer.Inner.Value;
                        var b = outer.Inner.Value;
                    }
                }
            }"
        ],
        [
            "Chain appears only once",
            @"
            namespace TestApp
            {
                public class C { public int Value; }
                public class B { public C C; }
                public class A { public B B; }
                public class MyService
                {
                    public void Process(A a)
                    {
                        var v = a.B.C.Value;
                    }
                }
            }"
        ],
        [
            "Same prefix but different continuation (different property names)",
            @"
            namespace TestApp
            {
                public class Nested { public int X; public int Y; }
                public class Middle { public Nested Nested; }
                public class Root { public Middle Middle; }
                public class MyService
                {
                    public void Process(Root root)
                    {
                        var x = root.Middle.Nested.X;
                        var y = root.Middle.Nested.Y;
                    }
                }
            }"
        ],
        [
            "Inside nameof (not real dereferencing)",
            @"
            namespace TestApp
            {
                public class C { public int Value; }
                public class B { public C C; }
                public class A { public B B; }
                public class MyService
                {
                    public void Process(A a)
                    {
                        var name1 = nameof(A.B.C);
                        var name2 = nameof(A.B.C);
                    }
                }
            }"
        ],
        [
            "Same chain in different scopes (method vs lambda)",
            @"
            using System;
            namespace TestApp
            {
                public class C { public int Value; }
                public class B { public C C; }
                public class A { public B B; }
                public class MyService
                {
                    public void Process(A a)
                    {
                        var v1 = a.B.C.Value;
                        Action act = () => { var v2 = a.B.C.Value; };
                    }
                }
            }"
        ],
        [
            "Same chain in two separate lambdas (each is its own scope)",
            @"
            using System;
            namespace TestApp
            {
                public class C { public int Value; }
                public class B { public C C; }
                public class A { public B B; }
                public class MyService
                {
                    public void Process(A a)
                    {
                        Action a1 = () => { var v = a.B.C.Value; };
                        Action a2 = () => { var v = a.B.C.Value; };
                    }
                }
            }"
        ],
        [
            "Chains that differ at the root",
            @"
            namespace TestApp
            {
                public class C { public int Value; }
                public class B { public C C; }
                public class A { public B B; }
                public class MyService
                {
                    public void Process(A a1, A a2)
                    {
                        var v1 = a1.B.C.Value;
                        var v2 = a2.B.C.Value;
                    }
                }
            }"
        ],
        [
            "Already extracted into a variable",
            @"
            namespace TestApp
            {
                public class Light { public bool IsOn() => true; }
                public class Room { public Light[] Lights; }
                public class Rooms { public Room Bathroom; }
                public class Home { public Rooms Rooms; }
                public class MyService
                {
                    public void Process(Home home)
                    {
                        var lights = home.Rooms.Bathroom.Lights;
                        var primary = lights[0].IsOn();
                        var secondary = lights[1].IsOn();
                    }
                }
            }"
        ],
        [
            "Same root and terminal but different intermediate member (Bathroom vs MainBedRoom)",
            @"
            namespace TestApp
            {
                public class Room { public int[] Lights; }
                public class Rooms { public Room Bathroom; public Room MainBedRoom; }
                public class Home { public Rooms Rooms; }
                public class MyService
                {
                    public void Process(Home home)
                    {
                        var a = home.Rooms.Bathroom.Lights[0];
                        var b = home.Rooms.MainBedRoom.Lights[0];
                    }
                }
            }"
        ],
        [
            "Same depth, same terminal member, but different paths through the object graph",
            @"
            namespace TestApp
            {
                public class Sensor { public double Value; public string Unit; }
                public class Zone { public Sensor Temperature; }
                public class Building { public Zone NorthWing; public Zone SouthWing; }
                public class Campus { public Building Building; }
                public class MyService
                {
                    public void Process(Campus campus)
                    {
                        var northTemp = campus.Building.NorthWing.Temperature.Value;
                        var southTemp = campus.Building.SouthWing.Temperature.Value;
                    }
                }
            }"
        ],
        [
            "Same prefix diverging into different branches at multiple levels",
            @"
            namespace TestApp
            {
                public class Leaf { public int X; }
                public class BranchA { public Leaf Leaf; }
                public class BranchB { public Leaf Leaf; }
                public class Trunk { public BranchA A; public BranchB B; }
                public class Tree { public Trunk Trunk; }
                public class MyService
                {
                    public void Process(Tree tree)
                    {
                        var x1 = tree.Trunk.A.Leaf.X;
                        var x2 = tree.Trunk.B.Leaf.X;
                    }
                }
            }"
        ],
        [
            "Nested type chain with constant at the end",
            @"
            namespace TestApp
            {
                public class Outer
                {
                    public class Middle
                    {
                        public class Inner
                        {
                            public const int MaxRetries = 3;
                            public const int Timeout = 5000;
                        }
                    }
                }
                public class MyService
                {
                    public void Process()
                    {
                        var retries = Outer.Middle.Inner.MaxRetries;
                        var timeout = Outer.Middle.Inner.Timeout;
                    }
                }
            }"
        ],
        [
            "Nested type chain with enum-like constants",
            @"
            namespace TestApp
            {
                public class StatusCodes
                {
                    public class Http
                    {
                        public class Success
                        {
                            public const int Ok = 200;
                            public const int Created = 201;
                            public const int NoContent = 204;
                        }
                    }
                }
                public class MyService
                {
                    public void Process()
                    {
                        var ok = StatusCodes.Http.Success.Ok;
                        var created = StatusCodes.Http.Success.Created;
                        var noContent = StatusCodes.Http.Success.NoContent;
                    }
                }
            }"
        ],
        [
            "Mixed namespace and nested type chain",
            @"
            namespace TestApp.Configuration
            {
                public class Defaults
                {
                    public class Timeouts
                    {
                        public const int Connection = 30;
                        public const int Read = 60;
                    }
                }
            }
            namespace TestApp
            {
                public class MyService
                {
                    public void Process()
                    {
                        var conn = Configuration.Defaults.Timeouts.Connection;
                        var read = Configuration.Defaults.Timeouts.Read;
                    }
                }
            }"
        ],
        [
            "Static property on fully qualified type followed by instance method",
            @"
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(byte[] buffer, int length)
                    {
                        var s1 = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
                        var s2 = System.Text.Encoding.UTF8.GetString(buffer, 0, length);
                    }
                }
            }"
        ],
        [
            "Static property on qualified type with different instance members",
            @"
            namespace TestApp
            {
                public class MyService
                {
                    public void Process()
                    {
                        var name = System.Text.Encoding.UTF8.EncodingName;
                        var preamble = System.Text.Encoding.UTF8.GetPreamble();
                    }
                }
            }"
        ],
        [
            "Fully qualified enum members (namespace + type qualification, not instance dereference)",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process()
                    {
                        RegexOptions options = System.Text.RegularExpressions.RegexOptions.IgnorePatternWhitespace
                            | System.Text.RegularExpressions.RegexOptions.Multiline
                            | System.Text.RegularExpressions.RegexOptions.IgnoreCase;
                    }
                }
            }"
        ],
        [
            "Fully qualified type cast with static members (type qualification)",
            @"
            namespace TestApp
            {
                public enum MyOptions { A = 1, B = 2, C = 4 }
                public class MyService
                {
                    public void Process()
                    {
                        var combined = TestApp.MyOptions.A | TestApp.MyOptions.B | TestApp.MyOptions.C;
                    }
                }
            }"
        ],
        [
            "Static constants accessed via fully qualified type name",
            @"
            namespace TestApp
            {
                public static class ErrorCodes
                {
                    public static class Database { public const int Timeout = 1; public const int ConnectionFailed = 2; }
                }
                public class MyService
                {
                    public void Process()
                    {
                        var a = TestApp.ErrorCodes.Database.Timeout;
                        var b = TestApp.ErrorCodes.Database.ConnectionFailed;
                    }
                }
            }"
        ],
        [
            "Conditional access chain (not detected, different syntax kind)",
            @"
            namespace TestApp
            {
                public class Deep { public int Value; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class MyService
                {
                    public void Process(Middle middle)
                    {
                        var a = middle?.Inner?.Deep?.Value;
                        var b = middle?.Inner?.Deep?.Value;
                    }
                }
            }"
        ],
        [
            "Simple repeated property access (no deep chain)",
            @"
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(string input)
                    {
                        var len1 = input.Length;
                        var len2 = input.Length;
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InvalidThreshold_FallsBackToDefault()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class Inner { public int Value; }
                public class Outer { public Inner Inner; }
                public class MyService
                {
                    public void Process(Outer outer)
                    {
                        var v1 = outer.Inner.Value;
                        var v2 = outer.Inner.Value;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = invalid_value
"));

        // Default threshold 3 applies; outer.Inner.Value at depth 2 is below threshold
        // No diagnostics expected
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CustomThreshold_LowerThanDefault_FlagsShallowerChains()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class Inner { public int X; public int Y; }
                public class Outer { public Inner Inner; }
                public class MyService
                {
                    public void Process(Outer outer)
                    {
                        var x = {|#0:outer.Inner|}.X;
                        var y = {|#1:outer.Inner|}.Y;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = 1
"));

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("outer.Inner"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1)
                .WithArguments("outer.Inner"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CustomThreshold_HigherThanDefault_SuppressesDeeperChains()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class C { public int Value; }
                public class B { public C C; }
                public class A { public B B; }
                public class Root { public A A; }
                public class MyService
                {
                    public void Process(Root root)
                    {
                        var v1 = root.A.B.C.Value;
                        var v2 = root.A.B.C.Value;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.max_repeated_dereferenciation_depth = 5
"));

        // No diagnostics expected — depth 4 is below threshold of 5
        await test.RunAsync().ConfigureAwait(false);
    }
}
