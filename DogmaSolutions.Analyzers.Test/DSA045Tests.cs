using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA045 end to end: a pure data carrier that is not excluded from the code coverage.</summary>
[TestClass]
public partial class DSA045Tests
{
    internal const string Library = "TestLibrary";

    /// <summary>Types compiled into an assembly of their own, so that they are read from metadata, where the bodies are not visible.</summary>
    internal const string LibrarySource = @"
namespace TestLib
{
    public class PureBase { public int Id { get; set; } public string Name { get; set; } }
    public class ImpureBase { public int Id { get; set; } public int Compute() => 1; }
    public class ParameterBase { public ParameterBase(int id) { } public int Id { get; set; } }
    public class StaticBase { public static int Counter; public int Id { get; set; } }
    public class FieldBase { public int Id; }
    public class EmptyBase { }
    public record RecordBase(int Id);
    public class BackedBase { private int _x; public int X { get { return _x; } set { _x = value; } } }
    public class IndexerBase { public int this[int i] { get => i; } }
}";

    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA045Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    /// <summary>The expected diagnostic: the kind of type, its name, the description of its members and the note about the base types.</summary>
    internal static string E(string kind, string name, string members, bool baseTypes = false) => kind + "|" + name + "|" + members + "|" + (baseTypes ? ", base types included" : string.Empty);

    /// <summary>A source file with the given types. <c>[[Name]]</c> marks the type the diagnostic is expected on (the markup is dropped when nothing is expected).</summary>
    internal static string BuildSource(string pre, string declaration, bool expectDiagnostic, string usings = "using System;\nusing System.Collections.Generic;\nusing System.Diagnostics.CodeAnalysis;\n")
    {
        var body = System.Text.RegularExpressions.Regex.Replace(declaration, @"\[\[(.*?)\]\]", match => expectDiagnostic ? "{|#0:" + match.Groups[1].Value + "|}" : match.Groups[1].Value);
        return usings + "namespace TestApp\n{\n" + pre + "\n" + body + "\n}";
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic.DSA045." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    private static MetadataReference _libraryReference;

    private static async Task<MetadataReference> GetLibraryReferenceAsync()
    {
        if (_libraryReference != null)
            return _libraryReference;

        var references = await ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, default).ConfigureAwait(false);
        var compilation = CSharpCompilation.Create(Library, new[] { CSharpSyntaxTree.ParseText(LibrarySource) }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var stream = new System.IO.MemoryStream();
        var result = compilation.Emit(stream);
        Assert.IsTrue(result.Success, string.Join("\n", result.Diagnostics));

        // tests run in parallel: computing it twice is harmless
        _libraryReference = MetadataReference.CreateFromImage(stream.ToArray());
        return _libraryReference;
    }

    internal static async Task<CSharpAnalyzerVerifier<DSA045Analyzer>.Test> CreateTestAsync(string source, string settings, string expected)
    {
        var test = new CSharpAnalyzerVerifier<DSA045Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AdditionalReferences.Add(await GetLibraryReferenceAsync().ConfigureAwait(false));
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));

        if (!string.IsNullOrEmpty(expected))
        {
            var parts = expected.Split('|');
            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(nameof(Resources.DSA045AnalyzerMessageFormat), Resources.Culture), parts[0], parts[1], parts[2], parts[3]);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA045Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(0).WithMessage(message));
        }

        return test;
    }

    internal static async Task RunAsync(string pre, string declaration, string settings, string expected, string usings = null)
    {
        var source = usings == null ? BuildSource(pre, declaration, !string.IsNullOrEmpty(expected)) : BuildSource(pre, declaration, !string.IsNullOrEmpty(expected), usings);
        var test = await CreateTestAsync(source, settings, expected).ConfigureAwait(false);
        await test.RunAsync().ConfigureAwait(false);
    }
}
