using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA005StopwatchCodeFixTests
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

    #endregion

    #region 3 variables (1 pair + 1 extra)

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

    #endregion

    #region 5 variables (2 pairs + 1 extra)

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

    [TestMethod]
    public async Task FiveVars_TwoPairsPlusExtraBetween()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void DoSomething(DateTime dt) {}

        {|#0:public void Test()
        {
            var phaseBegin = DateTime.UtcNow;
            DoWorkA();
            var phaseFinish = DateTime.UtcNow;
            var phaseDuration = phaseFinish - phaseBegin;

            DoSomething(DateTime.UtcNow);

            var taskStart = DateTime.UtcNow;
            DoWorkB();
            var taskEnd = DateTime.UtcNow;
            var taskDuration = taskEnd - taskStart;
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
        private void DoSomething(DateTime dt) {}

        public void Test()
        {
            var phaseBegin = Stopwatch.StartNew();
            DoWorkA();
            var phaseDuration = phaseBegin.Elapsed;

            DoSomething(DateTime.UtcNow);

            var taskStart = Stopwatch.StartNew();
            DoWorkB();
            var taskDuration = taskStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    #endregion

    #region 6 variables (3 pairs)

    [TestMethod]
    public async Task SixVars_ThreePairs_AllAssigned()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void DoWorkC() {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadEnd = DateTime.UtcNow;
            var downloadElapsed = downloadEnd - downloadStart;

            var processBegin = DateTime.UtcNow;
            DoWorkB();
            var processFinish = DateTime.UtcNow;
            var processDuration = processFinish - processBegin;

            var uploadInit = DateTime.UtcNow;
            DoWorkC();
            var uploadComplete = DateTime.UtcNow;
            var uploadDuration = uploadComplete - uploadInit;
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
        private void DoWorkC() {}

        public void Test()
        {
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;

            var processBegin = Stopwatch.StartNew();
            DoWorkB();
            var processDuration = processBegin.Elapsed;

            var uploadInit = Stopwatch.StartNew();
            DoWorkC();
            var uploadDuration = uploadInit.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SixVars_ThreePairs_AllInline()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void DoWorkC() {}
        private void Log(string msg, object val) {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadEnd = DateTime.UtcNow;
            Log(""Download"", downloadEnd - downloadStart);

            var processBegin = DateTime.UtcNow;
            DoWorkB();
            var processFinish = DateTime.UtcNow;
            Log(""Process"", processFinish - processBegin);

            var uploadInit = DateTime.UtcNow;
            DoWorkC();
            var uploadComplete = DateTime.UtcNow;
            Log(""Upload"", uploadComplete - uploadInit);
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
        private void DoWorkC() {}
        private void Log(string msg, object val) {}

        public void Test()
        {
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;
            Log(""Download"", downloadElapsed);

            var processBegin = Stopwatch.StartNew();
            DoWorkB();
            var processElapsed = processBegin.Elapsed;
            Log(""Process"", processElapsed);

            var uploadInit = Stopwatch.StartNew();
            DoWorkC();
            var uploadElapsed = uploadInit.Elapsed;
            Log(""Upload"", uploadElapsed);
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SixVars_ThreePairs_MixedKeywordsAndStyles()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWorkA() {}
        private void DoWorkB() {}
        private void DoWorkC() {}
        private void Log(string msg, object val) {}

        {|#0:public void Test()
        {
            var downloadStart = DateTime.UtcNow;
            DoWorkA();
            var downloadStop = DateTime.UtcNow;
            var downloadElapsed = downloadStop - downloadStart;

            var processBegin = DateTime.UtcNow;
            DoWorkB();
            var processEnd = DateTime.UtcNow;
            Log(""Process"", processEnd - processBegin);

            var uploadInit = DateTime.UtcNow;
            DoWorkC();
            var uploadComplete = DateTime.UtcNow;
            var uploadDuration = uploadComplete - uploadInit;
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
        private void DoWorkC() {}
        private void Log(string msg, object val) {}

        public void Test()
        {
            var downloadStart = Stopwatch.StartNew();
            DoWorkA();
            var downloadElapsed = downloadStart.Elapsed;

            var processBegin = Stopwatch.StartNew();
            DoWorkB();
            var processElapsed = processBegin.Elapsed;
            Log(""Process"", processElapsed);

            var uploadInit = Stopwatch.StartNew();
            DoWorkC();
            var uploadDuration = uploadInit.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
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
            var operationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = operationStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CaseInsensitiveKeywordMatching()
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
            var START_time = DateTime.UtcNow;
            DoWork();
            var END_time = DateTime.UtcNow;
            var elapsed = END_time - START_time;
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
            var START_time = Stopwatch.StartNew();
            DoWork();
            var elapsed = START_time.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoStopwatchFix_WhenNoMatchingNames()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTime dt) {}
        private void DoOtherThings(DateTime dt) {}

        {|#0:public void Test()
        {
            DoSomething(DateTime.UtcNow);
            DoOtherThings(DateTime.UtcNow);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTime dt) {}
        private void DoOtherThings(DateTime dt) {}

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            DoSomething(utcNow);
            DoOtherThings(utcNow);
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
    public async Task NoStopwatchFix_WhenStartVarUsedElsewhere()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoWork() {}
        private void Log(string msg, DateTime dt) {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var operationEnd = DateTime.UtcNow;
            var elapsed = operationEnd - operationStart;
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
        private void Log(string msg, DateTime dt) {}

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var operationStart = utcNow;
            DoWork();
            var operationEnd = utcNow;
            var elapsed = operationEnd - operationStart;
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
    public async Task NoStopwatchFix_WhenNoSubtraction()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void Log(string msg, DateTime dt) {}
        private void DoWork() {}

        {|#0:public void Test()
        {
            var operationStart = DateTime.UtcNow;
            DoWork();
            var operationEnd = DateTime.UtcNow;
            Log(""Start"", operationStart);
            Log(""End"", operationEnd);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void Log(string msg, DateTime dt) {}
        private void DoWork() {}

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var operationStart = utcNow;
            DoWork();
            var operationEnd = utcNow;
            Log(""Start"", operationStart);
            Log(""End"", operationEnd);
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
    public async Task NoStopwatchFix_WhenDifferentDateTimeProperties()
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
            var operationEnd = DateTime.Now;
            var elapsed = operationEnd - operationStart;
            var extra = DateTime.UtcNow;
            var extra2 = DateTime.Now;
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

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var now = DateTime.Now;
            var operationStart = utcNow;
            DoWork();
            var operationEnd = now;
            var elapsed = operationEnd - operationStart;
            var extra = utcNow;
            var extra2 = now;
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
    public async Task NoStopwatchFix_WhenEndKeywordInsideWord()
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
            var renderStart = DateTime.UtcNow;
            DoWork();
            var blended = DateTime.UtcNow;
            var elapsed = blended - renderStart;
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

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var renderStart = utcNow;
            DoWork();
            var blended = utcNow;
            var elapsed = blended - renderStart;
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
    public async Task NoStopwatchFix_WhenStartKeywordInsideWord()
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
            var restarted = DateTime.UtcNow;
            DoWork();
            var weekend = DateTime.UtcNow;
            var elapsed = weekend - restarted;
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

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var restarted = utcNow;
            DoWork();
            var weekend = utcNow;
            var elapsed = weekend - restarted;
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
    public async Task Stopwatch_WordBoundary_SnakeCase()
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
            var op_start = DateTime.UtcNow;
            DoWork();
            var op_end = DateTime.UtcNow;
            var elapsed = op_end - op_start;
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
            var op_start = Stopwatch.StartNew();
            DoWork();
            var elapsed = op_start.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Stopwatch_WordBoundary_PascalCase()
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
            var OperationStart = DateTime.UtcNow;
            DoWork();
            var OperationEnd = DateTime.UtcNow;
            var elapsed = OperationEnd - OperationStart;
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
            var OperationStart = Stopwatch.StartNew();
            DoWork();
            var elapsed = OperationStart.Elapsed;
        }
    }
}";

        await VerifyStopwatchFixAsync(source, fixedSource).ConfigureAwait(false);
    }

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

    #endregion

    #region 1 variable (no pair possible - only extract fix)

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

    #endregion
}
