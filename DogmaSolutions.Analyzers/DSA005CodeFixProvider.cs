using System;
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
    private static readonly string[] StartKeywords = { "start", "begin", "init" };
    private static readonly string[] EndKeywords = { "stop", "finish", "end", "complete" };

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

        var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        if (semanticModel != null)
        {
            var pairs = FindElapsedTimePairs(method.Body, semanticModel);
            if (pairs.Count > 0)
            {
                var dateTimeProp = pairs[0].DateTimeProperty;
                var typeName = pairs[0].TypeName;
                var title = dateTimeProp == "UtcNow"
                    ? $"Replace {typeName}.UtcNow with Stopwatch"
                    : $"Replace {typeName}.Now with Stopwatch";

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: title,
                        createChangedDocument: ct => ReplaceWithStopwatchAsync(context.Document, method, ct),
                        equivalenceKey: DSA005Analyzer.DiagnosticId + "_Stopwatch"),
                    diagnostic);
            }
        }

        ReviewCommentCodeFix.Register(context, diagnostic, node, DSA005Analyzer.DiagnosticId, nameof(Resources.DSA005ReviewComment));
    }

    #region Extract to variable (existing fix)

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

    #endregion

    #region Replace with Stopwatch

    private static async Task<Document> ReplaceWithStopwatchAsync(
        Document document,
        MethodDeclarationSyntax method,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || method.Body == null)
            return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null)
            return document;

        var pairs = FindElapsedTimePairs(method.Body, semanticModel);
        if (pairs.Count == 0)
            return document;

        var existingNames = CollectExistingNames(method);

        var removeAnnotation = new SyntaxAnnotation("DSA005_remove");
        var replacements = new Dictionary<SyntaxNode, SyntaxNode>();
        var replaceAnnotations = new List<(SyntaxAnnotation Annotation, string ElapsedVarName, string StartVarName)>();

        foreach (var pair in pairs)
        {
            replacements[pair.StartInitializerExpression] = CreateStopwatchStartNewExpression();

            bool singleAssigned = pair.Subtractions.Count == 1 && pair.Subtractions[0].IsAssignedToVariable;

            if (singleAssigned)
            {
                replacements[pair.Subtractions[0].Expression] = CreateElapsedAccessExpression(pair.StartVarName);
                replacements[pair.EndStatement] = pair.EndStatement.WithAdditionalAnnotations(removeAnnotation);
            }
            else
            {
                var elapsedVarName = DeriveElapsedVariableName(pair.StartVarName, existingNames);
                existingNames.Add(elapsedVarName);

                var annotation = new SyntaxAnnotation("DSA005_elapsed_" + elapsedVarName);
                replaceAnnotations.Add((annotation, elapsedVarName, pair.StartVarName));
                replacements[pair.EndStatement] = pair.EndStatement.WithAdditionalAnnotations(annotation);

                foreach (var sub in pair.Subtractions)
                    replacements[sub.Expression] = SyntaxFactory.IdentifierName(elapsedVarName);
            }
        }

        var newRoot = root.ReplaceNodes(replacements.Keys, (original, rewritten) =>
        {
            var replacement = replacements[original];
            return replacement
                .WithLeadingTrivia(rewritten.GetLeadingTrivia())
                .WithTrailingTrivia(rewritten.GetTrailingTrivia());
        });

        while (true)
        {
            var nodeToRemove = newRoot.GetAnnotatedNodes(removeAnnotation).FirstOrDefault();
            if (nodeToRemove == null)
                break;
            newRoot = newRoot.RemoveNode(nodeToRemove, SyntaxRemoveOptions.KeepNoTrivia);
        }

        foreach (var entry in replaceAnnotations)
        {
            var annotatedNode = newRoot.GetAnnotatedNodes(entry.Annotation).FirstOrDefault();
            if (annotatedNode == null)
                continue;

            var newDecl = CreateElapsedVarDeclaration(entry.ElapsedVarName, entry.StartVarName)
                .WithLeadingTrivia(annotatedNode.GetLeadingTrivia())
                .WithTrailingTrivia(annotatedNode.GetTrailingTrivia());
            newRoot = newRoot.ReplaceNode(annotatedNode, newDecl);
        }

        newRoot = EnsureUsingDirective(newRoot, "System.Diagnostics");

        return document.WithSyntaxRoot(newRoot);
    }

    private static List<ElapsedTimePairInfo> FindElapsedTimePairs(BlockSyntax body, SemanticModel model)
    {
        var dateTimeVars = new List<DateTimeVarInfo>();

        foreach (var localDecl in body.DescendantNodes().OfType<LocalDeclarationStatementSyntax>())
        {
            foreach (var declarator in localDecl.Declaration.Variables)
            {
                if (declarator.Initializer?.Value is MemberAccessExpressionSyntax memberAccess &&
                    memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "DateTime" or "DateTimeOffset" } typeSyntax &&
                    memberAccess.Name?.Identifier.ValueText is "Now" or "UtcNow")
                {
                    dateTimeVars.Add(new DateTimeVarInfo(
                        declarator,
                        localDecl,
                        memberAccess,
                        memberAccess.Name.Identifier.ValueText,
                        typeSyntax.Identifier.ValueText));
                }
            }
        }

        var pairs = new List<ElapsedTimePairInfo>();
        var usedDeclarators = new HashSet<VariableDeclaratorSyntax>();

        foreach (var startVar in dateTimeVars.Where(v => ContainsStartKeyword(v.Declarator.Identifier.ValueText)))
        {
            if (usedDeclarators.Contains(startVar.Declarator))
                continue;

            foreach (var endVar in dateTimeVars.Where(v => ContainsEndKeyword(v.Declarator.Identifier.ValueText)))
            {
                if (usedDeclarators.Contains(endVar.Declarator))
                    continue;

                if (startVar.DateTimeProperty != endVar.DateTimeProperty || startVar.TypeName != endVar.TypeName)
                    continue;
                if (startVar.Declarator == endVar.Declarator)
                    continue;

                var subtractions = FindSubtractionExpressions(body, startVar.Declarator, endVar.Declarator, model);
                if (subtractions.Count == 0)
                    continue;

                if (!AreVariablesOnlyUsedInSubtractions(body, startVar.Declarator, endVar.Declarator, subtractions, model))
                    continue;

                usedDeclarators.Add(startVar.Declarator);
                usedDeclarators.Add(endVar.Declarator);

                pairs.Add(new ElapsedTimePairInfo(
                    startVar.Declarator,
                    endVar.Declarator,
                    endVar.Statement,
                    startVar.InitializerExpression,
                    startVar.Declarator.Identifier.ValueText,
                    endVar.Declarator.Identifier.ValueText,
                    startVar.DateTimeProperty,
                    startVar.TypeName,
                    subtractions));

                break;
            }
        }

        return pairs;
    }

    private static List<SubtractionMatch> FindSubtractionExpressions(
        BlockSyntax body,
        VariableDeclaratorSyntax startDeclarator,
        VariableDeclaratorSyntax endDeclarator,
        SemanticModel model)
    {
        var startSymbol = model.GetDeclaredSymbol(startDeclarator);
        var endSymbol = model.GetDeclaredSymbol(endDeclarator);
        if (startSymbol == null || endSymbol == null)
            return [];

        var result = new List<SubtractionMatch>();

        foreach (var binary in body.DescendantNodes().OfType<BinaryExpressionSyntax>()
                     .Where(b => b.IsKind(SyntaxKind.SubtractExpression)))
        {
            var leftSymbol = model.GetSymbolInfo(binary.Left).Symbol;
            var rightSymbol = model.GetSymbolInfo(binary.Right).Symbol;

            if (SymbolEqualityComparer.Default.Equals(leftSymbol, endSymbol) &&
                SymbolEqualityComparer.Default.Equals(rightSymbol, startSymbol))
            {
                bool isAssigned = binary.Parent is EqualsValueClauseSyntax evc &&
                                  evc.Parent is VariableDeclaratorSyntax;
                result.Add(new SubtractionMatch(binary, isAssigned));
            }
        }

        return result;
    }

    private static bool AreVariablesOnlyUsedInSubtractions(
        BlockSyntax body,
        VariableDeclaratorSyntax startDeclarator,
        VariableDeclaratorSyntax endDeclarator,
        List<SubtractionMatch> subtractions,
        SemanticModel model)
    {
        var startSymbol = model.GetDeclaredSymbol(startDeclarator);
        var endSymbol = model.GetDeclaredSymbol(endDeclarator);
        if (startSymbol == null || endSymbol == null)
            return false;

        var subtractionSpans = subtractions.Select(s => s.Expression.Span).ToList();

        foreach (var identifier in body.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            var symbol = model.GetSymbolInfo(identifier).Symbol;
            if (!SymbolEqualityComparer.Default.Equals(symbol, startSymbol) &&
                !SymbolEqualityComparer.Default.Equals(symbol, endSymbol))
                continue;

            bool insideSubtraction = subtractionSpans.Any(span => span.Contains(identifier.Span));
            if (!insideSubtraction)
                return false;
        }

        return true;
    }

    private static ExpressionSyntax CreateStopwatchStartNewExpression()
    {
        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Stopwatch"),
                SyntaxFactory.IdentifierName("StartNew")));
    }

    private static ExpressionSyntax CreateElapsedAccessExpression(string stopwatchVarName)
    {
        return SyntaxFactory.MemberAccessExpression(
            SyntaxKind.SimpleMemberAccessExpression,
            SyntaxFactory.IdentifierName(stopwatchVarName),
            SyntaxFactory.IdentifierName("Elapsed"));
    }

    private static LocalDeclarationStatementSyntax CreateElapsedVarDeclaration(
        string elapsedVarName, string startVarName)
    {
        return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.IdentifierName("var"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(elapsedVarName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(
                                CreateElapsedAccessExpression(startVarName))))))
            .NormalizeWhitespace();
    }

    private static string DeriveElapsedVariableName(string startVarName, HashSet<string> existingNames)
    {
        var lower = startVarName.ToLowerInvariant();

        foreach (var keyword in StartKeywords)
        {
            var searchIdx = 0;
            while ((searchIdx = lower.IndexOf(keyword, searchIdx, StringComparison.Ordinal)) >= 0)
            {
                var endIdx = searchIdx + keyword.Length;
                var leftBoundary = searchIdx == 0 || startVarName[searchIdx - 1] == '_' || char.IsUpper(startVarName[searchIdx]);
                var rightBoundary = endIdx == startVarName.Length || startVarName[endIdx] == '_' || char.IsUpper(startVarName[endIdx]);

                if (leftBoundary && rightBoundary)
                {
                    var replacement = char.IsUpper(startVarName[searchIdx]) ? "Elapsed" : "elapsed";
                    var derived = startVarName.Substring(0, searchIdx) + replacement + startVarName.Substring(endIdx);
                    return ResolveNameConflicts(derived, existingNames);
                }

                searchIdx++;
            }
        }

        return ResolveNameConflicts(startVarName + "Elapsed", existingNames);
    }

    private static SyntaxNode EnsureUsingDirective(SyntaxNode root, string namespaceName)
    {
        if (root is not CompilationUnitSyntax compilationUnit)
            return root;

        if (compilationUnit.Usings.Any(u => u.Name?.ToString() == namespaceName))
            return root;

        var parts = namespaceName.Split('.');
        NameSyntax name = SyntaxFactory.IdentifierName(parts[0]);
        for (var i = 1; i < parts.Length; i++)
            name = SyntaxFactory.QualifiedName(name, SyntaxFactory.IdentifierName(parts[i]));

        var eol = GetEndOfLineTrivia(compilationUnit);
        var usingDirective = SyntaxFactory.UsingDirective(name)
            .NormalizeWhitespace()
            .WithTrailingTrivia(eol);

        return compilationUnit.AddUsings(usingDirective);
    }

    #endregion

    #region Shared helpers

    private static bool ContainsStartKeyword(string name)
    {
        return StartKeywords.Any(k => ContainsKeywordAtWordBoundary(name, k));
    }

    private static bool ContainsEndKeyword(string name)
    {
        return EndKeywords.Any(k => ContainsKeywordAtWordBoundary(name, k));
    }

    private static bool ContainsKeywordAtWordBoundary(string name, string keyword)
    {
        var lower = name.ToLowerInvariant();
        var idx = 0;
        while ((idx = lower.IndexOf(keyword, idx, StringComparison.Ordinal)) >= 0)
        {
            var endIdx = idx + keyword.Length;
            var leftBoundary = idx == 0 || name[idx - 1] == '_' || char.IsUpper(name[idx]);
            var rightBoundary = endIdx == name.Length || name[endIdx] == '_' || char.IsUpper(name[endIdx]);

            if (leftBoundary && rightBoundary)
                return true;

            idx++;
        }

        return false;
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

    #endregion

    #region Data types

    private sealed class DateTimeVarInfo
    {
        public DateTimeVarInfo(
            VariableDeclaratorSyntax declarator,
            LocalDeclarationStatementSyntax statement,
            MemberAccessExpressionSyntax initializerExpression,
            string dateTimeProperty,
            string typeName)
        {
            Declarator = declarator;
            Statement = statement;
            InitializerExpression = initializerExpression;
            DateTimeProperty = dateTimeProperty;
            TypeName = typeName;
        }

        public VariableDeclaratorSyntax Declarator { get; }
        public LocalDeclarationStatementSyntax Statement { get; }
        public MemberAccessExpressionSyntax InitializerExpression { get; }
        public string DateTimeProperty { get; }
        public string TypeName { get; }
    }

    private sealed class ElapsedTimePairInfo
    {
        public ElapsedTimePairInfo(
            VariableDeclaratorSyntax startDeclarator,
            VariableDeclaratorSyntax endDeclarator,
            LocalDeclarationStatementSyntax endStatement,
            MemberAccessExpressionSyntax startInitializerExpression,
            string startVarName,
            string endVarName,
            string dateTimeProperty,
            string typeName,
            List<SubtractionMatch> subtractions)
        {
            StartDeclarator = startDeclarator;
            EndDeclarator = endDeclarator;
            EndStatement = endStatement;
            StartInitializerExpression = startInitializerExpression;
            StartVarName = startVarName;
            EndVarName = endVarName;
            DateTimeProperty = dateTimeProperty;
            TypeName = typeName;
            Subtractions = subtractions;
        }

        public VariableDeclaratorSyntax StartDeclarator { get; }
        public VariableDeclaratorSyntax EndDeclarator { get; }
        public LocalDeclarationStatementSyntax EndStatement { get; }
        public MemberAccessExpressionSyntax StartInitializerExpression { get; }
        public string StartVarName { get; }
        public string EndVarName { get; }
        public string DateTimeProperty { get; }
        public string TypeName { get; }
        public List<SubtractionMatch> Subtractions { get; }
    }

    private sealed class SubtractionMatch
    {
        public SubtractionMatch(BinaryExpressionSyntax expression, bool isAssignedToVariable)
        {
            Expression = expression;
            IsAssignedToVariable = isAssignedToVariable;
        }

        public BinaryExpressionSyntax Expression { get; }
        public bool IsAssignedToVariable { get; }
    }

    #endregion
}
