using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA038CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA038CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA038Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root?.FindNode(diagnostic.Location.SourceSpan);
        // Whether a reproducible sequence is intended is a human judgement, so only the Review-Comment fix is offered.
        if (node is not null)
            ReviewCommentCodeFix.Register(context, diagnostic, node, DSA038Analyzer.DiagnosticId, nameof(Resources.DSA038ReviewComment));
    }
}
