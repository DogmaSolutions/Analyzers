using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA017Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Existence check and insert on different collections",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public bool Validate(string key, Dictionary<string, int> registry)
                    {
                        var allowedKeys = new HashSet<string> { ""key1"", ""key2"" };
                        if (!allowedKeys.Contains(key))
                        {
                            registry.Add(key, 0);
                        }
                        return true;
                    }
                }
            }"
        ],
        [
            "List check-then-act (DSA018 territory, not DSA017)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(List<string> items, string item)
                    {
                        if (!items.Any(x => x == item))
                        {
                            items.Add(item);
                        }
                    }
                }
            }"
        ],
        [
            "Dictionary: just Add without existence check",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict)
                    {
                        dict.Add(""key"", 1);
                    }
                }
            }"
        ],
        [
            "Dictionary: ContainsKey without Add in body",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key)
                    {
                        if (!dict.ContainsKey(key))
                        {
                            System.Console.WriteLine(""not found"");
                        }
                    }
                }
            }"
        ],
        [
            "Non-collection condition + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, bool someFlag)
                    {
                        if (someFlag)
                        {
                            dict.Add(""key"", 1);
                        }
                    }
                }
            }"
        ],
        [
            "HashSet: Contains + Add with additional logic in body (cache/guard pattern)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly HashSet<string> _cache = new HashSet<string>();
                    public void MyMethod(string key)
                    {
                        if (!_cache.Contains(key))
                        {
                            var data = LoadExpensiveData(key);
                            _cache.Add(key);
                        }
                    }
                    private string LoadExpensiveData(string key) => null;
                }
            }"
        ],
        [
            "SortedSet: Contains + Add with initialization logic (complex body)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly SortedSet<int> _processed = new SortedSet<int>();
                    public void MyMethod(int id)
                    {
                        if (!_processed.Contains(id))
                        {
                            ProcessItem(id);
                            _processed.Add(id);
                        }
                    }
                    private void ProcessItem(int id) { }
                }
            }"
        ],
        [
            "HashSet: positive Contains + else with complex body (cache pattern, Pattern C)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly HashSet<string> _seen = new HashSet<string>();
                    public void MyMethod(string key)
                    {
                        if (_seen.Contains(key))
                        {
                            System.Console.WriteLine(""already processed"");
                        }
                        else
                        {
                            InitializeResource(key);
                            _seen.Add(key);
                        }
                    }
                    private void InitializeResource(string key) { }
                }
            }"
        ],
        [
            "DbSet check-then-act (DSA012 territory, not DSA017)",
            @"
            using System.Linq;
            using Microsoft.EntityFrameworkCore;
            namespace TestApp
            {
                public class Item { public int Id { get; set; } public string Name { get; set; } }
                public class MyDbContext : DbContext { public DbSet<Item> Items { get; set; } }
                public class MyService
                {
                    private readonly MyDbContext _db = null;
                    public void AddItem(string name)
                    {
                        if (!_db.Items.Any(x => x.Name == name))
                        {
                            _db.Items.Add(new Item { Name = name });
                        }
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
        var test = new CSharpAnalyzerVerifier<DSA017Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        await test.RunAsync().ConfigureAwait(false);
    }
}
