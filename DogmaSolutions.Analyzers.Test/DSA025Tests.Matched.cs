using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA025Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "LogInformation with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string userName)
                    {
                        _logger.LogInformation({|#0:$""User {userName} logged in""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "LogWarning with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int count)
                    {
                        _logger.LogWarning({|#0:$""Threshold exceeded: {count}""|});
                    }
                }
            }",
            "LogWarning"
        ],
        [
            "LogError with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex)
                    {
                        _logger.LogError({|#0:$""Operation failed: {ex.Message}""|});
                    }
                }
            }",
            "LogError"
        ],
        [
            "LogDebug with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string id)
                    {
                        _logger.LogDebug({|#0:$""Processing item {id}""|});
                    }
                }
            }",
            "LogDebug"
        ],
        [
            "LogCritical with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string service)
                    {
                        _logger.LogCritical({|#0:$""Service {service} is down""|});
                    }
                }
            }",
            "LogCritical"
        ],
        [
            "LogTrace with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int x, int y)
                    {
                        _logger.LogTrace({|#0:$""Coordinates: {x}, {y}""|});
                    }
                }
            }",
            "LogTrace"
        ],
        [
            "LogInformation with member access in interpolation",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class User { public string Name; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(User user)
                    {
                        _logger.LogInformation({|#0:$""User {user.Name} logged in""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "LogError with multiple interpolation holes",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string userId, string action, int duration)
                    {
                        _logger.LogError({|#0:$""User {userId} action {action} took {duration}ms""|});
                    }
                }
            }",
            "LogError"
        ],
        [
            "ILogger<T> with interpolated string",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger<MyService> _logger;
                    public void Process(string item)
                    {
                        _logger.LogInformation({|#0:$""Processing {item}""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "Generic Log method with LogLevel parameter",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string item)
                    {
                        _logger.Log(LogLevel.Information, {|#0:$""Processing {item}""|});
                    }
                }
            }",
            "Log"
        ],
        [
            "LogError with exception and interpolated message",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex, string op)
                    {
                        _logger.LogError(ex, {|#0:$""Operation {op} failed""|});
                    }
                }
            }",
            "LogError"
        ],
        [
            "Ternary expression in interpolation hole",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(bool isAdmin, string name)
                    {
                        _logger.LogInformation({|#0:$""User {(isAdmin ? ""admin"" : name)} logged in""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "Null-conditional in interpolation hole",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class User { public string Name; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(User user)
                    {
                        _logger.LogInformation({|#0:$""User: {user?.Name}""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "Verbatim interpolated string $@",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string path)
                    {
                        _logger.LogInformation({|#0:$@""Path: {path}\subdir""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "LogError with EventId and exception",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex, string msg)
                    {
                        _logger.LogError(new EventId(1), ex, {|#0:$""Error: {msg}""|});
                    }
                }
            }",
            "LogError"
        ],
        [
            "Log with LogLevel and EventId",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string x)
                    {
                        _logger.Log(LogLevel.Information, new EventId(1), {|#0:$""Msg: {x}""|});
                    }
                }
            }",
            "Log"
        ],
        [
            "Log with LogLevel EventId and Exception",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex, string msg)
                    {
                        _logger.Log(LogLevel.Error, new EventId(1), ex, {|#0:$""Error: {msg}""|});
                    }
                }
            }",
            "Log"
        ],
        [
            "Named argument syntax message:",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation(message: {|#0:$""User {name}""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "Interpolation with null-coalescing operator",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation({|#0:$""User: {name ?? ""unknown""}""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "Interpolation with ToString call",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int count)
                    {
                        _logger.LogInformation({|#0:$""Count: {count.ToString()}""|});
                    }
                }
            }",
            "LogInformation"
        ],
        [
            "Interpolation mixing nameof constant with runtime value",
            @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string userId)
                    {
                        _logger.LogInformation({|#0:$""{nameof(MyService)} processing user {userId}""|});
                    }
                }
            }",
            "LogInformation"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string methodName
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

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA025Analyzer>.Diagnostic(DSA025Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments(methodName));

        await test.RunAsync().ConfigureAwait(false);
    }
}
