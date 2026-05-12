using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA025Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Plain string literal message",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process()
                    {
                        _logger.LogInformation(""Processing started"");
                    }
                }
            }"
        ],
        [
            "Structured template with parameters",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string userName)
                    {
                        _logger.LogInformation(""User {UserName} logged in"", userName);
                    }
                }
            }"
        ],
        [
            "Non-logger method with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(string name)
                    {
                        System.Console.WriteLine($""Hello {name}"");
                    }
                }
            }"
        ],
        [
            "Interpolated string without holes (just text)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process()
                    {
                        _logger.LogInformation($""Processing started"");
                    }
                }
            }"
        ],
        [
            "String variable passed to logger",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string userName)
                    {
                        var msg = $""User {userName} logged in"";
                        _logger.LogInformation(msg);
                    }
                }
            }"
        ],
        [
            "Custom method named LogInformation on non-logger class",
            @"
            namespace TestApp
            {
                public class NotALogger
                {
                    public void LogInformation(string msg) {}
                }
                public class MyService
                {
                    public void Process(string name)
                    {
                        var log = new NotALogger();
                        log.LogInformation($""Hello {name}"");
                    }
                }
            }"
        ],
        [
            "String concatenation (not interpolation)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation(""Hello "" + name);
                    }
                }
            }"
        ],
        [
            "BeginScope with interpolated string (not a log method)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string id)
                    {
                        using (_logger.BeginScope($""Request {id}""))
                        {
                        }
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
        var test = new CSharpAnalyzerVerifier<DSA025Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.Extensions.Logging.Abstractions", "8.0.0")
            }
        ]);

        await test.RunAsync().ConfigureAwait(false);
    }
}
