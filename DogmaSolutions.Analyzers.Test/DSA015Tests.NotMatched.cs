using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA015Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Endpoint has direct RequireAuthorization",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .RequireAuthorization();
                    }
                }
            }"
        ],
        [
            "Endpoint has direct AllowAnonymous",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .AllowAnonymous();
                    }
                }
            }"
        ],
        [
            "Endpoint has RequireAuthorization with policy",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .RequireAuthorization(""AdminPolicy"");
                    }
                }
            }"
        ],
        [
            "Extension method, call site passes group with auth",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        api.MapItems();
                    }
                }
            }"
        ],
        [
            "Regular method, call site passes group with separate auth",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"");
                        api.RequireAuthorization();
                        Endpoints.MapItems(api);
                    }
                }
            }"
        ],
        [
            "Call site passes nested group whose ancestor has auth",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        var v1 = api.MapGroup(""/v1"");
                        Endpoints.MapItems(v1);
                    }
                }
            }"
        ],
        [
            "Receiver is a local variable (DSA013 territory, not DSA015)",
            @"
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
                        builder.MapGet(""/api/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Receiver is a RouteGroupBuilder parameter (DSA014 territory, not DSA015)",
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
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Non-Map method invocation",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class Startup
                {
                    public void Configure()
                    {
                        var items = new List<string>();
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "Recursive pass-through: outer call site has auth on group",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }

                    public static void ConfigureAll(IEndpointRouteBuilder outerBuilder)
                    {
                        MapItems(outerBuilder);
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.ConfigureAll(api);
                    }
                }
            }"
        ],
        [
            "Multiple call sites, ALL have auth",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(api);

                        var admin = mainBuilder.MapGroup(""/admin"").RequireAuthorization(""AdminPolicy"");
                        Endpoints.MapItems(admin);
                    }
                }
            }"
        ],
        [
            "Named argument at call site with auth",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(builder: api);
                    }
                }
            }"
        ],
        [
            "Extension method called as static method, call site has auth",
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
                        builder.MapGet(""/items"", () => Results.Ok());
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder mainBuilder)
                    {
                        var api = mainBuilder.MapGroup(""/api"").RequireAuthorization();
                        EndpointExtensions.MapItems(api);
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
        var test = new CSharpAnalyzerVerifier<DSA015Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.App.Ref", "8.0.0"),
            }
        ]);

        await test.RunAsync().ConfigureAwait(false);
    }
}
