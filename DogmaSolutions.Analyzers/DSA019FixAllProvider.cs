using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Fix All for DSA019. Every occurrence of a repeated chain is reported, and the fix of any of them rewrites all the
/// occurrences of that chain, so the batch fixer would compute (and then discard as duplicates) as many identical fixes as there
/// are occurrences: quadratic in the number of repetitions. This provider only removes, before the batch fixer runs, the
/// diagnostics whose fix is certain to be identical to the fix of an earlier one; the fixes that remain are merged by the very
/// same batch fixer, so the result is unchanged.
/// </summary>
internal sealed class DSA019FixAllProvider : FixAllProvider
{
    internal static readonly DSA019FixAllProvider Instance = new();

    public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() => WellKnownFixAllProviders.BatchFixer.GetSupportedFixAllScopes();

    public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
    {
        if (fixAllContext == null) throw new ArgumentNullException(nameof(fixAllContext));

        var reducedContext = CreateReducedContext(fixAllContext);
        return await WellKnownFixAllProviders.BatchFixer.GetFixAsync(reducedContext).ConfigureAwait(false);
    }

    private static FixAllContext CreateReducedContext(FixAllContext original)
    {
        var provider = new ReducedDiagnosticProvider(original);
        return original.Document != null
            ? new FixAllContext(original.Document, original.CodeFixProvider, original.Scope, original.CodeActionEquivalenceKey, original.DiagnosticIds, provider, original.CancellationToken)
            : new FixAllContext(original.Project, original.CodeFixProvider, original.Scope, original.CodeActionEquivalenceKey, original.DiagnosticIds, provider, original.CancellationToken);
    }

    private sealed class ReducedDiagnosticProvider : FixAllContext.DiagnosticProvider
    {
        private readonly FixAllContext _context;

        internal ReducedDiagnosticProvider(FixAllContext context)
        {
            _context = context;
        }

        public override async Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            var all = await _context.GetAllDiagnosticsAsync(project).ConfigureAwait(false);
            return await ReduceAsync(project, all, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var all = await _context.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            return await ReduceAsync(document.Project, all, cancellationToken).ConfigureAwait(false);
        }

        public override async Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken)
        {
            return await _context.GetProjectDiagnosticsAsync(project).ConfigureAwait(false);
        }

        private static async Task<IEnumerable<Diagnostic>> ReduceAsync(Project project, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var kept = new List<Diagnostic>(diagnostics.Length);
            foreach (var documentDiagnostics in diagnostics.GroupBy(d => d.Location.SourceTree))
            {
                var tree = documentDiagnostics.Key;
                var document = tree == null ? null : project.GetDocument(tree);
                if (document == null)
                {
                    kept.AddRange(documentDiagnostics);
                    continue;
                }

                var root = await tree.GetRootAsync(cancellationToken).ConfigureAwait(false);
                var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                var seen = new HashSet<FixIdentity>();
                foreach (var diagnostic in documentDiagnostics.OrderBy(d => d.Location.SourceSpan.Start))
                {
                    var identity = semanticModel == null ? null : GetFixIdentity(root, diagnostic, semanticModel);
                    if (identity == null || seen.Add(identity))
                        kept.Add(diagnostic);
                }
            }

            return kept;
        }
    }

    /// <summary>
    /// What the fix of a diagnostic depends on, when it can be told for sure: the scope, the kind and the text of the chain, and
    /// the symbols of all its identifiers. Two diagnostics with the same identity are repetitions of the same chain: the same
    /// occurrences are found, the same variable is declared at the same place and the same replacements are made.
    /// Null when this cannot be told with certainty (unresolved identifier, comments inside the chain): the diagnostic is kept.
    /// </summary>
    private static FixIdentity GetFixIdentity(SyntaxNode root, Diagnostic diagnostic, SemanticModel semanticModel)
    {
        var target = DSA019CodeFixProvider.FindTargetExpression(root, diagnostic.Location.SourceSpan);
        if (target == null)
            return null;

        var scope = SyntaxUtils.GetContainingScope(target);
        if (scope == null)
            return null;

        if (target.DescendantTrivia().Any(trivia => !trivia.IsKind(SyntaxKind.WhitespaceTrivia) && !trivia.IsKind(SyntaxKind.EndOfLineTrivia)))
            return null;

        var symbols = DSA019Analyzer.GetIdentifierSymbols(target, semanticModel);
        if (Array.IndexOf(symbols, null) >= 0)
            return null;

        return new FixIdentity(scope, target.Kind(), SyntaxUtils.NormalizeWhitespace(target.ToString()), symbols);
    }

    private sealed class FixIdentity : IEquatable<FixIdentity>
    {
        private readonly SyntaxNode _scope;
        private readonly SyntaxKind _kind;
        private readonly string _text;
        private readonly ISymbol[] _symbols;

        internal FixIdentity(SyntaxNode scope, SyntaxKind kind, string text, ISymbol[] symbols)
        {
            _scope = scope;
            _kind = kind;
            _text = text;
            _symbols = symbols;
        }

        public bool Equals(FixIdentity other)
        {
            return other != null &&
                   ReferenceEquals(_scope, other._scope) &&
                   _kind == other._kind &&
                   string.Equals(_text, other._text, StringComparison.Ordinal) &&
                   _symbols.Length == other._symbols.Length &&
                   _symbols.Zip(other._symbols, (a, b) => SymbolEqualityComparer.Default.Equals(a, b)).All(equal => equal);
        }

        public override bool Equals(object obj) => Equals(obj as FixIdentity);

        public override int GetHashCode()
        {
            var hash = System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(_scope);
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(_text));
            foreach (var symbol in _symbols)
                hash = unchecked((hash * 31) + SymbolEqualityComparer.Default.GetHashCode(symbol));

            return hash;
        }
    }
}
