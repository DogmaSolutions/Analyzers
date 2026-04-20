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
