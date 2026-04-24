using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Code fix provider for DSA019: extracts a repeated deeply nested member access chain
/// into a local variable and replaces all occurrences within the same scope.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA019CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA019CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA019Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        // The diagnostic may be on a MemberAccessExpression or an ElementAccessExpression
        ExpressionSyntax targetExpression = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(ma => ma.Span == diagnosticSpan);

        if (targetExpression == null)
        {
            targetExpression = root.FindToken(diagnosticSpan.Start).Parent?
                .AncestorsAndSelf()
                .OfType<ElementAccessExpressionSyntax>()
                .FirstOrDefault(ea => ea.Span == diagnosticSpan);
        }

        if (targetExpression == null)
            return;

        var variableName = GenerateVariableName(targetExpression);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract into local variable '{variableName}'",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, targetExpression, variableName, ct),
                equivalenceKey: DSA019Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> ExtractToVariableAsync(
        Document document,
        ExpressionSyntax targetExpression,
        string variableName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var targetKey = NormalizeWhitespace(targetExpression.ToString());
        var threshold = DSA019Analyzer.DefaultMaxDepth;

        // Find the containing scope
        var scope = GetContainingScope(targetExpression);
        if (scope == null)
            return document;

        // Find all duplicate occurrences in the same scope (MemberAccess or ElementAccess)
        var allOccurrences = FindMatchingExpressionsInScope(scope, targetExpression, targetKey, threshold, semanticModel);
        if (allOccurrences.Count < 2)
            return document;

        // Determine what to extract and what to replace
        var (nodesToReplace, expressionToExtract) = DetermineExtractionTarget(allOccurrences, targetExpression);

        // Check if the scope is an expression-body lambda that needs conversion to block
        var lambdaParent = FindExpressionBodyLambda(targetExpression);
        if (lambdaParent != null)
            return ExtractInExpressionBodyLambda(document, root, lambdaParent, nodesToReplace, expressionToExtract, variableName);

        // Block body: find the insertion point (earliest statement containing an occurrence)
        var insertionStatement = FindEarliestContainingStatement(allOccurrences, scope);
        if (insertionStatement == null)
            return document;

        // Resolve conflicts with existing variable names
        variableName = ResolveNameConflicts(variableName, scope);

        // Build the variable declaration
        var variableDeclaration = CreateVariableDeclaration(variableName, expressionToExtract, insertionStatement);

        // Apply changes using SyntaxEditor
        var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

        foreach (var nodeToReplace in nodesToReplace)
        {
            editor.ReplaceNode(nodeToReplace,
                SyntaxFactory.IdentifierName(variableName)
                    .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
                    .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia()));
        }

        editor.InsertBefore(insertionStatement, variableDeclaration);

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static Document ExtractInExpressionBodyLambda(
        Document document,
        SyntaxNode root,
        LambdaExpressionSyntax lambda,
        List<SyntaxNode> nodesToReplace,
        ExpressionSyntax expressionToExtract,
        string variableName)
    {
        var expressionBody = lambda is SimpleLambdaExpressionSyntax simple
            ? simple.ExpressionBody
            : (lambda as ParenthesizedLambdaExpressionSyntax)?.ExpressionBody;

        if (expressionBody == null)
            return document;

        variableName = ResolveNameConflicts(variableName, lambda);

        // Replace occurrences in the expression body
        var newExpression = expressionBody.ReplaceNodes(
            nodesToReplace.Where(o => expressionBody.Contains(o)),
            (original, _) => SyntaxFactory.IdentifierName(variableName)
                .WithLeadingTrivia(original.GetLeadingTrivia())
                .WithTrailingTrivia(original.GetTrailingTrivia()));

        // Build block body: variable declaration + return statement
        var variableDecl = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(variableName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(expressionToExtract.WithoutTrivia())))));

        var returnStatement = SyntaxFactory.ReturnStatement(newExpression.WithoutLeadingTrivia());

        var eol = GetEndOfLineString(lambda);
        var block = SyntaxFactory.Block(variableDecl, returnStatement)
            .NormalizeWhitespace(eol: eol);

        // Replace lambda body
        SyntaxNode newLambda;
        if (lambda is SimpleLambdaExpressionSyntax simpleLambda)
            newLambda = simpleLambda.WithExpressionBody(null).WithBlock(block);
        else if (lambda is ParenthesizedLambdaExpressionSyntax parenLambda)
            newLambda = parenLambda.WithExpressionBody(null).WithBlock(block);
        else
            return document;

        var newRoot = root.ReplaceNode(lambda, newLambda);
        return document.WithSyntaxRoot(newRoot);
    }

    private static LambdaExpressionSyntax FindExpressionBodyLambda(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is SimpleLambdaExpressionSyntax simple && simple.ExpressionBody != null)
                return simple;
            if (current is ParenthesizedLambdaExpressionSyntax paren && paren.ExpressionBody != null)
                return paren;
            if (current is BlockSyntax || current is MethodDeclarationSyntax ||
                current is LocalFunctionStatementSyntax)
                return null;
            current = current.Parent;
        }

        return null;
    }

    private static StatementSyntax FindEarliestContainingStatement(
        List<ExpressionSyntax> occurrences,
        SyntaxNode scope)
    {
        StatementSyntax earliest = null;
        var earliestStart = int.MaxValue;

        foreach (var occurrence in occurrences)
        {
            var statement = occurrence.Ancestors().OfType<StatementSyntax>()
                .LastOrDefault(s => s.Parent == scope || (scope is MethodDeclarationSyntax method && s.Parent == method.Body));

            if (statement == null)
            {
                // Try finding in block children
                statement = occurrence.Ancestors().OfType<StatementSyntax>().FirstOrDefault();
            }

            if (statement != null && statement.SpanStart < earliestStart)
            {
                earliest = statement;
                earliestStart = statement.SpanStart;
            }
        }

        return earliest;
    }

    private static string GenerateVariableName(ExpressionSyntax expression)
    {
        string name = null;

        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            name = memberAccess.Name.Identifier.ValueText;
        }
        else if (expression is ElementAccessExpressionSyntax elementAccess)
        {
            // Walk down to find the last named member in the chain
            var current = elementAccess.Expression;
            while (current is ElementAccessExpressionSyntax nested)
                current = nested.Expression;

            if (current is MemberAccessExpressionSyntax ma)
                name = ma.Name.Identifier.ValueText;
            else if (current is IdentifierNameSyntax id)
                name = id.Identifier.ValueText;
        }

        if (string.IsNullOrEmpty(name))
            name = "value";

        if (name.Length > 0 && char.IsUpper(name[0]))
            name = char.ToLowerInvariant(name[0]) + name.Substring(1);

        if (name.StartsWith("_", System.StringComparison.Ordinal))
            name = name.TrimStart('_');

        if (string.IsNullOrEmpty(name) || SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None)
            name = "value";

        return name;
    }

    private static string ResolveNameConflicts(string baseName, SyntaxNode scope)
    {
        var existingNames = new HashSet<string>(
            scope.DescendantNodes()
                .OfType<VariableDeclaratorSyntax>()
                .Select(v => v.Identifier.ValueText));

        // Also check parameter names
        foreach (var param in scope.DescendantNodes().OfType<ParameterSyntax>())
            existingNames.Add(param.Identifier.ValueText);

        if (!existingNames.Contains(baseName))
            return baseName;

        var suffix = 1;
        while (existingNames.Contains(baseName + suffix))
            suffix++;

        return baseName + suffix;
    }

    private static List<ExpressionSyntax> FindMatchingExpressionsInScope(
        SyntaxNode scope,
        ExpressionSyntax targetExpression,
        string targetKey,
        int threshold,
        SemanticModel semanticModel)
    {
        if (targetExpression is MemberAccessExpressionSyntax)
        {
            return GetMemberAccessesInScope(scope)
                .Where(ma => DSA019Analyzer.ComputeChainDepth(ma) >= threshold &&
                             NormalizeWhitespace(ma.ToString()) == targetKey &&
                             (ReferenceEquals(ma, targetExpression) ||
                              semanticModel == null ||
                              DSA019Analyzer.AreSemanticallySame(targetExpression, ma, semanticModel)))
                .Cast<ExpressionSyntax>()
                .ToList();
        }

        if (targetExpression is ElementAccessExpressionSyntax)
        {
            return DSA019Analyzer.GetElementAccessesInScope(scope)
                .Where(ea => DSA019Analyzer.ComputeChainDepth(ea) >= threshold &&
                             NormalizeWhitespace(ea.ToString()) == targetKey &&
                             (ReferenceEquals(ea, targetExpression) ||
                              semanticModel == null ||
                              DSA019Analyzer.AreSemanticallySame(targetExpression, ea, semanticModel)))
                .Cast<ExpressionSyntax>()
                .ToList();
        }

        return new List<ExpressionSyntax>();
    }

    /// <summary>
    /// Determines what to extract and what nodes to replace.
    /// For MemberAccessExpressions that are method calls:
    ///   - Same arguments → extract the full InvocationExpression
    ///   - Different arguments → extract the receiver (the Expression of the MemberAccess)
    /// For ElementAccessExpressions or plain MemberAccess → extract as-is.
    /// </summary>
    private static (List<SyntaxNode> NodesToReplace, ExpressionSyntax ExpressionToExtract) DetermineExtractionTarget(
        List<ExpressionSyntax> allOccurrences,
        ExpressionSyntax targetExpression)
    {
        if (targetExpression is MemberAccessExpressionSyntax targetMemberAccess &&
            targetMemberAccess.Parent is InvocationExpressionSyntax targetInvocation)
        {
            var targetArgsText = NormalizeWhitespace(targetInvocation.ArgumentList.ToString());
            var allAreInvocations = allOccurrences.All(o =>
                o is MemberAccessExpressionSyntax ma && ma.Parent is InvocationExpressionSyntax);

            if (allAreInvocations)
            {
                var allHaveSameArgs = allOccurrences.All(o =>
                    o is MemberAccessExpressionSyntax ma &&
                    ma.Parent is InvocationExpressionSyntax inv &&
                    NormalizeWhitespace(inv.ArgumentList.ToString()) == targetArgsText);

                if (allHaveSameArgs)
                {
                    // Same method, same args → extract the full InvocationExpression
                    var invocationNodes = allOccurrences
                        .Select(o => (SyntaxNode)o.Parent)
                        .ToList();
                    return (invocationNodes, targetInvocation);
                }

                // Same method, different args → extract the receiver of the MemberAccess
                var receivers = allOccurrences
                    .Cast<MemberAccessExpressionSyntax>()
                    .Select(ma => (SyntaxNode)ma.Expression)
                    .ToList();
                return (receivers, targetMemberAccess.Expression);
            }
        }

        // Default: extract the expression as-is
        return (allOccurrences.Cast<SyntaxNode>().ToList(), targetExpression);
    }

    private static LocalDeclarationStatementSyntax CreateVariableDeclaration(
        string variableName,
        ExpressionSyntax expression,
        StatementSyntax insertBeforeStatement)
    {
        return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(variableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                expression.WithoutTrivia())))))
            .WithLeadingTrivia(insertBeforeStatement.GetLeadingTrivia())
            .WithTrailingTrivia(GetEndOfLineTrivia(insertBeforeStatement));
    }

    private static SyntaxNode GetContainingScope(SyntaxNode node)
    {
        var current = node.Parent;
        while (current != null)
        {
            if (current is SimpleLambdaExpressionSyntax simpleLambda)
                return simpleLambda.Body;
            if (current is ParenthesizedLambdaExpressionSyntax parenLambda)
                return parenLambda.Body;
            if (current is AnonymousMethodExpressionSyntax anonMethod)
                return anonMethod.Body;
            if (current is LocalFunctionStatementSyntax localFunc)
                return (SyntaxNode)localFunc.Body ?? localFunc.ExpressionBody?.Expression;
            if (current is MethodDeclarationSyntax method)
                return (SyntaxNode)method.Body ?? method.ExpressionBody?.Expression;
            if (current is ConstructorDeclarationSyntax ctor)
                return (SyntaxNode)ctor.Body ?? ctor.ExpressionBody?.Expression;
            if (current is AccessorDeclarationSyntax accessor)
                return (SyntaxNode)accessor.Body ?? accessor.ExpressionBody?.Expression;
            if (current is CompilationUnitSyntax compilationUnit)
                return compilationUnit;

            current = current.Parent;
        }

        return null;
    }

    private static IEnumerable<MemberAccessExpressionSyntax> GetMemberAccessesInScope(SyntaxNode scope)
    {
        return scope.DescendantNodes(n => !IsNestedScope(n))
            .OfType<MemberAccessExpressionSyntax>();
    }

    private static bool IsNestedScope(SyntaxNode node)
    {
        return node is SimpleLambdaExpressionSyntax ||
               node is ParenthesizedLambdaExpressionSyntax ||
               node is AnonymousMethodExpressionSyntax ||
               node is LocalFunctionStatementSyntax;
    }

    /// <summary>
    /// Extracts the end-of-line trivia from an existing node to match the file's line ending style.
    /// </summary>
    private static SyntaxTrivia GetEndOfLineTrivia(SyntaxNode node)
    {
        foreach (var trivia in node.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return trivia;
        }

        return SyntaxFactory.LineFeed;
    }

    /// <summary>
    /// Returns the end-of-line string ("\n" or "\r\n") used by the file containing the given node.
    /// </summary>
    private static string GetEndOfLineString(SyntaxNode node)
    {
        foreach (var trivia in node.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return trivia.ToString();
        }

        return "\n";
    }

    private static string NormalizeWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        var lastWasSpace = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastWasSpace = false;
            }
        }

        return sb.ToString().Trim();
    }
}