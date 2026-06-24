using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA019Tests
{

    [TestMethod]
    public async Task ExcludedPrefix_SingleEntry_SuppressesChain()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class NullConstraint { public int And; }
                public class NotConstraint { public NullConstraint Null; }
                public static class Is { public static NotConstraint Not; }
                public class MyTests
                {
                    public void Verify()
                    {
                        var a = Is.Not.Null.And;
                        var b = Is.Not.Null.And;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.excluded_prefixes = Is.Not
"));

        // No diagnostics expected — Is.Not prefix is excluded
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExcludedPrefix_MultipleEntries_SuppressesAll()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class Result { public int Value; }
                public class NotConstraint { public Result Null; }
                public static class Is { public static NotConstraint Not; }
                public static class Has { public static NotConstraint No; }
                public class MyTests
                {
                    public void Verify()
                    {
                        var a = Is.Not.Null.Value;
                        var b = Is.Not.Null.Value;
                        var c = Has.No.Null.Value;
                        var d = Has.No.Null.Value;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.excluded_prefixes = Is.Not, Has.No
"));

        // No diagnostics — both prefixes excluded
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExcludedPrefix_NonMatchingPrefix_StillFlags()
    {
        var sourceCode = @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class Outer { public Middle Middle; }
                public class MyService
                {
                    public void Process(Outer outer)
                    {
                        var x = {|#0:outer.Middle.Inner.Deep|}.X;
                        var y = {|#1:outer.Middle.Inner.Deep|}.Y;
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.excluded_prefixes = Is.Not, Has.No
"));

        // outer.Middle.Inner.Deep does NOT match any excluded prefix — still flagged
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.Middle.Inner.Deep", 2));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.Middle.Inner.Deep", 2));

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
                .WithArguments("outer.Inner", 2));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1)
                .WithArguments("outer.Inner", 2));

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

    [TestMethod]
    public async Task IgnoredIntermediateMembers_DefaultsApplyWithoutConfig()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyContext { public IEnumerable<Item> Items; }
                public static class Extensions
                {
                    public static IEnumerable<T> TagWithCallSite<T>(this IEnumerable<T> source) => source;
                }
                public class MyService
                {
                    public void Process(MyContext context)
                    {
                        var a = context.Items.TagWithCallSite().First(x => x.Id == 1);
                        var b = context.Items.TagWithCallSite().First(x => x.Id == 2);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task IgnoredIntermediateMembers_SingleEntry_SuppressesChain()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyContext { public IEnumerable<Item> Items; }
                public static class Extensions
                {
                    public static IEnumerable<T> TagWithCallSite<T>(this IEnumerable<T> source) => source;
                }
                public class MyService
                {
                    public void Process(MyContext context)
                    {
                        var a = context.Items.TagWithCallSite().First(x => x.Id == 1);
                        var b = context.Items.TagWithCallSite().First(x => x.Id == 2);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.ignored_intermediate_members = TagWithCallSite
"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task IgnoredIntermediateMembers_MultipleEntries_SuppressesChain()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyContext { public IEnumerable<Item> Items; }
                public static class Extensions
                {
                    public static IEnumerable<T> TagWithCallSite<T>(this IEnumerable<T> source) => source;
                    public static IEnumerable<T> AsNoTracking<T>(this IEnumerable<T> source) => source;
                }
                public class MyService
                {
                    public void Process(MyContext context)
                    {
                        var a = context.Items.AsNoTracking().TagWithCallSite().First(x => x.Id == 1);
                        var b = context.Items.AsNoTracking().TagWithCallSite().First(x => x.Id == 2);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.ignored_intermediate_members = TagWithCallSite, AsNoTracking
"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task IgnoredIntermediateMembers_NonMatchingEntry_StillFlags()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyContext { public IEnumerable<Item> Items; }
                public static class Extensions
                {
                    public static IEnumerable<T> TagWithCallSite<T>(this IEnumerable<T> source) => source;
                }
                public class MyService
                {
                    public void Process(MyContext context)
                    {
                        var a = {|#0:context.Items.TagWithCallSite().First|}(x => x.Id == 1);
                        var b = {|#1:context.Items.TagWithCallSite().First|}(x => x.Id == 2);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA019.ignored_intermediate_members = SomeOtherMethod
"));

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("context.Items.TagWithCallSite().First", 2));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("context.Items.TagWithCallSite().First", 2));

        await test.RunAsync().ConfigureAwait(false);
    }
}
