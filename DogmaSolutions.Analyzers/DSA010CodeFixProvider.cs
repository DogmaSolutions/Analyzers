using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA010CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA010CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA010Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        // The right remedy (lock the whole operation, volatile, Interlocked, an immutable snapshot) depends on what the code does with the
        // value: removing the lock would be a guess, so only the Review-Comment fix is offered.
        if (root != null) ReviewCommentCodeFix.Register(context, diagnostic, root.FindNode(diagnostic.Location.SourceSpan), DSA010Analyzer.DiagnosticId, nameof(Resources.DSA010ReviewComment));
    }
}
