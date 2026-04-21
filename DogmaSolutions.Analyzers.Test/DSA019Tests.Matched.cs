using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA019Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "Same deep chain with different indexer (depth 3)",
            @"
            namespace TestApp
            {
                public class Light { public bool IsOn() => true; }
                public class Room { public Light[] Lights; }
                public class Rooms { public Room Bathroom; }
                public class Home { public Rooms Rooms; }
                public class MyService
                {
                    public void Process(Home home)
                    {
                        var status = new
                        {
                            Primary = {|#0:home.Rooms.Bathroom.Lights|}[0].IsOn(),
                            Secondary = {|#1:home.Rooms.Bathroom.Lights|}[1].IsOn(),
                        };
                    }
                }
            }",
            "home.Rooms.Bathroom.Lights",
            2
        ],
        [
            "Same deep chain with different terminal property (depth 3)",
            @"
            namespace TestApp
            {
                public class DbSettings { public string ConnectionString; public int Timeout; }
                public class Infra { public DbSettings Database; }
                public class Settings { public Infra Infrastructure; }
                public class AppConfig { public Settings Settings; }
                public class MyService
                {
                    public void Process(AppConfig config)
                    {
                        var connStr = {|#0:config.Settings.Infrastructure.Database|}.ConnectionString;
                        var timeout = {|#1:config.Settings.Infrastructure.Database|}.Timeout;
                    }
                }
            }",
            "config.Settings.Infrastructure.Database",
            2
        ],
        [
            "Three repetitions of the same chain (depth 3)",
            @"
            namespace TestApp
            {
                public class Address { public string Street; public string City; public string Zip; }
                public class Profile { public Address Address; }
                public class Contact { public Profile Profile; }
                public class Customer { public Contact Contact; }
                public class MyService
                {
                    public void Process(Customer customer)
                    {
                        var street = {|#0:customer.Contact.Profile.Address|}.Street;
                        var city = {|#1:customer.Contact.Profile.Address|}.City;
                        var zip = {|#2:customer.Contact.Profile.Address|}.Zip;
                    }
                }
            }",
            "customer.Contact.Profile.Address",
            3
        ],
        [
            "Chain with method call in the middle (depth 3)",
            @"
            namespace TestApp
            {
                public class Data { public int Count; public string Label; }
                public class Report { public Data Summary; }
                public class Service { public Report GetReport() => null; }
                public class Provider { public Service Service; }
                public class MyService
                {
                    public void Process(Provider provider)
                    {
                        var count = {|#0:provider.Service.GetReport().Summary|}.Count;
                        var label = {|#1:provider.Service.GetReport().Summary|}.Label;
                    }
                }
            }",
            "provider.Service.GetReport().Summary",
            2
        ],
        [
            "Deep chain repeated in a lambda (depth 3)",
            @"
            using System.Linq;
            namespace TestApp
            {
                public class ItemDetail { public string Name; public decimal Price; }
                public class ItemSpec { public ItemDetail Detail; }
                public class ItemCategory { public ItemSpec Spec; }
                public class CatalogEntry { public ItemCategory Category; }
                public class MyService
                {
                    public void Process(CatalogEntry[] entries)
                    {
                        var result = entries.Select(e => new
                        {
                            Name = {|#0:e.Category.Spec.Detail|}.Name,
                            Price = {|#1:e.Category.Spec.Detail|}.Price,
                        });
                    }
                }
            }",
            "e.Category.Spec.Detail",
            2
        ],
        [
            "this. prefix (depth 3 from this)",
            @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class MyService
                {
                    private Middle _field;
                    public void Process()
                    {
                        var x = {|#0:this._field.Inner.Deep|}.X;
                        var y = {|#1:this._field.Inner.Deep|}.Y;
                    }
                }
            }",
            "this._field.Inner.Deep",
            2
        ],
        [
            "Static member access via singleton instance (depth 3)",
            @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Config
                {
                    public static Config Instance = new Config();
                    public Inner Inner;
                }
                public class MyService
                {
                    public void Process()
                    {
                        var x = {|#0:Config.Instance.Inner.Deep|}.X;
                        var y = {|#1:Config.Instance.Inner.Deep|}.Y;
                    }
                }
            }",
            "Config.Instance.Inner.Deep",
            2
        ],
        [
            "Chained method calls traversed transparently (depth 4)",
            @"
            namespace TestApp
            {
                public class Result { public int Total; public string Status; }
                public class Query { public Result Execute() => null; }
                public class Builder { public Query Build() => null; }
                public class Factory { public Builder CreateBuilder() => null; }
                public class MyService
                {
                    public void Process(Factory factory)
                    {
                        var total = {|#0:factory.CreateBuilder().Build().Execute|}().Total;
                        var status = {|#1:factory.CreateBuilder().Build().Execute|}().Status;
                    }
                }
            }",
            "factory.CreateBuilder().Build().Execute",
            2
        ],
        [
            "Standalone repeated ElementAccess (depth 3)",
            @"
            namespace TestApp
            {
                public class Grid
                {
                    public int[][] Cells;
                }
                public class MyService
                {
                    public void Process(Grid grid, int r, int c)
                    {
                        var a = {|#0:grid.Cells[r][c]|};
                        var b = {|#1:grid.Cells[r][c]|};
                    }
                }
            }",
            "grid.Cells[r][c]",
            2
        ],
        [
            "Chain in object initializer context",
            @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class Outer { public Middle Middle; }
                public class MyService
                {
                    public void Process(Outer outer)
                    {
                        var dto = new
                        {
                            X = {|#0:outer.Middle.Inner.Deep|}.X,
                            Y = {|#1:outer.Middle.Inner.Deep|}.Y,
                        };
                    }
                }
            }",
            "outer.Middle.Inner.Deep",
            2
        ],
        [
            "Chain in constructor body",
            @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class Outer { public Middle Middle; }
                public class MyClass
                {
                    private int _x;
                    private int _y;
                    public MyClass(Outer outer)
                    {
                        _x = {|#0:outer.Middle.Inner.Deep|}.X;
                        _y = {|#1:outer.Middle.Inner.Deep|}.Y;
                    }
                }
            }",
            "outer.Middle.Inner.Deep",
            2
        ],
        [
            "Chain as method argument",
            @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class Outer { public Middle Middle; }
                public class MyService
                {
                    private void Use(int a, int b) { }
                    public void Process(Outer outer)
                    {
                        Use({|#0:outer.Middle.Inner.Deep|}.X, {|#1:outer.Middle.Inner.Deep|}.Y);
                    }
                }
            }",
            "outer.Middle.Inner.Deep",
            2
        ],
        [
            "Chain on left side of assignment",
            @"
            namespace TestApp
            {
                public class Deep { public int X; public int Y; }
                public class Inner { public Deep Deep; }
                public class Middle { public Inner Inner; }
                public class Outer { public Middle Middle; }
                public class MyService
                {
                    public void Process(Outer outer)
                    {
                        {|#0:outer.Middle.Inner.Deep|}.X = 1;
                        {|#1:outer.Middle.Inner.Deep|}.Y = 2;
                    }
                }
            }",
            "outer.Middle.Inner.Deep",
            2
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string chainText,
        int expectedCount
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA019Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        for (var i = 0; i < expectedCount; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<DSA019Analyzer>.Diagnostic(DSA019Analyzer.DiagnosticId)
                    .WithLocation(i)
                    .WithArguments(chainText));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
