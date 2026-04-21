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

        var memberAccess = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<MemberAccessExpressionSyntax>()
            .FirstOrDefault(ma => ma.Span == diagnosticSpan);

        if (memberAccess == null)
            return;

        var variableName = GenerateVariableName(memberAccess);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{memberAccess}' into local variable '{variableName}'",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, memberAccess, variableName, ct),
                equivalenceKey: DSA019Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> ExtractToVariableAsync(
        Document document,
        MemberAccessExpressionSyntax targetMemberAccess,
        string variableName,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var targetKey = NormalizeWhitespace(targetMemberAccess.ToString());
        var threshold = DSA019Analyzer.DefaultMaxDepth;

        // Find the containing scope
        var scope = GetContainingScope(targetMemberAccess);
        if (scope == null)
            return document;

        // Find all duplicate occurrences in the same scope
        var allOccurrences = GetMemberAccessesInScope(scope)
            .Where(ma => !DSA019Analyzer.ComputeChainDepth(ma).Equals(0) &&
                         DSA019Analyzer.ComputeChainDepth(ma) >= threshold &&
                         NormalizeWhitespace(ma.ToString()) == targetKey)
            .ToList();

        if (allOccurrences.Count < 2)
            return document;

        // Check if the scope is an expression-body lambda that needs conversion to block
        var lambdaParent = FindExpressionBodyLambda(targetMemberAccess);
        if (lambdaParent != null)
            return await ExtractInExpressionBodyLambdaAsync(document, root, lambdaParent, allOccurrences, targetMemberAccess, variableName, cancellationToken).ConfigureAwait(false);

        // Block body: find the insertion point (earliest statement containing an occurrence)
        var insertionStatement = FindEarliestContainingStatement(allOccurrences, scope);
        if (insertionStatement == null)
            return document;

        // Resolve conflicts with existing variable names
        variableName = ResolveNameConflicts(variableName, scope);

        // Build the variable declaration
        var variableDeclaration = CreateVariableDeclaration(variableName, targetMemberAccess, insertionStatement);

        // Apply changes using SyntaxEditor
        var editor = new SyntaxEditor(root, document.Project.Solution.Workspace.Services);

        foreach (var occurrence in allOccurrences)
        {
            editor.ReplaceNode(occurrence,
                SyntaxFactory.IdentifierName(variableName)
                    .WithLeadingTrivia(occurrence.GetLeadingTrivia())
                    .WithTrailingTrivia(occurrence.GetTrailingTrivia()));
        }

        editor.InsertBefore(insertionStatement, variableDeclaration);

        return document.WithSyntaxRoot(editor.GetChangedRoot());
    }

    private static async Task<Document> ExtractInExpressionBodyLambdaAsync(
        Document document,
        SyntaxNode root,
        LambdaExpressionSyntax lambda,
        List<MemberAccessExpressionSyntax> allOccurrences,
        MemberAccessExpressionSyntax targetMemberAccess,
        string variableName,
        CancellationToken cancellationToken)
    {
        var expressionBody = lambda is SimpleLambdaExpressionSyntax simple
            ? simple.ExpressionBody
            : (lambda as ParenthesizedLambdaExpressionSyntax)?.ExpressionBody;

        if (expressionBody == null)
            return document;

        variableName = ResolveNameConflicts(variableName, lambda);

        // Replace occurrences in the expression body
        var newExpression = expressionBody.ReplaceNodes(
            allOccurrences.Where(o => expressionBody.Contains(o)),
            (original, _) => SyntaxFactory.IdentifierName(variableName)
                .WithLeadingTrivia(original.GetLeadingTrivia())
                .WithTrailingTrivia(original.GetTrailingTrivia()));

        // Build block body: variable declaration + return statement
        var variableDecl = SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.VariableDeclaration(
                SyntaxFactory.IdentifierName("var"),
                SyntaxFactory.SingletonSeparatedList(
                    SyntaxFactory.VariableDeclarator(variableName)
                        .WithInitializer(SyntaxFactory.EqualsValueClause(targetMemberAccess.WithoutTrivia())))));

        var returnStatement = SyntaxFactory.ReturnStatement(newExpression.WithoutLeadingTrivia())
            .WithLeadingTrivia(SyntaxFactory.LineFeed);

        var block = SyntaxFactory.Block(variableDecl, returnStatement);

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
        List<MemberAccessExpressionSyntax> occurrences,
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

    private static string GenerateVariableName(MemberAccessExpressionSyntax memberAccess)
    {
        var name = memberAccess.Name.Identifier.ValueText;

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

    private static LocalDeclarationStatementSyntax CreateVariableDeclaration(
        string variableName,
        MemberAccessExpressionSyntax expression,
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
            .WithTrailingTrivia(SyntaxFactory.LineFeed);
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
