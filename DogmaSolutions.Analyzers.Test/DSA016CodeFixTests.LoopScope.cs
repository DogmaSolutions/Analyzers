using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA016 code fix when the duplicated invocations are inside a loop: the variable is declared inside the loop body when
/// the extracted expression depends on the loop variable, and before the loop otherwise (<c>ConstrainInsertionToLoopScope</c>).
/// </summary>
public partial class DSA016CodeFixTests
{
    private static string LoopSource(string body) => @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class Item { public int Id; public List<Item> Children; }
    public class MyService
    {
        public void Process(List<Item> items, List<List<Item>> groups, int id)
        {
" + body + @"
        }
    }
}";

    [TestMethod]
    public async Task FixForEachLoopVariable_DeclaresInsideTheLoopBody()
    {
        var source = LoopSource(@"
            foreach (var item in items)
            {
                var a = {|#0:item.Children.Count()|};
                var b = {|#1:item.Children.Count()|};
            }");

        var fixedSource = LoopSource(@"
            foreach (var item in items)
            {
                var count = item.Children.Count();
                var a = count;
                var b = count;
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixForLoopVariable_DeclaresInsideTheLoopBody()
    {
        var source = LoopSource(@"
            for (var i = 0; i < items.Count; i++)
            {
                var a = {|#0:items[i].Children.Count()|};
                var b = {|#1:items[i].Children.Count()|};
            }");

        var fixedSource = LoopSource(@"
            for (var i = 0; i < items.Count; i++)
            {
                var count = items[i].Children.Count();
                var a = count;
                var b = count;
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixForEachLoopInvariant_DeclaresBeforeTheLoop()
    {
        var source = LoopSource(@"
            foreach (var item in items)
            {
                var a = {|#0:items.Count()|};
                var b = {|#1:items.Count()|};
            }");

        var fixedSource = LoopSource(@"
            var count = items.Count();

            foreach (var item in items)
            {
                var a = count;
                var b = count;
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixForLoopInvariant_DeclaresBeforeTheLoop()
    {
        var source = LoopSource(@"
            for (var i = 0; i < 3; i++)
            {
                var a = {|#0:items.Count()|};
                var b = {|#1:items.Count()|};
            }");

        var fixedSource = LoopSource(@"
            var count = items.Count();

            for (var i = 0; i < 3; i++)
            {
                var a = count;
                var b = count;
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixForWithoutDeclaration_DeclaresBeforeTheLoop()
    {
        var source = LoopSource(@"
            var i = 0;
            for (; i < 3; i++)
            {
                var a = {|#0:items.Count()|};
                var b = {|#1:items.Count()|};
            }");

        var fixedSource = LoopSource(@"
            var i = 0;
            var count = items.Count();
            for (; i < 3; i++)
            {
                var a = count;
                var b = count;
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNestedLoops_OuterLoopVariableOnly_DeclaresInsideTheOuterLoop()
    {
        var source = LoopSource(@"
            foreach (var group in groups)
            {
                foreach (var item in items)
                {
                    var a = {|#0:group.Count()|};
                    var b = {|#1:group.Count()|};
                }
            }");

        var fixedSource = LoopSource(@"
            foreach (var group in groups)
            {
                var count = group.Count();
                foreach (var item in items)
                {
                    var a = count;
                    var b = count;
                }
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNestedLoops_BothLoopVariables_DeclaresInsideTheInnerLoop()
    {
        var source = LoopSource(@"
            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    var a = {|#0:group.Count(c => c.Id == item.Id)|};
                    var b = {|#1:group.Count(c => c.Id == item.Id)|};
                }
            }");

        var fixedSource = LoopSource(@"
            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    var count = group.Count(c => c.Id == item.Id);
                    var a = count;
                    var b = count;
                }
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixForEachLoopVariable_UsedInANestedBlock_DeclaresBeforeTheEarliestStatementOfTheLoopBody()
    {
        var source = LoopSource(@"
            foreach (var item in items)
            {
                if (id > 0)
                {
                    var a = {|#0:item.Children.Count()|};
                    var b = {|#1:item.Children.Count()|};
                }
            }");

        var fixedSource = LoopSource(@"
            foreach (var item in items)
            {
                var count = item.Children.Count();
                if (id > 0)
                {
                    var a = count;
                    var b = count;
                }
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // A loop without braces: the variable can't be declared before the loop (it depends on the loop variable),
    // so the body is wrapped in a block.

    [TestMethod]
    public async Task FixForEachLoopVariable_WithoutBraces_WrapsTheBodyInABlock()
    {
        var source = LoopSource(@"
            foreach (var item in items)
                System.Console.WriteLine({|#0:item.Children.Count()|} + {|#1:item.Children.Count()|});");

        var fixedSource = LoopSource(@"
            foreach (var item in items)
            {
                var count = item.Children.Count();
                System.Console.WriteLine(count + count);
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixForLoopVariable_WithoutBraces_WrapsTheBodyInABlock()
    {
        var source = LoopSource(@"
            for (var i = 0; i < items.Count; i++)
                System.Console.WriteLine({|#0:items[i].Children.Count()|} + {|#1:items[i].Children.Count()|});");

        var fixedSource = LoopSource(@"
            for (var i = 0; i < items.Count; i++)
            {
                var count = items[i].Children.Count();
                System.Console.WriteLine(count + count);
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNestedLoopsWithoutBraces_WrapsTheInnermostBodyInABlock()
    {
        var source = LoopSource(@"
            foreach (var group in groups)
                foreach (var item in group)
                    System.Console.WriteLine({|#0:group.Count(c => c.Id == item.Id)|} + {|#1:group.Count(c => c.Id == item.Id)|});");

        var fixedSource = LoopSource(@"
            foreach (var group in groups)
                foreach (var item in group)
                {
                    var count = group.Count(c => c.Id == item.Id);
                    System.Console.WriteLine(count + count);
                }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    // The declaration goes inside the innermost loop whose variable the expression uses, even if the outer loops are unrelated.

    [TestMethod]
    public async Task FixNestedLoops_InnerLoopVariableOnly_DeclaresInsideTheInnerLoop()
    {
        var source = LoopSource(@"
            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    var a = {|#0:item.Children.Count()|};
                    var b = {|#1:item.Children.Count()|};
                }
            }");

        var fixedSource = LoopSource(@"
            foreach (var group in groups)
            {
                foreach (var item in group)
                {
                    var count = item.Children.Count();
                    var a = count;
                    var b = count;
                }
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNestedLoops_ForLoopVariableInsideAnUnrelatedForEach_DeclaresInsideTheForLoop()
    {
        var source = LoopSource(@"
            foreach (var group in groups)
            {
                for (var i = 0; i < group.Count; i++)
                {
                    var a = {|#0:group[i].Children.Count()|};
                    var b = {|#1:group[i].Children.Count()|};
                }
            }");

        var fixedSource = LoopSource(@"
            foreach (var group in groups)
            {
                for (var i = 0; i < group.Count; i++)
                {
                    var count = group[i].Children.Count();
                    var a = count;
                    var b = count;
                }
            }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNestedLoopsWithoutBraces_InnerLoopVariableOnly_WrapsTheInnerBodyInABlock()
    {
        var source = LoopSource(@"
            foreach (var group in groups)
                foreach (var item in group)
                    System.Console.WriteLine({|#0:item.Children.Count()|} + {|#1:item.Children.Count()|});");

        var fixedSource = LoopSource(@"
            foreach (var group in groups)
                foreach (var item in group)
                {
                    var count = item.Children.Count();
                    System.Console.WriteLine(count + count);
                }");

        await VerifyFixAsync(source, fixedSource, "Count", 2).ConfigureAwait(false);
    }
}
