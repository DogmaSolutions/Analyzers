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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA035CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA035CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA035Analyzer.DiagnosticId];

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
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(inv => inv.Span == diagnosticSpan);

        if (expression == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Hoist loop-invariant reflection call",
                createChangedDocument: ct => HoistExpressionAsync(context.Document, expression, ct),
                equivalenceKey: DSA035Analyzer.DiagnosticId),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, expression, DSA035Analyzer.DiagnosticId, nameof(Resources.DSA035ReviewComment));
    }

    private static async Task<Document> HoistExpressionAsync(
        Document document,
        InvocationExpressionSyntax expression,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        ExpressionSyntax hoistTarget = expression.Parent is ConditionalAccessExpressionSyntax ca
            ? ca
            : expression;

        var loopNode = DSA022CodeFixProvider.FindContainingLoop(hoistTarget);
        if (loopNode == null)
            return document;

        var loopStatement = loopNode as StatementSyntax;
        if (loopStatement == null)
            return document;

        var block = loopStatement.Parent as BlockSyntax;
        if (block == null)
            return document;

        var variableName = GenerateVariableName(expression);
        variableName = DSA022CodeFixProvider.ResolveNameConflicts(variableName, loopNode.Parent);

        var targetText = SyntaxUtils.NormalizeWhitespace(hoistTarget.ToString());
        var loopBody = SyntaxUtils.GetLoopBody(loopNode);
        if (loopBody == null)
            return document;

        var isConditionalAccess = hoistTarget is ConditionalAccessExpressionSyntax;
        var newLoop = loopNode;
        for (;;)
        {
            var body = SyntaxUtils.GetLoopBody(newLoop);
            SyntaxNode current;
            if (isConditionalAccess)
                current = body?.DescendantNodesAndSelf()
                    .OfType<ConditionalAccessExpressionSyntax>()
                    .FirstOrDefault(n => SyntaxUtils.NormalizeWhitespace(n.ToString()) == targetText);
            else
                current = body?.DescendantNodesAndSelf()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(inv => SyntaxUtils.NormalizeWhitespace(inv.ToString()) == targetText);

            if (current == null)
                break;

            newLoop = newLoop.ReplaceNode(current,
                SyntaxFactory.IdentifierName(variableName)
                    .WithLeadingTrivia(current.GetLeadingTrivia())
                    .WithTrailingTrivia(current.GetTrailingTrivia()));
        }

        var loopLeadingTrivia = loopStatement.GetLeadingTrivia();
        var eolTrivia = SyntaxUtils.GetEndOfLineTrivia(loopStatement);

        var variableDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(variableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                hoistTarget.WithoutTrivia())))))
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

    internal static string GenerateVariableName(InvocationExpressionSyntax invocation)
    {
        var (receiverName, methodName) = GetReceiverAndMethodNames(invocation);

        if (methodName == null)
            return "hoisted";

        return receiverName == null
            ? "hoisted_" + methodName
            : "hoisted_" + receiverName + "_" + methodName;
    }

    /// <summary>
    /// The name of the method called, and the name that identifies its receiver (a variable, or the previous call of a chain),
    /// for <c>receiver.Method()</c> and <c>receiver?.Method()</c>. Each is null when it can't be named.
    /// </summary>
    private static (string ReceiverName, string MethodName) GetReceiverAndMethodNames(InvocationExpressionSyntax invocation)
    {
        switch (invocation.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess:
                return (GetReceiverName(memberAccess.Expression), memberAccess.Name.Identifier.ValueText);

            case MemberBindingExpressionSyntax memberBinding when invocation.Parent is ConditionalAccessExpressionSyntax conditionalAccess:
                return ((conditionalAccess.Expression as IdentifierNameSyntax)?.Identifier.ValueText, memberBinding.Name.Identifier.ValueText);

            default:
                return (null, null);
        }
    }

    private static string GetReceiverName(ExpressionSyntax receiver)
    {
        switch (receiver)
        {
            case IdentifierNameSyntax identifier:
                return identifier.Identifier.ValueText;

            case InvocationExpressionSyntax { Expression: MemberAccessExpressionSyntax chainedMember }:
                return chainedMember.Name.Identifier.ValueText;

            default:
                return null;
        }
    }
}
