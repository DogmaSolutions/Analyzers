using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA005GetTimestampCodeFixTests
{
    private const string GetTimestampEquivalenceKey = DSA005Analyzer.DiagnosticId + "_GetTimestamp";

    private static async Task VerifyGetTimestampFixAsync(string source, string fixedSource)
    {
        var test = new CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = GetTimestampEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>
                .Diagnostic(DSA005Analyzer.DiagnosticId).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }

    #region 2 variables (1 pair)

    [TestMethod]
    public async Task TwoVars_StartEnd_AssignedSubtraction_UtcNow()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var operationEnd = DateTime.UtcNow;
            var elapsed = operationEnd - operationStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var elapsed = Stopwatch.GetElapsedTime(operationStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_StartEnd_InlineSubtraction_UtcNow()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void Log(string msg, object val) {}
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var operationEnd = DateTime.UtcNow;
            Log(""Elapsed"", operationEnd - operationStart);
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void Log(string msg, object val) {}
        private void DoWork() {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var operationElapsed = Stopwatch.GetElapsedTime(operationStart);
            Log(""Elapsed"", operationElapsed);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_StartEnd_AssignedSubtraction_Now()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        {|#0:public void Test()
        {
            var taskStart = DateTime.Now;
            DoWork();
            var taskEnd = DateTime.Now;
            var elapsed = taskEnd - taskStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        public void Test()
        {
            var taskStart = Stopwatch.GetTimestamp();
            DoWork();
            var elapsed = Stopwatch.GetElapsedTime(taskStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_DateTimeOffset_UtcNow()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTimeOffset.UtcNow;
            DoWork();
            var operationEnd = DateTimeOffset.UtcNow;
            var elapsed = operationEnd - operationStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var elapsed = Stopwatch.GetElapsedTime(operationStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_MultipleInlineSubtractions()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void Log(string msg, object val) {}
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var operationEnd = DateTime.UtcNow;
            Log(""First"", operationEnd - operationStart);
            Log(""Second"", operationEnd - operationStart);
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void Log(string msg, object val) {}
        private void DoWork() {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var operationElapsed = Stopwatch.GetElapsedTime(operationStart);
            Log(""First"", operationElapsed);
            Log(""Second"", operationElapsed);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    #endregion

    #region 4 variables (2 pairs)

    [TestMethod]
    public async Task FourVars_TwoPairs_BothAssigned()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadEnd = DateTime.UtcNow;
            var downloadElapsed = downloadEnd - downloadStart;

            var uploadStart = DateTime.UtcNow;
            DoWorkB();
            var uploadEnd = DateTime.UtcNow;
            var uploadElapsed = uploadEnd - uploadStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}

        public void Test()
        {
            var downloadStart = Stopwatch.GetTimestamp();
            DoWorkA();
            var downloadElapsed = Stopwatch.GetElapsedTime(downloadStart);

            var uploadStart = Stopwatch.GetTimestamp();
            DoWorkB();
            var uploadElapsed = Stopwatch.GetElapsedTime(uploadStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    #endregion

    #region Inline DateTime subtraction (no end variable)

    [TestMethod]
    public async Task InlineDateTime_AssignedToVariable()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var elapsed = DateTime.UtcNow - operationStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var elapsed = Stopwatch.GetElapsedTime(operationStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_UsedAsArgument()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void Log(string msg, object val) {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            Log(""Elapsed"", DateTime.UtcNow - operationStart);
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void Log(string msg, object val) {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            Log(""Elapsed"", Stopwatch.GetElapsedTime(operationStart));
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_InsideDoWhileLoop_ComparisonPattern()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        {|#0:public void Test()
        {
            var startCheck = DateTime.UtcNow;
            do
            {
                DoWork();

                if (DateTime.UtcNow - startCheck > TimeSpan.FromSeconds(30))
                    throw new TimeoutException();
            } while (true);
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        public void Test()
        {
            var startCheck = Stopwatch.GetTimestamp();
            do
            {
                DoWork();

                if (Stopwatch.GetElapsedTime(startCheck) > TimeSpan.FromSeconds(30))
                    throw new TimeoutException();
            } while (true);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_TwoPairs_NoEndVariables()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadElapsed = DateTime.UtcNow - downloadStart;

            var uploadStart = DateTime.UtcNow;
            DoWorkB();
            var uploadElapsed = DateTime.UtcNow - uploadStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}

        public void Test()
        {
            var downloadStart = Stopwatch.GetTimestamp();
            DoWorkA();
            var downloadElapsed = Stopwatch.GetElapsedTime(downloadStart);

            var uploadStart = Stopwatch.GetTimestamp();
            DoWorkB();
            var uploadElapsed = Stopwatch.GetElapsedTime(uploadStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    #endregion

    #region Edge cases

    [TestMethod]
    public async Task ExistingUsingDiagnostics_NotDuplicated()
    {
        var source = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var operationEnd = DateTime.UtcNow;
            var elapsed = operationEnd - operationStart;
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var elapsed = Stopwatch.GetElapsedTime(operationStart);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_WithExtraDateTimeCall()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void DoSomething(DateTime dt) {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var elapsed = DateTime.UtcNow - operationStart;
            DoSomething(DateTime.UtcNow);
        }|}
    }
}";

        var fixedSource = @"
using System;
using System.Diagnostics;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void DoSomething(DateTime dt) {}

        public void Test()
        {
            var operationStart = Stopwatch.GetTimestamp();
            DoWork();
            var elapsed = Stopwatch.GetElapsedTime(operationStart);
            DoSomething(DateTime.UtcNow);
        }
    }
}";

        await VerifyGetTimestampFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    #endregion
}
