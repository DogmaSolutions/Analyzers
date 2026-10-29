#nullable disable
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA056CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA056CodeFixProvider : CodeFixProvider
{
    internal const string ExcludeEquivalenceKey = DSA056Analyzer.DiagnosticId + ".Exclude";
    internal const string AttributeName = "global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA056Analyzer.DiagnosticId];

    // each fix changes one class: the batch fixer merges them without conflicts
    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        if (node.FirstAncestorOrSelf<ClassDeclarationSyntax>() is { } declaration)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "[DSA056] Exclude the test fixture from the code coverage",
                    createChangedDocument: ct => ExcludeAsync(context.Document, declaration, ct),
                    equivalenceKey: ExcludeEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA056Analyzer.DiagnosticId, nameof(Resources.DSA056ReviewComment));
    }

    private static async Task<Document> ExcludeAsync(Document document, ClassDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var leading = declaration.GetLeadingTrivia();
        var indentation = leading.Where(static trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().Take(1);
        var endOfLine = root.DescendantTrivia().FirstOrDefault(static trivia => trivia.IsKind(SyntaxKind.EndOfLineTrivia));
        var lineBreak = endOfLine.IsKind(SyntaxKind.EndOfLineTrivia) ? endOfLine : SyntaxFactory.EndOfLine("\n");

        // the attribute takes the place of the declaration (its comments stay above it), and the declaration goes to the next line with the same indentation
        var attribute = SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Attribute(SyntaxFactory.ParseName(AttributeName))))
            .WithLeadingTrivia(leading)
            .WithTrailingTrivia(lineBreak);

        var stripped = declaration.WithLeadingTrivia(indentation);
        return document.WithSyntaxRoot(root.ReplaceNode(declaration, stripped.WithAttributeLists(stripped.AttributeLists.Insert(0, attribute))));
    }
}
