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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA022CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA022CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA022Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var expression = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<BinaryExpressionSyntax>()
            .FirstOrDefault(b => b.Span == diagnosticSpan);

        if (expression == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Hoist loop-invariant expression",
                createChangedDocument: ct => HoistExpressionAsync(context.Document, expression, ct),
                equivalenceKey: DSA022Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> HoistExpressionAsync(
        Document document,
        BinaryExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var loopNode = FindContainingLoop(expression);
        if (loopNode == null)
            return document;

        var loopStatement = loopNode as StatementSyntax;
        if (loopStatement == null)
            return document;

        var block = loopStatement.Parent as BlockSyntax;
        if (block == null)
            return document;

        var variableName = GenerateVariableName(expression);
        variableName = ResolveNameConflicts(variableName, loopNode.Parent);

        var expressionText = NormalizeWhitespace(expression.ToString());
        var loopBody = GetLoopBody(loopNode);
        if (loopBody == null)
            return document;

        var newLoop = loopNode;
        for (;;)
        {
            var body = GetLoopBody(newLoop);
            var current = body?.DescendantNodesAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .FirstOrDefault(b => NormalizeWhitespace(b.ToString()) == expressionText);

            if (current == null)
                break;

            newLoop = newLoop.ReplaceNode(current,
                SyntaxFactory.IdentifierName(variableName)
                    .WithLeadingTrivia(current.GetLeadingTrivia())
                    .WithTrailingTrivia(current.GetTrailingTrivia()));
        }

        var loopLeadingTrivia = loopStatement.GetLeadingTrivia();
        var eolTrivia = GetEndOfLineTrivia(loopStatement);

        var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(variableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                expression.WithoutTrivia())))))
            .NormalizeWhitespace()
            .WithLeadingTrivia(loopLeadingTrivia)
            .WithTrailingTrivia(eolTrivia);

        var modifiedLoop = ((StatementSyntax)newLoop)
            .WithLeadingTrivia(loopLeadingTrivia);

        var index = block.Statements.IndexOf(loopStatement);
        var newStatements = block.Statements
            .Replace(loopStatement, modifiedLoop)
            .Insert(index, variableDeclaration);

        var newRoot = root.ReplaceNode(block, block.WithStatements(newStatements));
        return document.WithSyntaxRoot(newRoot);
    }

    private static SyntaxNode FindContainingLoop(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is ForStatementSyntax || current is ForEachStatementSyntax ||
                current is WhileStatementSyntax || current is DoStatementSyntax)
                return current;
            current = current.Parent;
        }

        return null;
    }

    private static StatementSyntax GetLoopBody(SyntaxNode loopNode)
    {
        switch (loopNode)
        {
            case ForStatementSyntax forStmt: return forStmt.Statement;
            case ForEachStatementSyntax forEachStmt: return forEachStmt.Statement;
            case WhileStatementSyntax whileStmt: return whileStmt.Statement;
            case DoStatementSyntax doStmt: return doStmt.Statement;
            default: return null;
        }
    }

    private static string GenerateVariableName(BinaryExpressionSyntax expression)
    {
        var identifiers = expression.DescendantNodesAndSelf()
            .OfType<IdentifierNameSyntax>()
            .Select(id => id.Identifier.ValueText)
            .ToList();

        if (identifiers.Count >= 2)
            return "hoisted_" + identifiers[0] + "_" + identifiers[1];

        if (identifiers.Count == 1)
            return "hoisted_" + identifiers[0];

        return "hoisted";
    }

    private static string ResolveNameConflicts(string baseName, SyntaxNode scope)
    {
        if (scope == null)
            return baseName;

        var existingNames = new HashSet<string>(
            scope.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(v => v.Identifier.ValueText));

        foreach (var param in scope.DescendantNodes().OfType<ParameterSyntax>())
            existingNames.Add(param.Identifier.ValueText);

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

    private static string NormalizeWhitespace(string text)
    {
        var chars = new List<char>(text.Length);
        var lastWasSpace = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    chars.Add(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                chars.Add(c);
                lastWasSpace = false;
            }
        }

        return new string(chars.ToArray()).Trim();
    }
}
