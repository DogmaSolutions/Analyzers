using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA040CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA040CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA040Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root?.FindNode(diagnostic.Location.SourceSpan);
        // Swapping System.Random for RandomNumberGenerator changes the calling shape (byte buffers vs Next*), so it is
        // not a safe mechanical rewrite; only the Review-Comment fix is offered.
        if (node is not null)
            ReviewCommentCodeFix.Register(context, diagnostic, node, DSA040Analyzer.DiagnosticId, nameof(Resources.DSA040ReviewComment));
    }
}
