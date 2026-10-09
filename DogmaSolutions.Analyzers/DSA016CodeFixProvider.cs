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
/// Code fix provider for DSA016: extracts a repeated enumeration method invocation
/// into a local variable and replaces all identical calls within the same scope.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA016CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA016CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA016Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var invocation = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(inv => inv.Span == diagnosticSpan);

        if (invocation == null)
        {
            // Conditional access: the diagnostic is on a MemberBindingExpression inside a ConditionalAccess
            var memberBinding = root.FindToken(diagnosticSpan.Start).Parent?
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .FirstOrDefault(inv => inv.Expression is MemberBindingExpressionSyntax &&
                                       inv.Span.IntersectsWith(diagnosticSpan));
            if (memberBinding != null)
                invocation = memberBinding;
        }

        if (invocation == null)
            return;

        var displayFragment = invocation.ToString();
        if (displayFragment.Length > 60)
            displayFragment = displayFragment.Substring(0, 57) + "...";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{displayFragment}' to local variable",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, invocation, ct),
                equivalenceKey: DSA016Analyzer.DiagnosticId),
            diagnostic);

        ReviewCommentCodeFix.Register(context, diagnostic, invocation, DSA016Analyzer.DiagnosticId, nameof(Resources.DSA016ReviewComment));
    }

    private static async Task<Document> ExtractToVariableAsync(
        Document document,
        InvocationExpressionSyntax targetInvocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var scope = SyntaxUtils.GetContainingScope(targetInvocation);
        if (scope == null)
            return document;

        var allOccurrences = FindDuplicateInvocations(targetInvocation, scope, semanticModel);
        if (allOccurrences.Count < 2)
            return document;

        // Determine what expression to extract (the invocation itself, or the whole
        // ConditionalAccessExpression that wraps it for ?. chains)
        var (nodesToReplace, expressionToExtract) = DetermineExtractionTarget(allOccurrences);

        var variableName = GenerateVariableName(targetInvocation);

        // Check if the scope is an expression-body lambda that needs conversion to block
        var lambdaParent = FindExpressionBodyLambda(targetInvocation);
        if (lambdaParent != null)
            return ExtractInExpressionBodyLambda(document, root, lambdaParent, nodesToReplace, expressionToExtract, variableName);

        // Block body: find the insertion point (earliest statement containing an occurrence)
        var insertionStatement = FindEarliestContainingStatement(nodesToReplace, scope);
        if (insertionStatement == null)
            return document;

        insertionStatement = ConstrainInsertionToLoopScope(insertionStatement, expressionToExtract, nodesToReplace, semanticModel)
                             ?? insertionStatement;

        variableName = ResolveNameConflicts(variableName, scope);

        var eolTrivia = SyntaxUtils.GetEndOfLineTrivia(insertionStatement);
        var variableDeclaration = CreateVariableDeclaration(variableName, expressionToExtract, insertionStatement, eolTrivia);

        var identifierName = SyntaxFactory.IdentifierName(variableName);

        var insertionAnnotation = new SyntaxAnnotation("DSA016_insertion");
        var replaceAnnotation = new SyntaxAnnotation("DSA016_replace");

        var nodesToReplaceSet = new HashSet<SyntaxNode>(nodesToReplace);
        var allNodesToAnnotate = new HashSet<SyntaxNode>(nodesToReplace) { insertionStatement };

        var annotatedRoot = root.ReplaceNodes(allNodesToAnnotate, (original, rewritten) =>
        {
            var result = rewritten;
            if (original == insertionStatement)
                result = result.WithAdditionalAnnotations(insertionAnnotation);
            if (nodesToReplaceSet.Contains(original))
                result = result.WithAdditionalAnnotations(replaceAnnotation);
            return result;
        });

        var newRoot = annotatedRoot;
        SyntaxNode nodeToReplace;
        while ((nodeToReplace = newRoot.GetAnnotatedNodes(replaceAnnotation).FirstOrDefault()) != null)
        {
            newRoot = newRoot.ReplaceNode(
                nodeToReplace,
                identifierName
                    .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
                    .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia()));
        }

        var finalInsertion = newRoot.GetAnnotatedNodes(insertionAnnotation).First() as StatementSyntax;
        var containingBlock = finalInsertion?.Parent as BlockSyntax;
        if (containingBlock == null)
            return document.WithSyntaxRoot(newRoot);

        var idx = containingBlock.Statements.IndexOf(finalInsertion);
        var newStatements = containingBlock.Statements.Insert(idx, variableDeclaration);
        newRoot = newRoot.ReplaceNode(containingBlock, containingBlock.WithStatements(newStatements));

        return document.WithSyntaxRoot(newRoot);
    }

    private static List<InvocationExpressionSyntax> FindDuplicateInvocations(
        InvocationExpressionSyntax target,
        SyntaxNode scope,
        SemanticModel semanticModel)
    {
        if (!TryGetInvocationKey(target, semanticModel, out var targetKey))
            return new List<InvocationExpressionSyntax> { target };

        var results = new List<InvocationExpressionSyntax>();
        foreach (var inv in GetInvocationsInScope(scope))
        {
            if (!TryGetInvocationKey(inv, semanticModel, out var key))
                continue;

            if (key == targetKey)
                results.Add(inv);
        }

        return results;
    }

    private static bool TryGetInvocationKey(
        InvocationExpressionSyntax invocation,
        SemanticModel semanticModel,
        out string key)
    {
        key = null;
        string receiverText;
        string methodName;
        string argsText;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            receiverText = memberAccess.Expression.ToString();
            methodName = memberAccess.Name.Identifier.ValueText;
            argsText = invocation.ArgumentList.ToString();
        }
        else if (invocation.Expression is MemberBindingExpressionSyntax memberBinding)
        {
            methodName = memberBinding.Name.Identifier.ValueText;
            argsText = invocation.ArgumentList.ToString();

            var ancestor = invocation.Parent;
            while (ancestor != null)
            {
                if (ancestor is ConditionalAccessExpressionSyntax ca)
                {
                    receiverText = ca.Expression.ToString();
                    goto build;
                }

                ancestor = ancestor.Parent;
            }

            return false;
        }
        else
        {
            return false;
        }

        build:
        var symbolKey = ResolveReceiverSymbolKey(invocation, semanticModel);
        var receiver = symbolKey ?? SyntaxUtils.NormalizeWhitespace(receiverText);
        key = $"{receiver}|{methodName}|{SyntaxUtils.NormalizeWhitespace(argsText)}";
        return true;
    }

    private static string ResolveReceiverSymbolKey(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model == null)
            return null;

        ExpressionSyntax receiverExpr = null;
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            receiverExpr = memberAccess.Expression;
        }
        else if (invocation.Expression is MemberBindingExpressionSyntax)
        {
            var ancestor = invocation.Parent;
            while (ancestor != null)
            {
                if (ancestor is ConditionalAccessExpressionSyntax ca)
                {
                    receiverExpr = ca.Expression;
                    break;
                }

                ancestor = ancestor.Parent;
            }
        }

        if (receiverExpr == null)
            return null;

        return ResolveChainKey(receiverExpr, model);
    }

    private static string ResolveChainKey(ExpressionSyntax expr, SemanticModel model)
    {
        if (expr is MemberAccessExpressionSyntax memberAccess)
        {
            var leftKey = ResolveChainKey(memberAccess.Expression, model);
            if (leftKey == null)
                return null;

            return $"{leftKey}.{memberAccess.Name.Identifier.ValueText}";
        }

        var symbol = model.GetSymbolInfo(expr).Symbol;
        if (symbol == null)
            return null;

        var refs = symbol.DeclaringSyntaxReferences;
        if (refs.Length > 0)
            return $"{symbol.Name}@{refs[0].Span.Start}";

        return null;
    }

    /// <summary>
    /// Determines the extraction target. For conditional access chains (items?.Method()),
    /// the entire ConditionalAccessExpression is extracted. Otherwise the invocation itself.
    /// </summary>
    private static (List<SyntaxNode> NodesToReplace, ExpressionSyntax ExpressionToExtract)
        DetermineExtractionTarget(List<InvocationExpressionSyntax> occurrences)
    {
        // Check if all occurrences are inside ConditionalAccessExpressions (e.g., items?.FirstOrDefault(x => ...)).
        var conditionalAccesses = occurrences.Select(GetContainingConditionalAccess).ToList();

        // The first one is the expression to extract: its WhenNotNull part must end at the invocation. When the
        // conditional access has trailing access after the invocation (e.g., items?.FirstOrDefault(...)?.Name),
        // or the occurrences do not all have the same shape, we can't easily split it: fall back to extracting
        // the plain invocations.
        if (conditionalAccesses.All(ca => ca != null) &&
            conditionalAccesses[0].WhenNotNull is InvocationExpressionSyntax &&
            conditionalAccesses.Zip(occurrences, IsWholeConditionalAccessReplaceable).All(replaceable => replaceable))
        {
            return (conditionalAccesses.Cast<SyntaxNode>().ToList(), conditionalAccesses[0]);
        }

        return (occurrences.Cast<SyntaxNode>().ToList(), occurrences[0]);
    }

    /// <summary>
    /// True when the whole conditional access is the occurrence (no trailing access after the invocation), so it can be
    /// replaced by the extracted variable.
    /// </summary>
    private static bool IsWholeConditionalAccessReplaceable(ConditionalAccessExpressionSyntax conditionalAccess, InvocationExpressionSyntax occurrence)
    {
        return conditionalAccess.WhenNotNull is InvocationExpressionSyntax ||
               (conditionalAccess.WhenNotNull is MemberBindingExpressionSyntax && occurrence.Parent == conditionalAccess);
    }

    private static ConditionalAccessExpressionSyntax GetContainingConditionalAccess(InvocationExpressionSyntax invocation)
    {
        if (invocation.Expression is MemberBindingExpressionSyntax)
        {
            var ancestor = invocation.Parent;
            while (ancestor != null)
            {
                if (ancestor is ConditionalAccessExpressionSyntax ca)
                    return ca;
                ancestor = ancestor.Parent;
            }
        }

        return null;
    }

    private static string GenerateVariableName(InvocationExpressionSyntax invocation)
    {
        string name = null;

        if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
            name = memberAccess.Name.Identifier.ValueText;
        else if (invocation.Expression is MemberBindingExpressionSyntax memberBinding)
            name = memberBinding.Name.Identifier.ValueText;

        if (string.IsNullOrEmpty(name))
            name = "value";

        if (name.Length > 0 && char.IsUpper(name[0]))
            name = char.ToLowerInvariant(name[0]) + name.Substring(1);

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

        foreach (var param in scope.DescendantNodes().OfType<ParameterSyntax>())
            existingNames.Add(param.Identifier.ValueText);

        if (!existingNames.Contains(baseName))
            return baseName;

        var suffix = 1;
        while (existingNames.Contains(baseName + suffix))
            suffix++;

        return baseName + suffix;
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

        var newExpression = expressionBody.ReplaceNodes(
            nodesToReplace.Where(o => expressionBody.Contains(o)),
            (original, _) => SyntaxFactory.IdentifierName(variableName)
                .WithLeadingTrivia(original.GetLeadingTrivia())
                .WithTrailingTrivia(original.GetTrailingTrivia()));

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
        List<SyntaxNode> occurrences,
        SyntaxNode scope)
    {
        StatementSyntax earliest = null;
        var earliestStart = int.MaxValue;

        foreach (var occurrence in occurrences)
        {
            var statement = occurrence.Ancestors().OfType<StatementSyntax>()
                .LastOrDefault(s => s.Parent == scope ||
                                    (scope is MethodDeclarationSyntax method && s.Parent == method.Body));

            if (statement == null)
                statement = occurrence.Ancestors().OfType<StatementSyntax>().FirstOrDefault();

            if (statement != null && statement.SpanStart < earliestStart)
            {
                earliest = statement;
                earliestStart = statement.SpanStart;
            }
        }

        return earliest;
    }

    private static StatementSyntax ConstrainInsertionToLoopScope(
        StatementSyntax insertionStatement,
        ExpressionSyntax expressionToExtract,
        List<SyntaxNode> occurrences,
        SemanticModel semanticModel)
    {
        if (semanticModel == null)
            return insertionStatement;

        // Keep descending into nested loops as long as the expression depends on the variable of the current loop.
        StatementSyntax innerStatement;
        while ((innerStatement = FindInsertionStatementInsideLoop(insertionStatement, expressionToExtract, occurrences, semanticModel)) != null)
            insertionStatement = innerStatement;

        return insertionStatement;
    }

    /// <summary>
    /// When the statement is a loop declaring a variable used by the expression to extract, returns the earliest statement
    /// of the loop body containing an occurrence (the variable cannot be declared before the loop). Otherwise null.
    /// </summary>
    private static StatementSyntax FindInsertionStatementInsideLoop(
        StatementSyntax statement,
        ExpressionSyntax expressionToExtract,
        List<SyntaxNode> occurrences,
        SemanticModel semanticModel)
    {
        var (loopBody, loopVariables) = GetLoopBodyAndVariables(statement, semanticModel);
        if (loopBody == null || !loopVariables.Any(variable => ExpressionReferencesSymbol(expressionToExtract, variable, semanticModel)))
            return null;

        return FindEarliestContainingStatement(occurrences, loopBody);
    }

    private static (BlockSyntax Body, IEnumerable<ISymbol> Variables) GetLoopBodyAndVariables(StatementSyntax statement, SemanticModel semanticModel)
    {
        switch (statement)
        {
            case ForEachStatementSyntax forEach:
                return (forEach.Statement as BlockSyntax, new[] { semanticModel.GetDeclaredSymbol(forEach) }.Where(symbol => symbol != null));

            case ForStatementSyntax { Declaration: not null } forStatement:
                return (forStatement.Statement as BlockSyntax,
                    forStatement.Declaration.Variables.Select(declarator => semanticModel.GetDeclaredSymbol(declarator)).Where(symbol => symbol != null));

            default:
                return (null, Enumerable.Empty<ISymbol>());
        }
    }

    private static bool ExpressionReferencesSymbol(
        ExpressionSyntax expression,
        ISymbol symbol,
        SemanticModel semanticModel)
    {
        foreach (var id in expression.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
        {
            var refSymbol = semanticModel.GetSymbolInfo(id).Symbol;
            if (SymbolEqualityComparer.Default.Equals(refSymbol, symbol))
                return true;
        }

        return false;
    }

    private static IEnumerable<InvocationExpressionSyntax> GetInvocationsInScope(SyntaxNode scope)
    {
        return scope.DescendantNodes(n => !SyntaxUtils.IsNestedScope(n))
            .OfType<InvocationExpressionSyntax>();
    }

    private static LocalDeclarationStatementSyntax CreateVariableDeclaration(
        string variableName,
        ExpressionSyntax expression,
        StatementSyntax insertBeforeStatement,
        SyntaxTrivia eolTrivia)
    {
        var eolStr = eolTrivia.ToFullString();
        if (string.IsNullOrEmpty(eolStr))
            eolStr = "\r\n";

        return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(variableName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                expression.WithoutTrivia())))))
            .NormalizeWhitespace(eol: eolStr)
            .WithLeadingTrivia(SyntaxUtils.GetIndentationTrivia(insertBeforeStatement))
            .WithTrailingTrivia(eolTrivia);
    }

    private static string GetEndOfLineString(SyntaxNode node)
    {
        foreach (var trivia in node.DescendantTrivia())
        {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                return trivia.ToString();
        }

        return "\n";
    }

}
