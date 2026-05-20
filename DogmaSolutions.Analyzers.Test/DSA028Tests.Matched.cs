using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA028Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "IEnumerable<T> return type with direct ToList return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        return {|#0:source.Where(x => x > 0).ToList()|};
                    }
                }
            }"
        ],
        [
            "IReadOnlyCollection<T> return type with direct ToList return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyCollection<string> GetNames(IEnumerable<string> source)
                    {
                        return {|#0:source.Where(x => x.Length > 0).ToList()|};
                    }
                }
            }"
        ],
        [
            "IReadOnlyList<T> return type with direct ToList return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetValues(IEnumerable<int> source)
                    {
                        return {|#0:source.OrderBy(x => x).ToList()|};
                    }
                }
            }"
        ],
        [
            "Expression-body method with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source) => {|#0:source.ToList()|};
                }
            }"
        ],
        [
            "Variable assigned with ToList then returned",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<string> GetItems(IEnumerable<string> source)
                    {
                        var result = {|#0:source.Where(x => x != null).ToList()|};
                        return result;
                    }
                }
            }"
        ],
        [
            "IReadOnlyCollection<T> with variable and return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyCollection<int> GetNumbers(int[] data)
                    {
                        var filtered = {|#0:data.Where(x => x > 0).ToList()|};
                        return filtered;
                    }
                }
            }"
        ],
        [
            "Property getter returning ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly int[] _data = new int[] { 1, 2, 3 };
                    public IReadOnlyList<int> Items
                    {
                        get { return {|#0:_data.Where(x => x > 0).ToList()|}; }
                    }
                }
            }"
        ],
        [
            "Local function returning ToList with IEnumerable return type",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> source)
                    {
                        IEnumerable<int> GetFiltered()
                        {
                            return {|#0:source.Where(x => x > 0).ToList()|};
                        }
                        var items = GetFiltered();
                    }
                }
            }"
        ],
        [
            "Multiple return statements all with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, bool filter)
                    {
                        if (filter)
                            return {|#0:source.Where(x => x > 0).ToList()|};
                        return {|#1:source.ToList()|};
                    }
                }
            }"
        ],
        [
            "Interface implementation returning ToList through IReadOnlyCollection",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public interface IService
                {
                    IReadOnlyCollection<string> GetItems();
                }
                public class MyService : IService
                {
                    private readonly string[] _data = new string[] { ""a"", ""b"" };
                    public IReadOnlyCollection<string> GetItems()
                    {
                        return {|#0:_data.Where(x => x != null).ToList()|};
                    }
                }
            }"
        ],
        [
            "Chained LINQ with ToList returned as IEnumerable",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Category; public string Name; }
                public class MyService
                {
                    public IEnumerable<string> GetNames(IEnumerable<Item> items)
                    {
                        return {|#0:items.Where(x => x.Category == 1).Select(x => x.Name).ToList()|};
                    }
                }
            }"
        ],
        [
            "Variable assigned ToList, used only in return (no mutation, LINQ Count method)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IReadOnlyList<int> GetSorted(IEnumerable<int> source)
                    {
                        var sorted = {|#0:source.OrderBy(x => x).ToList()|};
                        System.Console.WriteLine(sorted.Count());
                        return sorted;
                    }
                }
            }"
        ],
        [
            "Expression-body local function with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> source)
                    {
                        IReadOnlyCollection<int> GetFiltered() => {|#0:source.Where(x => x > 0).ToList()|};
                        var items = GetFiltered();
                    }
                }
            }"
        ],
        [
            "Expression-bodied property returning ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly int[] _data = new int[] { 1, 2, 3 };
                    public IEnumerable<int> Items => {|#0:_data.Where(x => x > 0).ToList()|};
                }
            }"
        ],
        [
            "Async method returning Task<IEnumerable<T>> with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task<IEnumerable<int>> GetItemsAsync(IEnumerable<int> source)
                    {
                        await Task.Delay(1);
                        return {|#0:source.Where(x => x > 0).ToList()|};
                    }
                }
            }"
        ],
        [
            "Async method returning Task<IReadOnlyList<T>> with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async Task<IReadOnlyList<string>> GetNamesAsync(IEnumerable<string> source)
                    {
                        await Task.Delay(1);
                        return {|#0:source.OrderBy(x => x).ToList()|};
                    }
                }
            }"
        ],
        [
            "Property getter with expression body returning ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly int[] _data = new int[] { 1, 2, 3 };
                    public IReadOnlyCollection<int> Items
                    {
                        get => {|#0:_data.Where(x => x > 0).ToList()|};
                    }
                }
            }"
        ],
        [
            "Ternary return with ToList in both branches",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> a, IEnumerable<int> b, bool flag)
                    {
                        return flag ? {|#0:a.ToList()|} : {|#1:b.ToList()|};
                    }
                }
            }"
        ],
        [
            "Ternary return with ToList in one branch only",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
                    {
                        return flag ? {|#0:source.ToList()|} : System.Array.Empty<int>();
                    }
                }
            }"
        ],
        [
            "Null-coalescing with ToList on right operand",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> cached, IEnumerable<int> source)
                    {
                        return cached ?? {|#0:source.Where(x => x > 0).ToList()|};
                    }
                }
            }"
        ],
        [
            "Parenthesized ToList return",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        return ({|#0:source.ToList()|});
                    }
                }
            }"
        ],
        [
            "Switch expression with ToList in arms",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, int mode)
                    {
                        return mode switch
                        {
                            1 => {|#0:source.Where(x => x > 0).ToList()|},
                            _ => {|#1:source.ToList()|}
                        };
                    }
                }
            }"
        ],
        [
            "Expression-body ternary with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly int[] _data = new int[] { 1, 2, 3 };
                    public IEnumerable<int> GetItems(bool flag) => flag ? {|#0:_data.ToList()|} : System.Array.Empty<int>();
                }
            }"
        ],
        [
            "Indexer getter returning ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    private readonly int[][] _data = new int[][] { new[] { 1, 2 }, new[] { 3, 4 } };
                    public IReadOnlyList<int> this[int index]
                    {
                        get { return {|#0:_data[index].Where(x => x > 0).ToList()|}; }
                    }
                }
            }"
        ],
        [
            "Variable with explicit IEnumerable<T> type assigned ToList then returned",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        IEnumerable<int> result = {|#0:source.Where(x => x > 0).ToList()|};
                        return result;
                    }
                }
            }"
        ],
        [
            "Variable assigned ToList via separate assignment then returned",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source)
                    {
                        IEnumerable<int> result;
                        result = {|#0:source.Where(x => x > 0).ToList()|};
                        return result;
                    }
                }
            }"
        ],
        [
            "Async method returning ValueTask<IEnumerable<T>> with ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class MyService
                {
                    public async ValueTask<IEnumerable<int>> GetItemsAsync(IEnumerable<int> source)
                    {
                        await Task.Delay(1);
                        return {|#0:source.Where(x => x > 0).ToList()|};
                    }
                }
            }"
        ],
        [
            "Ternary return where variable branch was assigned ToList",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
                    {
                        var result = {|#0:source.ToList()|};
                        return flag ? result : System.Array.Empty<int>();
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA028Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

#pragma warning disable CA1062
        var diagnosticCount = 0;
        for (var i = 0; i < 10; i++)
        {
            if (sourceCode.Contains("{|#" + i + ":"))
                diagnosticCount++;
            else
                break;
        }
#pragma warning restore CA1062

        for (var i = 0; i < diagnosticCount; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<DSA028Analyzer>.Diagnostic(DSA028Analyzer.DiagnosticId)
                    .WithLocation(i));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
