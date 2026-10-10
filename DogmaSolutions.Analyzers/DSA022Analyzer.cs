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

            if (IsInsideNestedLoop(binExpr, body))
                continue;

            if (!IsInvariant(binExpr, modifiedSymbols, context.SemanticModel))
                continue;

            if (IsCompileTimeConstant(binExpr))
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

    internal static HashSet<ISymbol> CollectModifiedSymbols(StatementSyntax loopBody, SyntaxNode loopNode, SemanticModel model)
    {
        var symbols = GetLoopHeaderSymbols(loopNode, model)
            .Concat(loopBody is ForEachStatementSyntax bodyForEach
                ? new[] { model.GetDeclaredSymbol(bodyForEach) } // stacked loops without braces: the body IS the nested loop, not a descendant
                : Array.Empty<ISymbol>())
            .Concat(loopBody.DescendantNodes().Select(node => GetModifiedSymbol(node, model)));

        return new HashSet<ISymbol>(symbols.Where(symbol => symbol != null), SymbolEqualityComparer.Default);
    }

    private static IEnumerable<ISymbol> GetMutatedSymbols(SyntaxNode expression, SemanticModel model)
    {
        return expression.DescendantNodesAndSelf().Select(node => GetMutationTargetSymbol(node, model));
    }

    /// <summary>
    /// The symbols declared or modified by the header of the loop: the variables of a for / foreach, and what
    /// the incrementors of a for assign.
    /// </summary>
    private static IEnumerable<ISymbol> GetLoopHeaderSymbols(SyntaxNode loopNode, SemanticModel model)
    {
        switch (loopNode)
        {
            case ForStatementSyntax forStmt:
                var declared = forStmt.Declaration?.Variables.Select(variable => model.GetDeclaredSymbol(variable))
                               ?? Enumerable.Empty<ISymbol>();
                var incremented = forStmt.Incrementors.SelectMany(incrementor => GetMutatedSymbols(incrementor, model));
                var conditioned = forStmt.Condition != null ? GetMutatedSymbols(forStmt.Condition, model) : Enumerable.Empty<ISymbol>();
                return declared.Concat(incremented).Concat(conditioned);

            case WhileStatementSyntax whileStmt:
                return GetMutatedSymbols(whileStmt.Condition, model);

            case DoStatementSyntax doStmt:
                return GetMutatedSymbols(doStmt.Condition, model);

            case ForEachStatementSyntax forEachStmt:
                return new[] { model.GetDeclaredSymbol(forEachStmt) };

            case ForEachVariableStatementSyntax forEachVarStmt:
                return forEachVarStmt.Variable.DescendantNodesAndSelf().OfType<SingleVariableDesignationSyntax>()
                    .Select(designation => model.GetDeclaredSymbol(designation));

            default:
                return Enumerable.Empty<ISymbol>();
        }
    }

    /// <summary>
    /// The symbol a node of the loop body declares or modifies (null if none): a local, a deconstruction variable, a
    /// nested foreach variable, an assigned / incremented / decremented symbol, or a ref / out argument.
    /// </summary>
    private static ISymbol GetModifiedSymbol(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case VariableDeclaratorSyntax localDecl:
                return model.GetDeclaredSymbol(localDecl);

            case SingleVariableDesignationSyntax designation:
                return model.GetDeclaredSymbol(designation);

            case ForEachStatementSyntax nestedForEach:
                return model.GetDeclaredSymbol(nestedForEach);

            case ArgumentSyntax argument when !argument.RefOrOutKeyword.IsKind(SyntaxKind.None):
                return model.GetSymbolInfo(argument.Expression).Symbol;

            default:
                return GetMutationTargetSymbol(node, model);
        }
    }

    /// <summary>
    /// The symbol assigned by an assignment, or incremented / decremented by a ++ / -- (prefix or postfix); null otherwise.
    /// </summary>
    private static ISymbol GetMutationTargetSymbol(SyntaxNode node, SemanticModel model)
    {
        switch (node)
        {
            case AssignmentExpressionSyntax assignment:
                return model.GetSymbolInfo(assignment.Left).Symbol;

            case PrefixUnaryExpressionSyntax prefix
                when prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression):
                return model.GetSymbolInfo(prefix.Operand).Symbol;

            case PostfixUnaryExpressionSyntax postfix
                when postfix.IsKind(SyntaxKind.PostIncrementExpression) || postfix.IsKind(SyntaxKind.PostDecrementExpression):
                return model.GetSymbolInfo(postfix.Operand).Symbol;

            default:
                return null;
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

    private static bool IsCompileTimeConstant(ExpressionSyntax expr)
    {
        foreach (var node in expr.DescendantNodesAndSelf())
        {
            if (node is IdentifierNameSyntax)
                return false;
        }

        return true;
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