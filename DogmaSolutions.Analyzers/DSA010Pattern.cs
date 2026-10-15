using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Recognizes a critical section that is nothing but a single plain access to a field: <c>lock (l) { return f; }</c> or
    /// <c>lock (l) { f = value; }</c>.
    /// </summary>
    internal static class DSA010Pattern
    {
        /// <summary>Cheap and syntactic: the body is one <c>return</c> or one simple assignment, and the lock is on a plain name or on <c>this</c>.</summary>
        internal static bool HasSingleAccessShape(LockStatementSyntax lockStatement)
        {
            var body = GetSingleStatement(lockStatement.Statement);
            return IsPlainLockExpression(lockStatement.Expression) && body is ReturnStatementSyntax { Expression: not null } or ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax };
        }

        internal static StatementSyntax GetSingleStatement(StatementSyntax statement) =>
            statement is BlockSyntax { Statements.Count: 1 } block ? block.Statements[0] : statement;

        /// <summary><c>this</c>, <c>name</c> or <c>this.name</c>.</summary>
        internal static bool IsPlainLockExpression(ExpressionSyntax expression) =>
            expression is ThisExpressionSyntax or IdentifierNameSyntax || IsThisMember(expression);

        /// <summary><c>name</c> or <c>this.name</c>: the way a field is reached from its own type.</summary>
        internal static bool IsSimpleFieldReference(ExpressionSyntax expression) => expression is IdentifierNameSyntax || IsThisMember(expression);

        private static bool IsThisMember(ExpressionSyntax expression) => expression is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax };

        /// <summary>
        /// The critical section reads or assigns one field of the type, and what it assigns is a constant, a parameter (<c>value</c>
        /// included) or a local: no other shared state is involved.
        /// </summary>
        internal static bool TryMatch(LockStatementSyntax lockStatement, SemanticModel model, CancellationToken cancellationToken, out DSA010Access access, out IFieldSymbol field)
        {
            access = DSA010Access.Read;
            field = null;

            var body = GetSingleStatement(lockStatement.Statement);
            ExpressionSyntax target;
            if (body is ReturnStatementSyntax { Expression: { } returned })
            {
                target = Unwrap(returned);
            }
            else if (body is ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment } && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && IsInert(assignment.Right, model, cancellationToken))
            {
                access = DSA010Access.Write;
                target = Unwrap(assignment.Left);
            }
            else
            {
                return false;
            }

            field = GetField(target, model, cancellationToken);
            return field != null;
        }

        /// <summary>The field a simple reference (<c>f</c> or <c>this.f</c>) stands for; constants are not fields to protect.</summary>
        internal static IFieldSymbol GetField(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
        {
            if (!IsSimpleFieldReference(expression))
                return null;

            return model.GetSymbolInfo(expression, cancellationToken).Symbol is IFieldSymbol { IsConst: false } field ? field : null;
        }

        private static ExpressionSyntax Unwrap(ExpressionSyntax expression) => expression is ParenthesizedExpressionSyntax parenthesized ? Unwrap(parenthesized.Expression) : expression;

        /// <summary>Literals, <c>default</c>, constants, parameters and locals, possibly negated: reading them touches no shared state.</summary>
        private static bool IsInert(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
        {
            switch (expression)
            {
                case LiteralExpressionSyntax:
                case DefaultExpressionSyntax:
                    return true;
                case ParenthesizedExpressionSyntax parenthesized:
                    return IsInert(parenthesized.Expression, model, cancellationToken);
                case PrefixUnaryExpressionSyntax prefix when prefix.Kind() is SyntaxKind.UnaryMinusExpression or SyntaxKind.UnaryPlusExpression or SyntaxKind.LogicalNotExpression or SyntaxKind.BitwiseNotExpression:
                    return IsInert(prefix.Operand, model, cancellationToken);
                case IdentifierNameSyntax or MemberAccessExpressionSyntax:
                    return model.GetSymbolInfo(expression, cancellationToken).Symbol is IParameterSymbol or ILocalSymbol or IFieldSymbol { IsConst: true };
                default:
                    return false;
            }
        }
    }
}
