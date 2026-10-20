#nullable disable
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Turns the existence tests into <c>Any</c> / <c>AnyAsync</c> calls: a comparison becomes the call (negated when it tests for absence), and a local
    /// that is only compared becomes the boolean that the call returns. All the tests of a document are rewritten in one pass.
    /// </summary>
    internal static class ExistenceRewriter
    {
        internal static SyntaxNode Rewrite(SyntaxNode root, IReadOnlyList<ExistenceUse> uses, string localSuffix)
        {
            // the nodes are found again after each rewrite: a test can be inside the predicate of another one
            var nodes = uses.SelectMany(use => new SyntaxNode[] { use.Invocation, use.Value }.Concat(use.Comparisons.Select(comparison => comparison.Node)).Concat(use.Declaration == null ? Enumerable.Empty<SyntaxNode>() : new[] { use.Declaration })).Distinct();
            root = root.TrackNodes(nodes);

            // the names given to the locals of the document, so that two of them in a member do not get the same one
            var reserved = new HashSet<string>();
            var names = uses.Where(use => use.Shape == ExistenceShape.Local).OrderBy(use => use.Invocation.SpanStart).ToDictionary(use => use, use => GetFreeName(use, localSuffix, reserved));
            foreach (var use in uses.OrderByDescending(use => use.Invocation.SpanStart))
                root = use.Shape == ExistenceShape.Direct ? RewriteDirect(root, use) : RewriteLocal(root, use, names[use]);

            return root;
        }

        private static SyntaxNode RewriteDirect(SyntaxNode root, ExistenceUse use)
        {
            var comparison = root.GetCurrentNode(use.Comparisons[0].Node);
            var value = root.GetCurrentNode(use.Value);
            var invocation = root.GetCurrentNode(use.Invocation);

            // the call and what surrounds it (an await) are the operand of the comparison; its parentheses are not needed any more
            var operand = Unparenthesize(value.ReplaceNode(invocation, Rename(invocation, use.Call.Alternative.Name)));
            return root.ReplaceNode(comparison, Build(operand, use.Comparisons[0].Present, comparison.Parent).WithTriviaFrom(comparison));
        }

        private static SyntaxNode RewriteLocal(SyntaxNode root, ExistenceUse use, string name)
        {
            foreach (var comparison in use.Comparisons)
            {
                var current = root.GetCurrentNode(comparison.Node);
                root = root.ReplaceNode(current, Build(SyntaxFactory.IdentifierName(name), comparison.Present, current.Parent).WithTriviaFrom(current));
            }

            var declaration = root.GetCurrentNode(use.Declaration);
            var declarator = declaration.Declaration.Variables[0];
            var invocation = root.GetCurrentNode(use.Invocation);
            var value = declarator.Initializer.Value.ReplaceNode(invocation, Rename(invocation, use.Call.Alternative.Name));

            // an explicit type is the type of the element or of the number: it becomes a bool
            var type = declaration.Declaration.Type.IsVar ? declaration.Declaration.Type : SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.BoolKeyword)).WithTriviaFrom(declaration.Declaration.Type);
            var newDeclaration = declaration.WithDeclaration(declaration.Declaration.WithType(type).WithVariables(SyntaxFactory.SingletonSeparatedList(
                declarator.WithIdentifier(SyntaxFactory.Identifier(declarator.Identifier.LeadingTrivia, name, declarator.Identifier.TrailingTrivia)).WithInitializer(declarator.Initializer.WithValue(value)))));
            return root.ReplaceNode(declaration, newDeclaration);
        }

        /// <summary>The name of the bool that replaces the local: free in the member, so that it cannot change the meaning of another identifier.</summary>
        private static string GetFreeName(ExistenceUse use, string suffix, ISet<string> reserved)
        {
            var taken = new HashSet<string>((use.Declaration.FirstAncestorOrSelf<MemberDeclarationSyntax>() ?? (SyntaxNode)use.Declaration.SyntaxTree.GetRoot()).DescendantTokens().Where(token => token.IsKind(SyntaxKind.IdentifierToken)).Select(token => token.ValueText));
            var candidate = suffix.StartsWith("has", System.StringComparison.Ordinal) ? suffix : use.LocalName + suffix;
            var name = candidate;
            for (var index = 2; taken.Contains(name) || reserved.Contains(name); index++)
                name = candidate + index;

            reserved.Add(name);
            return name;
        }

        /// <summary>The call with the name of the method that answers the question: <c>q.FirstOrDefault(p)</c> becomes <c>q.Any(p)</c>, the type arguments and the arguments stay.</summary>
        private static InvocationExpressionSyntax Rename(InvocationExpressionSyntax invocation, string name)
        {
            switch (invocation.Expression)
            {
                case MemberAccessExpressionSyntax memberAccess:
                    return invocation.WithExpression(memberAccess.WithName(RenameName(memberAccess.Name, name)));
                default:
                    return invocation.WithExpression(RenameName((SimpleNameSyntax)invocation.Expression, name));
            }
        }

        private static SimpleNameSyntax RenameName(SimpleNameSyntax simpleName, string name) =>
            simpleName.WithIdentifier(SyntaxFactory.Identifier(simpleName.Identifier.LeadingTrivia, name, simpleName.Identifier.TrailingTrivia));

        private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            return expression.WithoutTrivia();
        }

        /// <summary>The expression that tells whether the element exists, or whether it does not: in parentheses when the place it goes to needs them.</summary>
        private static ExpressionSyntax Build(ExpressionSyntax operand, bool present, SyntaxNode parent)
        {
            ExpressionSyntax expression = present ? operand : SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, operand);
            return IsSafeInside(parent, expression) ? expression : SyntaxFactory.ParenthesizedExpression(expression);
        }

        // a call or an identifier stands anywhere; a negation or an await needs parentheses unless the place makes the expression stand alone
        private static bool IsSafeInside(SyntaxNode parent, ExpressionSyntax expression) =>
            expression is InvocationExpressionSyntax or IdentifierNameSyntax ||
            parent is ParenthesizedExpressionSyntax or ArgumentSyntax or EqualsValueClauseSyntax or ReturnStatementSyntax or ArrowExpressionClauseSyntax or IfStatementSyntax or WhileStatementSyntax or ExpressionStatementSyntax ||
            (parent is BinaryExpressionSyntax binary && (binary.IsKind(SyntaxKind.LogicalAndExpression) || binary.IsKind(SyntaxKind.LogicalOrExpression))) ||
            parent is PrefixUnaryExpressionSyntax || parent is ConditionalExpressionSyntax || parent is AssignmentExpressionSyntax;
    }
}
