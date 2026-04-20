using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA018Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Just Add without existence check",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "Negated Any without Add in body",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (!items.Any(x => x == ""test""))
                        {
                            System.Console.WriteLine(""not found"");
                        }
                    }
                }
            }"
        ],
        [
            "Non-existence condition + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(bool someFlag)
                    {
                        var items = new List<string>();
                        if (someFlag)
                        {
                            items.Add(""test"");
                        }
                    }
                }
            }"
        ],
        [
            "Dictionary check-then-act (DSA017 territory, not DSA018)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key, int value)
                    {
                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, value);
                        }
                    }
                }
            }"
        ],
        [
            "HashSet check-then-act (DSA017 territory, not DSA018)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(HashSet<string> set, string item)
                    {
                        if (!set.Contains(item))
                        {
                            set.Add(item);
                        }
                    }
                }
            }"
        ],
        [
            "DbSet check-then-act (DSA012 territory, not DSA018)",
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
        [
            "Positive Any + throw without Add after",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (items.Any(x => x == ""test""))
                            throw new System.InvalidOperationException();
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
        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
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
