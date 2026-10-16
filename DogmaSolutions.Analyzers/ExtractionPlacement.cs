using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Where the code fixes that extract a repeated expression into a local variable (DSA016, DSA019) put the declaration: in the narrowest block that
/// contains all the occurrences, and never where it would evaluate the expression on a path that the code guards against (an index checked by an
/// <c>if</c>, a call that a <c>&amp;&amp;</c> or a <c>try</c> protects).
/// </summary>
internal static class ExtractionPlacement
{
    /// <summary>The block or switch section that contains all the occurrences and is the innermost one that does (the scope itself when none does).</summary>
    internal static SyntaxNode FindNarrowestContainer(IReadOnlyList<SyntaxNode> occurrences, SyntaxNode scope)
    {
        var first = occurrences.OrderBy(occurrence => occurrence.SpanStart).First();
        foreach (var ancestor in first.Ancestors())
        {
            if (ancestor == scope)
                break;

            if (ancestor is BlockSyntax { Parent: not (WhileStatementSyntax or ForStatementSyntax or ForEachStatementSyntax or DoStatementSyntax) } or SwitchSectionSyntax &&
                occurrences.All(occurrence => ancestor.Span.Contains(occurrence.Span)))
                return ancestor;
        }

        return scope;
    }

    /// <summary>
    /// True when executing the statement, and then the ones that follow it in its block until one can leave, always evaluates an occurrence before anything else that could change what it evaluates to: in the first
    /// statement of every path through it (both branches of an <c>if</c> with an <c>else</c>, the block and the catches of a <c>try</c>), or in the
    /// part of it that is always evaluated (the condition of an <c>if</c> or of a loop, the expression of a <c>foreach</c>, a plain statement).
    /// The declaration can then be placed before the statement without evaluating the expression where the code did not.
    /// </summary>
    internal static bool EvaluatesOnEveryPath(StatementSyntax statement, IReadOnlyList<SyntaxNode> occurrences)
    {
        var siblings = statement.Parent switch
        {
            BlockSyntax block => block.Statements,
            SwitchSectionSyntax section => section.Statements,
            _ => SyntaxFactory.SingletonList(statement),
        };

        // a statement that does not evaluate the expression (and cannot leave) lets the path go on to the next one, which may
        for (var i = siblings.IndexOf(statement); i < siblings.Count; i++)
        {
            if (EvaluatesFirst(siblings[i], occurrences))
                return true;

            if (CanLeave(siblings[i]))
                return false;
        }

        return false;
    }

    /// <summary>Whether the statement can end the path: a return, a throw, a break, a continue, a goto, a yield.</summary>
    private static bool CanLeave(StatementSyntax statement) =>
        statement.DescendantNodesAndSelf(static node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax))
            .Any(static node => node is ReturnStatementSyntax or ThrowStatementSyntax or ThrowExpressionSyntax or BreakStatementSyntax or ContinueStatementSyntax or GotoStatementSyntax or YieldStatementSyntax);

    /// <summary>The statement evaluates an occurrence before anything else of it that could change what it evaluates to or leave the path.</summary>
    private static bool EvaluatesFirst(StatementSyntax statement, IReadOnlyList<SyntaxNode> occurrences)
    {
        switch (statement)
        {
            case BlockSyntax block:
                return block.Statements.Count > 0 && EvaluatesFirst(block.Statements[0], occurrences);

            case IfStatementSyntax ifStatement:
                return HasUnconditionalOccurrence(ifStatement.Condition, ifStatement, occurrences) ||
                       (ifStatement.Else != null && EvaluatesFirst(ifStatement.Statement, occurrences) && EvaluatesFirst(ifStatement.Else.Statement, occurrences));

            case TryStatementSyntax tryStatement:
                // an exception thrown by the declaration is no longer caught by the catches: they have to evaluate the expression too
                return EvaluatesFirst(tryStatement.Block, occurrences) && tryStatement.Catches.All(catchClause => EvaluatesFirst(catchClause.Block, occurrences));

            // a loop is taken as one that runs: an expression that does not depend on the loop is declared before it
            case WhileStatementSyntax whileStatement:
                return HasUnconditionalOccurrence(whileStatement.Condition, whileStatement, occurrences) || EvaluatesFirst(whileStatement.Statement, occurrences);

            case ForStatementSyntax forStatement:
                return HasUnconditionalOccurrence(forStatement.Declaration, forStatement, occurrences) ||
                       forStatement.Initializers.Any(initializer => HasUnconditionalOccurrence(initializer, forStatement, occurrences)) ||
                       HasUnconditionalOccurrence(forStatement.Condition, forStatement, occurrences) ||
                       EvaluatesFirst(forStatement.Statement, occurrences);

            case ForEachStatementSyntax forEach:
                return HasUnconditionalOccurrence(forEach.Expression, forEach, occurrences) || EvaluatesFirst(forEach.Statement, occurrences);

            case DoStatementSyntax doStatement:
                return EvaluatesFirst(doStatement.Statement, occurrences);

            case SwitchStatementSyntax switchStatement:
                return HasUnconditionalOccurrence(switchStatement.Expression, switchStatement, occurrences);

            case LockStatementSyntax lockStatement:
                return HasUnconditionalOccurrence(lockStatement.Expression, lockStatement, occurrences) || EvaluatesFirst(lockStatement.Statement, occurrences);

            case UsingStatementSyntax usingStatement:
                return HasUnconditionalOccurrence(usingStatement.Declaration, usingStatement, occurrences) ||
                       HasUnconditionalOccurrence(usingStatement.Expression, usingStatement, occurrences) ||
                       EvaluatesFirst(usingStatement.Statement, occurrences);

            case LabeledStatementSyntax labeled:
                return EvaluatesFirst(labeled.Statement, occurrences);

            case ExpressionStatementSyntax or LocalDeclarationStatementSyntax or ReturnStatementSyntax or ThrowStatementSyntax or YieldStatementSyntax:
                return HasUnconditionalOccurrence(statement, statement, occurrences);

            default:
                // any other statement (a local function, an unsafe or fixed block...): its body is not looked into
                return false;
        }
    }

    private static bool HasUnconditionalOccurrence(SyntaxNode part, StatementSyntax statement, IReadOnlyList<SyntaxNode> occurrences)
    {
        return part != null && occurrences.Any(occurrence => part.Span.Contains(occurrence.Span) && IsEvaluatedUnconditionally(occurrence, statement));
    }

    /// <summary>
    /// True when the occurrence is evaluated every time the statement is entered: it is not in the right side of <c>&amp;&amp;</c>, <c>||</c> or
    /// <c>??</c>, in a branch of a conditional expression or of a switch expression, in a conditional access, or in the body or the incrementors
    /// of a loop.
    /// </summary>
    private static bool IsEvaluatedUnconditionally(SyntaxNode occurrence, StatementSyntax statement)
    {
        // the nodes from the occurrence up to the statement, each one with the node that contains it
        var path = occurrence.AncestorsAndSelf().TakeWhile(node => node != statement.Parent).ToList();
        return !path.Skip(1).Select((parent, index) => IsConditionalPart(parent, path[index])).Any(conditional => conditional);
    }

    /// <summary>Whether the child is evaluated only under a condition of its parent.</summary>
    private static bool IsConditionalPart(SyntaxNode parent, SyntaxNode child) => parent switch
    {
        BinaryExpressionSyntax binary => binary.Right == child && (binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression) || binary.IsKind(SyntaxKind.CoalesceExpression)),
        ConditionalExpressionSyntax conditionalExpression => conditionalExpression.Condition != child,
        ConditionalAccessExpressionSyntax conditionalAccess => conditionalAccess.WhenNotNull == child,
        SwitchExpressionArmSyntax => true,
        _ => false,
    };
}
