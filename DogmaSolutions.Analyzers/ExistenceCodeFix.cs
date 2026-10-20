#nullable disable
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What the code fixes of DSA052 and DSA053 do in the same way: replace the call that produces the value with <c>Any</c> / <c>AnyAsync</c>.</summary>
    internal sealed class ExistenceCodeFix
    {
        private readonly ExistenceKind _kind;
        private readonly string _diagnosticId;
        private readonly string _localSuffix;
        private readonly string _reviewCommentResource;

        internal ExistenceCodeFix(ExistenceKind kind, string diagnosticId, string localSuffix, string reviewCommentResource)
        {
            _kind = kind;
            _diagnosticId = diagnosticId;
            _localSuffix = localSuffix;
            _reviewCommentResource = reviewCommentResource;
            EquivalenceKey = diagnosticId + "_UseAny";
            FixAllProvider = new FixAll(this);
        }

        internal string EquivalenceKey { get; }

        internal FixAllProvider FixAllProvider { get; }

        internal async Task RegisterAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics[0];
            var invocation = FindInvocation(root, diagnostic.Location.SourceSpan);
            var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
            var use = invocation == null ? null : Analyze(invocation, model, context.CancellationToken);
            if (use != null)
            {
                var title = "Use '" + use.Call.Alternative.Name + "' instead of '" + use.Call.Method.Name + "'" + (use.Call.Method.Name.StartsWith("Single", System.StringComparison.Ordinal) ? " (drops the uniqueness check)" : string.Empty);
                context.RegisterCodeFix(
                    CodeAction.Create(title, ct => FixAsync(context.Document, new[] { diagnostic.Location.SourceSpan }, ct), EquivalenceKey),
                    diagnostic);
            }

            if (invocation != null)
                ReviewCommentCodeFix.Register(context, diagnostic, invocation, _diagnosticId, _reviewCommentResource);
        }

        private static InvocationExpressionSyntax FindInvocation(SyntaxNode root, TextSpan span) =>
            root?.FindNode(span).FirstAncestorOrSelf<InvocationExpressionSyntax>(invocation => invocation.Span == span);

        private ExistenceUse Analyze(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            var call = model == null ? null : ExistenceAnalysis.ResolveCall(invocation, model, cancellationToken);
            return call == null ? null : ExistenceAnalysis.Analyze(invocation, call, _kind, model, cancellationToken);
        }

        /// <summary>All the existence tests at the spans are rewritten in one pass: separate fixes would have to be merged text against text.</summary>
        private async Task<Document> FixAsync(Document document, IEnumerable<TextSpan> spans, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var uses = spans.Select(span => FindInvocation(root, span)).Where(invocation => invocation != null).Select(invocation => Analyze(invocation!, model, cancellationToken)).Where(use => use != null).ToList();
            return uses.Count == 0 ? document : document.WithSyntaxRoot(ExistenceRewriter.Rewrite(root!, uses!, _localSuffix));
        }

        /// <summary>Fix All: the tests of a document are rewritten together (the review comments are left to the batch fixer).</summary>
        private sealed class FixAll : FixAllProvider
        {
            private readonly ExistenceCodeFix _owner;
            private readonly DocumentFixAll _documents;

            internal FixAll(ExistenceCodeFix owner)
            {
                _owner = owner;
                _documents = new DocumentFixAll(owner);
            }

            public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() => WellKnownFixAllProviders.BatchFixer.GetSupportedFixAllScopes();

            public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext) =>
                fixAllContext.CodeActionEquivalenceKey == _owner.EquivalenceKey ? _documents.GetFixAsync(fixAllContext) : WellKnownFixAllProviders.BatchFixer.GetFixAsync(fixAllContext);
        }

        private sealed class DocumentFixAll : DocumentBasedFixAllProvider
        {
            private readonly ExistenceCodeFix _owner;

            internal DocumentFixAll(ExistenceCodeFix owner) => _owner = owner;

            protected override Task<Document> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics) =>
                _owner.FixAsync(document, diagnostics.Select(diagnostic => diagnostic.Location.SourceSpan), fixAllContext.CancellationToken);
        }
    }
}
