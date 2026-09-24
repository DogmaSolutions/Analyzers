using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA040CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA040CodeFixProvider : ReviewCommentOnlyCodeFixProvider
{
    protected override string DiagnosticId => DSA040Analyzer.DiagnosticId;
    protected override string ReviewCommentResourceKey => nameof(Resources.DSA040ReviewComment);
}
