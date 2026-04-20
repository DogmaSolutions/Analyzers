using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA017Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Dictionary: ContainsKey + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key, int value)
                    {
                        {|#0:if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, value);
                        }|}
                    }
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "HashSet: Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(HashSet<string> set, string item)
                    {
                        {|#0:if (!set.Contains(item))
                        {
                            set.Add(item);
                        }|}
                    }
                }
            }",
            "HashSet",
            "Add (already returns a bool indicating whether the element was added)"
        ],
        [
            "SortedSet: Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(SortedSet<string> set, string item)
                    {
                        {|#0:if (!set.Contains(item))
                        {
                            set.Add(item);
                        }|}
                    }
                }
            }",
            "SortedSet",
            "Add (already returns a bool indicating whether the element was added)"
        ],
        [
            "Dictionary: TryGetValue negated + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key, int value)
                    {
                        {|#0:if (!dict.TryGetValue(key, out var existing))
                        {
                            dict.Add(key, value);
                        }|}
                    }
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "Dictionary: ContainsKey + throw + Add after",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key, int value)
                    {
                        {|#0:if (dict.ContainsKey(key))
                            throw new System.InvalidOperationException();|}
                        dict.Add(key, value);
                    }
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "Dictionary: ContainsKey + else Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key, int value)
                    {
                        {|#0:if (dict.ContainsKey(key))
                        {
                            System.Console.WriteLine(""exists"");
                        }
                        else
                        {
                            dict.Add(key, value);
                        }|}
                    }
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "SortedDictionary: ContainsKey + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
                    {
                        {|#0:if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, value);
                        }|}
                    }
                }
            }",
            "SortedDictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string typeName,
        string suggestion
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA017Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA017Analyzer>.Diagnostic(DSA017Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(typeName, suggestion));

        await test.RunAsync().ConfigureAwait(false);
    }
}
