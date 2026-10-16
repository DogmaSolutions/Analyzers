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
        [
            "Dictionary: ContainsKey + complex body (still flagged, Dictionary is not set-like)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key)
                    {
                        {|#0:if (!dict.ContainsKey(key))
                        {
                            var value = ComputeExpensiveValue(key);
                            dict.Add(key, value);
                        }|}
                    }
                    private int ComputeExpensiveValue(string key) => 42;
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "Dictionary check-then-add in a lambda declared inside a lock (the lock does not protect the lambda)",
            @"
            using System.Collections.Concurrent;
            using System.Collections.Generic;
            using System;
            namespace TestApp
            {
                public class Page { }
                public class Catalog
                {
                    private readonly object _pagesLock = new object();
                    private readonly Dictionary<string, Page> _pages = new Dictionary<string, Page>();
                    public Func<string, Page> Make()
                    {
                        lock (_pagesLock)
                        {
                            return key =>
                            {
                                {|#0:if (!_pages.ContainsKey(key))
                                {
                                    _pages.Add(key, new Page());
                                }|}

                                return _pages[key];
                            };
                        }
                    }
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "Dictionary check-then-add in a local function declared inside a lock",
            @"
            using System.Collections.Concurrent;
            using System.Collections.Generic;
            using System;
            namespace TestApp
            {
                public class Page { }
                public class Catalog
                {
                    private readonly object _pagesLock = new object();
                    private readonly Dictionary<string, Page> _pages = new Dictionary<string, Page>();
                    public Func<string, Page> Make()
                    {
                        lock (_pagesLock)
                        {
                            Page Local(string key)
                            {
                                {|#0:if (!_pages.ContainsKey(key))
                                {
                                    _pages.Add(key, new Page());
                                }|}

                                return _pages[key];
                            }

                            return Local;
                        }
                    }
                }
            }",
            "Dictionary",
            "TryAdd or indexer assignment [key] = value"
        ],
        [
            "Dictionary check-then-add after the lock statement",
            @"
            using System.Collections.Concurrent;
            using System.Collections.Generic;
            using System;
            namespace TestApp
            {
                public class Page { }
                public class Catalog
                {
                    private readonly object _pagesLock = new object();
                    private readonly Dictionary<string, Page> _pages = new Dictionary<string, Page>();
                    public Func<string, Page> Make()
                    {
                        lock (_pagesLock)
                        {
                            Console.WriteLine(_pages.Count);
                        }

                        return key =>
                        {
                            {|#0:if (!_pages.ContainsKey(key))
                            {
                                _pages.Add(key, new Page());
                            }|}

                            return _pages[key];
                        };
                    }
                }
            }",
            "Dictionary",
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
