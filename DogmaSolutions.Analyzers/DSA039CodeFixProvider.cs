using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA039CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA039CodeFixProvider : ReviewCommentOnlyCodeFixProvider
{
    protected override string DiagnosticId => DSA039Analyzer.DiagnosticId;
    protected override string ReviewCommentResourceKey => nameof(Resources.DSA039ReviewComment);
}
