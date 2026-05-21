using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA022Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA022";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA022AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA022AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA022AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA022.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    private static readonly SyntaxKind[] ArithmeticAndBitwiseKinds =
    {
        SyntaxKind.AddExpression,
        SyntaxKind.SubtractExpression,
        SyntaxKind.MultiplyExpression,
        SyntaxKind.DivideExpression,
        SyntaxKind.ModuloExpression,
        SyntaxKind.LeftShiftExpression,
        SyntaxKind.RightShiftExpression,
        SyntaxKind.BitwiseAndExpression,
        SyntaxKind.BitwiseOrExpression,
        SyntaxKind.ExclusiveOrExpression,
    };

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeLoop,
            SyntaxKind.ForStatement,
            SyntaxKind.ForEachStatement,
            SyntaxKind.ForEachVariableStatement,
            SyntaxKind.WhileStatement,
            SyntaxKind.DoStatement);
    }

    private static void AnalyzeLoop(SyntaxNodeAnalysisContext context)
    {
        var loopNode = context.Node;
        var body = GetLoopBody(loopNode);
        if (body == null)
            return;

        var modifiedSymbols = CollectModifiedSymbols(body, loopNode, context.SemanticModel);

        var candidates = new List<BinaryExpressionSyntax>();
        foreach (var binExpr in body.DescendantNodes().OfType<BinaryExpressionSyntax>())
        {
            if (Array.IndexOf(ArithmeticAndBitwiseKinds, binExpr.Kind()) < 0)
                continue;

            if (binExpr.IsKind(SyntaxKind.AddExpression) && IsStringConcatenation(binExpr, context.SemanticModel))
                continue;

            if (IsLoopControlExpression(binExpr, loopNode))
                continue;

            if (IsInsideNestedLoop(binExpr, body))
                continue;

            if (!IsInvariant(binExpr, modifiedSymbols, context.SemanticModel))
                continue;

            candidates.Add(binExpr);
        }

        var outermost = FilterToOutermost(candidates);

        foreach (var expr in outermost)
        {
            var diagnostic = Diagnostic.Create(
                descriptor: _rule,
                location: expr.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                expr.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }

    private static StatementSyntax GetLoopBody(SyntaxNode loopNode)
    {
        switch (loopNode)
        {
            case ForStatementSyntax forStmt: return forStmt.Statement;
            case ForEachStatementSyntax forEachStmt: return forEachStmt.Statement;
            case ForEachVariableStatementSyntax forEachVarStmt: return forEachVarStmt.Statement;
            case WhileStatementSyntax whileStmt: return whileStmt.Statement;
            case DoStatementSyntax doStmt: return doStmt.Statement;
            default: return null;
        }
    }

    private static HashSet<ISymbol> CollectModifiedSymbols(StatementSyntax loopBody, SyntaxNode loopNode, SemanticModel model)
    {
        var modified = new HashSet<ISymbol>(SymbolEqualityComparer.Default);

        if (loopNode is ForStatementSyntax forStmt)
        {
            if (forStmt.Declaration != null)
            {
                foreach (var variable in forStmt.Declaration.Variables)
                {
                    var symbol = model.GetDeclaredSymbol(variable);
                    if (symbol != null)
                        modified.Add(symbol);
                }
            }

            foreach (var incrementor in forStmt.Incrementors)
                CollectAssignmentTargets(incrementor, modified, model);
        }

        if (loopNode is ForEachStatementSyntax forEachStmt)
        {
            var symbol = model.GetDeclaredSymbol(forEachStmt);
            if (symbol != null)
                modified.Add(symbol);
        }

        if (loopNode is ForEachVariableStatementSyntax forEachVarStmt)
        {
            foreach (var designation in forEachVarStmt.Variable.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>())
            {
                var symbol = model.GetDeclaredSymbol(designation);
                if (symbol != null)
                    modified.Add(symbol);
            }
        }

        // Handle stacked loops without braces: the body IS the nested loop, not a descendant
        if (loopBody is ForEachStatementSyntax bodyForEach)
        {
            var sym = model.GetDeclaredSymbol(bodyForEach);
            if (sym != null)
                modified.Add(sym);
        }

        foreach (var node in loopBody.DescendantNodes())
        {
            ISymbol symbol;
            switch (node)
            {
                case VariableDeclaratorSyntax localDecl:
                    symbol = model.GetDeclaredSymbol(localDecl);
                    if (symbol != null)
                        modified.Add(symbol);
                    break;

                case SingleVariableDesignationSyntax designation:
                    symbol = model.GetDeclaredSymbol(designation);
                    if (symbol != null)
                        modified.Add(symbol);
                    break;

                case ForEachStatementSyntax nestedForEach:
                    symbol = model.GetDeclaredSymbol(nestedForEach);
                    if (symbol != null)
                        modified.Add(symbol);
                    break;

                case AssignmentExpressionSyntax assignment:
                    symbol = model.GetSymbolInfo(assignment.Left).Symbol;
                    if (symbol != null)
                        modified.Add(symbol);
                    break;

                case PrefixUnaryExpressionSyntax prefix
                    when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression):
                    symbol = model.GetSymbolInfo(prefix.Operand).Symbol;
                    if (symbol != null)
                        modified.Add(symbol);
                    break;

                case PostfixUnaryExpressionSyntax postfix
                    when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression):
                    symbol = model.GetSymbolInfo(postfix.Operand).Symbol;
                    if (symbol != null)
                        modified.Add(symbol);
                    break;

                case ArgumentSyntax argument when !argument.RefOrOutKeyword.IsKind(SyntaxKind.None):
                    symbol = model.GetSymbolInfo(argument.Expression).Symbol;
                    if (symbol != null)
                        modified.Add(symbol);
                    break;
            }
        }

        return modified;
    }

    private static void CollectAssignmentTargets(ExpressionSyntax expression, HashSet<ISymbol> modified, SemanticModel model)
    {
        foreach (var node in expression.DescendantNodesAndSelf())
        {
            ISymbol symbol = null;

            if (node is AssignmentExpressionSyntax assignment)
                symbol = model.GetSymbolInfo(assignment.Left).Symbol;
            else if (node is PrefixUnaryExpressionSyntax prefix &&
                     (prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression)))
                symbol = model.GetSymbolInfo(prefix.Operand).Symbol;
            else if (node is PostfixUnaryExpressionSyntax postfix &&
                     (postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression)))
                symbol = model.GetSymbolInfo(postfix.Operand).Symbol;

            if (symbol != null)
                modified.Add(symbol);
        }
    }

    internal static bool IsInvariant(ExpressionSyntax expr, HashSet<ISymbol> modifiedSymbols, SemanticModel model)
    {
        foreach (var node in expr.DescendantNodesAndSelf())
        {
            switch (node)
            {
                case InvocationExpressionSyntax:
                case ObjectCreationExpressionSyntax:
                case ElementAccessExpressionSyntax:
                case MemberAccessExpressionSyntax:
                case AwaitExpressionSyntax:
                    return false;

                case IdentifierNameSyntax identifier:
                    var symbol = model.GetSymbolInfo(identifier).Symbol;
                    if (symbol == null)
                        return false;

                    if (modifiedSymbols.Contains(symbol))
                        return false;

                    if (!(symbol is ILocalSymbol) && !(symbol is IParameterSymbol) && !(symbol is IFieldSymbol { IsConst: true }))
                        return false;
                    break;
            }
        }

        return true;
    }

    private static bool IsLoopControlExpression(SyntaxNode expr, SyntaxNode loopNode)
    {
        if (loopNode is ForStatementSyntax forStmt)
        {
            if (forStmt.Condition != null && forStmt.Condition.Contains(expr))
                return true;

            foreach (var inc in forStmt.Incrementors)
            {
                if (inc.Contains(expr))
                    return true;
            }

            if (forStmt.Declaration != null && forStmt.Declaration.Contains(expr))
                return true;
        }

        if (loopNode is WhileStatementSyntax whileStmt && whileStmt.Condition.Contains(expr))
            return true;

        if (loopNode is DoStatementSyntax doStmt && doStmt.Condition.Contains(expr))
            return true;

        return false;
    }

    private static bool IsInsideNestedLoop(SyntaxNode expr, StatementSyntax loopBody)
    {
        var current = expr.Parent;
        while (current != null && current != loopBody)
        {
            if (current is ForStatementSyntax || current is ForEachStatementSyntax ||
                current is ForEachVariableStatementSyntax ||
                current is WhileStatementSyntax || current is DoStatementSyntax)
                return true;
            current = current.Parent;
        }

        return false;
    }

    private static bool IsStringConcatenation(BinaryExpressionSyntax expr, SemanticModel model)
    {
        var typeInfo = model.GetTypeInfo(expr);
        return typeInfo.Type?.SpecialType == SpecialType.System_String;
    }

    private static List<BinaryExpressionSyntax> FilterToOutermost(List<BinaryExpressionSyntax> candidates)
    {
        var result = new List<BinaryExpressionSyntax>();
        foreach (var candidate in candidates)
        {
            var isChild = false;
            foreach (var other in candidates)
            {
                if (!ReferenceEquals(candidate, other) && other.Contains(candidate))
                {
                    isChild = true;
                    break;
                }
            }

            if (!isChild)
                result.Add(candidate);
        }

        return result;
    }
}