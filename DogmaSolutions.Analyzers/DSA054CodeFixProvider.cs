#nullable disable
using System.Collections.Immutable;
using System.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA054CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA054CodeFixProvider : CodeFixProvider
{
    internal const string MemoizeEquivalenceKey = DSA054Analyzer.DiagnosticId + ".Memoize";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA054Analyzer.DiagnosticId];

    // each fix changes one operator token: the batch fixer merges them without conflicts
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        // '??=' changes what the code does (it starts to cache), and it is only valid when the field can be null: it is offered only when
        // the analyzer proved that nothing else has to change. The other cases are a decision of the developer.
        if (diagnostic.Properties.ContainsKey(DSA054Analyzer.CanMemoizeProperty) && node is AssignmentExpressionSyntax assignment &&
            assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && SupportsCoalesceAssignment(root))
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "[DSA054] Memoize the value with '??='",
                    createChangedDocument: ct => MemoizeAsync(context.Document, assignment, ct),
                    equivalenceKey: MemoizeEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA054Analyzer.DiagnosticId, nameof(Resources.DSA054ReviewComment));
    }

    private static bool SupportsCoalesceAssignment(SyntaxNode root) =>
        root.SyntaxTree.Options is CSharpParseOptions options && options.LanguageVersion >= LanguageVersion.CSharp8;

    private static async Task<Document> MemoizeAsync(Document document, AssignmentExpressionSyntax assignment, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var operatorToken = SyntaxFactory.Token(SyntaxKind.QuestionQuestionEqualsToken).WithTriviaFrom(assignment.OperatorToken);
        var replacement = SyntaxFactory.AssignmentExpression(SyntaxKind.CoalesceAssignmentExpression, assignment.Left, operatorToken, assignment.Right).WithTriviaFrom(assignment);
        return document.WithSyntaxRoot(root.ReplaceNode(assignment, replacement));
    }
}
