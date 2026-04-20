using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA014Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Local group without auth, MapGet without auth",
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
                        var group = builder.MapGroup(""/api"");
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Local group without auth, MapPost without auth",
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
                        var group = builder.MapGroup(""/api"");
                        {|#0:group.MapPost(""/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapPost"
        ],
        [
            "Local group without auth, MapGet with fluent chain but no auth",
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
                        var group = builder.MapGroup(""/api"");
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|}
                            .WithName(""GetItems"")
                            .Produces(StatusCodes.Status200OK);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Nested groups: neither outer nor inner has auth",
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
                        var api = builder.MapGroup(""/api"");
                        var v1 = api.MapGroup(""/v1"");
                        {|#0:v1.MapGet(""/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Group parameter, call site has no auth on the group",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(RouteGroupBuilder group)
                    {
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|};
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"");
                        Endpoints.MapItems(api);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Group parameter, no call sites found in compilation",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(RouteGroupBuilder group)
                    {
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Multiple endpoints in group: some with direct auth, some without",
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
                        var group = builder.MapGroup(""/api"");
                        group.MapGet(""/public"", () => Results.Ok()).AllowAnonymous();
                        {|#0:group.MapPost(""/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapPost"
        ],
        [
            "Inline MapGroup().MapGet() without auth, no variable",
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
                        {|#0:builder.MapGroup(""/api"").MapGet(""/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Group parameter, multiple call sites, SOME have auth and some do not",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(RouteGroupBuilder group)
                    {
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|};
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var secured = builder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(secured);

                        var unsecured = builder.MapGroup(""/other"");
                        Endpoints.MapItems(unsecured);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Group parameter, recursive pass-through, no auth at outermost call site",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class Endpoints
                {
                    public static void MapItems(RouteGroupBuilder group)
                    {
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|};
                    }

                    public static void ConfigureAll(RouteGroupBuilder outerGroup)
                    {
                        MapItems(outerGroup);
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"");
                        Endpoints.ConfigureAll(api);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Group extension method parameter without auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public static class EndpointExtensions
                {
                    public static RouteGroupBuilder MapItems(this RouteGroupBuilder group)
                    {
                        {|#0:group.MapGet(""/items"", () => Results.Ok())|};
                        return group;
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"");
                        api.MapItems();
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
        var test = new CSharpAnalyzerVerifier<DSA014Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.App.Ref", "8.0.0"),
            }
        ]);

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA014Analyzer>.Diagnostic(DSA014Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(mapMethodName));

        await test.RunAsync().ConfigureAwait(false);
    }
}
