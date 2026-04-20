using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA013Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "MapGet on local builder without auth",
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
                        {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "MapPost on local builder without auth",
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
                        {|#0:builder.MapPost(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapPost"
        ],
        [
            "MapPut on local builder without auth",
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
                        {|#0:builder.MapPut(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapPut"
        ],
        [
            "MapDelete on local builder without auth",
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
                        {|#0:builder.MapDelete(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapDelete"
        ],
        [
            "MapPatch on local builder without auth",
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
                        {|#0:builder.MapPatch(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapPatch"
        ],
        [
            "MapMethods on local builder without auth",
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
                        {|#0:builder.MapMethods(""/api/items"", new[] { ""GET"", ""POST"" }, () => Results.Ok())|};
                    }
                }
            }",
            "MapMethods"
        ],
        [
            "Map on local builder without auth",
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
                        {|#0:builder.Map(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "Map"
        ],
        [
            "MapGet with fluent chain but no auth",
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
                        {|#0:builder.MapGet(""/api/items"", () => Results.Ok())|}
                            .WithName(""GetItems"")
                            .Produces(StatusCodes.Status200OK);
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Inline expression receiver without auth",
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
                        {|#0:GetBuilder().MapGet(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Field receiver without auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public class Startup
                {
                    private readonly IEndpointRouteBuilder _builder = null;
                    public void Configure()
                    {
                        {|#0:_builder.MapGet(""/api/items"", () => Results.Ok())|};
                    }
                }
            }",
            "MapGet"
        ],
        [
            "Property receiver without auth",
            @"
            using Microsoft.AspNetCore.Builder;
            using Microsoft.AspNetCore.Http;
            using Microsoft.AspNetCore.Routing;
            namespace TestApp
            {
                public class Startup
                {
                    private IEndpointRouteBuilder Builder { get; set; }
                    public void Configure()
                    {
                        {|#0:Builder.MapGet(""/api/items"", () => Results.Ok())|};
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
        var test = new CSharpAnalyzerVerifier<DSA013Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.App.Ref", "8.0.0"),
            }
        ]);

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA013Analyzer>.Diagnostic(DSA013Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(mapMethodName));

        await test.RunAsync().ConfigureAwait(false);
    }
}
