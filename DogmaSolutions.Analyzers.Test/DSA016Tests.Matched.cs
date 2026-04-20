using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA016Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "FirstOrDefault with same predicate called 3 times in a Select lambda",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class OrderLine { public int OrderId; public string Description; public int Quantity; public decimal UnitPrice; }
                public class Order { public int OrderId; }
                public class MyService
                {
                    public void Process(IEnumerable<Order> orders, IEnumerable<OrderLine> lines)
                    {
                        var result = orders.Select(o => new
                        {
                            Desc = {|#0:lines.FirstOrDefault(l => l.OrderId == o.OrderId)|},
                            Qty = {|#1:lines.FirstOrDefault(l => l.OrderId == o.OrderId)|},
                            Price = {|#2:lines.FirstOrDefault(l => l.OrderId == o.OrderId)|},
                        });
                    }
                }
            }",
            "FirstOrDefault",
            3
        ],
        [
            "FirstOrDefault with same predicate called 2 times in a method body",
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
                        var a = {|#0:items.FirstOrDefault(x => x.Id == id)|};
                        var b = {|#1:items.FirstOrDefault(x => x.Id == id)|};
                    }
                }
            }",
            "FirstOrDefault",
            2
        ],
        [
            "Any with same predicate called 2 times",
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
                        var a = {|#0:items.Any(x => x.Id == id)|};
                        var b = {|#1:items.Any(x => x.Id == id)|};
                    }
                }
            }",
            "Any",
            2
        ],
        [
            "Count with same predicate called 2 times",
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
                        var a = {|#0:items.Count(x => x.Id == id)|};
                        var b = {|#1:items.Count(x => x.Id == id)|};
                    }
                }
            }",
            "Count",
            2
        ],
        [
            "Count parameterless called 2 times on same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> items)
                    {
                        var a = {|#0:items.Count()|};
                        var b = {|#1:items.Count()|};
                    }
                }
            }",
            "Count",
            2
        ],
        [
            "Conditional access ?.FirstOrDefault called 2 times",
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
                        var a = items?{|#0:.FirstOrDefault(x => x.Id == id)|}?.Name;
                        var b = items?{|#1:.FirstOrDefault(x => x.Id == id)|}?.Id;
                    }
                }
            }",
            "FirstOrDefault",
            2
        ],
        [
            "Min called 2 times on same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> items)
                    {
                        var a = {|#0:items.Min()|};
                        var b = {|#1:items.Min()|};
                    }
                }
            }",
            "Min",
            2
        ],
        [
            "Contains with same argument called 2 times",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> items, int value)
                    {
                        var a = {|#0:items.Contains(value)|};
                        var b = {|#1:items.Contains(value)|};
                    }
                }
            }",
            "Contains",
            2
        ],
        [
            "Chained receiver: Where().FirstOrDefault() called 2 times",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Id; public bool Active; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items, int id)
                    {
                        var a = {|#0:items.Where(x => x.Active).FirstOrDefault(x => x.Id == id)|};
                        var b = {|#1:items.Where(x => x.Active).FirstOrDefault(x => x.Id == id)|};
                    }
                }
            }",
            "FirstOrDefault",
            2
        ],
        [
            "All with same predicate called 2 times",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public bool Valid; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items)
                    {
                        var a = {|#0:items.All(x => x.Valid)|};
                        var b = {|#1:items.All(x => x.Valid)|};
                    }
                }
            }",
            "All",
            2
        ],
        [
            "Max with selector called 2 times",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public int Value; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items)
                    {
                        var a = {|#0:items.Max(x => x.Value)|};
                        var b = {|#1:items.Max(x => x.Value)|};
                    }
                }
            }",
            "Max",
            2
        ],
        [
            "SingleOrDefault with same predicate called 2 times",
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
                        var a = {|#0:items.SingleOrDefault(x => x.Id == id)|};
                        var b = {|#1:items.SingleOrDefault(x => x.Id == id)|};
                    }
                }
            }",
            "SingleOrDefault",
            2
        ],
        [
            "Last called 2 times on same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> items)
                    {
                        var a = {|#0:items.Last()|};
                        var b = {|#1:items.Last()|};
                    }
                }
            }",
            "Last",
            2
        ],
        [
            "Sum with selector called 2 times",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class Item { public decimal Price; }
                public class MyService
                {
                    public void Process(IEnumerable<Item> items)
                    {
                        var a = {|#0:items.Sum(x => x.Price)|};
                        var b = {|#1:items.Sum(x => x.Price)|};
                    }
                }
            }",
            "Sum",
            2
        ],
        [
            "Average called 2 times on same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> items)
                    {
                        var a = {|#0:items.Average()|};
                        var b = {|#1:items.Average()|};
                    }
                }
            }",
            "Average",
            2
        ],
        [
            "LongCount called 2 times on same receiver",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyService
                {
                    public void Process(IEnumerable<int> items)
                    {
                        var a = {|#0:items.LongCount()|};
                        var b = {|#1:items.LongCount()|};
                    }
                }
            }",
            "LongCount",
            2
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode,
        string methodName,
        int expectedCount
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA016Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        for (var i = 0; i < expectedCount; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpAnalyzerVerifier<DSA016Analyzer>.Diagnostic(DSA016Analyzer.DiagnosticId)
                    .WithLocation(i)
                    .WithArguments(methodName));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
