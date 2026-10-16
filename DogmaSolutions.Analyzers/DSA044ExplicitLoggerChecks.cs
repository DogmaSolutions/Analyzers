using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// A write to the console that the code chose on purpose because there is no logger:
    /// <c>if (logger != null) logger.LogError(...); else Console.WriteLine(...);</c>. The author has looked at the logger and decided what to do when
    /// it is missing, which is exactly what the rule asks for.
    /// </summary>
    internal static class DSA044ExplicitLoggerChecks
    {
        /// <summary>
        /// The call is in the branch that is taken when <b>the logger that is available there</b> is null, under a condition that tests that logger: the <c>else</c> of
        /// <c>logger != null</c>, the <c>then</c> of <c>logger == null</c>, the part of a conditional expression that does the same, or the code that
        /// follows an <c>if (logger != null) { ...; return; }</c>.
        /// </summary>
        internal static bool IsFallbackOfAnExplicitCheck(InvocationExpressionSyntax invocation, ISymbol enclosing, SemanticModel model, DSA044Settings settings, CancellationToken cancellationToken)
        {
            if (!settings.SkipExplicitLoggerChecks)
                return false;

            var tester = new LoggerTester(model, enclosing, settings, cancellationToken);
            SyntaxNode child = invocation;
            foreach (var ancestor in invocation.Ancestors().TakeWhile(node => node is not (MemberDeclarationSyntax or LocalFunctionStatementSyntax)))
            {
                if (IsFallback(ancestor, child, tester))
                    return true;

                child = ancestor;
            }

            return false;
        }

        private static bool IsFallback(SyntaxNode ancestor, SyntaxNode child, LoggerTester tester)
        {
            switch (ancestor)
            {
                case IfStatementSyntax ifStatement:
                    return IsFallbackBranch(ifStatement.Condition, isThenBranch: child == ifStatement.Statement, isElseBranch: child == ifStatement.Else, tester);
                case ConditionalExpressionSyntax conditional:
                    return IsFallbackBranch(conditional.Condition, isThenBranch: child == conditional.WhenTrue, isElseBranch: child == conditional.WhenFalse, tester);
                case BlockSyntax block:
                    return block.Statements.TakeWhile(statement => statement != child).Any(statement => IsGuardThatExits(statement, tester));
                default:
                    return false;
            }
        }

        /// <summary>The code of the then branch runs when a logger is null, if the condition tests that it is; the one of the else branch, if it tests that it is not.</summary>
        private static bool IsFallbackBranch(ExpressionSyntax condition, bool isThenBranch, bool isElseBranch, LoggerTester tester)
        {
            var tests = tester.FindTests(condition).ToList();
            return (isThenBranch && tests.Contains(LoggerTest.IsNull)) || (isElseBranch && tests.Contains(LoggerTest.IsNotNull));
        }

        /// <summary><c>if (logger != null) { ...; return; }</c>: what follows runs only when the logger is null.</summary>
        private static bool IsGuardThatExits(StatementSyntax statement, LoggerTester tester) =>
            statement is IfStatementSyntax { Else: null } ifStatement && AlwaysExits(ifStatement.Statement) && tester.FindTests(ifStatement.Condition).Contains(LoggerTest.IsNotNull);

        private static bool AlwaysExits(StatementSyntax statement)
        {
            var last = statement is BlockSyntax block ? block.Statements.LastOrDefault() : statement;
            return last is ReturnStatementSyntax or ThrowStatementSyntax or ContinueStatementSyntax or BreakStatementSyntax;
        }

        // ---- the tests of a logger in a condition ---------------------------------------------------------------------------

        private enum LoggerTest
        {
            IsNull,
            IsNotNull,
        }

        /// <summary>
        /// Finds, in a condition, the tests that the available logger is null or is not null: the logger the call could have used, by its name
        /// (<c>logger</c>, <c>_logger</c>, <c>this._logger</c>) or through a local variable that copies it. The test of any other logger (the one a call returns, the
        /// one of another object) says nothing about the logger of the code.
        /// </summary>
        private sealed class LoggerTester
        {
            private const int MaxAliasDepth = 3;

            private readonly SemanticModel _model;
            private readonly ISymbol _enclosing;
            private readonly DSA044Settings _settings;
            private readonly CancellationToken _cancellationToken;

            internal LoggerTester(SemanticModel model, ISymbol enclosing, DSA044Settings settings, CancellationToken cancellationToken)
            {
                _model = model;
                _enclosing = enclosing;
                _settings = settings;
                _cancellationToken = cancellationToken;
            }

            internal IEnumerable<LoggerTest> FindTests(ExpressionSyntax expression) => FindTests(expression, negated: false);

            private IEnumerable<LoggerTest> FindTests(ExpressionSyntax expression, bool negated)
            {
                switch (expression)
                {
                    case ParenthesizedExpressionSyntax parenthesized:
                        return FindTests(parenthesized.Expression, negated);
                    case PrefixUnaryExpressionSyntax prefix when prefix.IsKind(SyntaxKind.LogicalNotExpression):
                        return FindTests(prefix.Operand, !negated);
                    case BinaryExpressionSyntax binary when IsNullComparison(binary):
                        return TestOf(IsNullLiteral(binary.Right) ? binary.Left : binary.Right, isNullTest: binary.IsKind(SyntaxKind.EqualsExpression) != negated);
                    case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.IsExpression):
                        // logger is ILogger: true only when there is a logger
                        return TestOf(binary.Left, isNullTest: negated);
                    case IsPatternExpressionSyntax isPattern:
                        return TestOf(isPattern.Expression, isNullTest: IsNullPattern(isPattern.Pattern) != negated);
                    case ConditionalAccessExpressionSyntax access:
                        // logger?.IsEnabled(...) is true only when there is a logger
                        return IsLogger(access.Expression) ? new[] { negated ? LoggerTest.IsNull : LoggerTest.IsNotNull } : Enumerable.Empty<LoggerTest>();
                    default:
                        return expression.ChildNodes().OfType<ExpressionSyntax>().SelectMany(child => FindTests(child, negated));
                }
            }

            private IEnumerable<LoggerTest> TestOf(ExpressionSyntax operand, bool isNullTest)
            {
                if (IsLogger(operand))
                    return new[] { isNullTest ? LoggerTest.IsNull : LoggerTest.IsNotNull };

                // the operand may itself be made of tests: (logger?.IsEnabled(...)) != null
                return FindTests(operand, negated: isNullTest);
            }

            private bool IsLogger(ExpressionSyntax expression, int depth = 0)
            {
                var name = expression is ParenthesizedExpressionSyntax parenthesized ? parenthesized.Expression : expression;
                if (name is not (IdentifierNameSyntax or MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax }))
                    return false;

                var symbol = _model.GetSymbolInfo(name, _cancellationToken).Symbol;
                if (symbol is ILocalSymbol local)
                    return depth < MaxAliasDepth && GetInitializer(local, _cancellationToken) is { } initializer && IsLogger(initializer, depth + 1);

                return symbol != null && DSA044LoggerAvailability.IsAvailableLogger(symbol, _enclosing, _settings);
            }

            /// <summary><c>var copy = _logger;</c>: what the local variable was given.</summary>
            private static ExpressionSyntax GetInitializer(ILocalSymbol local, CancellationToken cancellationToken) =>
                (local.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(cancellationToken) as VariableDeclaratorSyntax)?.Initializer?.Value;

            /// <summary><c>x == null</c>, <c>null != x</c>...</summary>
            private static bool IsNullComparison(BinaryExpressionSyntax binary) =>
                (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)) && (IsNullLiteral(binary.Right) || IsNullLiteral(binary.Left));

            private static bool IsNullLiteral(ExpressionSyntax expression) => expression.IsKind(SyntaxKind.NullLiteralExpression);

            /// <summary><c>null</c>, or <c>not not null</c>...: true when the pattern matches a null value, false when it needs a value.</summary>
            private static bool IsNullPattern(PatternSyntax pattern)
            {
                switch (pattern)
                {
                    case ConstantPatternSyntax constant:
                        return IsNullLiteral(constant.Expression);
                    case UnaryPatternSyntax unary:
                        return !IsNullPattern(unary.Pattern);
                    default:
                        // a type, a declaration or a property pattern: they match a value
                        return false;
                }
            }
        }
    }
}
