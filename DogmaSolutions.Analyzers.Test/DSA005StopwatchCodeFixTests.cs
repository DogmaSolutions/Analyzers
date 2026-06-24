using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA005StopwatchCodeFixTests
{
    private const string StopwatchEquivalenceKey = DSA005Analyzer.DiagnosticId + "_Stopwatch";

    private static async Task VerifyStopwatchFixAsync(string source, string fixedSource)
    {
        var test = new CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = StopwatchEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>
                .Diagnostic(DSA005Analyzer.DiagnosticId).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }


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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var operationElapsed = operationStart.Elapsed;
            Log(""Elapsed"", operationElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var taskStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = taskStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_BeginFinish_AssignedSubtraction()
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
            var processBegin = DateTime.UtcNow;
            DoWork();
            var processFinish = DateTime.UtcNow;
            var duration = processFinish - processBegin;
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
            var processBegin = Stopwatch.StartNew();
            DoWork();
            var duration = processBegin.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_InitComplete_AssignedSubtraction()
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
            var loadComplete = DateTime.UtcNow;
            var duration = loadComplete - loadInit;
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
            var duration = loadInit.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoVars_StartStop_AssignedSubtraction()
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
            var timerStart = DateTime.UtcNow;
            DoWork();
            var timerStop = DateTime.UtcNow;
            var elapsed = timerStop - timerStart;
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
            var timerStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = timerStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var operationElapsed = operationStart.Elapsed;
            Log(""First"", operationElapsed);
            Log(""Second"", operationElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }



    [TestMethod]
    public async Task ThreeVars_OnePairPlusExtra_AssignedSubtraction()
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
            var operationEnd = DateTime.UtcNow;
            var elapsed = operationEnd - operationStart;
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
    public async Task ThreeVars_OnePairPlusExtraBeforeStart()
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
            DoSomething(DateTime.UtcNow);
            var taskStart = DateTime.UtcNow;
            DoWork();
            var taskEnd = DateTime.UtcNow;
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
        private void DoSomething(DateTime dt) {}

        public void Test()
        {
            DoSomething(DateTime.UtcNow);
            var taskStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = taskStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }



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
    public async Task FourVars_TwoPairs_BothInline()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void Log(string msg, object val) {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadEnd = DateTime.UtcNow;
            Log(""Download"", downloadEnd - downloadStart);

            var uploadStart = DateTime.UtcNow;
            DoWorkB();
            var uploadEnd = DateTime.UtcNow;
            Log(""Upload"", uploadEnd - uploadStart);
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
        private void Log(string msg, object val) {}

        public void Test()
        {
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;
            Log(""Download"", downloadElapsed);

            var uploadStart = Stopwatch.StartNew();
            DoWorkB();
            var uploadElapsed = uploadStart.Elapsed;
            Log(""Upload"", uploadElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FourVars_TwoPairs_MixedAssignedAndInline()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void Log(string msg, object val) {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadEnd = DateTime.UtcNow;
            var downloadElapsed = downloadEnd - downloadStart;

            var uploadStart = DateTime.UtcNow;
            DoWorkB();
            var uploadEnd = DateTime.UtcNow;
            Log(""Upload"", uploadEnd - uploadStart);
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
        private void Log(string msg, object val) {}

        public void Test()
        {
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;

            var uploadStart = Stopwatch.StartNew();
            DoWorkB();
            var uploadElapsed = uploadStart.Elapsed;
            Log(""Upload"", uploadElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FourVars_TwoPairs_DifferentKeywords()
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
            var phaseBegin = DateTime.UtcNow;
            DoWorkA();
            var phaseFinish = DateTime.UtcNow;
            var phaseDuration = phaseFinish - phaseBegin;

            var taskInit = DateTime.UtcNow;
            DoWorkB();
            var taskComplete = DateTime.UtcNow;
            var taskDuration = taskComplete - taskInit;
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
            var phaseBegin = Stopwatch.StartNew();
            DoWorkA();
            var phaseDuration = phaseBegin.Elapsed;

            var taskInit = Stopwatch.StartNew();
            DoWorkB();
            var taskDuration = taskInit.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }



    [TestMethod]
    public async Task FiveVars_TwoPairsPlusExtra()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void Snapshot(DateTime dt) {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadEnd = DateTime.UtcNow;
            var downloadElapsed = downloadEnd - downloadStart;

            Snapshot(DateTime.UtcNow);

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
        private void Snapshot(DateTime dt) {}

        public void Test()
        {
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;

            Snapshot(DateTime.UtcNow);

            var uploadStart = Stopwatch.StartNew();
            DoWorkB();
            var uploadElapsed = uploadStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

}
