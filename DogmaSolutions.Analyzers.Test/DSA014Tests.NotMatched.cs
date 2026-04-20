using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA014Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Group with inline RequireAuthorization, MapGet inherits",
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
                        var group = builder.MapGroup(""/api"").RequireAuthorization();
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Group with separate RequireAuthorization call",
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
                        group.RequireAuthorization();
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Group with inline AllowAnonymous",
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
                        var group = builder.MapGroup(""/api"").AllowAnonymous();
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Group with separate AllowAnonymous call",
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
                        group.AllowAnonymous();
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Nested groups: outer has inline auth, inner inherits",
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
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        var v1 = api.MapGroup(""/v1"");
                        v1.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Nested groups: outer has separate-call auth, inner inherits",
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
                        api.RequireAuthorization();
                        var v1 = api.MapGroup(""/v1"");
                        v1.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Nested groups: inner has auth, outer does not",
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
                        var v1 = api.MapGroup(""/v1"").RequireAuthorization();
                        v1.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Endpoint has direct RequireAuthorization (group has no auth)",
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
                        group.MapGet(""/items"", () => Results.Ok())
                            .RequireAuthorization();
                    }
                }
            }"
        ],
        [
            "Endpoint has direct AllowAnonymous (group has no auth)",
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
                        group.MapGet(""/items"", () => Results.Ok())
                            .AllowAnonymous();
                    }
                }
            }"
        ],
        [
            "Group parameter, call site passes group with inline auth",
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

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(api);
                    }
                }
            }"
        ],
        [
            "Group parameter, call site passes group with separate auth",
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

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"");
                        api.RequireAuthorization();
                        Endpoints.MapItems(api);
                    }
                }
            }"
        ],
        [
            "Group parameter, call site passes nested group whose parent has auth",
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

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        var v1 = api.MapGroup(""/v1"");
                        Endpoints.MapItems(v1);
                    }
                }
            }"
        ],
        [
            "Group parameter with separate auth call in the method body",
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
                        group.RequireAuthorization();
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Multiple endpoints all covered by group-level auth",
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
                        var group = builder.MapGroup(""/api"").RequireAuthorization();
                        group.MapGet(""/items"", () => Results.Ok());
                        group.MapPost(""/items"", () => Results.Ok());
                        group.MapDelete(""/items/1"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Non-RouteGroupBuilder receiver (should not trigger DSA014)",
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
            "Inline MapGroup().RequireAuthorization().MapGet() with auth in preceding chain",
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
                        builder.MapGroup(""/api"").RequireAuthorization()
                            .MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Group parameter, multiple call sites, ALL have auth",
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

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(api);

                        var admin = builder.MapGroup(""/admin"").RequireAuthorization(""AdminPolicy"");
                        Endpoints.MapItems(admin);
                    }
                }
            }"
        ],
        [
            "Group extension method parameter with auth at call site",
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
                        group.MapGet(""/items"", () => Results.Ok());
                        return group;
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        api.MapItems();
                    }
                }
            }"
        ],
        [
            "Group parameter, recursive pass-through, outermost call site has auth",
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

                    public static void ConfigureAll(RouteGroupBuilder outerGroup)
                    {
                        MapItems(outerGroup);
                    }
                }

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.ConfigureAll(api);
                    }
                }
            }"
        ],
        [
            "Group parameter, named argument at call site with auth",
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

                public class Startup
                {
                    public void Configure(IEndpointRouteBuilder builder)
                    {
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        Endpoints.MapItems(group: api);
                    }
                }
            }"
        ],
        [
            "Three-level nested groups: outermost has auth",
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
                        var api = builder.MapGroup(""/api"").RequireAuthorization();
                        var v1 = api.MapGroup(""/v1"");
                        var items = v1.MapGroup(""/items"");
                        items.MapGet(""/"", () => Results.Ok());
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
        var test = new CSharpAnalyzerVerifier<DSA014Analyzer>.Test();
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
