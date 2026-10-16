using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Where the code fix of DSA019 declares the variable: in the narrowest block that contains the occurrences, and never where the declaration
/// would evaluate the expression on a path that the code guards against.
/// </summary>
public partial class DSA019CodeFixTests
{
    private const string ScopeTypes = @"
    public class Cell { public int Value; }
    public class Row { public Cell[] Cells; public string Label; }
    public class Sheet { public Row Row; }
    public class Book { public Sheet Sheet; public bool Ready; }";

    private static async Task RunScopeFixAsync(string source, string fixedSource, params (int Line, int Start, int End, string Chain)[] diagnostics)
    {
        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        foreach (var (line, start, end, chain) in diagnostics)
        {
            test.ExpectedDiagnostics.Add(
                CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                    .WithSpan(line, start, line, end).WithArguments(chain, 2));
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredInTheBranchThatUsesItAndNotBeforeTheGuard()
    {
        var source = @"
namespace TestApp
{" + ScopeTypes + @"
    public class MyService
    {
        public void Refresh(Book book, int[] outputs)
        {
            if (!book.Ready)
            {
                outputs[0] = 0;
            }
            else
            {
                outputs[0] = book.Sheet.Row.Cells[0].Value;
                outputs[1] = book.Sheet.Row.Cells[0].Value + 1;
            }
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{" + ScopeTypes + @"
    public class MyService
    {
        public void Refresh(Book book, int[] outputs)
        {
            if (!book.Ready)
            {
                outputs[0] = 0;
            }
            else
            {
                var cells = book.Sheet.Row.Cells;
                outputs[0] = cells[0].Value;
                outputs[1] = cells[0].Value + 1;
            }
        }
    }
}";

        await RunScopeFixAsync(source, fixedSource, (18, 30, 50, "book.Sheet.Row.Cells"), (19, 30, 50, "book.Sheet.Row.Cells"), (18, 30, 53, "book.Sheet.Row.Cells[0]"), (18, 30, 59, "book.Sheet.Row.Cells[0].Value"), (19, 30, 53, "book.Sheet.Row.Cells[0]"), (19, 30, 59, "book.Sheet.Row.Cells[0].Value")).ConfigureAwait(false);
    }

    private static async Task RunLabelFixAsync(string body, string fixedBody, int occurrences, string types = ScopeTypes)
    {
        string Wrap(string text) => "\nnamespace TestApp\n{" + types + "\n    public class MyService\n    {\n" + text + "\n    }\n}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = Wrap(body);
        test.FixedCode = Wrap(fixedBody);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        for (var i = 0; i < occurrences; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                    .WithLocation(i).WithArguments("book.Sheet.Row.Label", occurrences));
        }

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredInTheBlockThatContainsAllTheOccurrences()
    {
        await RunLabelFixAsync(
            @"        public void Show(Book book, bool verbose, string[] output)
        {
            if (verbose)
            {
                output[0] = {|#0:book.Sheet.Row.Label|};
                output[1] = {|#1:book.Sheet.Row.Label|} + ""!"";
            }

            output[2] = ""done"";
        }",
            @"        public void Show(Book book, bool verbose, string[] output)
        {
            if (verbose)
            {
                var label = book.Sheet.Row.Label;
                output[0] = label;
                output[1] = label + ""!"";
            }

            output[2] = ""done"";
        }",
            2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredBeforeAnIfWhoseBranchesBothUseIt()
    {
        await RunLabelFixAsync(
            @"        public void Show(Book book, bool verbose, string[] output)
        {
            if (verbose)
            {
                output[0] = {|#0:book.Sheet.Row.Label|};
            }
            else
            {
                output[1] = {|#1:book.Sheet.Row.Label|};
            }
        }",
            @"        public void Show(Book book, bool verbose, string[] output)
        {
            var label = book.Sheet.Row.Label;
            if (verbose)
            {
                output[0] = label;
            }
            else
            {
                output[1] = label;
            }
        }",
            2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredBeforeAnIfWhoseConditionUsesIt()
    {
        await RunLabelFixAsync(
            @"        public void Show(Book book, string[] output)
        {
            if ({|#0:book.Sheet.Row.Label|} != null)
            {
                output[0] = {|#1:book.Sheet.Row.Label|};
            }
        }",
            @"        public void Show(Book book, string[] output)
        {
            var label = book.Sheet.Row.Label;
            if (label != null)
            {
                output[0] = label;
            }
        }",
            2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredAfterTheGuardThatReturns()
    {
        await RunLabelFixAsync(
            @"        public void Show(Book book, bool ready, string[] output)
        {
            if (!ready)
            {
                return;
            }

            output[0] = {|#0:book.Sheet.Row.Label|};
            output[1] = {|#1:book.Sheet.Row.Label|} + ""!"";
        }",
            @"        public void Show(Book book, bool ready, string[] output)
        {
            if (!ready)
            {
                return;
            }

            var label = book.Sheet.Row.Label;
            output[0] = label;
            output[1] = label + ""!"";
        }",
            2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TheVariableIsDeclaredInTheSwitchSectionThatUsesIt()
    {
        await RunLabelFixAsync(
            @"        public void Show(Book book, int kind, string[] output)
        {
            switch (kind)
            {
                case 0:
                    output[0] = ""none"";
                    break;
                default:
                    output[0] = {|#0:book.Sheet.Row.Label|};
                    output[1] = {|#1:book.Sheet.Row.Label|} + ""!"";
                    break;
            }
        }",
            @"        public void Show(Book book, int kind, string[] output)
        {
            switch (kind)
            {
                case 0:
                    output[0] = ""none"";
                    break;
                default:
                    var label = book.Sheet.Row.Label;
                    output[0] = label;
                    output[1] = label + ""!"";
                    break;
            }
        }",
            2).ConfigureAwait(false);
    }

    // title, body of a method, the chain that is repeated
    [TestMethod]
    [DataRow(
        "the occurrences are in two branches that exclude each other",
        "if (ready) { output[0] = book.Sheet.Row.Label; } if (!ready) { output[1] = book.Sheet.Row.Label; }")]
    [DataRow(
        "both occurrences are in the branches of a conditional expression",
        "output[0] = ready ? book.Sheet.Row.Label : string.Empty; output[1] = ready ? book.Sheet.Row.Label : string.Empty;")]
    [DataRow(
        "both occurrences are in incrementors",
        "for (var i = 0; i < 3; output[i] = book.Sheet.Row.Label, output[i + 1] = book.Sheet.Row.Label) { }")]
    [DataRow(
        "both occurrences are in arms of a switch expression",
        "output[0] = output.Length switch { 0 => book.Sheet.Row.Label, _ => book.Sheet.Row.Label };")]
    [DataRow(
        "statements whose body is not looked into",
        "checked { output[0] = book.Sheet.Row.Label; } checked { output[1] = book.Sheet.Row.Label; }")]
    [DataRow(
        "embedded statements without a block",
        "if (ready) output[0] = book.Sheet.Row.Label; if (!ready) output[1] = book.Sheet.Row.Label;")]
    [DataRow(
        "two tries whose catches do not evaluate it",
        "try { output[0] = book.Sheet.Row.Label; } catch { output[0] = string.Empty; } try { output[1] = book.Sheet.Row.Label; } catch { output[1] = string.Empty; }")]
    [DataRow(
        "the later occurrence follows a statement that can return",
        "if (ready) { output[0] = book.Sheet.Row.Label; } if (!ready) { return; } output[1] = book.Sheet.Row.Label;")]
    [DataRow(
        "the first occurrence is in the right side of an and",
        "if (ready && book.Sheet.Row.Label.Length > 0) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow(
        "a section of a switch",
        "switch (output.Length) { case 0: output[0] = book.Sheet.Row.Label; break; } output[1] = book.Sheet.Row.Label;")]
    public void NoExtractionIsOfferedWhenTheDeclarationWouldEvaluateTheExpressionWhereTheCodeDoesNot(string title, string statements)
    {
        var source = "namespace TestApp\n{" + ScopeTypes + "\n    public class MyService\n    {\n        public void Show(Book book, bool ready, string[] output)\n        {\n            " + statements + "\n        }\n    }\n}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("t", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var model = compilation.GetSemanticModel(tree);
        var target = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().First(ma => ma.ToString() == "book.Sheet.Row.Label");

        Assert.IsFalse(DSA019CodeFixProvider.CanExtract(model, target), title);
    }

    [TestMethod]
    [DataRow("a later occurrence evaluates it on every path", "if (ready) { output[0] = book.Sheet.Row.Label; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("a later occurrence, after a branch of a conditional expression", "output[0] = ready ? book.Sheet.Row.Label : string.Empty; output[1] = book.Sheet.Row.Label;")]
    [DataRow("a loop is taken as one that runs", "foreach (var c in output) { output[0] = book.Sheet.Row.Label; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("a later occurrence after a try", "try { output[0] = book.Sheet.Row.Label; } catch { output[0] = string.Empty; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("a later occurrence after a statement whose body is not looked into", "checked { output[0] = book.Sheet.Row.Label; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("a later occurrence after an embedded statement", "if (ready) output[0] = book.Sheet.Row.Label; output[1] = book.Sheet.Row.Label;")]
    [DataRow("a plain statement", "output[0] = book.Sheet.Row.Label; output[1] = book.Sheet.Row.Label;")]
    [DataRow("a return", "output[0] = book.Sheet.Row.Label; if (ready) { return; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("the condition of an if", "if (book.Sheet.Row.Label != null) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the left side of an and", "if (book.Sheet.Row.Label != null && ready) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the condition of a conditional expression", "output[0] = book.Sheet.Row.Label != null ? book.Sheet.Row.Label : string.Empty;")]
    [DataRow("the left side of a coalesce", "output[0] = book.Sheet.Row.Label ?? book.Sheet.Row.Label;")]
    [DataRow("both branches of an if with an else", "if (ready) { output[0] = book.Sheet.Row.Label; } else { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("a try and its catch", "try { output[0] = book.Sheet.Row.Label; } catch { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("a try without a catch", "try { output[0] = book.Sheet.Row.Label; } finally { output[1] = book.Sheet.Row.Label; }")]
    [DataRow("the condition of a while", "while (book.Sheet.Row.Label == null) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the declaration of a for", "for (var i = book.Sheet.Row.Label.Length; i < 3; i++) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the initializer of a for", "int j; for (j = book.Sheet.Row.Label.Length; j < 3; j++) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the condition of a for", "for (var i = 0; i < book.Sheet.Row.Label.Length; i++) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the expression of a foreach", "foreach (var c in book.Sheet.Row.Label) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the first statement of a do", "do { output[0] = book.Sheet.Row.Label; } while (ready); output[1] = book.Sheet.Row.Label;")]
    [DataRow("the expression of a switch", "switch (book.Sheet.Row.Label) { default: output[0] = book.Sheet.Row.Label; break; }")]
    [DataRow("the expression of a lock", "lock (book.Sheet.Row.Label) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the body of a lock", "lock (output) { output[0] = book.Sheet.Row.Label; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("the expression of a using", "using (new System.IO.StringReader(book.Sheet.Row.Label)) { output[0] = book.Sheet.Row.Label; }")]
    [DataRow("the body of a using", "using (new System.IO.StringReader(\"x\")) { output[0] = book.Sheet.Row.Label; } output[1] = book.Sheet.Row.Label;")]
    [DataRow("the body of a while that contains all the occurrences", "while (ready) { output[0] = book.Sheet.Row.Label; output[1] = book.Sheet.Row.Label; }")]
    [DataRow("a labeled statement", "start: output[0] = book.Sheet.Row.Label; output[1] = book.Sheet.Row.Label;")]
    [DataRow("an element access", "output[0] = book.Sheet.Row.Cells[0].Value.ToString(); output[1] = book.Sheet.Row.Cells[0].Value.ToString();")]
    public void AnExtractionIsOfferedWhenTheExpressionIsEvaluatedFirstOnEveryPath(string title, string statements)
    {
        var source = "namespace TestApp\n{" + ScopeTypes + "\n    public class MyService\n    {\n        public void Show(Book book, bool ready, string[] output)\n        {\n            " + statements + "\n        }\n    }\n}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("t", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var target = tree.GetRoot().DescendantNodes().OfType<ExpressionSyntax>().First(e => e.ToString() is "book.Sheet.Row.Label" or "book.Sheet.Row.Cells");

        Assert.IsTrue(DSA019CodeFixProvider.CanExtract(compilation.GetSemanticModel(tree), target), title);
    }

    [TestMethod]
    [DataRow("an expression-bodied method has no list of statements", "public string Show(Book book) => book.Sheet.Row.Label + book.Sheet.Row.Label;", "book.Sheet.Row.Label")]
    [DataRow("a single occurrence is not worth a variable", "public string Show(Book book) { return book.Sheet.Row.Label; }", "book.Sheet.Row.Label")]
    [DataRow("a default value has no scope", "public abstract class Shape { public abstract void Draw(string s = Defaults.Name); }\n    public static class Defaults { public const string Name = \"x\"; }", "Defaults.Name")]
    public void NoExtractionIsOfferedWhereThereIsNoPlaceForTheDeclaration(string title, string members, string chain)
    {
        var source = "namespace TestApp\n{" + ScopeTypes + "\n    public class MyService\n    {\n        " + members + "\n    }\n}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("t", new[] { tree }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        var target = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().First(ma => ma.ToString() == chain);

        Assert.IsFalse(DSA019CodeFixProvider.CanExtract(compilation.GetSemanticModel(tree), target), title);
    }

    [TestMethod]
    public async Task ThePrefixIsExtractedWhenTheWholeChainIsFirstUsedBehindACheckOnThePrefix()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class Album { public List<string> Pictures; }
    public class Frame { public Album Album; }
    public class Slideshow
    {
        private Frame frame;
        private bool enabled;

        public int Measure(int position)
        {
            if (this.enabled)
            {
                if (this.frame.Album == null || position < 0 || position >= {|#0:this.frame.Album.Pictures|}.Count)
                    return 0;

                return {|#1:this.frame.Album.Pictures|}[position].Length + 4;
            }

            return 1;
        }
    }
}";

        // the chain cannot go above the null check, but its prefix is the first thing the check reads
        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class Album { public List<string> Pictures; }
    public class Frame { public Album Album; }
    public class Slideshow
    {
        private Frame frame;
        private bool enabled;

        public int Measure(int position)
        {
            if (this.enabled)
            {
                var album = this.frame.Album;
                if (album == null || position < 0 || position >= album.Pictures.Count)
                    return 0;

                return album.Pictures[position].Length + 4;
            }

            return 1;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId).WithLocation(0).WithArguments("this.frame.Album.Pictures", 2));
        test.ExpectedDiagnostics.Add(CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId).WithLocation(1).WithArguments("this.frame.Album.Pictures", 2));
        await test.RunAsync().ConfigureAwait(false);
    }
}
