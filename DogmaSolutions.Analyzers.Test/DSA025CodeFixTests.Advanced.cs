using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA025CodeFixTests
{

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
