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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA005CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA005CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA005Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var node = root.FindNode(diagnosticSpan);
        var method = node as MethodDeclarationSyntax
            ?? node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

        if (method?.Body == null)
            return;

        var expressions = FindMatchingExpressions(method.Body);
        var hasExtractableGroup = expressions
            .GroupBy(BuildKey)
            .Any(g => g.Count() >= 2);

        if (!hasExtractableGroup)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Extract to single point-in-time variable",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, method, ct),
                equivalenceKey: DSA005Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> ExtractToVariableAsync(
        Document document,
        MethodDeclarationSyntax method,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || method.Body == null)
            return document;

        var body = method.Body;
        var expressions = FindMatchingExpressions(body);
        var extractableGroups = expressions
            .GroupBy(BuildKey)
            .Where(g => g.Count() >= 2)
            .ToList();

        if (extractableGroups.Count == 0)
            return document;

        var existingNames = CollectExistingNames(method);
        var nameMap = new Dictionary<string, string>();
        foreach (var group in extractableGroups)
        {
            var propName = group.First().Name.Identifier.ValueText;
            var baseName = propName == "UtcNow" ? "utcNow" : "now";
            var varName = ResolveNameConflicts(baseName, existingNames);
            existingNames.Add(varName);
            nameMap[group.Key] = varName;
        }

        var expressionsToReplace = extractableGroups.SelectMany(g => g).ToList();
        var newBody = body.ReplaceNodes(expressionsToReplace, (original, rewritten) =>
        {
            var key = BuildKey(original);
            return SyntaxFactory.IdentifierName(nameMap[key])
                .WithLeadingTrivia(rewritten.GetLeadingTrivia())
                .WithTrailingTrivia(rewritten.GetTrailingTrivia());
        });

        var firstStmtTrivia = body.Statements.First().GetLeadingTrivia();
        var eolTrivia = GetEndOfLineTrivia(body);

        var declarations = new List<StatementSyntax>();
        foreach (var group in extractableGroups)
        {
            var varName = nameMap[group.Key];
            var declaration = SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.IdentifierName("var"),
                        SyntaxFactory.SingletonSeparatedList(
                            SyntaxFactory.VariableDeclarator(varName)
                                .WithInitializer(SyntaxFactory.EqualsValueClause(
                                    group.First().WithoutTrivia())))))
                .NormalizeWhitespace()
                .WithLeadingTrivia(firstStmtTrivia)
                .WithTrailingTrivia(eolTrivia);

            declarations.Add(declaration);
        }

        var newStatements = newBody.Statements;
        for (var i = declarations.Count - 1; i >= 0; i--)
            newStatements = newStatements.Insert(0, declarations[i]);

        newBody = newBody.WithStatements(newStatements);

        var newRoot = root.ReplaceNode(body, newBody);
        return document.WithSyntaxRoot(newRoot);
    }

    private static List<MemberAccessExpressionSyntax> FindMatchingExpressions(SyntaxNode container)
    {
        return container.DescendantNodes()
            .OfType<MemberAccessExpressionSyntax>()
            .Where(m => m.Expression is IdentifierNameSyntax { Identifier.ValueText: "DateTime" or "DateTimeOffset" } &&
                        m.Name?.Identifier.ValueText is "Now" or "UtcNow")
            .ToList();
    }

    private static string BuildKey(MemberAccessExpressionSyntax expr)
    {
        var typeName = ((IdentifierNameSyntax)expr.Expression).Identifier.ValueText;
        var propName = expr.Name.Identifier.ValueText;
        return typeName + "." + propName;
    }

    private static HashSet<string> CollectExistingNames(SyntaxNode scope)
    {
        var names = new HashSet<string>(
            scope.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(v => v.Identifier.ValueText));

        foreach (var param in scope.DescendantNodes().OfType<ParameterSyntax>())
            names.Add(param.Identifier.ValueText);

        return names;
    }

    private static string ResolveNameConflicts(string baseName, HashSet<string> existingNames)
    {
        if (!existingNames.Contains(baseName))
            return baseName;

        var suffix = 1;
        while (existingNames.Contains(baseName + suffix))
            suffix++;

        return baseName + suffix;
    }

    private static SyntaxTrivia GetEndOfLineTrivia(SyntaxNode node)
    {
        foreach (var trivia in node.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return trivia;
        }

        return SyntaxFactory.LineFeed;
    }
}
