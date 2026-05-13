using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA025CodeFixTests
{
    private static ReferenceAssemblies GetReferenceAssemblies() =>
        ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.Extensions.Logging.Abstractions", "8.0.0")
            }
        ]);

    [TestMethod]
    public async Task ConvertsSingleInterpolationHole()
    {
        var source = @"
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
            }";

        var fixedSource = @"
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
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsMultipleInterpolationHoles()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string userId, string action, int duration)
                    {
                        _logger.LogError(""User {UserId} action {Action} took {Duration}ms"", userId, action, duration);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogError"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsMemberAccessExpression()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class User { public string Name; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(User user)
                    {
                        _logger.LogInformation(""User {UserName} logged in"", user.Name);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsMethodCallExpression()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class User { public string GetFullName() => null; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(User user)
                    {
                        _logger.LogInformation({|#0:$""User {user.GetFullName()} logged in""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class User { public string GetFullName() => null; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(User user)
                    {
                        _logger.LogInformation(""User {UserGetFullName} logged in"", user.GetFullName());
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PreservesFormatSpecifier()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(decimal price, System.DateTime timestamp)
                    {
                        _logger.LogInformation({|#0:$""Price: {price:C2} at {timestamp:yyyy-MM-dd}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(decimal price, System.DateTime timestamp)
                    {
                        _logger.LogInformation(""Price: {Price:C2} at {Timestamp:yyyy-MM-dd}"", price, timestamp);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DeduplicatesIdenticalPlaceholderNames()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class Point { public int X; public int Y; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Point start, Point end)
                    {
                        _logger.LogInformation({|#0:$""From ({start.X}, {start.Y}) to ({end.X}, {end.Y})""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class Point { public int X; public int Y; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Point start, Point end)
                    {
                        _logger.LogInformation(""From ({StartX}, {StartY}) to ({EndX}, {EndY})"", start.X, start.Y, end.X, end.Y);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DeduplicatesWhenSameNameGenerated()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name, string name2)
                    {
                        _logger.LogInformation({|#0:$""First: {name}, Second: {name}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name, string name2)
                    {
                        _logger.LogInformation(""First: {Name}, Second: {Name2}"", name, name);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsArithmeticExpression()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class Item { public decimal Price; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Item item, int quantity)
                    {
                        _logger.LogInformation({|#0:$""Total: {item.Price * quantity}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class Item { public decimal Price; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Item item, int quantity)
                    {
                        _logger.LogInformation(""Total: {ItemPriceQuantity}"", item.Price * quantity);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsIndexerExpression()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Dictionary<string, string> dict)
                    {
                        _logger.LogInformation({|#0:$""Value: {dict[""key""]}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Dictionary<string, string> dict)
                    {
                        _logger.LogInformation(""Value: {DictKey}"", dict[""key""]);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task PreservesExistingExceptionArgument()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex, string operation)
                    {
                        _logger.LogError(ex, {|#0:$""Operation {operation} failed""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex, string operation)
                    {
                        _logger.LogError(ex, ""Operation {Operation} failed"", operation);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogError"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsDeepMemberAccessChain()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class Inner { public string Message; }
                public class MyException { public Inner InnerException; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(MyException e)
                    {
                        _logger.LogError({|#0:$""Inner error: {e.InnerException.Message}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class Inner { public string Message; }
                public class MyException { public Inner InnerException; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(MyException e)
                    {
                        _logger.LogError(""Inner error: {EInnerExceptionMessage}"", e.InnerException.Message);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogError"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsMathMaxExpression()
    {
        var source = @"
            using System;
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int a, int b)
                    {
                        _logger.LogInformation({|#0:$""Max: {Math.Max(a, b)}""|});
                    }
                }
            }";

        var fixedSource = @"
            using System;
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int a, int b)
                    {
                        _logger.LogInformation(""Max: {MathMaxAB}"", Math.Max(a, b));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsCastExpression()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public enum Status { Active, Inactive }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Status status)
                    {
                        _logger.LogDebug({|#0:$""Status code: {(int)status}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public enum Status { Active, Inactive }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(Status status)
                    {
                        _logger.LogDebug(""Status code: {IntStatus}"", (int)status);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogDebug"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsGenericLogMethod()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string item)
                    {
                        _logger.Log(LogLevel.Information, ""Processing {Item}"", item);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("Log"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsTernaryExpression()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(bool isAdmin, string name)
                    {
                        _logger.LogInformation(""User {IsAdminAdminName} logged in"", (isAdmin ? ""admin"" : name));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsNullConditionalExpression()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class User { public string Name; }
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(User user)
                    {
                        _logger.LogInformation(""User: {UserName}"", user?.Name);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DeduplicatesThreeIdenticalNames()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation({|#0:$""A: {name}, B: {name}, C: {name}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation(""A: {Name}, B: {Name2}, C: {Name3}"", name, name, name);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DropsAlignmentSpecifier()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name, int count)
                    {
                        _logger.LogInformation({|#0:$""Name: {name,20} Count: {count,-10:N0}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name, int count)
                    {
                        _logger.LogInformation(""Name: {Name} Count: {Count:N0}"", name, count);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsVerbatimInterpolatedString()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string path)
                    {
                        _logger.LogInformation({|#0:$@""Loading file: {path}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string path)
                    {
                        _logger.LogInformation(""Loading file: {Path}"", path);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsFullLogOverload()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(System.Exception ex, string msg)
                    {
                        _logger.Log(LogLevel.Error, new EventId(1), ex, ""Error: {Msg}"", msg);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("Log"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsNamedArgumentMessage()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation(message: ""User {Name}"", name);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsNullCoalescingInHole()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string name)
                    {
                        _logger.LogInformation(""User: {NameUnknown}"", name ?? ""unknown"");
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsNameofMixedWithRuntimeValue()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string items)
                    {
                        _logger.LogInformation({|#0:$""{nameof(MyService)} processing {items}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string items)
                    {
                        _logger.LogInformation(""{NameofMyService} processing {Items}"", nameof(MyService), items);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsToStringInHole()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int count)
                    {
                        _logger.LogInformation(""Count: {CountToString}"", count.ToString());
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsILoggerGenericReceiver()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger<MyService> _logger;
                    public void Process(string x)
                    {
                        _logger.LogInformation({|#0:$""Value: {x}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger<MyService> _logger;
                    public void Process(string x)
                    {
                        _logger.LogInformation(""Value: {X}"", x);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConvertsVerbatimInterpolatedStringWithBackslash()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string path)
                    {
                        _logger.LogInformation({|#0:$@""Loading: {path}\subdir""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(string path)
                    {
                        _logger.LogInformation(""Loading: {Path}\\subdir"", path);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DropsAlignmentSpecifierOnly()
    {
        var source = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int count)
                    {
                        _logger.LogInformation({|#0:$""Count: {count,-10}""|});
                    }
                }
            }";

        var fixedSource = @"
            using Microsoft.Extensions.Logging;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly ILogger _logger;
                    public void Process(int count)
                    {
                        _logger.LogInformation(""Count: {Count}"", count);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = GetReferenceAssemblies();
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA025Analyzer, DSA025CodeFixProvider>
                .Diagnostic(DSA025Analyzer.DiagnosticId).WithLocation(0).WithArguments("LogInformation"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
