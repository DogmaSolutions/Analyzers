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

        var displayFragment = targetExpression.ToString();
        if (displayFragment.Length > 60)
            displayFragment = displayFragment.Substring(0, 57) + "...";

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{displayFragment}' to short variable",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, targetExpression, ct, nameStyle: VariableNameStyle.Short),
                equivalenceKey: DSA019Analyzer.DiagnosticId),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{displayFragment}' to long variable",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, targetExpression, ct, nameStyle: VariableNameStyle.Long),
                equivalenceKey: DSA019Analyzer.DiagnosticId + "_Long"),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: $"Extract '{displayFragment}' to compact variable",
                createChangedDocument: ct => ExtractToVariableAsync(context.Document, targetExpression, ct, nameStyle: VariableNameStyle.Compact),
                equivalenceKey: DSA019Analyzer.DiagnosticId + "_Compact"),
            diagnostic);

        if (targetExpression is MemberAccessExpressionSyntax targetMa &&
            targetMa.Expression is MemberAccessExpressionSyntax prefixMa)
        {
            var scope = GetContainingScope(targetExpression);
            if (scope != null)
            {
                var prefixKey = NormalizeWhitespace(prefixMa.ToString());
                var targetKey = NormalizeWhitespace(targetExpression.ToString());

                var prefixCount = 0;
                var targetCount = 0;
                foreach (var ma in GetMemberAccessesInScope(scope))
                {
                    var text = NormalizeWhitespace(ma.ToString());
                    if (text == prefixKey) prefixCount++;
                    if (text == targetKey) targetCount++;
                }

                if (prefixCount > targetCount)
                {
                    var rootVarName = GenerateVariableName(prefixMa);
                    rootVarName = ResolveNameConflicts(rootVarName, scope);

                    var displayChain = prefixMa.ToString();
                    if (displayChain.Length > 60)
                        displayChain = displayChain.Substring(0, 57) + "...";

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: $"Extract {prefixCount} occurrences of '{displayChain}' into local variable '{rootVarName}'",
                            createChangedDocument: ct => ExtractToVariableAsync(context.Document, prefixMa, ct, 1),
                            equivalenceKey: DSA019Analyzer.DiagnosticId + "_root"),
                        diagnostic);
                }
            }
        }

        ReviewCommentCodeFix.Register(context, diagnostic, targetExpression, DSA019Analyzer.DiagnosticId, nameof(Resources.DSA019ReviewComment));
    }

    private static async Task<Document> ExtractToVariableAsync(
        Document document,
        ExpressionSyntax targetExpression,
        CancellationToken cancellationToken,
        int? thresholdOverride = null,
        VariableNameStyle nameStyle = VariableNameStyle.Short)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);

        var targetKey = NormalizeWhitespace(targetExpression.ToString());
        var threshold = thresholdOverride ?? DSA019Analyzer.DefaultMaxDepth;

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

        var variableName = GenerateVariableName(expressionToExtract, nameStyle);

        // Check if the scope is an expression-body lambda that needs conversion to block
        var lambdaParent = FindExpressionBodyLambda(targetExpression);
        if (lambdaParent != null)
            return ExtractInExpressionBodyLambda(document, root, lambdaParent, nodesToReplace, expressionToExtract, variableName);

        // Block body: find the insertion point (earliest statement containing an occurrence)
        var insertionStatement = FindEarliestContainingStatement(allOccurrences, scope);
        if (insertionStatement == null)
            return document;

        insertionStatement = ConstrainInsertionToLoopScope(insertionStatement, expressionToExtract, allOccurrences, semanticModel)
                             ?? insertionStatement;

        // Resolve conflicts with existing variable names
        variableName = ResolveNameConflicts(variableName, scope);

        // Build the variable declaration
        var eolTrivia = GetEndOfLineTrivia(insertionStatement);
        var variableDeclaration = CreateVariableDeclaration(variableName, expressionToExtract, insertionStatement, eolTrivia);

        var identifierName = SyntaxFactory.IdentifierName(variableName);

        var insertionAnnotation = new SyntaxAnnotation("DSA019_insertion");
        var replaceAnnotation = new SyntaxAnnotation("DSA019_replace");

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
        var containingBlock = finalInsertion.Parent as BlockSyntax;
        if (containingBlock == null)
            return document.WithSyntaxRoot(newRoot);

        var idx = containingBlock.Statements.IndexOf(finalInsertion);
        var newStatements = containingBlock.Statements.Insert(idx, variableDeclaration);
        newRoot = newRoot.ReplaceNode(containingBlock, containingBlock.WithStatements(newStatements));

        return document.WithSyntaxRoot(newRoot);
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

    private static StatementSyntax ConstrainInsertionToLoopScope(
        StatementSyntax insertionStatement,
        ExpressionSyntax expressionToExtract,
        List<ExpressionSyntax> occurrences,
        SemanticModel semanticModel)
    {
        while (insertionStatement != null)
        {
            BlockSyntax loopBody = null;
            var referencesLoopVariable = false;

            if (insertionStatement is ForEachStatementSyntax forEach)
            {
                loopBody = forEach.Statement as BlockSyntax;
                var iterVar = semanticModel.GetDeclaredSymbol(forEach);
                referencesLoopVariable = iterVar != null &&
                                         ExpressionReferencesSymbol(expressionToExtract, iterVar, semanticModel);
            }
            else if (insertionStatement is ForStatementSyntax forStmt && forStmt.Declaration != null)
            {
                loopBody = forStmt.Statement as BlockSyntax;
                foreach (var declarator in forStmt.Declaration.Variables)
                {
                    var sym = semanticModel.GetDeclaredSymbol(declarator);
                    if (sym != null && ExpressionReferencesSymbol(expressionToExtract, sym, semanticModel))
                    {
                        referencesLoopVariable = true;
                        break;
                    }
                }
            }

            if (referencesLoopVariable && loopBody != null)
            {
                var innerStatement = FindEarliestContainingStatement(occurrences, loopBody);
                if (innerStatement != null)
                {
                    insertionStatement = innerStatement;
                    continue;
                }
            }

            break;
        }

        return insertionStatement;
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

    private static string GenerateVariableName(ExpressionSyntax expression, VariableNameStyle style = VariableNameStyle.Short)
    {
        if (style == VariableNameStyle.Long)
            return GenerateLongVariableName(expression);
        if (style == VariableNameStyle.Compact)
            return GenerateCompactVariableName(expression);

        return GenerateShortVariableName(expression);
    }

    private static string GenerateShortVariableName(ExpressionSyntax expression)
    {
        string name = null;

        if (expression is InvocationExpressionSyntax invocation)
        {
            if (invocation.Expression is MemberAccessExpressionSyntax invokedMember)
                name = invokedMember.Name.Identifier.ValueText;
            else if (invocation.Expression is IdentifierNameSyntax invokedId)
                name = invokedId.Identifier.ValueText;
        }
        else if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            name = memberAccess.Name.Identifier.ValueText;
        }
        else if (expression is ElementAccessExpressionSyntax elementAccess)
        {
            var current = elementAccess.Expression;
            while (current is ElementAccessExpressionSyntax nested)
                current = nested.Expression;

            if (current is MemberAccessExpressionSyntax ma)
                name = ma.Name.Identifier.ValueText;
            else if (current is IdentifierNameSyntax id)
                name = id.Identifier.ValueText;
        }

        return SanitizeVariableName(name);
    }

    private static string GenerateLongVariableName(ExpressionSyntax expression)
    {
        var parts = CollectChainIdentifiers(expression);
        if (parts.Count == 0)
            return SanitizeVariableName(null);

        var sb = new StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            var part = parts[i];
            if (i == 0)
                sb.Append(char.ToLowerInvariant(part[0]) + part.Substring(1));
            else
                sb.Append(char.ToUpperInvariant(part[0]) + part.Substring(1));
        }

        return SanitizeVariableName(sb.ToString());
    }

    private static string GenerateCompactVariableName(ExpressionSyntax expression)
    {
        var parts = CollectChainIdentifiers(expression);
        if (parts.Count == 0)
            return SanitizeVariableName(null);

        var sb = new StringBuilder();
        for (var i = 0; i < parts.Count; i++)
        {
            var word = ExtractFirstWord(parts[i]);
            if (i == 0)
                sb.Append(char.ToLowerInvariant(word[0]) + word.Substring(1));
            else
                sb.Append(char.ToUpperInvariant(word[0]) + word.Substring(1));
        }

        return SanitizeVariableName(sb.ToString());
    }

    private static List<string> CollectChainIdentifiers(ExpressionSyntax expression)
    {
        var parts = new List<string>();
        CollectChainIdentifiersRecursive(expression, parts);
        return parts;
    }

    private static void CollectChainIdentifiersRecursive(ExpressionSyntax expression, List<string> parts)
    {
        if (expression is MemberAccessExpressionSyntax memberAccess)
        {
            CollectChainIdentifiersRecursive(memberAccess.Expression, parts);
            parts.Add(memberAccess.Name.Identifier.ValueText);
        }
        else if (expression is InvocationExpressionSyntax invocation)
        {
            CollectChainIdentifiersRecursive(invocation.Expression, parts);
        }
        else if (expression is ElementAccessExpressionSyntax elementAccess)
        {
            CollectChainIdentifiersRecursive(elementAccess.Expression, parts);
        }
        else if (expression is IdentifierNameSyntax identifier)
        {
            parts.Add(identifier.Identifier.ValueText);
        }
        else if (expression is ParenthesizedExpressionSyntax paren)
        {
            CollectChainIdentifiersRecursive(paren.Expression, parts);
        }
        else if (expression is AwaitExpressionSyntax awaitExpr)
        {
            CollectChainIdentifiersRecursive(awaitExpr.Expression, parts);
        }
        else if (expression is CastExpressionSyntax castExpr)
        {
            CollectChainIdentifiersRecursive(castExpr.Expression, parts);
        }
    }

    private static string ExtractFirstWord(string pascalCaseIdentifier)
    {
        if (string.IsNullOrEmpty(pascalCaseIdentifier))
            return pascalCaseIdentifier;

        // Handle leading underscore
        var start = 0;
        while (start < pascalCaseIdentifier.Length && pascalCaseIdentifier[start] == '_')
            start++;

        if (start >= pascalCaseIdentifier.Length)
            return pascalCaseIdentifier;

        // Find where the first word ends (next uppercase letter)
        for (var i = start + 1; i < pascalCaseIdentifier.Length; i++)
        {
            if (char.IsUpper(pascalCaseIdentifier[i]))
                return pascalCaseIdentifier.Substring(start, i - start);
        }

        return pascalCaseIdentifier.Substring(start);
    }

    private static string SanitizeVariableName(string name)
    {
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

    internal enum VariableNameStyle
    {
        Short,
        Long,
        Compact,
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
            .WithLeadingTrivia(GetIndentationTrivia(insertBeforeStatement))
            .WithTrailingTrivia(eolTrivia);
    }

    private static SyntaxTriviaList GetIndentationTrivia(SyntaxNode node)
    {
        var leading = node.GetLeadingTrivia();
        var startIndex = leading.Count;

        for (var i = leading.Count - 1; i >= 0; i--)
        {
            var kind = leading[i].Kind();
            if (kind == SyntaxKind.WhitespaceTrivia || kind == SyntaxKind.EndOfLineTrivia)
                startIndex = i;
            else
                break;
        }

        if (startIndex >= leading.Count)
            return SyntaxTriviaList.Empty;

        var result = new List<SyntaxTrivia>();
        for (var i = startIndex; i < leading.Count; i++)
            result.Add(leading[i]);

        return SyntaxFactory.TriviaList(result);
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
        for (var current = node; current != null; current = current.Parent)
        {
            foreach (var trivia in current.GetLeadingTrivia())
            {
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    return trivia;
            }

            foreach (var trivia in current.GetTrailingTrivia())
            {
                if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
                    return trivia;
            }
        }

        var firstEol = node.SyntaxTree.GetRoot().DescendantTokens()
            .SelectMany(t => t.TrailingTrivia)
            .FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (firstEol != default)
            return firstEol;

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