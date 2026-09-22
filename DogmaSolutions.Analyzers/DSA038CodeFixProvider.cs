using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA038CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA038CodeFixProvider : ReviewCommentOnlyCodeFixProvider
{
    protected override string DiagnosticId => DSA038Analyzer.DiagnosticId;
    protected override string ReviewCommentResourceKey => nameof(Resources.DSA038ReviewComment);
}
