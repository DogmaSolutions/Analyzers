using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA005StopwatchCodeFixTests
{

    [TestMethod]
    public async Task InlineSubtraction_ElapsedNameDerivation_Begin()
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
            var taskBegin = DateTime.UtcNow;
            DoWork();
            var taskFinish = DateTime.UtcNow;
            Log(""Duration"", taskFinish - taskBegin);
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
            var taskBegin = Stopwatch.StartNew();
            DoWork();
            var taskElapsed = taskBegin.Elapsed;
            Log(""Duration"", taskElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineSubtraction_ElapsedNameDerivation_Init()
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
            var taskInit = DateTime.UtcNow;
            DoWork();
            var taskComplete = DateTime.UtcNow;
            Log(""Duration"", taskComplete - taskInit);
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
            var taskInit = Stopwatch.StartNew();
            DoWork();
            var taskElapsed = taskInit.Elapsed;
            Log(""Duration"", taskElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }



    [TestMethod]
    public async Task InlineDateTime_AssignedToVariable_UtcNow()
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_AssignedToVariable_Now()
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
            var elapsed = DateTime.Now - taskStart;
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
            var taskStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = taskStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            Log(""Elapsed"", operationStart.Elapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_DateTimeOffset_UtcNow()
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
            var elapsed = DateTimeOffset.UtcNow - operationStart;
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_BeginKeyword()
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
            var taskBegin = DateTime.UtcNow;
            DoWork();
            Log(""Duration"", DateTime.UtcNow - taskBegin);
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
            var taskBegin = Stopwatch.StartNew();
            DoWork();
            Log(""Duration"", taskBegin.Elapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_InitKeyword()
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
            var loadInit = DateTime.UtcNow;
            DoWork();
            var elapsed = DateTime.UtcNow - loadInit;
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
            var loadInit = Stopwatch.StartNew();
            DoWork();
            var elapsed = loadInit.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_MultipleSubtractions()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void DoMoreWork() {}
        private void Log(string msg, object val) {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            Log(""First"", DateTime.UtcNow - operationStart);
            DoMoreWork();
            Log(""Second"", DateTime.UtcNow - operationStart);
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
        private void DoMoreWork() {}
        private void Log(string msg, object val) {}

        public void Test()
        {
            var operationStart = Stopwatch.StartNew();
            DoWork();
            Log(""First"", operationStart.Elapsed);
            DoMoreWork();
            Log(""Second"", operationStart.Elapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
            DoSomething(DateTime.UtcNow);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var startCheck = Stopwatch.StartNew();
            do
            {
                DoWork();

                if (startCheck.Elapsed > TimeSpan.FromSeconds(30))
                    throw new TimeoutException();
            } while (true);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InlineDateTime_StopwatchIsPreferredOverExtract()
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
        }
    }
}";

        // No CodeActionEquivalenceKey → picks the first registered action.
        // Verifies Stopwatch is offered instead of extract when both patterns match.
        var test = new CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>
                .Diagnostic(DSA005Analyzer.DiagnosticId).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoStopwatchFix_InlineDateTime_WhenStartUsedElsewhere()
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
            var elapsed = DateTime.UtcNow - operationStart;
            Log(""Started at"", operationStart);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void Log(string msg, object val) {}

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var operationStart = utcNow;
            DoWork();
            var elapsed = utcNow - operationStart;
            Log(""Started at"", operationStart);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA005Analyzer.DiagnosticId;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>
                .Diagnostic(DSA005Analyzer.DiagnosticId).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
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
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;

            var uploadStart = Stopwatch.StartNew();
            DoWorkB();
            var uploadElapsed = uploadStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }



    [TestMethod]
    public async Task OneVar_NoPairPossible_OnlyExtractFix()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public TimeSpan Test()
        {
            var now = DateTime.UtcNow;
            return now - now;
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA005Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }

}
