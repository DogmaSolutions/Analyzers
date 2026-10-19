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

/// <summary>
/// Adds <c>[ExcludeFromCodeCoverage]</c> to the data carrier (and the <c>using</c> of its namespace when it is missing), with the justification
/// configured in <c>code_fix_justification</c> if any. The review comment is always offered.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA045CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA045CodeFixProvider : CodeFixProvider
{
    internal const string AddAttributeEquivalenceKey = DSA045Analyzer.DiagnosticId + "_AddExcludeFromCodeCoverage";
    internal const string RemoveAttributeEquivalenceKey = DSA045Analyzer.DiagnosticId + "_RemoveExcludeFromCodeCoverage";
    private const string AttributeNamespace = "System.Diagnostics.CodeAnalysis";
    private const string AttributeName = "ExcludeFromCodeCoverage";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA045Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<BaseTypeDeclarationSyntax>() is { } declaration)
            await RegisterFixesAsync(context, diagnostic, declaration).ConfigureAwait(false);
    }

    private static async Task RegisterFixesAsync(CodeFixContext context, Diagnostic diagnostic, BaseTypeDeclarationSyntax declaration)
    {
        // the type is excluded already: the opposite scenario, where the attribute is to be removed
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var settings = DSA045Settings.Cache.Get(context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(declaration.SyntaxTree));
        var attributes = model?.GetDeclaredSymbol(declaration, context.CancellationToken) is { } type ? DSA045Exclusions.FindExclusionAttributes(type, settings) : default;
        if (!attributes.IsDefaultOrEmpty)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Remove [" + attributes[0].AttributeClass!.Name.Replace("Attribute", string.Empty) + "]",
                    createChangedSolution: ct => RemoveAttributesAsync(context.Document.Project.Solution, attributes, ct),
                    equivalenceKey: RemoveAttributeEquivalenceKey),
                diagnostic);

            ReviewCommentCodeFix.Register(context, diagnostic, declaration, DSA045Analyzer.DiagnosticId, nameof(Resources.DSA045ReviewCommentExcludedWithLogic));
            return;
        }

        // the attribute is not available everywhere (it is not in every target framework): without it the fix would not compile
        var compilation = await context.Document.Project.GetCompilationAsync(context.CancellationToken).ConfigureAwait(false);
        if (compilation?.GetTypeByMetadataName(DSA045Settings.ExcludeFromCodeCoverageType) != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Add [ExcludeFromCodeCoverage]",
                    createChangedDocument: ct => AddAttributeAsync(context.Document, declaration, ct),
                    equivalenceKey: AddAttributeEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, declaration, DSA045Analyzer.DiagnosticId, nameof(Resources.DSA045ReviewComment));
    }

    /// <summary>Removes the attributes from every part of the type, in whatever file it is: the attribute list goes with them when they were its only attributes, and the documentation and the indentation stay on top of the type.</summary>
    private static async Task<Solution> RemoveAttributesAsync(Solution solution, ImmutableArray<AttributeData> attributes, CancellationToken cancellationToken)
    {
        // an attribute whose file is not part of the solution (read from metadata, or from a deleted document) cannot be edited
        var documents = attributes.Select(attribute => attribute.ApplicationSyntaxReference).Where(reference => reference != null).GroupBy(reference => reference!.SyntaxTree)
            .Select(group => (Document: solution.GetDocument(group.Key), References: group)).Where(item => item.Document != null).ToList();
        foreach (var (document, references) in documents)
        {
            var root = await document!.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var targets = references.Select(reference => (AttributeSyntax)reference!.GetSyntax(cancellationToken)).ToList();
            root = root!.TrackNodes(targets);
            foreach (var target in targets)
                root = RemoveAttribute(root, root.GetCurrentNode(target)!);

            solution = solution.WithDocumentSyntaxRoot(document.Id, root);
        }

        return solution;
    }

    private static SyntaxNode RemoveAttribute(SyntaxNode root, AttributeSyntax attribute)
    {
        var list = (AttributeListSyntax)attribute.Parent!;
        if (list.Attributes.Count > 1)
            return root.RemoveNode(attribute, SyntaxRemoveOptions.KeepNoTrivia)!;

        var owner = list.Parent!;
        var isFirst = ((BaseTypeDeclarationSyntax)owner).AttributeLists[0] == list;
        var newOwner = ((BaseTypeDeclarationSyntax)owner).WithAttributeLists(((BaseTypeDeclarationSyntax)owner).AttributeLists.Remove(list));

        // the documentation and the indentation that preceded the first attribute list go to what follows it
        if (isFirst)
        {
            var token = newOwner.GetFirstToken();
            newOwner = newOwner.ReplaceToken(token, token.WithLeadingTrivia(list.GetLeadingTrivia()));
        }

        return root.ReplaceNode(owner, newOwner);
    }

    private static async Task<Document> AddAttributeAsync(Document document, BaseTypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var settings = DSA045Settings.Cache.Get(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(root.SyntaxTree));
        var eol = SyntaxUtils.GetEndOfLineTrivia(root);

        // the documentation and the indentation that precede the type go before the new attribute; the type keeps its indentation only
        var leading = declaration.GetLeadingTrivia();
        var indentation = leading.Reverse().TakeWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().ToSyntaxTriviaList();
        var attributeList = CreateAttributeList(settings.CodeFixJustification).WithLeadingTrivia(leading).WithTrailingTrivia(eol);

        var newDeclaration = declaration.WithLeadingTrivia(indentation);
        newDeclaration = newDeclaration.WithAttributeLists(newDeclaration.AttributeLists.Insert(0, attributeList));

        return document.WithSyntaxRoot(AddUsing(root.ReplaceNode(declaration, newDeclaration), eol));
    }

    private static AttributeListSyntax CreateAttributeList(string justification)
    {
        var attribute = SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(AttributeName));
        if (justification.Length > 0)
        {
            var argument = SyntaxFactory.AttributeArgument(
                SyntaxFactory.NameEquals("Justification"),
                null,
                SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(justification)));
            attribute = attribute.WithArgumentList(SyntaxFactory.AttributeArgumentList(SyntaxFactory.SingletonSeparatedList(argument)));
        }

        return SyntaxFactory.AttributeList(SyntaxFactory.SingletonSeparatedList(attribute));
    }

    private static CompilationUnitSyntax AddUsing(CompilationUnitSyntax root, SyntaxTrivia eol)
    {
        if (root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(directive => directive.Name?.ToString() == AttributeNamespace))
            return root;

        var directive = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(AttributeNamespace));
        if (root.Usings.Count > 0)
            return root.AddUsings(directive.WithTrailingTrivia(eol));

        // the first using of the file: a blank line separates it from the code, and a header (a license, for example) stays on top
        if (root.Members.FirstOrDefault() is BaseNamespaceDeclarationSyntax namespaceDeclaration)
        {
            var header = namespaceDeclaration.GetLeadingTrivia();
            root = root.ReplaceNode(namespaceDeclaration, namespaceDeclaration.WithLeadingTrivia(SyntaxFactory.TriviaList()));
            directive = directive.WithLeadingTrivia(header);
        }

        return root.WithUsings(SyntaxFactory.SingletonList(directive.WithTrailingTrivia(eol, eol)));
    }
}
