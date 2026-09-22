using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA037CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA037CodeFixProvider : ReviewCommentOnlyCodeFixProvider
{
    protected override string DiagnosticId => DSA037Analyzer.DiagnosticId;
    protected override string ReviewCommentResourceKey => nameof(Resources.DSA037ReviewComment);
}
