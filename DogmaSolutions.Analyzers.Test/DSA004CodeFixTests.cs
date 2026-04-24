using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA004CodeFixTests
{
    [TestMethod]
    public async Task Fixes_DateTime_Now()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public DateTime GetTime()
        {
            return {|#0:DateTime.Now|};
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public DateTime GetTime()
        {
            return DateTime.UtcNow;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_SystemDateTime_Now()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public DateTime GetTime()
        {
            return {|#0:System.DateTime.Now|};
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public DateTime GetTime()
        {
            return System.DateTime.UtcNow;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_InAssignment()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Process()
        {
            var now = {|#0:DateTime.Now|};
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Process()
        {
            var now = DateTime.UtcNow;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_InCondition()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Process()
        {
            if ({|#0:DateTime.Now|} > DateTime.MinValue)
                return;
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Process()
        {
            if (DateTime.UtcNow > DateTime.MinValue)
                return;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_UsingStatic_Now()
    {
        var source = @"
using System;
using static System.DateTime;
namespace TestApp
{
    public class MyClass
    {
        public DateTime GetTime()
        {
            return {|#0:Now|};
        }
    }
}";

        var fixedSource = @"
using System;
using static System.DateTime;
namespace TestApp
{
    public class MyClass
    {
        public DateTime GetTime()
        {
            return UtcNow;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_PropertyAccessOnNow()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public int GetYear()
        {
            return {|#0:DateTime.Now|}.Year;
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public int GetYear()
        {
            return DateTime.UtcNow.Year;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_InMethodArgument()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void Use(DateTime dt) { }
        public void Process()
        {
            Use({|#0:DateTime.Now|});
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        private void Use(DateTime dt) { }
        public void Process()
        {
            Use(DateTime.UtcNow);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA004Analyzer, DSA004CodeFixProvider>
                .Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
