using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA015Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Method parameter without auth, no call sites",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Extension method parameter without auth, no call sites",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class EndpointExtensions
                {
                    public static IEndpointRouteBuilder MapItems(this IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|}
                            .WithName(""GetItems"")
                            .Produces(StatusCodes.Status200OK);
                        return builder;
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Method parameter, call site passes builder without auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/items"", () => Results.Ok())|};
                    }
                }

                public class Program
                {
                    private static IEndpointRouteBuilder GetBuilder() => null;
                    public static void Main()
                    {
                        var app = GetBuilder();
                        Endpoints.MapItems(app);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Extension method, call site has no auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class EndpointExtensions
                {
                    public static void MapItems(this IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/items"", () => Results.Ok())|};
                    }
                }

                public class Program
                {
                    private static IEndpointRouteBuilder GetBuilder() => null;
                    public static void Main()
                    {
                        var app = GetBuilder();
                        app.MapItems();
                    }
                }
            }",
            "MapGet"
        ],
        [
            "MapPost on parameter without auth",
            @"
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
            }",
            "MapPost"
        ],
        [
            "MapDelete on parameter without auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapDelete(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapDelete"
        ],
        [
            "Parameter with fluent chain but no auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|}
                            .WithName(""GetItems"")
                            .Produces(StatusCodes.Status200OK);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Recursive pass-through: neither method has auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/items"", () => Results.Ok())|};
                    }

                    public static void ConfigureAll(IEndpointRouteBuilder outerBuilder)
                    {
                        MapItems(outerBuilder);
                    }
                }

                public class Program
                {
                    private static IEndpointRouteBuilder GetBuilder() => null;
                    public static void Main()
                    {
                        var app = GetBuilder();
                        Endpoints.ConfigureAll(app);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Multiple call sites, SOME have auth and some do not",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/items"", () => Results.Ok())|};
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var secured = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(secured);

                        var unsecured = mainBuilder.MapGroup(""/other"");
                        Endpoints.MapItems(unsecured);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Extension method called as static method, call site has no auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class EndpointExtensions
                {
                    public static void MapItems(this IEndpointRouteBuilder builder)
                    {
                        {|#0:builder.MapGet(""/items"", () => Results.Ok())|};
                    }
                }

                public class Program
                {
                    private static IEndpointRouteBuilder GetBuilder() => null;
                    public static void Main()
                    {
                        var app = GetBuilder();
                        EndpointExtensions.MapItems(app);
                    }
                }
            }",
            "MapGet"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string mapMethodName
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA015Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.App.Ref", "8.0.0"),
            }
        ]);

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA015Analyzer>.Diagnostic(DSA015Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(mapMethodName));

        await test.RunAsync().ConfigureAwait(false);
    }
}
