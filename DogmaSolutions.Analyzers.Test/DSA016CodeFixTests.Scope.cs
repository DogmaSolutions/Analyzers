using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Where the code fix of DSA016 declares the variable: in the narrowest block that contains the occurrences, and never where the declaration
/// would call the method on a path that the code guards against (a call such as First throws on an empty source).
/// </summary>
public partial class DSA016CodeFixTests
{
    [TestMethod]
    public async Task TheVariableIsDeclaredInTheBranchThatUsesItAndNotBeforeTheGuard()
    {
        var source = @"
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(List<int> items, bool ready)
        {
            if (!ready)
            {
                return;
            }
            else
            {
                var a = {|#0:items.First(x => x > 1)|};
                var b = {|#1:items.First(x => x > 1)|};
            }
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public void Process(List<int> items, bool ready)
        {
            if (!ready)
            {
                return;
            }
            else
            {
                var first = items.First(x => x > 1);
                var a = first;
                var b = first;
            }
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "First", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredInsideTheTryThatHandlesTheException()
    {
        var source = @"
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public int Process(List<int> items)
        {
            try
            {
                var a = {|#0:items.First(x => x > 1)|};
                return a + {|#1:items.First(x => x > 1)|};
            }
            catch (System.InvalidOperationException)
            {
                return -1;
            }
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public int Process(List<int> items)
        {
            try
            {
                var first = items.First(x => x > 1);
                var a = first;
                return a + first;
            }
            catch (System.InvalidOperationException)
            {
                return -1;
            }
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "First", 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredBeforeAnIfWhoseConditionCallsTheMethod()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public int Process(List<int> items)
        {
            if ({|#0:items.First(x => x > 1)|} > 0)
            {
                return {|#1:items.First(x => x > 1)|};
            }

            return 0;
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
        public int Process(List<int> items)
        {
            var first = items.First(x => x > 1);
            if (first > 0)
            {
                return first;
            }

            return 0;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, "First", 2).ConfigureAwait(false);
    }

    [TestMethod]
    [DataRow("the first call is in the right side of an and", "if (ready && items.First(x => x > 1) > 0) { return items.First(x => x > 1); } return 0;")]
    [DataRow("the calls are in branches that exclude each other", "if (ready) { return items.First(x => x > 1); } if (!ready) { return items.First(x => x > 1); } return 0;")]
    [DataRow("both calls are in the branches of a conditional expression", "return ready ? items.First(x => x > 1) : -items.First(x => x > 1);")]
    [DataRow("the later call follows a return", "if (ready) { var a = items.First(x => x > 1); return a; } if (!ready) { return 0; } return items.First(x => x > 1);")]
    [DataRow("a catch that does not call it", "try { var a = items.First(x => x > 1); return a; } catch (System.InvalidOperationException) { return 0; } finally { } try { return items.First(x => x > 1); } catch (System.InvalidOperationException) { return -1; }")]
    public void NoExtractionIsOfferedWhenTheDeclarationWouldCallTheMethodWhereTheCodeDoesNot(string title, string statements)
    {
        var source = "using System.Collections.Generic;\nusing System.Linq;\nnamespace TestApp\n{\n    public class MyService\n    {\n        public int Process(List<int> items, bool ready)\n        {\n            " + statements + "\n        }\n    }\n}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("t", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location) });
        var target = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First(call => call.ToString() == "items.First(x => x > 1)");

        Assert.IsFalse(DSA016CodeFixProvider.CanExtract(compilation.GetSemanticModel(tree), target), title);
    }

    [TestMethod]
    [DataRow("a later call evaluates it on every path", "if (ready) { var a = items.First(x => x > 1); } return items.First(x => x > 1);")]
    [DataRow("the first call is in the left side of an and", "if (items.First(x => x > 1) > 0 && ready) { return items.First(x => x > 1); } return 0;")]
    [DataRow("a loop is taken as one that runs", "foreach (var x in items) { var a = items.Count(); var b = items.Count(); } return 0;")]
    public void AnExtractionIsOfferedWhenTheMethodIsCalledFirstOnEveryPath(string title, string statements)
    {
        var source = "using System.Collections.Generic;\nusing System.Linq;\nnamespace TestApp\n{\n    public class MyService\n    {\n        public int Process(List<int> items, bool ready)\n        {\n            " + statements + "\n        }\n    }\n}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("t", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location), MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location), MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location) });
        var target = tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>().First(call => call.ToString() is "items.First(x => x > 1)" or "items.Count()");

        Assert.IsTrue(DSA016CodeFixProvider.CanExtract(compilation.GetSemanticModel(tree), target), title);
    }
}
