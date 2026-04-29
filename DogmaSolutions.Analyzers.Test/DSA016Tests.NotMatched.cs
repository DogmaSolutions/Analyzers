using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA016Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "FirstOrDefault called only once",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items, int id)
                    {
                        var a = items.FirstOrDefault(x => x.Id == id);
                    }
                }
            }"
        ],
        [
            "FirstOrDefault with different predicates on same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; public string Name; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items, int id)
                    {
                        var a = items.FirstOrDefault(x => x.Id == id);
                        var b = items.FirstOrDefault(x => x.Name == ""test"");
                    }
                }
            }"
        ],
        [
            "FirstOrDefault with same predicate on different receivers",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items1, IEnumerable<Item> items2, int id)
                    {
                        var a = items1.FirstOrDefault(x => x.Id == id);
                        var b = items2.FirstOrDefault(x => x.Id == id);
                    }
                }
            }"
        ],
        [
            "Same invocation in different scopes (method body vs nested lambda)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items, IEnumerable<int> ids)
                    {
                        var a = items.FirstOrDefault(x => x.Id == 1);
                        var results = ids.Select(id => items.FirstOrDefault(x => x.Id == 1));
                    }
                }
            }"
        ],
        [
            "Different methods on same receiver with same predicate (Any + FirstOrDefault)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items, int id)
                    {
                        var exists = items.Any(x => x.Id == id);
                        var item = items.FirstOrDefault(x => x.Id == id);
                    }
                }
            }"
        ],
        [
            "Non-tracked method called twice (ToString)",
            @"
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(int value)
                    {
                        var a = value.ToString();
                        var b = value.ToString();
                    }
                }
            }"
        ],
        [
            "Non-tracked method called twice (custom method)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyService
                {
                    private string GetData(int id) => null;
                    public void Process()
                    {
                        var a = GetData(1);
                        var b = GetData(1);
                    }
                }
            }"
        ],
        [
            "Same method on different receivers with different args",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items1, IEnumerable<Item> items2)
                    {
                        var a = items1.FirstOrDefault(x => x.Id == 1);
                        var b = items2.FirstOrDefault(x => x.Id == 2);
                    }
                }
            }"
        ],
        [
            "Count called once with predicate and once without (different signatures)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items)
                    {
                        var total = items.Count();
                        var filtered = items.Count(x => x.Id > 0);
                    }
                }
            }"
        ],
        [
            "Same invocation in two separate lambdas (each is its own scope)",
            @"
            using System;
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items)
                    {
                        Action a1 = () => { var x = items.FirstOrDefault(x => x.Id == 1); };
                        Action a2 = () => { var x = items.FirstOrDefault(x => x.Id == 1); };
                    }
                }
            }"
        ],
        [
            "Static method call: File.Exists called twice (not an enumeration method)",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(string path)
                    {
                        var a = File.Exists(path);
                        var b = File.Exists(path);
                    }
                }
            }"
        ],
        [
            "Same-named variables in sibling scopes: not the same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Patch { public float Label; }
                public class MyService
                {
                    public void Process(List<Patch> allPatches, bool useMicro)
                    {
                        if (useMicro)
                        {
                            var patches = allPatches.Where(p => p.Label > 0).ToList();
                            var pos = patches.Count(p => p.Label > 0.5f);
                            var neg = patches.Count(p => p.Label < 0.5f);
                        }
                        else
                        {
                            var patches = allPatches.Where(p => p.Label < 0).ToList();
                            var pos = patches.Count(p => p.Label > 0.5f);
                            var neg = patches.Count(p => p.Label < 0.5f);
                        }
                    }
                }
            }"
        ],
        [
            "Static method call: Directory.Exists called twice",
            @"
            using System.IO;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(string path)
                    {
                        var a = Directory.Exists(path);
                        var b = Directory.Exists(path);
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
        var test = new CSharpAnalyzerVerifier<DSA016Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
