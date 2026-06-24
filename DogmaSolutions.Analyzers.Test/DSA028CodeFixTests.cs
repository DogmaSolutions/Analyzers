using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA028CodeFixTests
{
    [TestMethod]
    public async Task FixDirectToListReturnWithIEnumerable()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            return source.Where(x => x > 0).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDirectToListReturnWithIReadOnlyCollection()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IReadOnlyCollection<string> GetNames(IEnumerable<string> source)
        {
            return source.Where(x => x.Length > 0).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDirectToListReturnWithIReadOnlyList()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IReadOnlyList<int> GetValues(IEnumerable<int> source)
        {
            return source.OrderBy(x => x).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixExpressionBodyMethodToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source) => {|#0:source.ToList()|};
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source) => source.ToArray();
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixVariableToListThenReturn()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<string> GetItems(IEnumerable<string> source)
        {
            var result = source.Where(x => x != null).ToArray();
            return result;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixVariableToListThenReturnIReadOnlyCollection()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IReadOnlyCollection<int> GetNumbers(int[] data)
        {
            var filtered = data.Where(x => x > 0).ToArray();
            return filtered;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixPropertyGetterToList()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IReadOnlyList<int> Items
        {
            get { return _data.Where(x => x > 0).ToArray(); }
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixLocalFunctionToList()
    {
        var source = @"
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
}";

        var fixedSource = @"
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
                return source.Where(x => x > 0).ToArray();
            }
            var items = GetFiltered();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixMultipleReturnStatements()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, bool filter)
        {
            if (filter)
                return source.Where(x => x > 0).ToArray();
            return source.ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixInterfaceImplementationToList()
    {
        var source = @"
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
}";

        var fixedSource = @"
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
            return _data.Where(x => x != null).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixChainedLinqToList()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Category; public string Name; }
    public class MyService
    {
        public IEnumerable<string> GetNames(IEnumerable<Item> items)
        {
            return items.Where(x => x.Category == 1).Select(x => x.Name).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixVariableReadWithLinqCount()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IReadOnlyList<int> GetSorted(IEnumerable<int> source)
        {
            var sorted = source.OrderBy(x => x).ToArray();
            System.Console.WriteLine(sorted.Count());
            return sorted;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixExpressionBodyLocalFunction()
    {
        var source = @"
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
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(IEnumerable<int> source)
        {
            IReadOnlyCollection<int> GetFiltered() => source.Where(x => x > 0).ToArray();
            var items = GetFiltered();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixExpressionBodiedPropertyToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IEnumerable<int> Items => {|#0:_data.Where(x => x > 0).ToList()|};
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IEnumerable<int> Items => _data.Where(x => x > 0).ToArray();
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixAsyncMethodToList()
    {
        var source = @"
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
}";

        var fixedSource = @"
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
            return source.Where(x => x > 0).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }
}
