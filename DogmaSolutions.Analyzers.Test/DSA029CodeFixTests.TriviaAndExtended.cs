using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA029CodeFixTests
{

    [TestMethod]
    public async Task Fixes_Required_middle_of_three_attr_lists_with_xml_doc()
    {
        var source = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// Count.
        /// </summary>
        {|#0:[Description(""count"")]
        [Required]
        [Range(1, 100)]
        public int Count { get; set; }|}
    }
}";

        var fixedSource = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// Count.
        /// </summary>
        [Description(""count"")]
        [Range(1, 100)]
        public int Count { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_shared_list_Required_first()
    {
        var source = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required, Description(""test"")]
        public int MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Description(""test"")]
        public int MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_shared_list_Required_second()
    {
        var source = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Description(""test""), Required]
        public int MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Description(""test"")]
        public int MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_Range_replacement_with_xml_doc_and_other_attrs()
    {
        var source = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// Count of items.
        /// </summary>
        {|#0:[Required]
        [Description(""count"")]
        public int Count { get; set; }|}
    }
}";

        var fixedSource = @"
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// Count of items.
        /// </summary>
        [Range(1, int.MaxValue)]
        [Description(""count"")]
        public int Count { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_preserves_other_attributes()
    {
        var source = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Description(""test"")]
        [Required] public int MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Description(""test"")]
        public int MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_Guid_removes_only_no_Range_offered()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public Guid MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        public Guid MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_sbyte_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public sbyte MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, sbyte.MaxValue)] public sbyte MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_ushort_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public ushort MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, ushort.MaxValue)] public ushort MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_uint_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public uint MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, uint.MaxValue)] public uint MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_ulong_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public ulong MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, ulong.MaxValue)] public ulong MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_char_removes_only_no_Range_offered()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public char MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        public char MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_struct_removes_only_no_Range_offered()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public struct MyStruct { public int X; }

    public class MyClass
    {
        {|#0:[Required] public MyStruct MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public struct MyStruct { public int X; }

    public class MyClass
    {
        public MyStruct MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_Int32_CLRName_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public Int32 MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, int.MaxValue)] public Int32 MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_SystemInt32_FullNamespace_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public System.Int32 MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, int.MaxValue)] public System.Int32 MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_Int64_CLRName_replaces_with_Range()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public Int64 MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        [Range(1, long.MaxValue)] public Int64 MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Range";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_Boolean_CLRName_removes_attribute()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public Boolean MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        public Boolean MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_Decimal_CLRName_removes_only_no_Range_offered()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public Decimal MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        public Decimal MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA029.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA029Analyzer, DSA029CodeFixProvider>
                .Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
