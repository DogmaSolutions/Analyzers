using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Avoid "if not exists, then insert" check-then-act antipattern (TOCTOU)
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA012Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA012";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA012AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA012AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA012AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Design;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA012");

    private static readonly string[] BooleanExistenceMethods = { "Any", "AnyAsync", "Exists", "ExistsAsync", "Contains", "ContainsAsync" };
    private static readonly string[] CountMethods = { "Count", "CountAsync" };
    private static readonly string[] FindMethods = { "FirstOrDefault", "FirstOrDefaultAsync", "SingleOrDefault", "SingleOrDefaultAsync", "Find", "FindAsync" };
    private static readonly string[] InsertMethods = { "Add", "AddAsync", "AddRange", "AddRangeAsync" };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        // Pattern A: if (!collection.Any(...)) { collection.Add(...); }
        if (IsNegatedExistenceCheck(ifStatement.Condition))
        {
            if (ContainsInsertInvocation(ifStatement.Statement))
            {
                ReportDiagnostic(context, ifStatement);
                return;
            }
        }

        if (IsPositiveExistenceCheck(ifStatement.Condition))
        {
            // Pattern B: if (collection.Any(...)) { throw; } ... collection.Add(...);
            if (ContainsThrowStatement(ifStatement.Statement) && ifStatement.Else == null)
            {
                if (HasSubsequentInsertInvocation(ifStatement))
                {
                    ReportDiagnostic(context, ifStatement);
                    return;
                }
            }

            // Pattern C: if (collection.Any(...)) { ... } else { collection.Add(...); }
            if (ifStatement.Else != null && ContainsInsertInvocation(ifStatement.Else.Statement))
            {
                ReportDiagnostic(context, ifStatement);
            }
        }
    }

    private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, IfStatementSyntax ifStatement)
    {
        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: ifStatement.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null);
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsNegatedExistenceCheck(ExpressionSyntax condition)
    {
        // Handle: !collection.Any(...)
        if (condition is PrefixUnaryExpressionSyntax prefixUnary &&
            prefixUnary.IsKind(SyntaxKind.LogicalNotExpression) &&
            IsExistenceCheckInvocation(prefixUnary.Operand, BooleanExistenceMethods))
        {
            return true;
        }

        if (condition is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.EqualsExpression))
        {
            // Handle: collection.Any(...) == false
            if ((IsExistenceCheckInvocation(binary.Left, BooleanExistenceMethods) && IsFalseLiteral(binary.Right)) ||
                (IsExistenceCheckInvocation(binary.Right, BooleanExistenceMethods) && IsFalseLiteral(binary.Left)))
                return true;

            // Handle: collection.Count(...) == 0
            if ((IsExistenceCheckInvocation(binary.Left, CountMethods) && IsZeroLiteral(binary.Right)) ||
                (IsExistenceCheckInvocation(binary.Right, CountMethods) && IsZeroLiteral(binary.Left)))
                return true;

            // Handle: collection.FirstOrDefault(...) == null
            if ((IsExistenceCheckInvocation(binary.Left, FindMethods) && IsNullLiteral(binary.Right)) ||
                (IsExistenceCheckInvocation(binary.Right, FindMethods) && IsNullLiteral(binary.Left)))
                return true;
        }

        return false;
    }

    private static bool IsPositiveExistenceCheck(ExpressionSyntax condition)
    {
        // Handle: collection.Any(...)
        if (IsExistenceCheckInvocation(condition, BooleanExistenceMethods))
            return true;

        if (condition is BinaryExpressionSyntax binary)
        {
            // Handle: collection.Count(...) > 0
            if (binary.IsKind(SyntaxKind.GreaterThanExpression) &&
                IsExistenceCheckInvocation(binary.Left, CountMethods) && IsZeroLiteral(binary.Right))
                return true;

            // Handle: 0 < collection.Count(...)
            if (binary.IsKind(SyntaxKind.LessThanExpression) &&
                IsZeroLiteral(binary.Left) && IsExistenceCheckInvocation(binary.Right, CountMethods))
                return true;

            // Handle: collection.Count(...) != 0
            if (binary.IsKind(SyntaxKind.NotEqualsExpression))
            {
                if ((IsExistenceCheckInvocation(binary.Left, CountMethods) && IsZeroLiteral(binary.Right)) ||
                    (IsExistenceCheckInvocation(binary.Right, CountMethods) && IsZeroLiteral(binary.Left)))
                    return true;

                // Handle: collection.FirstOrDefault(...) != null
                if ((IsExistenceCheckInvocation(binary.Left, FindMethods) && IsNullLiteral(binary.Right)) ||
                    (IsExistenceCheckInvocation(binary.Right, FindMethods) && IsNullLiteral(binary.Left)))
                    return true;
            }
        }

        return false;
    }

    private static bool IsExistenceCheckInvocation(ExpressionSyntax expression, string[] methodNames)
    {
        // Handle await expressions
        if (expression is AwaitExpressionSyntax awaitExpression)
            expression = awaitExpression.Expression;

        // Handle parenthesized expressions
        while (expression is ParenthesizedExpressionSyntax paren)
            expression = paren.Expression;

        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            return Array.IndexOf(methodNames, methodName) >= 0;
        }

        return false;
    }

    private static bool ContainsInsertInvocation(SyntaxNode node)
    {
        return node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv => inv.Expression is MemberAccessExpressionSyntax memberAccess &&
                        Array.IndexOf(InsertMethods, memberAccess.Name.Identifier.ValueText) >= 0);
    }

    private static bool ContainsThrowStatement(StatementSyntax statement)
    {
        if (statement is ThrowStatementSyntax)
            return true;

        if (statement is BlockSyntax block)
            return block.Statements.Any(s => s is ThrowStatementSyntax);

        return false;
    }

    private static bool HasSubsequentInsertInvocation(IfStatementSyntax ifStatement)
    {
        if (ifStatement.Parent is not BlockSyntax block)
            return false;

        var ifIndex = block.Statements.IndexOf(ifStatement);
        if (ifIndex < 0)
            return false;

        for (var i = ifIndex + 1; i < block.Statements.Count; i++)
        {
            if (ContainsInsertInvocation(block.Statements[i]))
                return true;
        }

        return false;
    }

    private static bool IsFalseLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.FalseLiteralExpression);
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
               literal.Token.ValueText == "0";
    }

    private static bool IsNullLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression);
    }
}
