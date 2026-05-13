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
        [
            "string.Format passed to logger",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int x)
                    {
                        _logger.LogInformation(string.Format(""x={0}"", x));
                    }
                }
            }"
        ],
        [
            "Constant string field passed to logger",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private const string Msg = ""hello"";
                    private readonly ILogger _logger;
                    public void Process()
                    {
                        _logger.LogInformation(Msg);
                    }
                }
            }"
        ],
        [
            "LoggerMessage.Define high-performance pattern",
            @"
            using System;
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private static readonly Action<ILogger, string, Exception> _logAction =
                        LoggerMessage.Define<string>(LogLevel.Information, new EventId(1), ""User {Name}"");
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logAction(_logger, name, null);
                    }
                }
            }"
        ],
        [
            "Logger wrapper class with LogInformation method",
            @"
            namespace TestApp
            {
                public class MyLogger
                {
                    public void LogInformation(string message) { }
                }
                public class MyService
                {
                    private readonly MyLogger _logger = new MyLogger();
                    public void Process(string name)
                    {
                        _logger.LogInformation($""Hello {name}"");
                    }
                }
            }"
        ],
        [
            "Interpolation with nameof expression (compile-time constant)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string items)
                    {
                        _logger.LogInformation($""Processing {nameof(items)}"");
                    }
                }
            }"
        ],
        [
            "Interpolation with const local (compile-time constant)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process()
                    {
                        const string prefix = ""MyService"";
                        _logger.LogInformation($""{prefix} is starting."");
                    }
                }
            }"
        ],
        [
            "Interpolation with const field (compile-time constant)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private const string ServiceName = ""MyService"";
                    private readonly ILogger _logger;
                    public void Process()
                    {
                        _logger.LogInformation($""{ServiceName} is starting."");
                    }
                }
            }"
        ],
        [
            "Interpolation with multiple compile-time constants",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private const int Version = 2;
                    private readonly ILogger _logger;
                    public void Process()
                    {
                        _logger.LogInformation($""{nameof(MyService)} v{Version} is starting."");
                    }
                }
            }"
        ],
        [
            "Interpolation with nameof of type (compile-time constant)",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class NotificationCenterHostedService
                {
                    private readonly ILogger _logger;
                    public void Start()
                    {
                        _logger.LogInformation($""{nameof(NotificationCenterHostedService)} is starting."");
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
