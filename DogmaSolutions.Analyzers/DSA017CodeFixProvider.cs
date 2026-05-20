using System;
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA017CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA017CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA017Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        if (node is not IfStatementSyntax ifStatement)
            return;

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return;

        if (!CheckThenActUtils.TryMatchCheckThenAct(ifStatement, out var receiver))
            return;

        var receiverType = CheckThenActUtils.ResolveReceiverType(receiver, semanticModel);
        if (receiverType == null)
            return;

        var typeName = receiverType.Name;
        var ns = receiverType.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        var fix = ClassifyFix(typeName, ns, ifStatement);
        if (fix == FixKind.None)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: GetFixTitle(fix),
                createChangedDocument: ct => ApplyFixAsync(context.Document, ifStatement, fix, ct),
                equivalenceKey: DSA017Analyzer.DiagnosticId + "." + fix),
            diagnostic);
    }

    private enum FixKind
    {
        None,
        DictionaryTryAdd,
        SetAddReturnsBool,
        DictionaryTryAddThrowPattern,
        SetAddThrowPattern,
    }

    private static FixKind ClassifyFix(string typeName, string ns, IfStatementSyntax ifStatement)
    {
        if (TryGetTryGetValueOutVariableName(ifStatement.Condition, out var outVarName))
        {
            if (IsOutVariableUsedBeyondCondition(outVarName, ifStatement) || ifStatement.Else != null)
                return FixKind.None;
        }

        if (CheckThenActUtils.IsNegatedExistenceCheck(ifStatement.Condition, out _))
        {
            if (!HasSimpleInsertBody(ifStatement.Statement))
                return FixKind.None;

            if (IsDictionaryLikeWithTryAdd(typeName, ns))
                return FixKind.DictionaryTryAdd;

            if (IsSetLike(typeName, ns))
                return FixKind.SetAddReturnsBool;
        }

        if (CheckThenActUtils.IsPositiveExistenceCheck(ifStatement.Condition, out _))
        {
            if (CheckThenActUtils.ContainsThrowStatement(ifStatement.Statement) &&
                ifStatement.Else == null &&
                HasAdjacentInsertStatement(ifStatement))
            {
                if (IsDictionaryLikeWithTryAdd(typeName, ns))
                    return FixKind.DictionaryTryAddThrowPattern;

                if (IsSetLike(typeName, ns))
                    return FixKind.SetAddThrowPattern;
            }

            if (ifStatement.Else != null && HasSimpleInsertBody(ifStatement.Else.Statement))
            {
                if (IsDictionaryLikeWithTryAdd(typeName, ns))
                    return FixKind.DictionaryTryAdd;

                if (IsSetLike(typeName, ns))
                    return FixKind.SetAddReturnsBool;
            }
        }

        return FixKind.None;
    }

    private static bool HasAdjacentInsertStatement(IfStatementSyntax ifStatement)
    {
        if (ifStatement.Parent is not BlockSyntax parentBlock)
            return false;

        var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
        return FindSubsequentInsertStatement(parentBlock, ifIndex) != null;
    }

    private static bool HasSimpleInsertBody(StatementSyntax body)
    {
        if (body is ExpressionStatementSyntax)
            return true;

        if (body is BlockSyntax block && block.Statements.Count == 1)
            return true;

        return false;
    }

    private static bool IsDictionaryLikeWithTryAdd(string typeName, string ns)
    {
        if (ns == "System.Collections.Generic")
            return typeName is "Dictionary" or "SortedDictionary";

        if (ns == "System.Collections.Concurrent")
            return typeName == "ConcurrentDictionary";

        return false;
    }

    private static bool IsSetLike(string typeName, string ns)
    {
        if (ns == "System.Collections.Generic")
            return typeName is "HashSet" or "SortedSet";

        return false;
    }

    private static string GetFixTitle(FixKind fix)
    {
        return fix switch
        {
            FixKind.DictionaryTryAdd => "Use TryAdd",
            FixKind.SetAddReturnsBool => "Use Add (returns bool)",
            FixKind.DictionaryTryAddThrowPattern => "Use TryAdd",
            FixKind.SetAddThrowPattern => "Use Add (returns bool)",
            _ => "Use atomic operation"
        };
    }

    private static async Task<Document> ApplyFixAsync(
        Document document,
        IfStatementSyntax ifStatement,
        FixKind fix,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        return fix switch
        {
            FixKind.DictionaryTryAdd => ApplyDictionaryTryAdd(document, root, ifStatement),
            FixKind.SetAddReturnsBool => ApplySetAdd(document, root, ifStatement),
            FixKind.DictionaryTryAddThrowPattern => ApplyThrowPattern(document, root, ifStatement),
            FixKind.SetAddThrowPattern => ApplySetThrowPattern(document, root, ifStatement),
            _ => document
        };
    }

    private static Document ApplyDictionaryTryAdd(Document document, SyntaxNode root, IfStatementSyntax ifStatement)
    {
        var addInvocation = FindInsertInvocation(ifStatement.Statement);
        var insertInElse = false;
        if (addInvocation == null && ifStatement.Else != null)
        {
            addInvocation = FindInsertInvocation(ifStatement.Else.Statement);
            insertInElse = true;
        }

        if (addInvocation == null)
            return document;

        if (addInvocation.Expression is not MemberAccessExpressionSyntax addMemberAccess)
            return document;

        var tryAddCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                addMemberAccess.Expression.WithoutTrivia(),
                SyntaxFactory.IdentifierName("TryAdd")),
            addInvocation.ArgumentList.WithoutTrivia());

        var otherBody = insertInElse ? ifStatement.Statement : ifStatement.Else?.Statement;

        if (otherBody != null)
        {
            var newCondition = SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                SyntaxFactory.ParenthesizedExpression(tryAddCall));

            var newIf = SyntaxFactory.IfStatement(newCondition, otherBody)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(ifStatement, newIf);
            return document.WithSyntaxRoot(newRoot);
        }

        var tryAddStatement = SyntaxFactory.ExpressionStatement(tryAddCall)
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var replacedRoot = root.ReplaceNode(ifStatement, tryAddStatement);
        return document.WithSyntaxRoot(replacedRoot);
    }

    private static Document ApplySetAdd(Document document, SyntaxNode root, IfStatementSyntax ifStatement)
    {
        var addInvocation = FindInsertInvocation(ifStatement.Statement);
        var insertInElse = false;
        if (addInvocation == null && ifStatement.Else != null)
        {
            addInvocation = FindInsertInvocation(ifStatement.Else.Statement);
            insertInElse = true;
        }

        if (addInvocation == null)
            return document;

        var otherBody = insertInElse ? ifStatement.Statement : ifStatement.Else?.Statement;

        if (otherBody != null)
        {
            var newCondition = SyntaxFactory.PrefixUnaryExpression(
                SyntaxKind.LogicalNotExpression,
                SyntaxFactory.ParenthesizedExpression(addInvocation.WithoutTrivia()));

            var newIf = SyntaxFactory.IfStatement(newCondition, otherBody)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(ifStatement, newIf);
            return document.WithSyntaxRoot(newRoot);
        }

        var addStatement = SyntaxFactory.ExpressionStatement(addInvocation.WithoutTrivia())
            .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
            .WithTrailingTrivia(ifStatement.GetTrailingTrivia());

        var replacedRoot = root.ReplaceNode(ifStatement, addStatement);
        return document.WithSyntaxRoot(replacedRoot);
    }

    private static Document ApplyThrowPattern(Document document, SyntaxNode root, IfStatementSyntax ifStatement)
    {
        if (ifStatement.Parent is not BlockSyntax parentBlock)
            return document;

        var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
        if (ifIndex < 0)
            return document;

        var addStatement = FindSubsequentInsertStatement(parentBlock, ifIndex);
        if (addStatement == null)
            return document;

        var addInvocation = FindInsertInvocation(addStatement);
        if (addInvocation == null)
            return document;

        if (addInvocation.Expression is not MemberAccessExpressionSyntax addMemberAccess)
            return document;

        var tryAddCall = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                addMemberAccess.Expression.WithoutTrivia(),
                SyntaxFactory.IdentifierName("TryAdd")),
            addInvocation.ArgumentList.WithoutTrivia());

        var newCondition = SyntaxFactory.PrefixUnaryExpression(
            SyntaxKind.LogicalNotExpression,
            SyntaxFactory.ParenthesizedExpression(tryAddCall));

        var newIf = ifStatement
            .WithCondition(newCondition.WithLeadingTrivia(SyntaxFactory.Space))
            .WithElse(null);

        var addIndex = parentBlock.Statements.IndexOf(addStatement);
        var newStatements = parentBlock.Statements
            .Replace(ifStatement, newIf)
            .RemoveAt(addIndex);

        var newBlock = parentBlock.WithStatements(newStatements);
        var newRoot = root.ReplaceNode(parentBlock, newBlock);
        return document.WithSyntaxRoot(newRoot);
    }

    private static Document ApplySetThrowPattern(Document document, SyntaxNode root, IfStatementSyntax ifStatement)
    {
        if (ifStatement.Parent is not BlockSyntax parentBlock)
            return document;

        var ifIndex = parentBlock.Statements.IndexOf(ifStatement);
        if (ifIndex < 0)
            return document;

        var addStatement = FindSubsequentInsertStatement(parentBlock, ifIndex);
        if (addStatement == null)
            return document;

        var addInvocation = FindInsertInvocation(addStatement);
        if (addInvocation == null)
            return document;

        var newCondition = SyntaxFactory.PrefixUnaryExpression(
            SyntaxKind.LogicalNotExpression,
            SyntaxFactory.ParenthesizedExpression(addInvocation.WithoutTrivia()));

        var newIf = ifStatement
            .WithCondition(newCondition.WithLeadingTrivia(SyntaxFactory.Space))
            .WithElse(null);

        var addIndex = parentBlock.Statements.IndexOf(addStatement);
        var newStatements = parentBlock.Statements
            .Replace(ifStatement, newIf)
            .RemoveAt(addIndex);

        var newBlock = parentBlock.WithStatements(newStatements);
        var newRoot = root.ReplaceNode(parentBlock, newBlock);
        return document.WithSyntaxRoot(newRoot);
    }

    private static InvocationExpressionSyntax FindInsertInvocation(StatementSyntax statement)
    {
        return statement.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(inv =>
                inv.Expression is MemberAccessExpressionSyntax memberAccess &&
                Array.IndexOf(CheckThenActUtils.InsertMethods, memberAccess.Name.Identifier.ValueText) >= 0);
    }

    private static ExpressionStatementSyntax FindSubsequentInsertStatement(BlockSyntax block, int afterIndex)
    {
        var nextIndex = afterIndex + 1;
        if (nextIndex >= block.Statements.Count)
            return null;

        if (block.Statements[nextIndex] is ExpressionStatementSyntax exprStmt &&
            FindInsertInvocation(exprStmt) != null)
        {
            return exprStmt;
        }

        return null;
    }

    private static bool TryGetTryGetValueOutVariableName(ExpressionSyntax condition, out string outVarName)
    {
        outVarName = null;

        var tryGetValueInvocation = condition.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(inv => inv.Expression is MemberAccessExpressionSyntax ma &&
                                   ma.Name.Identifier.ValueText == "TryGetValue");

        if (tryGetValueInvocation == null)
            return false;

        foreach (var arg in tryGetValueInvocation.ArgumentList.Arguments)
        {
            if (!arg.RefKindKeyword.IsKind(SyntaxKind.OutKeyword))
                continue;

            if (arg.Expression is DeclarationExpressionSyntax decl &&
                decl.Designation is SingleVariableDesignationSyntax designation)
            {
                outVarName = designation.Identifier.ValueText;
                return true;
            }

            if (arg.Expression is IdentifierNameSyntax identifier)
            {
                outVarName = identifier.Identifier.ValueText;
                return true;
            }
        }

        return false;
    }

    private static bool IsOutVariableUsedBeyondCondition(string variableName, IfStatementSyntax ifStatement)
    {
        if (ContainsIdentifierReference(ifStatement.Statement, variableName))
            return true;

        if (ifStatement.Else != null && ContainsIdentifierReference(ifStatement.Else.Statement, variableName))
            return true;

        if (ifStatement.Parent is BlockSyntax block)
        {
            var ifIndex = block.Statements.IndexOf(ifStatement);
            for (var i = ifIndex + 1; i < block.Statements.Count; i++)
            {
                if (ContainsIdentifierReference(block.Statements[i], variableName))
                    return true;
            }
        }

        return false;
    }

    private static bool ContainsIdentifierReference(SyntaxNode node, string identifierName)
    {
        return node.DescendantNodes()
            .OfType<IdentifierNameSyntax>()
            .Any(id => id.Identifier.ValueText == identifierName);
    }
}
