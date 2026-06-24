using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class SyntaxUtils
{
    internal static SyntaxNode GetContainingScope(SyntaxNode node)
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
