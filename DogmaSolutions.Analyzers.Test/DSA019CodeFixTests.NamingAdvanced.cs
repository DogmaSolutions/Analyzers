using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA019CodeFixTests
{

    [TestMethod]
    public async Task CompactName_WithMethodCallInChain()
    {
        var source = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var count = {|#0:provider.Service.GetReport().Summary|}.Count;
            var label = {|#1:provider.Service.GetReport().Summary|}.Label;
        }
    }
}";

        // GetReport → first word is "Get", Summary → "Summary"
        // provider, Service, Get, Summary → providerServiceGetSummary
        var fixedSource = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var providerServiceGetSummary = provider.Service.GetReport().Summary;
            var count = providerServiceGetSummary.Count;
            var label = providerServiceGetSummary.Label;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("provider.Service.GetReport().Summary", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("provider.Service.GetReport().Summary", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_WithThisPrefix()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var x = {|#0:this.MiddleField.InnerChild.DeepChild|}.X;
            var y = {|#1:this.MiddleField.InnerChild.DeepChild|}.Y;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var middleFieldInnerChildDeepChild = this.MiddleField.InnerChild.DeepChild;
            var x = middleFieldInnerChildDeepChild.X;
            var y = middleFieldInnerChildDeepChild.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_ThreeOccurrences()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; public int Z; }
    public class Inner { public Deep DeepLevel; }
    public class Middle { public Inner InnerLevel; }
    public class Outer { public Middle MiddleLevel; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var x = {|#0:outer.MiddleLevel.InnerLevel.DeepLevel|}.X;
            var y = {|#1:outer.MiddleLevel.InnerLevel.DeepLevel|}.Y;
            var z = {|#2:outer.MiddleLevel.InnerLevel.DeepLevel|}.Z;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; public int Z; }
    public class Inner { public Deep DeepLevel; }
    public class Middle { public Inner InnerLevel; }
    public class Outer { public Middle MiddleLevel; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var outerMiddleLevelInnerLevelDeepLevel = outer.MiddleLevel.InnerLevel.DeepLevel;
            var x = outerMiddleLevelInnerLevelDeepLevel.X;
            var y = outerMiddleLevelInnerLevelDeepLevel.Y;
            var z = outerMiddleLevelInnerLevelDeepLevel.Z;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("outer.MiddleLevel.InnerLevel.DeepLevel", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("outer.MiddleLevel.InnerLevel.DeepLevel", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("outer.MiddleLevel.InnerLevel.DeepLevel", 3));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_WithThisPrefix()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var x = {|#0:this.MiddleField.InnerChild.DeepChild|}.X;
            var y = {|#1:this.MiddleField.InnerChild.DeepChild|}.Y;
        }
    }
}";

        // this is excluded from chain, so: MiddleField → Middle, InnerChild → Inner, DeepChild → Deep
        // → middleInnerDeep
        var fixedSource = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep DeepChild; }
    public class Middle { public Inner InnerChild; }
    public class MyService
    {
        public Middle MiddleField;

        public void Process()
        {
            var middleInnerDeep = this.MiddleField.InnerChild.DeepChild;
            var x = middleInnerDeep.X;
            var y = middleInnerDeep.Y;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("this.MiddleField.InnerChild.DeepChild", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ShortName_RepeatedChainAcrossSeparateObjectInitializersInArray()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TestApp
{
    public class Tenant { public int Id; }
    public class UserInfo { public Tenant Tenant; }
    public class TestCtx { public UserInfo CurrentUser; }
    public class WsDto { public int TenantId; public string Name; }
    public class MyService
    {
        public static Task<IReadOnlyCollection<WsDto>> GetWorkingEnvs(TestCtx testContext)
        {
            return Task.FromResult<IReadOnlyCollection<WsDto>>(
                new[]
                {
                    new WsDto
                    {
                        TenantId = {|#0:testContext.CurrentUser.Tenant.Id|},
                        Name = ""Test 1""
                    },
                    new WsDto
                    {
                        TenantId = {|#1:testContext.CurrentUser.Tenant.Id|},
                        Name = ""Test 2""
                    }
                });
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
namespace TestApp
{
    public class Tenant { public int Id; }
    public class UserInfo { public Tenant Tenant; }
    public class TestCtx { public UserInfo CurrentUser; }
    public class WsDto { public int TenantId; public string Name; }
    public class MyService
    {
        public static Task<IReadOnlyCollection<WsDto>> GetWorkingEnvs(TestCtx testContext)
        {
            var id = testContext.CurrentUser.Tenant.Id;
            return Task.FromResult<IReadOnlyCollection<WsDto>>(
                new[]
                {
                    new WsDto
                    {
                        TenantId = id,
                        Name = ""Test 1""
                    },
                    new WsDto
                    {
                        TenantId = id,
                        Name = ""Test 2""
                    }
                });
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("testContext.CurrentUser.Tenant.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("testContext.CurrentUser.Tenant.Id", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_WithElementAccessInMiddle()
    {
        var source = @"
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
            var status = new
            {
                Primary = {|#0:home.Rooms.Bathroom.Lights|}[0].IsOn(),
                Secondary = {|#1:home.Rooms.Bathroom.Lights|}[1].IsOn(),
            };
        }
    }
}";

        var fixedSource = @"
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
            var homeRoomsBathroomLights = home.Rooms.Bathroom.Lights;
            var status = new
            {
                Primary = homeRoomsBathroomLights[0].IsOn(),
                Secondary = homeRoomsBathroomLights[1].IsOn(),
            };
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("home.Rooms.Bathroom.Lights", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("home.Rooms.Bathroom.Lights", 2));

        await test.RunAsync().ConfigureAwait(false);
    }


}
