using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA023CodeFixTests
{
    [TestMethod]
    public async Task ReplacesLiteralWithEmbeddedBackslash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + ""subfolder\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Combine(basePath, ""subfolder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesLiteralWithLeadingBackslash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + ""\\subfolder\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Combine(basePath, ""subfolder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesForwardSlashSeparators()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + ""/subfolder/file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Combine(basePath, ""subfolder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesSimpleFilenameWithoutSeparator()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + ""file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Combine(basePath, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RemovesSeparatorOnlyMiddleSegment_Backslash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists({|#0:""folder"" + ""\\"" + ""file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists(Path.Combine(""folder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RemovesSeparatorOnlyMiddleSegment_ForwardSlash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists({|#0:""folder"" + ""/"" + ""file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists(Path.Combine(""folder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task StripsTrailingBackslashFromLiteral()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string fileName)
                    {
                        File.Exists({|#0:""folder\\"" + fileName|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string fileName)
                    {
                        File.Exists(Path.Combine(""folder"", fileName));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task StripsTrailingForwardSlashFromLiteral()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string fileName)
                    {
                        File.Exists({|#0:""folder/"" + fileName|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string fileName)
                    {
                        File.Exists(Path.Combine(""folder"", fileName));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesDirectoryCreateDirectory()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Directory.CreateDirectory({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Directory.CreateDirectory(Path.Combine(basePath, ""subfolder""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("Directory.CreateDirectory"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesNewFileInfo()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var fi = new FileInfo({|#0:basePath + ""\\file.txt""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var fi = new FileInfo(Path.Combine(basePath, ""file.txt""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("new FileInfo"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesStaticMemberAccess()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class MyFiles
                {
                    public static string BasePath { get; set; }
                }
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists({|#0:MyFiles.BasePath + ""\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class MyFiles
                {
                    public static string BasePath { get; set; }
                }
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists(Path.Combine(MyFiles.BasePath, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task StripsLeadingBackslashFromFilename()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Combine(basePath, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TwoLiteralsWithForwardSlash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists({|#0:""folder"" + ""/file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists(Path.Combine(""folder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FourSegmentConcatenation()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string root, string sub)
                    {
                        File.Exists({|#0:root + ""\\"" + sub + ""\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string root, string sub)
                    {
                        File.Exists(Path.Combine(root, sub, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task VerbatimStringLiteral()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + @""\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Combine(basePath, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Exists"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FileCopyBothArgs()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Copy({|#0:basePath + ""\\src.txt""|}, {|#1:basePath + ""\\dst.txt""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Copy(Path.Combine(basePath, ""src.txt""), Path.Combine(basePath, ""dst.txt""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("File.Copy"));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(1).WithArguments("File.Copy"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesNewStreamWriter()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var sw = new StreamWriter({|#0:basePath + ""\\output.log""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var sw = new StreamWriter(Path.Combine(basePath, ""output.log""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("new StreamWriter"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesNewFileStreamConstructor()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var fs = new FileStream({|#0:basePath + ""\\file.bin""|}, FileMode.Open);
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var fs = new FileStream(Path.Combine(basePath, ""file.bin""), FileMode.Open);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA023Analyzer, DSA023CodeFixProvider>
                .Diagnostic(DSA023Analyzer.DiagnosticId).WithLocation(0).WithArguments("new FileStream"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
