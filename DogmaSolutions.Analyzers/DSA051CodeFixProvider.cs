using System.Collections.Generic;
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
/// Generates the ToString override from the members the heuristic picks (see <see cref="DSA051Planner"/>). Nothing is generated for a type with
/// nothing to show. The review comment is always offered.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA051CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA051CodeFixProvider : CodeFixProvider
{
    internal const string GenerateEquivalenceKey = DSA051Analyzer.DiagnosticId + "_GenerateToString";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA051Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => FixAll.Instance;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>() is { } declaration)
            await RegisterFixesAsync(context, diagnostic, declaration).ConfigureAwait(false);
    }

    private static async Task RegisterFixesAsync(CodeFixContext context, Diagnostic diagnostic, TypeDeclarationSyntax declaration)
    {
        var plan = await CreatePlanAsync(context.Document, declaration, context.CancellationToken).ConfigureAwait(false);
        if (plan.Strategy != DSA051Strategy.None)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Describe(plan),
                    createChangedDocument: ct => GenerateAsync(context.Document, declaration, ct),
                    equivalenceKey: GenerateEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, declaration, DSA051Analyzer.DiagnosticId, nameof(Resources.DSA051ReviewComment));
    }

    private static string Describe(DSA051ToStringPlan plan) =>
        plan.Strategy switch
        {
            DSA051Strategy.WellKnown => $"Generate ToString() returning '{plan.Members[0].Name}'",
            DSA051Strategy.PublicProperties => "Generate ToString() listing the public properties",
            DSA051Strategy.ProtectedProperties => "Generate ToString() listing the protected properties",
            DSA051Strategy.Fields => "Generate ToString() listing the fields",
            _ => "Generate ToString() listing the properties",
        };

    private static async Task<DSA051ToStringPlan> CreatePlanAsync(Document document, TypeDeclarationSyntax declaration, CancellationToken cancellationToken)
    {
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        return CreatePlan(model, declaration, GetSettings(document, declaration), cancellationToken);
    }

    private static DSA051ToStringPlan CreatePlan(SemanticModel? model, TypeDeclarationSyntax declaration, DSA051Settings settings, CancellationToken cancellationToken) =>
        model?.GetDeclaredSymbol(declaration, cancellationToken) is { } type
            ? DSA051Planner.Create(type, settings)
            : new DSA051ToStringPlan(DSA051Strategy.None, ImmutableArray<ISymbol>.Empty, 0);

    private static DSA051Settings GetSettings(Document document, SyntaxNode node) =>
        DSA051Settings.Cache.Get(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GetOptions(node.SyntaxTree));

    private static Task<Document> GenerateAsync(Document document, TypeDeclarationSyntax declaration, CancellationToken cancellationToken) =>
        GenerateAsync(document, new[] { declaration }, cancellationToken);

    /// <summary>Adds the ToString to all the declarations in one pass: the changes are not merged from separate fixes, which would have to be diffed.</summary>
    private static async Task<Document> GenerateAsync(Document document, IReadOnlyCollection<TypeDeclarationSyntax> declarations, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        var plans = declarations.ToDictionary(declaration => declaration, declaration => CreatePlan(model, declaration, GetSettings(document, declaration), cancellationToken));

        var newRoot = root!.ReplaceNodes(declarations, (original, rewritten) => rewritten.AddMembers(CreateMember(original, plans[original], GetSettings(document, original))));
        return document.WithSyntaxRoot(newRoot);
    }

    private static MemberDeclarationSyntax CreateMember(TypeDeclarationSyntax declaration, DSA051ToStringPlan plan, DSA051Settings settings)
    {
        var eol = SyntaxUtils.GetEndOfLineTrivia(declaration).ToString();
        var indent = GetMemberIndentation(declaration);
        var unit = indent.Contains("\t") ? "\t" : "    ";
        var text = settings.UseExpressionBody
            ? $"{indent}public override string ToString() => {plan.BuildInterpolatedString(settings)};{eol}"
            : $"{indent}public override string ToString(){eol}{indent}{{{eol}{indent}{unit}return {plan.BuildInterpolatedString(settings)};{eol}{indent}}}{eol}";

        // a blank line separates the new member from the last one
        var member = SyntaxFactory.ParseMemberDeclaration(text)!;
        return member.WithLeadingTrivia(member.GetLeadingTrivia().Insert(0, SyntaxFactory.EndOfLine(eol)));
    }

    /// <summary>Fix All: the declarations of a document are fixed together (the review comments are left to the batch fixer).</summary>
    private sealed class FixAll : FixAllProvider
    {
        internal static readonly FixAll Instance = new();

        private readonly DocumentBasedFixAll _generate = new();

        public override IEnumerable<FixAllScope> GetSupportedFixAllScopes() => WellKnownFixAllProviders.BatchFixer.GetSupportedFixAllScopes();

        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext) =>
            fixAllContext.CodeActionEquivalenceKey == GenerateEquivalenceKey ? _generate.GetFixAsync(fixAllContext) : WellKnownFixAllProviders.BatchFixer.GetFixAsync(fixAllContext);
    }

    private sealed class DocumentBasedFixAll : DocumentBasedFixAllProvider
    {
        protected override async Task<Document?> FixAllAsync(FixAllContext fixAllContext, Document document, ImmutableArray<Diagnostic> diagnostics)
        {
            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var model = await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var declarations = diagnostics
                .Select(diagnostic => root?.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<TypeDeclarationSyntax>())
                .Where(declaration => declaration != null && CreatePlan(model, declaration, GetSettings(document, declaration), fixAllContext.CancellationToken).Strategy != DSA051Strategy.None)
                .Distinct()
                .ToList();

            return declarations.Count == 0 ? document : await GenerateAsync(document, declarations!, fixAllContext.CancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>The indentation of the first member of the type (its own indentation plus one level when it has none).</summary>
    private static string GetMemberIndentation(TypeDeclarationSyntax declaration)
    {
        var anchor = declaration.Members.Count > 0 ? (SyntaxNode)declaration.Members[0] : declaration;
        var whitespace = new string(anchor.GetLeadingTrivia().Reverse().TakeWhile(trivia => trivia.IsKind(SyntaxKind.WhitespaceTrivia)).Reverse().SelectMany(trivia => trivia.ToString()).ToArray());
        return declaration.Members.Count > 0 ? whitespace : whitespace + "    ";
    }
}
