using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA043CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA043CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA043Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root?.FindNode(diagnostic.Location.SourceSpan);

        // Adding a logger changes the constructor signature, which breaks every caller (and the DI registrations that build the class by
        // hand): it is not a safe mechanical fix, so the class is only annotated for the code review.
        if (node != null)
            ReviewCommentCodeFix.Register(context, diagnostic, node, DSA043Analyzer.DiagnosticId, nameof(Resources.DSA043ReviewComment));
    }
}
