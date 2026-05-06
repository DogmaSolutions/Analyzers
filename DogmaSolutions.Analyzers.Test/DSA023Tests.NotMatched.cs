using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA023Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "No concatenation: single variable argument",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string path)
                    {
                        File.Exists(path);
                    }
                }
            }"
        ],
        [
            "No concatenation: single string literal",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test()
                    {
                        File.Exists(""C:\\folder\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Already using Path.Combine",
            @"
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
            }"
        ],
        [
            "Already using Path.Join",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(Path.Join(basePath, ""file.xml""));
                    }
                }
            }"
        ],
        [
            "Concatenation in non-IO method",
            @"
            namespace TestApp
            {
                public static class SomeHelper
                {
                    public static void Process(string path) {}
                }
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        SomeHelper.Process(basePath + ""\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Concatenation in content parameter of File.WriteAllText",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string path, string prefix, string suffix)
                    {
                        File.WriteAllText(path, prefix + suffix);
                    }
                }
            }"
        ],
        [
            "Protocol prefix: file:// URL construction",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(""file://"" + basePath + ""\\file.xml"");
                    }
                }
            }"
        ],
        [
            "Protocol prefix: https:// URL construction",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string host)
                    {
                        File.Exists(""https://"" + host + ""/path"");
                    }
                }
            }"
        ],
        [
            "Path.Combine itself: concatenation inside Path.Combine argument is excluded",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string a, string b)
                    {
                        var result = Path.Combine(a + b, ""file.xml"");
                    }
                }
            }"
        ],
        [
            "Method call result, no concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    private string GetPath() => ""test"";
                    public void Test()
                    {
                        File.Exists(GetPath());
                    }
                }
            }"
        ],
        [
            "Indirect usage: concatenation assigned to variable then passed",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        var path = basePath + ""\\file.xml"";
                        File.Exists(path);
                    }
                }
            }"
        ],
        [
            "Integer addition in non-path parameter",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string path, int a, int b)
                    {
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None, a + b);
                    }
                }
            }"
        ],
        [
            "String interpolation instead of concatenation",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists($""{basePath}\\file.xml"");
                    }
                }
            }"
        ],
        [
            "string.Concat call instead of + operator",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string basePath)
                    {
                        File.Exists(string.Concat(basePath, ""\\file.xml""));
                    }
                }
            }"
        ],
        [
            "Directory.GetFiles searchPattern parameter is not a path",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string path, string ext)
                    {
                        Directory.GetFiles(path, ""*."" + ext);
                    }
                }
            }"
        ],
        [
            "Path.ChangeExtension: concatenation in path parameter is excluded",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string baseName)
                    {
                        Path.ChangeExtension(baseName + "".bak"", "".txt"");
                    }
                }
            }"
        ],
        [
            "Extension appending: filename + .log in File.CreateText",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string filename)
                    {
                        File.CreateText(filename + "".log"");
                    }
                }
            }"
        ],
        [
            "Extension appending: filename + .txt in File.Exists",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string filename)
                    {
                        File.Exists(filename + "".txt"");
                    }
                }
            }"
        ],
        [
            "Extension appending: multi-segment name + .json with no path separators",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyClass
                {
                    public void Test(string prefix, string name)
                    {
                        File.ReadAllText(prefix + name + "".json"");
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA023Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
