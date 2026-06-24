using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA016CodeFixTests
{
    // ---------------------------------------------------------------
    // Pattern A: same method called 2 times in a method body
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixFirstOrDefaultCalledTwice()
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
            var a = {|#0:items.FirstOrDefault(x => x.Id == id)|};
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
        public void Process(IEnumerable<Item> items, int id)
        {
            var firstOrDefault = items.FirstOrDefault(x => x.Id == id);
            var a = firstOrDefault;
            var b = firstOrDefault;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixAnyCalledTwice()
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
            var a = {|#0:items.Any(x => x.Id == id)|};
            var b = {|#1:items.Any(x => x.Id == id)|};
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
            var any = items.Any(x => x.Id == id);
            var a = any;
            var b = any;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Any", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixCountParameterlessCalledTwice()
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
            var a = {|#0:items.Count()|};
            var b = {|#1:items.Count()|};
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
            var a = count;
            var b = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixCountWithPredicateCalledTwice()
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
            var a = {|#0:items.Count(x => x.Id == id)|};
            var b = {|#1:items.Count(x => x.Id == id)|};
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
            var count = items.Count(x => x.Id == id);
            var a = count;
            var b = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixMinCalledTwice()
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
            var a = {|#0:items.Min()|};
            var b = {|#1:items.Min()|};
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
            var min = items.Min();
            var a = min;
            var b = min;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Min", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixMaxWithSelectorCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Value; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items)
        {
            var max = items.Max(x => x.Value);
            var a = max;
            var b = max;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Max", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixContainsCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> items, int value)
        {
            var contains = items.Contains(value);
            var a = contains;
            var b = contains;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Contains", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSumWithSelectorCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public decimal Price; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items)
        {
            var sum = items.Sum(x => x.Price);
            var a = sum;
            var b = sum;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Sum", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixAllCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public bool Valid; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items)
        {
            var all = items.All(x => x.Valid);
            var a = all;
            var b = all;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "All", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixExistsCalledTwice()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class Item { public int Id; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var a = {|#0:items.Exists(x => x.Id == id)|};
            var b = {|#1:items.Exists(x => x.Id == id)|};
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class Item { public int Id; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var exists = items.Exists(x => x.Id == id);
            var a = exists;
            var b = exists;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Exists", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Three occurrences
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixFirstOrDefaultCalledThreeTimes()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; public int Value; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id)
        {
            var a = {|#0:items.FirstOrDefault(x => x.Id == id)|};
            var b = {|#1:items.FirstOrDefault(x => x.Id == id)|};
            var c = {|#2:items.FirstOrDefault(x => x.Id == id)|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; public int Value; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id)
        {
            var firstOrDefault = items.FirstOrDefault(x => x.Id == id);
            var a = firstOrDefault;
            var b = firstOrDefault;
            var c = firstOrDefault;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 3).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Chained receiver
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixChainedReceiverCalledTwice()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public bool Active; }
    public class MyService
    {
        public void Process(IEnumerable<Item> items, int id)
        {
            var firstOrDefault = items.Where(x => x.Active).FirstOrDefault(x => x.Id == id);
            var a = firstOrDefault;
            var b = firstOrDefault;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Name conflict resolution
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixWithNameConflict()
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
            var count = ""already taken"";
            var a = {|#0:items.Count()|};
            var b = {|#1:items.Count()|};
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
            var count = ""already taken"";
            var count1 = items.Count();
            var a = count1;
            var b = count1;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Insertion before earliest usage
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixInsertionBeforeEarliestUsage()
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
            var unrelated = 42;
            System.Console.WriteLine(unrelated);
            var a = {|#0:items.Count()|};
            var b = {|#1:items.Count()|};
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
            var unrelated = 42;
            System.Console.WriteLine(unrelated);
            var count = items.Count();
            var a = count;
            var b = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }
}
