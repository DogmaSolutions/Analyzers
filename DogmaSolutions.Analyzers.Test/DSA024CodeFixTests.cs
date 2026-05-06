using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA024CodeFixTests
{
    [TestMethod]
    public async Task ReplacesLiteralWithEmbeddedBackslash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""subfolder\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process(Path.Combine(basePath, ""subfolder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesLiteralWithLeadingBackslash()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string filePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string filePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process(Path.Combine(basePath, ""subfolder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesForwardSlashSeparators()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""/subfolder/file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process(Path.Combine(basePath, ""subfolder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReplacesSimpleFilenameWithoutSeparator()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process(Path.Combine(basePath, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RemovesSeparatorOnlyMiddleSegment()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test()
                    {
                        Helper.Process({|#0:""folder"" + ""\\"" + ""file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test()
                    {
                        Helper.Process(Path.Combine(""folder"", ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task StripsTrailingBackslashFromLiteral()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string fileName)
                    {
                        Helper.Process({|#0:""folder\\"" + fileName|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string fileName)
                    {
                        Helper.Process(Path.Combine(""folder"", fileName));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesConstructor()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class Config
                {
                    public Config(string filePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var c = new Config({|#0:basePath + ""\\config.json""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class Config
                {
                    public Config(string filePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var c = new Config(Path.Combine(basePath, ""config.json""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("new Config"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesFourSegmentConcatenation()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string root, string sub)
                    {
                        Helper.Process({|#0:root + ""\\"" + sub + ""\\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string root, string sub)
                    {
                        Helper.Process(Path.Combine(root, sub, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesVerbatimStringLiteral()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + @""\file.xml""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process(Path.Combine(basePath, ""file.xml""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Helper.Process"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesInstanceMethod()
    {
        var source = @"
            using System.IO;
            namespace TestApp
            {
                public class Processor
                {
                    public void Save(string filePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var p = new Processor();
                        p.Save({|#0:basePath + ""\\output.dat""|});
                    }
                }
            }";

        var fixedSource = @"
            using System.IO;
            namespace TestApp
            {
                public class Processor
                {
                    public void Save(string filePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var p = new Processor();
                        p.Save(Path.Combine(basePath, ""output.dat""));
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA024Analyzer, DSA024CodeFixProvider>
                .Diagnostic(DSA024Analyzer.DiagnosticId).WithLocation(0).WithArguments("Processor.Save"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
