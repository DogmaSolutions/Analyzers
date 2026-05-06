using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA024Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Exact match: parameter named 'path'",
            @"
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
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'filePath'",
            @"
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
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Suffix match: parameter named 'outputFilePath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string outputFilePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'fileName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string fileName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'directoryPath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string directoryPath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'directoryName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string directoryName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'folderPath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string folderPath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'folderName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string folderName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'fileFullPath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string fileFullPath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'directoryFullPath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string directoryFullPath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'fileFullName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string fileFullName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'directoryFullName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string directoryFullName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'xmlFile'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string xmlFile) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\data.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'xmlFilePath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string xmlFilePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\data.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'xmlFileName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string xmlFileName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\data.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'jsonFile'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string jsonFile) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\data.json""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'jsonFileName'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string jsonFileName) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\data.json""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Prefix match: parameter named 'jsonFilePath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string jsonFilePath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\data.json""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Case-insensitive exact match: parameter named 'PATH'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string PATH) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Case-insensitive prefix match: parameter named 'FILEPATH'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string FILEPATH) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process({|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Suffix match: parameter named 'sourceDirectoryPath'",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Copy(string sourceDirectoryPath) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Copy({|#0:basePath + ""\\subfolder""|});
                    }
                }
            }",
            "Helper.Copy"
        ],
        [
            "Instance method with path parameter",
            @"
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
            }",
            "Processor.Save"
        ],
        [
            "Constructor with path parameter (new keyword)",
            @"
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
            }",
            "new Config"
        ],
        [
            "Implicit object creation with path parameter",
            @"
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
                        Config c = new({|#0:basePath + ""\\config.json""|});
                    }
                }
            }",
            "new Config"
        ],
        [
            "Forward slash separator",
            @"
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
                        Helper.Process({|#0:basePath + ""/file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Three-segment concatenation with separator-only middle",
            @"
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
            }",
            "Helper.Process"
        ],
        [
            "Named argument with matching parameter",
            @"
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
                        Helper.Process(path: {|#0:basePath + ""\\file.xml""|});
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Parenthesized concatenation expression",
            @"
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
                        Helper.Process(({|#0:basePath + ""\\file.xml""|}));
                    }
                }
            }",
            "Helper.Process"
        ],
        [
            "Extension with path separators in chain: basePath + separator + name + .log is still flagged",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath, string name)
                    {
                        Helper.Process({|#0:basePath + ""\\"" + name + "".log""|});
                    }
                }
            }",
            "Helper.Process"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(string title, string sourceCode, string expectedMethodDisplay)
    {
        var test = new CSharpAnalyzerVerifier<DSA024Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA024Analyzer>.Diagnostic(DSA024Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(expectedMethodDisplay));

        await test.RunAsync().ConfigureAwait(false);
    }
}
