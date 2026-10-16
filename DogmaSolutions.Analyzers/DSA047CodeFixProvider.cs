using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA047CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA047CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA047Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        // Making the field readonly or private changes the design of the class (who swaps it, who uses it from outside): that is a decision of the
        // developer, so only the Review-Comment fix is offered.
        if (root != null) ReviewCommentCodeFix.Register(context, diagnostic, root.FindNode(diagnostic.Location.SourceSpan), DSA047Analyzer.DiagnosticId, nameof(Resources.DSA047ReviewComment));
    }
}
