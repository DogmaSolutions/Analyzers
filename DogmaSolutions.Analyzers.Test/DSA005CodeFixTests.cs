using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA005CodeFixTests
{
    [TestMethod]
    public async Task Extracts_DateTime_UtcNow()
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
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>
                .Diagnostic(DSA005Analyzer.DiagnosticId).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Extracts_DateTime_Now()
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
            DoSomething(DateTime.Now);
            DoOtherThings(DateTime.Now);
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
            var now = DateTime.Now;
            DoSomething(now);
            DoOtherThings(now);
        }
    }
}";

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
    public async Task Extracts_DateTimeOffset_UtcNow()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTimeOffset dt) {}
        private void DoOtherThings(DateTimeOffset dt) {}

        {|#0:public void Test()
        {
            DoSomething(DateTimeOffset.UtcNow);
            DoOtherThings(DateTimeOffset.UtcNow);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTimeOffset dt) {}
        private void DoOtherThings(DateTimeOffset dt) {}

        public void Test()
        {
            var utcNow = DateTimeOffset.UtcNow;
            DoSomething(utcNow);
            DoOtherThings(utcNow);
        }
    }
}";

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
    public async Task Extracts_DateTimeOffset_Now()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTimeOffset dt) {}
        private void DoOtherThings(DateTimeOffset dt) {}

        {|#0:public void Test()
        {
            DoSomething(DateTimeOffset.Now);
            DoOtherThings(DateTimeOffset.Now);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTimeOffset dt) {}
        private void DoOtherThings(DateTimeOffset dt) {}

        public void Test()
        {
            var now = DateTimeOffset.Now;
            DoSomething(now);
            DoOtherThings(now);
        }
    }
}";

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
    public async Task Extracts_ThreeOccurrences()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoA(DateTime dt) {}
        private void DoB(DateTime dt) {}
        private void DoC(DateTime dt) {}

        {|#0:public void Test()
        {
            DoA(DateTime.UtcNow);
            DoB(DateTime.UtcNow);
            DoC(DateTime.UtcNow);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoA(DateTime dt) {}
        private void DoB(DateTime dt) {}
        private void DoC(DateTime dt) {}

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            DoA(utcNow);
            DoB(utcNow);
            DoC(utcNow);
        }
    }
}";

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
    public async Task Extracts_WithNameConflict()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoSomething(DateTime dt) {}
        private void DoOtherThings(DateTime dt) {}

        {|#0:public void Test(int utcNow)
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

        public void Test(int utcNow)
        {
            var utcNow1 = DateTime.UtcNow;
            DoSomething(utcNow1);
            DoOtherThings(utcNow1);
        }
    }
}";

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
    public async Task Extracts_InForLoop()
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
            for (int i = 0; i < 10; i++)
            {
                DoOtherThings(DateTime.UtcNow);
            }
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
            for (int i = 0; i < 10; i++)
            {
                DoOtherThings(utcNow);
            }
        }
    }
}";

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
    public async Task Extracts_BothNowAndUtcNow()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoA(DateTime dt) {}
        private void DoB(DateTime dt) {}

        {|#0:public void Test()
        {
            DoA(DateTime.UtcNow);
            DoB(DateTime.Now);
            DoA(DateTime.UtcNow);
            DoB(DateTime.Now);
        }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void DoA(DateTime dt) {}
        private void DoB(DateTime dt) {}

        public void Test()
        {
            var utcNow = DateTime.UtcNow;
            var now = DateTime.Now;
            DoA(utcNow);
            DoB(now);
            DoA(utcNow);
            DoB(now);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA005Analyzer, DSA005CodeFixProvider>
                .Diagnostic(DSA005Analyzer.DiagnosticId).WithLocation(0));
        await test.RunAsync().ConfigureAwait(false);
    }
}
