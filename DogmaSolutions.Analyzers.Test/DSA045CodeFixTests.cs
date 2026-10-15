using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA045: <c>[ExcludeFromCodeCoverage]</c> is added to the data carrier.</summary>
[TestClass]
public class DSA045CodeFixTests
{
    private static CSharpCodeFixVerifier<DSA045Analyzer, DSA045CodeFixProvider>.Test CreateTest(string source, string fixedSource, string settings = "", int diagnostics = 1)
    {
        var test = new CSharpCodeFixVerifier<DSA045Analyzer, DSA045CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA045CodeFixProvider.AddAttributeEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", DSA045Tests.ToEditorConfig(settings)));
        for (var i = 0; i < diagnostics; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA045Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        return test;
    }

    // title, source, fixed source, settings
    private static IEnumerable<object[]> GetFixes =>
    [
        [
            "The attribute and the using are added",
            "namespace TestApp\n{\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "The using that is there is not added again",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "The using inside the namespace is not added again",
            "namespace TestApp\n{\n    using System.Diagnostics.CodeAnalysis;\n\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "namespace TestApp\n{\n    using System.Diagnostics.CodeAnalysis;\n\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "The other usings are kept",
            "using System;\n\nnamespace TestApp\n{\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System;\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "An existing attribute is kept",
            "using System;\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [Serializable]\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System;\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    [Serializable]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "The documentation stays on top",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    /// <summary>A data carrier.</summary>\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    /// <summary>A data carrier.</summary>\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "The documentation and the attributes stay on top",
            "using System;\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    /// <summary>A data carrier.</summary>\n    [Serializable]\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System;\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    /// <summary>A data carrier.</summary>\n    [ExcludeFromCodeCoverage]\n    [Serializable]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "A comment that precedes the type stays on top",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    // the data of an order\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    // the data of an order\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "File-scoped namespace",
            "namespace TestApp;\n\npublic class {|#0:Dto|}\n{\n    public int A { get; set; }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp;\n\n[ExcludeFromCodeCoverage]\npublic class Dto\n{\n    public int A { get; set; }\n}",
            "",
        ],
        [
            "A header stays on top of the new using",
            "// Copyright (c) Someone\n// All rights reserved\nnamespace TestApp\n{\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "// Copyright (c) Someone\n// All rights reserved\nusing System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "",
        ],
        [
            "No namespace",
            "public class {|#0:Dto|}\n{\n    public int A { get; set; }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\n[ExcludeFromCodeCoverage]\npublic class Dto\n{\n    public int A { get; set; }\n}",
            "",
        ],
        [
            "Nested type",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    public class Outer\n    {\n        public void Run() { }\n\n        public class {|#0:Dto|}\n        {\n            public int A { get; set; }\n        }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    public class Outer\n    {\n        public void Run() { }\n\n        [ExcludeFromCodeCoverage]\n        public class Dto\n        {\n            public int A { get; set; }\n        }\n    }\n}",
            "",
        ],
        [
            "Record",
            "namespace TestApp\n{\n    public record {|#0:Dto|}(int A);\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public record Dto(int A);\n}",
            "included_type_kinds=record",
        ],
        [
            "Struct",
            "namespace TestApp\n{\n    public struct {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public struct Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "included_type_kinds=struct",
        ],
        [
            "A justification from the options",
            "namespace TestApp\n{\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage(Justification = \"Data carrier, no logic\")]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "code_fix_justification=Data carrier, no logic",
        ],
        [
            "A blank justification adds none",
            "namespace TestApp\n{\n    public class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public class Dto\n    {\n        public int A { get; set; }\n    }\n}",
            "code_fix_justification=   ",
        ],
        [
            "Partial type: the first part gets the attribute",
            "namespace TestApp\n{\n    public partial class {|#0:Dto|}\n    {\n        public int A { get; set; }\n    }\n\n    public partial class Dto\n    {\n        public int B { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public partial class Dto\n    {\n        public int A { get; set; }\n    }\n\n    public partial class Dto\n    {\n        public int B { get; set; }\n    }\n}",
            "",
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(DSA045Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA045Tests))]
    public async Task Adds_the_attribute(string title, string source, string fixedSource, string settings)
    {
        await CreateTest(source, fixedSource, settings).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Fixes_every_data_carrier_of_the_document_at_once()
    {
        var test = CreateTest(
            "namespace TestApp\n{\n    public class {|#0:One|}\n    {\n        public int A { get; set; }\n    }\n\n    public class {|#1:Two|}\n    {\n        public int B { get; set; }\n    }\n}",
            "using System.Diagnostics.CodeAnalysis;\n\nnamespace TestApp\n{\n    [ExcludeFromCodeCoverage]\n    public class One\n    {\n        public int A { get; set; }\n    }\n\n    [ExcludeFromCodeCoverage]\n    public class Two\n    {\n        public int B { get; set; }\n    }\n}",
            diagnostics: 2);

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Uses_the_line_endings_of_the_document()
    {
        var source = "namespace TestApp\r\n{\r\n    public class {|#0:Dto|}\r\n    {\r\n        public int A { get; set; }\r\n    }\r\n}";
        var fixedSource = "using System.Diagnostics.CodeAnalysis;\r\n\r\nnamespace TestApp\r\n{\r\n    [ExcludeFromCodeCoverage]\r\n    public class Dto\r\n    {\r\n        public int A { get; set; }\r\n    }\r\n}";

        await CreateTest(source, fixedSource).RunAsync().ConfigureAwait(false);
    }

    // ---- what is offered ------------------------------------------------------------------------------------------------------

    [TestMethod]
    public async Task Offers_the_attribute_and_the_review_comment()
    {
        var titles = await GetTitlesAsync("public class Dto { public int A { get; set; } }", CreateReferences()).ConfigureAwait(false);

        CollectionAssert.AreEqual(new[] { "Add [ExcludeFromCodeCoverage]", "[DSA045] Add comment for code review" }, titles);
    }

    [TestMethod]
    public async Task Offers_only_the_review_comment_when_the_attribute_does_not_exist()
    {
        // a core library of its own, without ExcludeFromCodeCoverageAttribute
        const string corlib = "namespace System { public class Object { } public class ValueType { } public struct Void { } public struct Int32 { } public struct Boolean { } public class String { } public class Enum { } public class Attribute { } public class Type { } public class Delegate { } public struct IntPtr { } public enum AttributeTargets { All = 32767 } public sealed class AttributeUsageAttribute : Attribute { public AttributeUsageAttribute(AttributeTargets targets) { } public bool AllowMultiple { get; set; } public bool Inherited { get; set; } } }";
        var compilation = CSharpCompilation.Create("corlib", new[] { CSharpSyntaxTree.ParseText(corlib) }, null, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new System.IO.MemoryStream();
        var emitted = compilation.Emit(stream);
        Assert.IsTrue(emitted.Success, string.Join("\n", emitted.Diagnostics));

        var titles = await GetTitlesAsync("public class Dto { public System.Int32 A { get; set; } }", new[] { MetadataReference.CreateFromImage(stream.ToArray()) }).ConfigureAwait(false);

        CollectionAssert.AreEqual(new[] { "[DSA045] Add comment for code review" }, titles);
    }

    private static MetadataReference[] CreateReferences() => DSA043TestSupport.CreateCompilation().References.ToArray();

    private static async Task<string[]> GetTitlesAsync(string source, IEnumerable<MetadataReference> references)
    {
        using var workspace = new AdhocWorkspace();
        var project = workspace.AddProject("p", LanguageNames.CSharp).WithMetadataReferences(references);
        var document = project.AddDocument("a.cs", source);

        var compilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var diagnostics = await compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new DSA045Analyzer())).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        Assert.AreEqual(1, diagnostics.Length, string.Join("\n", compilation.GetDiagnostics()));

        var titles = new List<string>();
        await new DSA045CodeFixProvider()
            .RegisterCodeFixesAsync(new CodeFixContext(document, diagnostics[0], (action, _) => titles.Add(action.Title), CancellationToken.None))
            .ConfigureAwait(false);
        return titles.ToArray();
    }
}
