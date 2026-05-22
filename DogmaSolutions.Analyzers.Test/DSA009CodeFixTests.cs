using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA009CodeFixTests
{
    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_removes_attribute_inline()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required] public DateTimeOffset MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_removes_attribute_separate_line()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required]
        public DateTimeOffset MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_only_attr_with_xml_doc()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// My date property.
        /// </summary>
        {|#0:[Required]
        public DateTimeOffset MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// My date property.
        /// </summary>
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_first_attr_other_after_with_xml_doc()
    {
        var source = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// My date property.
        /// </summary>
        {|#0:[Required]
        [Description(""test"")]
        public DateTimeOffset MyProperty { get; set; }|}
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
        /// <summary>
        /// My date property.
        /// </summary>
        [Description(""test"")]
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_last_attr_other_before_no_xml_doc()
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
        [Required] public DateTimeOffset MyProperty { get; set; }|}
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
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_last_attr_other_before_with_xml_doc()
    {
        var source = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// My date property.
        /// </summary>
        {|#0:[Description(""test"")]
        [Required]
        public DateTimeOffset MyProperty { get; set; }|}
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
        /// <summary>
        /// My date property.
        /// </summary>
        [Description(""test"")]
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_middle_of_three_attr_lists_with_xml_doc()
    {
        var source = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// My date property.
        /// </summary>
        {|#0:[Description(""test"")]
        [Required]
        [DisplayFormat(DataFormatString = ""{0:yyyy-MM-dd}"")]
        public DateTimeOffset MyProperty { get; set; }|}
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
        /// <summary>
        /// My date property.
        /// </summary>
        [Description(""test"")]
        [DisplayFormat(DataFormatString = ""{0:yyyy-MM-dd}"")]
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_shared_list_Required_first()
    {
        var source = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Required, Description(""test"")]
        public DateTimeOffset MyProperty { get; set; }|}
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
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_shared_list_Required_second()
    {
        var source = @"
using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[Description(""test""), Required]
        public DateTimeOffset MyProperty { get; set; }|}
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
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_Required_DateTimeOffset_between_properties_with_xml_docs()
    {
        var source = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// Name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Created date.
        /// </summary>
        {|#0:[Required]
        public DateTimeOffset CreatedAt { get; set; }|}

        /// <summary>
        /// Label.
        /// </summary>
        [Required]
        public string Label { get; set; }
    }
}";

        var fixedSource = @"
using System;
using System.ComponentModel.DataAnnotations;
namespace TestApp
{
    public class MyClass
    {
        /// <summary>
        /// Name.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Created date.
        /// </summary>
        public DateTimeOffset CreatedAt { get; set; }

        /// <summary>
        /// Label.
        /// </summary>
        [Required]
        public string Label { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_FullyQualified_Required_DateTimeOffset_removes_attribute()
    {
        var source = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        {|#0:[System.ComponentModel.DataAnnotations.Required] public DateTimeOffset MyProperty { get; set; }|}
    }
}";

        var fixedSource = @"
using System;
namespace TestApp
{
    public class MyClass
    {
        public DateTimeOffset MyProperty { get; set; }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.CodeActionEquivalenceKey = "DSA009.Remove";
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA009Analyzer, DSA009CodeFixProvider>
                .Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
