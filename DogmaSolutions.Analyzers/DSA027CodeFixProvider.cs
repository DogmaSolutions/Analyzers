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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA027CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA027CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA027Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        if (node is not AssignmentExpressionSyntax assignment)
            return;

        var loop = FindEnclosingLoop(assignment);
        if (loop?.Parent is not BlockSyntax)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Use StringBuilder",
                createChangedDocument: ct => ConvertToStringBuilderAsync(context.Document, assignment, ct),
                equivalenceKey: DSA027Analyzer.DiagnosticId),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA027Analyzer.DiagnosticId, nameof(Resources.DSA027ReviewComment));
    }

    private static async Task<Document> ConvertToStringBuilderAsync(
        Document document,
        AssignmentExpressionSyntax assignment,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var leftSymbol = semanticModel.GetSymbolInfo(assignment.Left, cancellationToken).Symbol;
        if (leftSymbol == null)
            return document;

        var variableName = leftSymbol.Name;
        var loop = FindEnclosingLoop(assignment);
        if (loop?.Parent is not BlockSyntax block)
            return document;

        var sbName = ResolveBuilderName(variableName, loop);

        var loopBody = DSA027Analyzer.GetLoopBody(loop);
        if (loopBody == null)
            return document;

        var concatenations = FindAllConcatenations(loopBody, leftSymbol, semanticModel);
        if (concatenations.Count == 0)
            return document;

        var newLoop = loop.ReplaceNodes(
            concatenations.Select(c => c.Parent).OfType<ExpressionStatementSyntax>(),
            (original, _) =>
            {
                var expr = ((ExpressionStatementSyntax)original).Expression as AssignmentExpressionSyntax;
                if (expr == null)
                    return original;

                var appendExpr = ExtractAppendExpression(expr, leftSymbol, semanticModel);
                if (appendExpr == null)
                    return original;

                var appendCall = SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName(sbName),
                                SyntaxFactory.IdentifierName("Append")),
                            SyntaxFactory.ArgumentList(
                                SyntaxFactory.SingletonSeparatedList(
                                    SyntaxFactory.Argument(appendExpr.WithoutTrivia())))))
                    .WithLeadingTrivia(original.GetLeadingTrivia())
                    .WithTrailingTrivia(original.GetTrailingTrivia());

                return appendCall;
            });

        var loopIndex = block.Statements.IndexOf((StatementSyntax)loop);
        if (loopIndex < 0)
            return document;

        var eol = SyntaxUtils.GetEndOfLineTrivia(block);
        var leadingTrivia = ((StatementSyntax)loop).GetLeadingTrivia();

        var sbDecl = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(sbName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                SyntaxFactory.ObjectCreationExpression(
                                    SyntaxFactory.QualifiedName(
                                        SyntaxFactory.QualifiedName(
                                            SyntaxFactory.IdentifierName("System"),
                                            SyntaxFactory.IdentifierName("Text")),
                                        SyntaxFactory.IdentifierName("StringBuilder")),
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SingletonSeparatedList(
                                            SyntaxFactory.Argument(
                                                SyntaxFactory.IdentifierName(variableName)))),
                                    null))))))
            .NormalizeWhitespace()
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(eol);

        var toStringAssign = SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    SyntaxFactory.IdentifierName(variableName),
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.IdentifierName(sbName),
                            SyntaxFactory.IdentifierName("ToString")))))
            .NormalizeWhitespace()
            .WithLeadingTrivia(leadingTrivia)
            .WithTrailingTrivia(eol);

        var newStatements = block.Statements
            .RemoveAt(loopIndex)
            .InsertRange(loopIndex, new StatementSyntax[] { sbDecl, ((StatementSyntax)newLoop).WithLeadingTrivia(leadingTrivia), toStringAssign });

        var newBlock = block.WithStatements(newStatements);
        var newRoot = root.ReplaceNode(block, newBlock);

        return document.WithSyntaxRoot(newRoot);
    }

    private static ExpressionSyntax ExtractAppendExpression(
        AssignmentExpressionSyntax assignment,
        ISymbol selfSymbol,
        SemanticModel model)
    {
        if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            return assignment.Right;

        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
            assignment.Right is BinaryExpressionSyntax binary)
        {
            var parts = new List<ExpressionSyntax>();
            FlattenAddChain(binary, parts);

            var nonSelf = parts
                .Where(p => !IsSelfReference(p, selfSymbol, model))
                .ToList();

            if (nonSelf.Count == 0)
                return null;

            var result = nonSelf[0];
            for (var i = 1; i < nonSelf.Count; i++)
            {
                result = SyntaxFactory.BinaryExpression(
                    SyntaxKind.AddExpression,
                    result,
                    nonSelf[i]);
            }

            return result;
        }

        return null;
    }

    private static void FlattenAddChain(ExpressionSyntax expr, List<ExpressionSyntax> parts)
    {
        if (expr is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
        {
            FlattenAddChain(binary.Left, parts);
            FlattenAddChain(binary.Right, parts);
        }
        else
        {
            parts.Add(expr);
        }
    }

    private static bool IsSelfReference(ExpressionSyntax expr, ISymbol selfSymbol, SemanticModel model)
    {
        if (expr is not IdentifierNameSyntax identifier)
            return false;

        var symbol = model.GetSymbolInfo(identifier).Symbol;
        return SymbolEqualityComparer.Default.Equals(symbol, selfSymbol);
    }

    private static List<AssignmentExpressionSyntax> FindAllConcatenations(
        SyntaxNode loopBody,
        ISymbol variableSymbol,
        SemanticModel model)
    {
        var result = new List<AssignmentExpressionSyntax>();

        foreach (var node in loopBody.DescendantNodes(n =>
                     n is not ForStatementSyntax and
                     not ForEachStatementSyntax and
                     not ForEachVariableStatementSyntax and
                     not WhileStatementSyntax and
                     not DoStatementSyntax))
        {
            if (node is not AssignmentExpressionSyntax assignment)
                continue;

            var leftSymbol = model.GetSymbolInfo(assignment.Left).Symbol;
            if (!SymbolEqualityComparer.Default.Equals(leftSymbol, variableSymbol))
                continue;

            if (assignment.IsKind(SyntaxKind.AddAssignmentExpression))
            {
                result.Add(assignment);
            }
            else if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) &&
                     assignment.Right is BinaryExpressionSyntax)
            {
                var parts = new List<ExpressionSyntax>();
                FlattenAddChain(assignment.Right, parts);
                if (parts.Any(p => IsSelfReference(p, variableSymbol, model)))
                    result.Add(assignment);
            }
        }

        return result;
    }

    private static SyntaxNode FindEnclosingLoop(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (current is ParenthesizedLambdaExpressionSyntax or
                SimpleLambdaExpressionSyntax or
                AnonymousMethodExpressionSyntax or
                LocalFunctionStatementSyntax)
                return null;

            if (current is ForStatementSyntax or
                ForEachStatementSyntax or
                ForEachVariableStatementSyntax or
                WhileStatementSyntax or
                DoStatementSyntax)
                return current;
        }

        return null;
    }

    private static string ResolveBuilderName(string variableName, SyntaxNode scope)
    {
        var baseName = variableName + "Builder";
        var existing = new HashSet<string>(
            scope.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(v => v.Identifier.ValueText));

        if (!existing.Contains(baseName))
            return baseName;

        var suffix = 1;
        while (existing.Contains(baseName + suffix))
            suffix++;

        return baseName + suffix;
    }

}
