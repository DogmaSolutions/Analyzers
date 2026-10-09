using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
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

    [TestMethod]
    public async Task FixChainedConditionalAccessCalledTwice_KeepsTheTailOnTheVariable()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var a = items?{|#0:.FirstOrDefault(x => x.Id == id)|}?.Name;
            var b = items?{|#1:.FirstOrDefault(x => x.Id == id)|}?.Id;
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var firstOrDefault = items?.FirstOrDefault(x => x.Id == id);
            var a = firstOrDefault?.Name;
            var b = firstOrDefault?.Id;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixChainedConditionalAccessWithLongTail_KeepsTheWholeTailOnTheVariable()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var a = items?{|#0:.FirstOrDefault(x => x.Id == id)|}?.Name.Length;
            var b = items?{|#1:.FirstOrDefault(x => x.Id == id)|}?.Name.Trim();
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var firstOrDefault = items?.FirstOrDefault(x => x.Id == id);
            var a = firstOrDefault?.Name.Length;
            var b = firstOrDefault?.Name.Trim();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixChainedConditionalAccessInExpressionBodyLambda_KeepsTheTailOnTheVariable()
    {
        var source = @"
using System;
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            Func<int, string> f = n => (items?{|#0:.FirstOrDefault(x => x.Id == id)|}?.Name) + (items?{|#1:.FirstOrDefault(x => x.Id == id)|}?.Name);
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            Func<int, string> f = n => {
    var firstOrDefault = items?.FirstOrDefault(x => x.Id == id);
    return (firstOrDefault?.Name) + (firstOrDefault?.Name);
};
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    // Extracting 'items?.Method()' out of 'items?.Method().Tail' would turn the null short-circuit of the whole chain
    // into a null dereference of the tail: no extraction is offered.

    [TestMethod]
    public async Task ConditionalAccessWithNonConditionalTail_OffersNoExtraction()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public string Name; }
    public class MyService
    {
        public void Process(List<Item> items, int id)
        {
            var a = items?{|#0:.FirstOrDefault(x => x.Id == id)|}.Name;
            var b = items?{|#1:.FirstOrDefault(x => x.Id == id)|}.Id;
        }
    }
}";

        await VerifyNoExtractionAsync(source, "FirstOrDefault", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ConditionalAccessFollowedByAnotherCall_OffersNoExtraction()
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
            var a = items?{|#0:.Count()|}.ToString();
            var b = items?{|#1:.Count()|}.ToString();
        }
    }
}";

        await VerifyNoExtractionAsync(source, "Count", 2).ConfigureAwait(false);
    }

    private static async Task VerifyNoExtractionAsync(string source, string methodName, int expectedCount)
    {
        var test = new CSharpCodeFixVerifier<DSA016Analyzer, DSA016CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = source;
        test.CodeActionEquivalenceKey = DSA016Analyzer.DiagnosticId;
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
