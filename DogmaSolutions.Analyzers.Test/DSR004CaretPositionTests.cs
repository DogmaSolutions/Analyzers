using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DataRow

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// "Split into partial files ..." is a type-level action: it must be offered on the class declaration header and on the namespace
/// declaration header, and nowhere else in the file (not in the usings, not in the body, not for a selection that spans the file).
/// </summary>
[TestClass]
public class DSR004CaretPositionTests
{
    private const string BlockNamespace =
        "using System;\n\nnamespace TestApp\n{\n    /// <summary>doc</summary>\n    [Serializable]\n    public class MyWidget : IDisposable\n    {\n        private int _state;\n        public void Render() { }\n        public void Dispose() { }\n    }\n}\n";

    private const string FileScopedNamespace =
        "using System;\n\nnamespace TestApp;\n\n/// <summary>doc</summary>\npublic class MyWidget\n{\n    private int _state;\n    public void Render() { }\n}\n";

    private static (Document Document, string Source) CreateDocument(string source)
    {
        var workspace = new AdhocWorkspace();
        return (workspace.AddProject("p", LanguageNames.CSharp).AddDocument("a.cs", source), source);
    }

    private static async Task<bool> RefactoringIsOfferedAsync(Document document, TextSpan span)
    {
        var count = 0;
        await new DSR004RefactoringProvider()
            .ComputeRefactoringsAsync(new CodeRefactoringContext(document, span, _ => count++, CancellationToken.None))
            .ConfigureAwait(false);
        return count == 2;
    }

    private static (TextSpan Type, TextSpan Namespace) GetHeaders(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        var type = root.DescendantNodes().OfType<TypeDeclarationSyntax>().Single();
        var ns = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().Single();
        return (AnalyzersUtils.GetTypeDeclarationHeaderSpan(type), AnalyzersUtils.GetNamespaceHeaderSpan(ns));
    }

    [TestMethod]
    [DataRow(BlockNamespace)]
    [DataRow(FileScopedNamespace)]
    public async Task Offered_for_every_caret_position_of_the_class_or_namespace_header_and_nowhere_else(string source)
    {
        var (document, _) = CreateDocument(source);
        var (typeHeader, namespaceHeader) = GetHeaders(source);

        for (var position = 0; position <= source.Length; position++)
        {
            var expected = typeHeader.Contains(position) || typeHeader.End == position || namespaceHeader.Contains(position) || namespaceHeader.End == position;
            var offered = await RefactoringIsOfferedAsync(document, new TextSpan(position, 0)).ConfigureAwait(false);

            Assert.AreEqual(expected, offered, "caret at " + position + " [" + source.Substring(position, System.Math.Min(12, source.Length - position)).Replace("\n", "\\n") + "]");
        }
    }

    [TestMethod]
    [DataRow(BlockNamespace)]
    [DataRow(FileScopedNamespace)]
    public async Task Not_offered_for_a_selection_that_goes_beyond_the_header(string source)
    {
        var (document, _) = CreateDocument(source);
        var (typeHeader, _) = GetHeaders(source);

        Assert.IsFalse(await RefactoringIsOfferedAsync(document, new TextSpan(0, source.Length)).ConfigureAwait(false), "the whole file is selected");
        Assert.IsFalse(await RefactoringIsOfferedAsync(document, TextSpan.FromBounds(typeHeader.Start, source.Length)).ConfigureAwait(false), "from the header to the end of the file");
        Assert.IsTrue(await RefactoringIsOfferedAsync(document, typeHeader).ConfigureAwait(false), "the header itself");
    }
}
