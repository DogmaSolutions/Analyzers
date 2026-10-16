using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA049CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA049CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA049Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        // The right remedy (take the same lock, volatile, Interlocked, an immutable snapshot) depends on what the code does with the value: that is a decision of the
        // developer, so only the Review-Comment fix is offered.
        if (root != null) ReviewCommentCodeFix.Register(context, diagnostic, root.FindNode(diagnostic.Location.SourceSpan), DSA049Analyzer.DiagnosticId, nameof(Resources.DSA049ReviewComment));
    }
}
