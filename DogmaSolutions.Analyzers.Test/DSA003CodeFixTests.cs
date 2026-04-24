using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA003CodeFixTests
{
    [TestMethod]
    public async Task Fixes_string_IsNullOrEmpty()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_String_IsNullOrEmpty()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return {|#0:String.IsNullOrEmpty(s)|};
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return String.IsNullOrWhiteSpace(s);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_SystemString_IsNullOrEmpty()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return {|#0:System.String.IsNullOrEmpty(s)|};
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return System.String.IsNullOrWhiteSpace(s);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_UsingStatic_IsNullOrEmpty()
    {
        var source = @"
using System;
using static System.String;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return {|#0:IsNullOrEmpty(s)|};
        }
    }
}";

        var fixedSource = @"
using System;
using static System.String;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return IsNullOrWhiteSpace(s);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_UsingStaticGlobal_IsNullOrEmpty()
    {
        var source = @"
using System;
using static global::System.String;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return {|#0:IsNullOrEmpty(s)|};
        }
    }
}";

        var fixedSource = @"
using System;
using static global::System.String;
namespace TestApp
{
    public class MyClass
    {
        public bool Check(string s)
        {
            return IsNullOrWhiteSpace(s);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

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
        public void Process(string s)
        {
            if ({|#0:string.IsNullOrEmpty(s)|})
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
        public void Process(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_NegatedInCondition()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Process(string s)
        {
            if (!{|#0:string.IsNullOrEmpty(s)|})
                System.Console.WriteLine(s);
        }
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public void Process(string s)
        {
            if (!string.IsNullOrWhiteSpace(s))
                System.Console.WriteLine(s);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA003Analyzer, DSA003CodeFixProvider>
                .Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
