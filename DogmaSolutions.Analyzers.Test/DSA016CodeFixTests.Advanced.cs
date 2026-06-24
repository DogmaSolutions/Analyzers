using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA016CodeFixTests
{

    // ---------------------------------------------------------------
    // Field receiver
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixFieldReceiver()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly List<int> _items = new List<int>();
        public void Process()
        {
            var a = {|#0:_items.Count()|};
            var b = {|#1:_items.Count()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly List<int> _items = new List<int>();
        public void Process()
        {
            var count = _items.Count();
            var a = count;
            var b = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Property receiver
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixPropertyReceiver()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public List<int> Items { get; } = new List<int>();
        public void Process()
        {
            var a = {|#0:Items.Count()|};
            var b = {|#1:Items.Count()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public List<int> Items { get; } = new List<int>();
        public void Process()
        {
            var count = Items.Count();
            var a = count;
            var b = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Member access chain receiver
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixMemberAccessChainReceiver()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Container { public List<int> Items { get; } = new List<int>(); }
    public class MyService
    {
        private readonly Container _container = new Container();
        public void Process()
        {
            var a = {|#0:_container.Items.Count()|};
            var b = {|#1:_container.Items.Count()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Container { public List<int> Items { get; } = new List<int>(); }
    public class MyService
    {
        private readonly Container _container = new Container();
        public void Process()
        {
            var count = _container.Items.Count();
            var a = count;
            var b = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Used in expressions (not just assignments)
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixUsedInIfConditionAndAssignment()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            if ({|#0:items.Any()|})
            {
                System.Console.WriteLine(""not empty"");
            }
            var flag = {|#1:items.Any()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            var any = items.Any();
            if (any)
            {
                System.Console.WriteLine(""not empty"");
            }
            var flag = any;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Any", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Expression-body lambda conversion
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixInExpressionBodyLambdaSelectCalledThreeTimes()
    {
        var source = @"
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
}";

        var fixedSource = @"
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
            var result = orders.Select(o => {
    var firstOrDefault = lines.FirstOrDefault(l => l.OrderId == o.OrderId);
    return new
    {
        Desc = firstOrDefault,
        Qty = firstOrDefault,
        Price = firstOrDefault,
    };
});
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 3).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // No-fix cases: diagnostic fires but no code change
    // (complex scenarios where the code fix cannot safely apply)
    // ---------------------------------------------------------------

    // ---------------------------------------------------------------
    // SingleOrDefault, Last, Average, LongCount
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixSingleOrDefaultCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id)
        {
            var singleOrDefault = items.SingleOrDefault(x => x.Id == id);
            var a = singleOrDefault;
            var b = singleOrDefault;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "SingleOrDefault", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixLastCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            var last = items.Last();
            var a = last;
            var b = last;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Last", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixAverageCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            var average = items.Average();
            var a = average;
            var b = average;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Average", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixLongCountCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            var longCount = items.LongCount();
            var a = longCount;
            var b = longCount;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "LongCount", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Used in method argument and assignment
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixUsedAsMethodArgument()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            System.Console.WriteLine({|#0:items.Count()|});
            var c = {|#1:items.Count()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items)
        {
            var count = items.Count();
            System.Console.WriteLine(count);
            var c = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Same call in a branch AND after the branching construct
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixInBranchAndAfterBranch()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id, bool flag)
        {
            if (flag)
            {
                var a = {|#0:items.FirstOrDefault(x => x.Id == id)|};
            }
            var b = {|#1:items.FirstOrDefault(x => x.Id == id)|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id, bool flag)
        {
            var firstOrDefault = items.FirstOrDefault(x => x.Id == id);
            if (flag)
            {
                var a = firstOrDefault;
            }
            var b = firstOrDefault;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Same call twice within the same switch case branch
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixTwiceInSameSwitchCase()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public enum Mode { A, B }
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id, Mode mode)
        {
            switch (mode)
            {
                case Mode.A:
                    var x = {|#0:items.FirstOrDefault(i => i.Id == id)|}?.Name;
                    var y = {|#1:items.FirstOrDefault(i => i.Id == id)|}?.Id;
                    break;
            }
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public enum Mode { A, B }
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id, Mode mode)
        {
            var firstOrDefault = items.FirstOrDefault(i => i.Id == id);
            switch (mode)
            {
                case Mode.A:
                    var x = firstOrDefault?.Name;
                    var y = firstOrDefault?.Id;
                    break;
            }
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------

    private static async Task VerifyFixAsync(
        string source,
        string fixedSource,
        string methodName,
        int expectedCount)
    {
        var test = new CSharpCodeFixVerifier<DSA016Analyzer, DSA016CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        for (var i = 0; i < expectedCount; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpCodeFixVerifier<DSA016Analyzer, DSA016CodeFixProvider>
                    .Diagnostic(DSA016Analyzer.DiagnosticId).WithLocation(i)
                    .WithArguments(methodName, expectedCount));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
