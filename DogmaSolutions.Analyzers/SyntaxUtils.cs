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
}
