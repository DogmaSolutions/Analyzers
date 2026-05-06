using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA024Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "No concatenation: single variable argument",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string path)
                    {
                        Helper.Process(path);
                    }
                }
            }"
        ],
        [
            "No concatenation: single string literal",
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
                        Helper.Process(""C:\\folder\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Parameter name does not match any configured name",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string data) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Helper.Process(basePath + ""\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Parameter named 'content' with concatenation",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path, string content) {}
                }
                public class MyClass
                {
                    public void Test(string prefix, string suffix)
                    {
                        Helper.Process(""file.txt"", prefix + suffix);
                    }
                }
            }"
        ],
        [
            "Well-known System.IO type (File.Exists) is excluded (handled by DSA023)",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(basePath + ""\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Well-known System.IO type (new FileInfo) is excluded (handled by DSA023)",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var fi = new FileInfo(basePath + ""\\file.txt"");
                    }
                }
            }"
        ],
        [
            "Well-known System.IO type (Directory.CreateDirectory) is excluded",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        Directory.CreateDirectory(basePath + ""\\subfolder"");
                    }
                }
            }"
        ],
        [
            "Well-known System.IO type (new StreamReader) is excluded",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var sr = new StreamReader(basePath + ""\\file.txt"");
                    }
                }
            }"
        ],
        [
            "Well-known System.IO type (new StreamWriter) is excluded",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var sw = new StreamWriter(basePath + ""\\file.txt"");
                    }
                }
            }"
        ],
        [
            "Well-known System.IO type (new FileStream) is excluded",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        using var fs = new FileStream(basePath + ""\\file.bin"", FileMode.Open);
                    }
                }
            }"
        ],
        [
            "Protocol prefix: file:// URL construction",
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
                        Helper.Process(""file://"" + basePath + ""\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Protocol prefix: https:// URL construction",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string host)
                    {
                        Helper.Process(""https://"" + host + ""/resource"");
                    }
                }
            }"
        ],
        [
            "String interpolation instead of concatenation",
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
                        Helper.Process($""{basePath}\\file.xml"");
                    }
                }
            }"
        ],
        [
            "string.Concat call instead of + operator",
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
                        Helper.Process(string.Concat(basePath, ""\\file.xml""));
                    }
                }
            }"
        ],
        [
            "Method call result, no concatenation",
            @"
            namespace TestApp
            {
                public static class Helper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    private string GetPath() => ""test"";
                    public void Test()
                    {
                        Helper.Process(GetPath());
                    }
                }
            }"
        ],
        [
            "Indirect usage: concatenation assigned to variable then passed",
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
                        var path = basePath + ""\\file.xml"";
                        Helper.Process(path);
                    }
                }
            }"
        ],
        [
            "Integer addition in non-path parameter",
            @"
            namespace TestApp
            {
                public class Processor
                {
                    public Processor(string path, int bufferSize) {}
                }
                public class MyClass
                {
                    public void Test(int a, int b)
                    {
                        var p = new Processor(""file.txt"", a + b);
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA024Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
