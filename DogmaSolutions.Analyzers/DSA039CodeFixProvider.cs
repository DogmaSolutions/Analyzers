using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA039CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA039CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA039Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // Rewriting the reduction to RandomNumberGenerator.GetInt32 depends on the surrounding range logic, so only
        // the Review-Comment fix is offered.
        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA039Analyzer.DiagnosticId, nameof(Resources.DSA039ReviewComment));
    }
}
