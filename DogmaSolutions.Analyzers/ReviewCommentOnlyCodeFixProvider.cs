using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Base for code-fix providers whose only action is the shared Review-Comment fix — used when no structural
/// rewrite is a safe, mechanical fix for the rule. A concrete provider only supplies the rule id and the
/// review-comment resource key; the (identical) registration glue lives here once, so the per-analyzer providers
/// carry no duplicated logic. Every analyzer ships at least this fix (see the Analyzer-authoring directive in the
/// project CLAUDE.md).
/// </summary>
public abstract class ReviewCommentOnlyCodeFixProvider : CodeFixProvider
{
    /// <summary>The diagnostic id this provider fixes (e.g. <c>DSA037Analyzer.DiagnosticId</c>).</summary>
    protected abstract string DiagnosticId { get; }

    /// <summary>The <c>Resources</c> key of the review-comment text (e.g. <c>nameof(Resources.DSA037ReviewComment)</c>).</summary>
    protected abstract string ReviewCommentResourceKey { get; }

    public sealed override ImmutableArray<string> FixableDiagnosticIds => [DiagnosticId];

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root?.FindNode(diagnostic.Location.SourceSpan);
        if (node is not null)
            ReviewCommentCodeFix.Register(context, diagnostic, node, DiagnosticId, ReviewCommentResourceKey);
    }
}
