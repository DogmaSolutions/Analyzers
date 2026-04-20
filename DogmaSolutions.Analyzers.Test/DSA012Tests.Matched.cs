using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA012Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        [
            "Negated Any + Add on DbSet",
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
                        {|#0:if (!_db.Items.Any(x => x.Name == name))
                        {
                            _db.Items.Add(new Item { Name = name });
                        }|}
                    }
                }
            }"
        ],
        [
            "Positive Any + throw + Add after on DbSet",
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
                        {|#0:if (_db.Items.Any(x => x.Name == name))
                            throw new System.InvalidOperationException();|}
                        _db.Items.Add(new Item { Name = name });
                    }
                }
            }"
        ],
        [
            "Count == 0 + Add on DbSet",
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
                        {|#0:if (_db.Items.Count(x => x.Name == name) == 0)
                        {
                            _db.Items.Add(new Item { Name = name });
                        }|}
                    }
                }
            }"
        ],
        [
            "FirstOrDefault == null + Add on DbSet",
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
                        {|#0:if (_db.Items.FirstOrDefault(x => x.Name == name) == null)
                        {
                            _db.Items.Add(new Item { Name = name });
                        }|}
                    }
                }
            }"
        ],
        [
            "Positive Any + else Add on DbSet",
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
                        {|#0:if (_db.Items.Any(x => x.Name == name))
                        {
                            System.Console.WriteLine(""exists"");
                        }
                        else
                        {
                            _db.Items.Add(new Item { Name = name });
                        }|}
                    }
                }
            }"
        ],
    ];


    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA012Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA012Analyzer>.Diagnostic(DSA012Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
