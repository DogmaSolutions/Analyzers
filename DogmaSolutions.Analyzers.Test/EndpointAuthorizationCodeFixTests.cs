using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class EndpointAuthorizationCodeFixTests
{
    private static readonly PackageIdentity[] AspNetPackages =
    [
        new("Microsoft.AspNetCore.App.Ref", "8.0.0"),
    ];

    [TestMethod]
    public async Task DSA013_AddsRequireAuthorization()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Program
    {
        private static IEndpointRouteBuilder GetBuilder() => null;
        public static void Main()
        {
            var builder = GetBuilder();
            {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|};
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Program
    {
        private static IEndpointRouteBuilder GetBuilder() => null;
        public static void Main()
        {
            var builder = GetBuilder();
            builder.MapGet(""/api/items"", () => Results.Ok()).RequireAuthorization();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA013Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddRequireAuthorization";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA013Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA013Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapGet"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DSA013_AddsAllowAnonymous()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Program
    {
        private static IEndpointRouteBuilder GetBuilder() => null;
        public static void Main()
        {
            var builder = GetBuilder();
            {|#0:builder.MapGet(""/api/health"", () => Results.Ok())|};
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Program
    {
        private static IEndpointRouteBuilder GetBuilder() => null;
        public static void Main()
        {
            var builder = GetBuilder();
            builder.MapGet(""/api/health"", () => Results.Ok()).AllowAnonymous();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA013Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddAllowAnonymous";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA013Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA013Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapGet"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DSA013_AppendsAfterExistingFluentChain()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Program
    {
        private static IEndpointRouteBuilder GetBuilder() => null;
        public static void Main()
        {
            var builder = GetBuilder();
            {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|}
                .WithName(""GetItems"");
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Program
    {
        private static IEndpointRouteBuilder GetBuilder() => null;
        public static void Main()
        {
            var builder = GetBuilder();
            builder.MapGet(""/api/items"", () => Results.Ok())
                .WithName(""GetItems"").RequireAuthorization();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA013Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddRequireAuthorization";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA013Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA013Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapGet"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DSA014_AddsAllowAnonymousOnGroupEndpoint()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup(""/api"");
            {|#0:group.MapGet(""/health"", () => Results.Ok())|};
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup(""/api"");
            group.MapGet(""/health"", () => Results.Ok()).AllowAnonymous();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA014Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddAllowAnonymous";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA014Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA014Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapGet"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DSA015_AddsAllowAnonymousOnParameterEndpoint()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            {|#0:builder.MapGet(""/api/health"", () => Results.Ok())|};
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            builder.MapGet(""/api/health"", () => Results.Ok()).AllowAnonymous();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA015Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddAllowAnonymous";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA015Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA015Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapGet"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DSA014_AddsRequireAuthorizationOnGroupEndpoint()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup(""/api"");
            {|#0:group.MapGet(""/items"", () => Results.Ok())|};
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            var group = builder.MapGroup(""/api"");
            group.MapGet(""/items"", () => Results.Ok()).RequireAuthorization();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA014Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddRequireAuthorization";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA014Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA014Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapGet"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DSA015_AddsRequireAuthorizationOnParameterEndpoint()
    {
        var source = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            {|#0:builder.MapPost(""/api/items"", () => Results.Ok())|};
        }
    }
}";

        var fixedSource = @"
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
namespace TestApp
{
    public class Startup
    {
        public void Configure(IEndpointRouteBuilder builder)
        {
            builder.MapPost(""/api/items"", () => Results.Ok()).RequireAuthorization();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA015Analyzer, EndpointAuthorizationCodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "AddRequireAuthorization";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages([..AspNetPackages]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA015Analyzer, EndpointAuthorizationCodeFixProvider>
                .Diagnostic(DSA015Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("MapPost"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
