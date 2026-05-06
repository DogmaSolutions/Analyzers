using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA023Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "String literal with embedded backslash separator",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "String literal with leading backslash separator",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "String literal with forward slash separators",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "String literal filename only (no separator)",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "String literal with leading backslash only",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "String literal with leading forward slash only",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists({|#0:basePath + ""/file.xml""|});
                    }
                }
            }",
            "File.Exists"
        ],
        [
            "Two string literals with forward slash",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Two string literals with backslash",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists({|#0:""folder"" + ""\\file.xml""|});
                    }
                }
            }",
            "File.Exists"
        ],
        [
            "Three operands with separator-only middle segment (backslash)",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Three operands with separator-only middle segment (forward slash)",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Literal with trailing backslash plus variable",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Literal with trailing forward slash plus variable",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Directory.CreateDirectory with concatenation",
            @"
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
            }",
            "Directory.CreateDirectory"
        ],
        [
            "new FileInfo with concatenation",
            @"
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
            }",
            "new FileInfo"
        ],
        [
            "new DirectoryInfo with concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var di = new DirectoryInfo({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "new DirectoryInfo"
        ],
        [
            "Static member access as left operand",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "File.ReadAllText with concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var content = File.ReadAllText({|#0:basePath + ""\\data.json""|});
                    }
                }
            }",
            "File.ReadAllText"
        ],
        [
            "Path.GetFileName with concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var name = Path.GetFileName({|#0:basePath + ""\\file.txt""|});
                    }
                }
            }",
            "Path.GetFileName"
        ],
        [
            "new StreamReader with concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var sr = new StreamReader({|#0:basePath + ""\\file.txt""|});
                    }
                }
            }",
            "new StreamReader"
        ],
        [
            "new FileStream with concatenation",
            @"
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
            }",
            "new FileStream"
        ],
        [
            "new StreamWriter with concatenation",
            @"
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
            }",
            "new StreamWriter"
        ],
        [
            "Implicit object creation (FileInfo fi = new(...))",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        FileInfo fi = new({|#0:basePath + ""\\file.txt""|});
                    }
                }
            }",
            "new FileInfo"
        ],
        [
            "Verbatim string literal with backslash",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Named argument with concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(path: {|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "File.Exists"
        ],
        [
            "Parenthesized concatenation expression",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(({|#0:basePath + ""\\file.xml""|}));
                    }
                }
            }",
            "File.Exists"
        ],
        [
            "Four-segment concatenation",
            @"
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
            }",
            "File.Exists"
        ],
        [
            "Instance method: DirectoryInfo.GetFiles with concatenation in path parameter",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var di = new DirectoryInfo(""C:\\temp"");
                        di.MoveTo({|#0:basePath + ""\\archive""|});
                    }
                }
            }",
            "DirectoryInfo.MoveTo"
        ],
        [
            "Extension with path separators in chain: basePath + separator + name + .log is still flagged",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath, string name)
                    {
                        File.Exists({|#0:basePath + ""\\"" + name + "".log""|});
                    }
                }
            }",
            "File.Exists"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(string title, string sourceCode, string expectedMethodDisplay)
    {
        var test = new CSharpAnalyzerVerifier<DSA023Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA023Analyzer>.Diagnostic(DSA023Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(expectedMethodDisplay));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FileCopy_BothPathArgsFlagged()
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

        var test = new CSharpAnalyzerVerifier<DSA023Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA023Analyzer>.Diagnostic(DSA023Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments("File.Copy"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA023Analyzer>.Diagnostic(DSA023Analyzer.DiagnosticId)
                .WithLocation(1)
                .WithArguments("File.Copy"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
