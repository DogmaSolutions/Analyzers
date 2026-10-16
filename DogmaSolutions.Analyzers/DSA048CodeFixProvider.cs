using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA048CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA048CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA048Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        // Replacing a polling loop with a signal changes the design of the class (who signals, who waits, for how long): that is a decision of the
        // developer, so only the Review-Comment fix is offered.
        if (root != null) ReviewCommentCodeFix.Register(context, diagnostic, root.FindNode(diagnostic.Location.SourceSpan), DSA048Analyzer.DiagnosticId, nameof(Resources.DSA048ReviewComment));
    }
}
