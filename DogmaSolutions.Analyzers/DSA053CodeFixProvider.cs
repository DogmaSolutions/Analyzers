#nullable disable
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Replaces the call whose result is only tested for existence with the call that answers the question. A local that is only compared becomes the
/// boolean that the call returns. The review comment is always offered.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA053CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA053CodeFixProvider : CodeFixProvider
{
    private static readonly ExistenceCodeFix _fix = new(ExistenceKind.Count, DSA053Analyzer.DiagnosticId, "hasItems", nameof(Resources.DSA053ReviewComment));

    internal static string UseAnyEquivalenceKey => _fix.EquivalenceKey;

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA053Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => _fix.FixAllProvider;

    public override Task RegisterCodeFixesAsync(CodeFixContext context) => _fix.RegisterAsync(context);
}
