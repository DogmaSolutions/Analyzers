using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace DogmaSolutions.Analyzers.Test;

/// <summary>Sources and helpers shared by the tests of DSA043.</summary>
internal static class DSA043TestSupport
{
    /// <summary>A minimal stand-in of Microsoft.Extensions.Logging, plus an HTTP client and a service locator.</summary>
    internal const string FakeLoggingSource = @"
namespace Microsoft.Extensions.Logging
{
    public interface ILogger { }
    public interface ILogger<T> : ILogger { }
    public interface ILoggerFactory { ILogger CreateLogger(string category); }
    public static class LoggerExtensions { public static void LogInformation(this ILogger logger, string message) { } }
    public static class LoggerMessage { public static System.Action<ILogger> Define(string format) => null; }
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class LoggerMessageAttribute : System.Attribute { }
    namespace Abstractions { public class NullLogger<T> : ILogger<T> { public static readonly NullLogger<T> Instance = new NullLogger<T>(); public static NullLogger<T> Shared { get; } } }
}
namespace Serilog { public interface ILog { } public static class Log { public static void Information(string message) { } } }
namespace MyCompany.Diagnostics { public interface IAppLogger { } }
namespace System.Net.Http { public class HttpClient { public System.Threading.Tasks.Task<string> GetStringAsync(string url) => null; } }
namespace System.Net.Http.Inner { public class Thing { public void Go() { } } }
namespace Fakes { public static class Services { public static T Get<T>(object provider) => default(T); } }
";

    /// <summary>Thresholds that any small class with a couple of decisions reaches.</summary>
    internal static readonly string[] LowThresholds =
    {
        "dotnet_diagnostic.DSA043.min_cyclomatic_complexity = 4",
        "dotnet_diagnostic.DSA043.min_method_complexity = 0",
        "dotnet_diagnostic.DSA043.min_lines = 0",
    };

    /// <summary>A class body with a method of the given cyclomatic complexity (1 + the number of ifs).</summary>
    internal static string Branchy(int complexity)
    {
        var ifs = string.Concat(Enumerable.Range(1, complexity - 1).Select(i => "if (x == " + i + ") { x++; }\n        "));
        return "public int Run(int x)\n    {\n        " + ifs + "return x;\n    }";
    }

    internal static INamedTypeSymbol GetType(string source, string typeName = "C")
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CreateCompilation(tree);
        return compilation.GetTypeByMetadataName(typeName);
    }

    // the assemblies of the running framework: enough to compile the small samples, and no package to resolve
    private static readonly System.Collections.Generic.List<MetadataReference> _references =
        ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
        .Split(System.IO.Path.PathSeparator)
        .Where(path => System.IO.Path.GetFileName(path).StartsWith("System.", StringComparison.Ordinal) || System.IO.Path.GetFileName(path) == "mscorlib.dll" || System.IO.Path.GetFileName(path) == "netstandard.dll")
        .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
        .ToList();

    internal static CSharpCompilation CreateCompilation(params SyntaxTree[] trees) =>
        CSharpCompilation.Create("t", trees, _references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
}
