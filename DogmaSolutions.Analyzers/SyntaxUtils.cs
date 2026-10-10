using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class SyntaxUtils
{
    /// <summary>
    /// Strips the value-preserving wrappers that sit AROUND an expression — parentheses, casts and
    /// <c>checked</c>/<c>unchecked</c> — returning the innermost operand (e.g. <c>unchecked((int)x)</c> → <c>x</c>).
    /// Shared by the RNG-seed and modulo-bias analyzers so the "peel to the real value" logic lives in one place.
    /// </summary>
    internal static ExpressionSyntax UnwrapParenthesesCastsAndChecked(ExpressionSyntax expression)
    {
        while (true)
        {
            switch (expression)
            {
                case ParenthesizedExpressionSyntax parenthesized:
                    expression = parenthesized.Expression;
                    continue;
                case CastExpressionSyntax cast:
                    expression = cast.Expression;
                    continue;
                case CheckedExpressionSyntax @checked:
                    expression = @checked.Expression;
                    continue;
                default:
                    return expression;
            }
        }
    }

    internal static SyntaxNode GetContainingScope(SyntaxNode node)
    {
        foreach (var ancestor in node.Ancestors())
        {
            if (TryGetScopeOfContainer(ancestor, out var scope))
                return scope;
        }

        return null;
    }

    /// <summary>
    /// When the node is a container of code (lambda, anonymous method, local function, method, constructor, accessor,
    /// compilation unit), returns true and its body as the scope; the scope is null for a container without body.
    /// </summary>
    private static bool TryGetScopeOfContainer(SyntaxNode node, out SyntaxNode scope)
    {
        switch (node)
        {
            case AnonymousFunctionExpressionSyntax anonymousFunction: // lambdas and anonymous methods
                scope = anonymousFunction.Body;
                return true;
            case LocalFunctionStatementSyntax localFunc:
                scope = (SyntaxNode)localFunc.Body ?? localFunc.ExpressionBody?.Expression;
                return true;
            case MethodDeclarationSyntax method:
                scope = (SyntaxNode)method.Body ?? method.ExpressionBody?.Expression;
                return true;
            case ConstructorDeclarationSyntax ctor:
                scope = (SyntaxNode)ctor.Body ?? ctor.ExpressionBody?.Expression;
                return true;
            case AccessorDeclarationSyntax accessor:
                scope = (SyntaxNode)accessor.Body ?? accessor.ExpressionBody?.Expression;
                return true;
            case CompilationUnitSyntax compilationUnit:
                scope = compilationUnit;
                return true;
            default:
                scope = null;
                return false;
        }
    }

    internal static bool IsNestedScope(SyntaxNode node)
    {
        return node is SimpleLambdaExpressionSyntax ||
               node is ParenthesizedLambdaExpressionSyntax ||
               node is AnonymousMethodExpressionSyntax ||
               node is LocalFunctionStatementSyntax;
    }

    internal static string NormalizeWhitespace(string text)
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

    internal static SyntaxTrivia GetEndOfLineTrivia(SyntaxNode node)
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

    internal static SyntaxTriviaList GetIndentationTrivia(SyntaxNode node)
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

    /// <summary>True for the loop statements: for, foreach (both forms), while and do.</summary>
    internal static bool IsLoopStatement(SyntaxNode node)
    {
        return node is ForStatementSyntax or
            ForEachStatementSyntax or
            ForEachVariableStatementSyntax or
            WhileStatementSyntax or
            DoStatementSyntax;
    }

    /// <summary>The embedded statement of a loop, or null when the node is not a loop.</summary>
    internal static StatementSyntax GetLoopBody(SyntaxNode loop)
    {
        return loop switch
        {
            ForStatementSyntax forStatement => forStatement.Statement,
            ForEachStatementSyntax forEachStatement => forEachStatement.Statement,
            ForEachVariableStatementSyntax forEachVariableStatement => forEachVariableStatement.Statement,
            WhileStatementSyntax whileStatement => whileStatement.Statement,
            DoStatementSyntax doStatement => doStatement.Statement,
            _ => null
        };
    }

    /// <summary>True for the nodes that open a new function body: lambdas, anonymous methods and local functions.</summary>
    internal static bool IsFunctionBoundary(SyntaxNode node)
    {
        return node is LambdaExpressionSyntax or AnonymousMethodExpressionSyntax or LocalFunctionStatementSyntax;
    }

    /// <summary>The nearest loop that contains the node within the same function body (null if a lambda, anonymous method or local function comes first).</summary>
    internal static SyntaxNode FindEnclosingLoop(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (IsFunctionBoundary(current))
                return null;

            if (IsLoopStatement(current))
                return current;
        }

        return null;
    }

    /// <summary>True when a lambda, anonymous method or local function sits between the node and the boundary.</summary>
    internal static bool IsInsideNestedFunction(SyntaxNode node, SyntaxNode boundary)
    {
        for (var current = node.Parent; current != null && current != boundary; current = current.Parent)
        {
            if (IsFunctionBoundary(current))
                return true;
        }

        return false;
    }

    /// <summary>True when a loop sits between the node and the loop body that is being analyzed.</summary>
    internal static bool IsInsideNestedLoop(SyntaxNode node, StatementSyntax loopBody)
    {
        for (var current = node.Parent; current != null && current != loopBody; current = current.Parent)
        {
            if (IsLoopStatement(current))
                return true;
        }

        return false;
    }

    /// <summary>
    /// True when at least one argument is a (possibly parenthesized) addition, i.e. a candidate for string concatenation.
    /// A cheap syntactic test that lets callers skip the semantic work for the vast majority of calls.
    /// </summary>
    internal static bool HasAddExpressionArgument(ArgumentListSyntax argumentList)
    {
        if (argumentList == null)
            return false;

        foreach (var argument in argumentList.Arguments)
        {
            var expression = argument.Expression;
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            if (expression.IsKind(SyntaxKind.AddExpression))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The simple name of the method an invocation calls (<c>M</c> in <c>M()</c>, <c>x.M()</c>, <c>x?.M()</c>, <c>x.M&lt;T&gt;()</c>),
    /// or null when the callee is not a plain name (delegate invocation, parenthesized expression, ...).
    /// </summary>
    internal static string GetInvokedName(InvocationExpressionSyntax invocation)
    {
        var name = invocation.Expression switch
        {
            SimpleNameSyntax simple => simple,
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            _ => null
        };

        return name?.Identifier.ValueText;
    }
}
