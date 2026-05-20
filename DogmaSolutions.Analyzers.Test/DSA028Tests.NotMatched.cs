using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA028Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Return type is List<T> — caller can use mutating members",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public List<int> GetItems(IEnumerable<int> source)
                    {
                        return source.Where(x => x > 0).ToList();
                    }
                }
            }"
        ],
        [
            "Return type is IList<T> — caller can Add/Remove",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IList<int> GetItems(IEnumerable<int> source)
                    {
                        return source.Where(x => x > 0).ToList();
                    }
                }
            }"
        ],
        [
            "Return type is ICollection<T> — caller can Add/Remove",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public ICollection<int> GetItems(IEnumerable<int> source)
                    {
                        return source.Where(x => x > 0).ToList();
                    }
                }
            }"
        ],
        [
            "Returns ToArray — already optimal",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        return source.Where(x => x > 0).ToArray();
                    }
                }
            }"
        ],
        [
            "Returns LINQ query without materialization",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        return source.Where(x => x > 0);
                    }
                }
            }"
        ],
        [
            "Variable mutated with Add after ToList — cannot use array",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.Where(x => x > 0).ToList();
                        result.Add(999);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with Remove after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<string> GetItems(IEnumerable<string> source)
                    {
                        var result = source.ToList();
                        result.Remove(""bad"");
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with Sort after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        result.Sort();
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with RemoveAll after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyCollection<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        result.RemoveAll(x => x < 0);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with AddRange after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, IEnumerable<int> extra)
                    {
                        var result = source.ToList();
                        result.AddRange(extra);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with Insert after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<string> GetItems(IEnumerable<string> source)
                    {
                        var result = source.ToList();
                        result.Insert(0, ""first"");
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with indexer assignment after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        result[0] = 42;
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with Clear after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, bool reset)
                    {
                        var result = source.ToList();
                        if (reset)
                            result.Clear();
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable mutated with Reverse after ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        result.Reverse();
                        return result;
                    }
                }
            }"
        ],
        [
            "Return type is void — no return type check applies",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                    }
                }
            }"
        ],
        [
            "ToList inside lambda — not the method's own return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<IEnumerable<int>> GetGrouped(IEnumerable<int> source)
                    {
                        return source.GroupBy(x => x % 2).Select(g => (IEnumerable<int>)g.ToList());
                    }
                }
            }"
        ],
        [
            "ToList with argument (custom comparer overload) — not parameterless",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public static class Extensions
                {
                    public static List<T> ToList<T>(this IEnumerable<T> source, int capacity)
                    {
                        var list = new List<T>(capacity);
                        list.AddRange(source);
                        return list;
                    }
                }
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        return source.ToList(100);
                    }
                }
            }"
        ],
        [
            "Variable reassigned after ToList — different value returned",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, IEnumerable<int> fallback)
                    {
                        var result = source.ToList();
                        result = fallback.ToList();
                        return result;
                    }
                }
            }"
        ],
        [
            "Return type is concrete array — no ToList used",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public int[] GetItems(IEnumerable<int> source)
                    {
                        return source.Where(x => x > 0).ToArray();
                    }
                }
            }"
        ],
        [
            "Variable declared with var uses .Count property (List-specific, would break with ToArray)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.OrderBy(x => x).ToList();
                        System.Console.WriteLine(result.Count);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable declared with var uses .Capacity property (List-specific)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        System.Console.WriteLine(result.Capacity);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable explicitly typed as List<T> — changing to ToArray would break compilation",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        List<int> result = source.Where(x => x > 0).ToList();
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable declared with var uses Find() — List-specific method, would break with ToArray",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        var found = result.Find(x => x > 5);
                        System.Console.WriteLine(found);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable declared with var uses ForEach() — List-specific method, would break with ToArray",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<string> GetItems(IEnumerable<string> source)
                    {
                        var result = source.ToList();
                        result.ForEach(x => System.Console.WriteLine(x));
                        return result;
                    }
                }
            }"
        ],
        [
            "Async method returning Task<List<T>> — caller can use mutating members",
            @"
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task<List<int>> GetItemsAsync(IEnumerable<int> source)
                    {
                        await Task.Delay(1);
                        return source.Where(x => x > 0).ToList();
                    }
                }
            }"
        ],
        [
            "Variable declared with var uses IndexOf() — List-specific method, would break with ToArray",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        var idx = result.IndexOf(42);
                        System.Console.WriteLine(idx);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable declared with var uses AsReadOnly() — List-specific method, would break with ToArray",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        var result = source.ToList();
                        var ro = result.AsReadOnly();
                        System.Console.WriteLine(ro.Count);
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable explicitly typed as List<T> assigned separately — changing to ToArray would break",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        List<int> result;
                        result = source.Where(x => x > 0).ToList();
                        return result;
                    }
                }
            }"
        ],
        [
            "Async method returning ValueTask<List<T>> — caller can use mutating members",
            @"
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async ValueTask<List<int>> GetItemsAsync(IEnumerable<int> source)
                    {
                        await Task.Delay(1);
                        return source.Where(x => x > 0).ToList();
                    }
                }
            }"
        ],
        [
            "Ternary where ToList result is mutated before conditional return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
                    {
                        var result = source.ToList();
                        result.Add(99);
                        return flag ? result : System.Array.Empty<int>();
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA028Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
