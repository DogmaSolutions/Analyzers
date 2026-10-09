using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA016 code fix when the duplicated invocations are conditional accesses (<c>items?.Method()</c>):
/// the whole conditional access chain is extracted, not just the invocation (<c>DetermineExtractionTarget</c>).
/// </summary>
public partial class DSA016CodeFixTests
{
    [TestMethod]
    public async Task FixConditionalAccessCalledTwice_ExtractsTheWholeConditionalAccess()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var a = items?{|#0:.FirstOrDefault(x => x.Id == id)|};
            var b = items?{|#1:.FirstOrDefault(x => x.Id == id)|};
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
        public void Process(List<Item> items, int id)
        {
            var firstOrDefault = items?.FirstOrDefault(x => x.Id == id);
            var a = firstOrDefault;
            var b = firstOrDefault;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixConditionalAccessCalledThreeTimes_ReplacesEveryConditionalAccess()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(List<int> items)
        {
            var a = items?{|#0:.Count()|};
            var b = items?{|#1:.Count()|};
            var c = items?{|#2:.Count()|};
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
        public void Process(List<int> items)
        {
            var count = items?.Count();
            var a = count;
            var b = count;
            var c = count;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "Count", 3).ConfigureAwait(false);
    }
}
