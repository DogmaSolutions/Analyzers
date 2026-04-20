using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA013Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "MapGet with RequireAuthorization on local builder",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .RequireAuthorization();
                    }
                }
            }"
        ],
        [
            "MapGet with AllowAnonymous on local builder",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .AllowAnonymous();
                    }
                }
            }"
        ],
        [
            "MapGet with RequireAuthorization with policy",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .RequireAuthorization(""AdminPolicy"");
                    }
                }
            }"
        ],
        [
            "MapGet with RequireAuthorization in the middle of the chain",
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
                        builder.MapGet(""/api/items"", () => Results.Ok())
                            .RequireAuthorization()
                            .WithName(""GetItems"");
                    }
                }
            }"
        ],
        [
            "Receiver is a parameter (DSA015 territory, not DSA013)",
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
                        builder.MapGet(""/api/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Receiver is a RouteGroupBuilder (DSA014 territory, not DSA013)",
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
                        var group = builder.MapGroup(""/api"");
                        group.MapGet(""/items"", () => Results.Ok());
                    }
                }
            }"
        ],
        [
            "Non-Map method invocation (no false positive)",
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
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA013Analyzer>.Test();
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
