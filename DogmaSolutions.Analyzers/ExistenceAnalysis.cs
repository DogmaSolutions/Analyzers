#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What the value is: an element that is only tested against null, or a number that is only tested against zero.</summary>
    internal enum ExistenceKind
    {
        Item,
        Count,
    }

    /// <summary>How the value reaches the test.</summary>
    internal enum ExistenceShape
    {
        /// <summary>The call is compared right away: <c>q.FirstOrDefault(p) != null</c>.</summary>
        Direct,

        /// <summary>The result is kept in a local that is only compared: <c>var item = q.FirstOrDefault(p); if (item == null)</c>.</summary>
        Local,
    }

    /// <summary>The call that produces the value (FirstOrDefault, Count...), and the call that answers the existence question instead (Any).</summary>
    internal sealed class ExistenceCall
    {
        internal ExistenceCall(IMethodSymbol method, IMethodSymbol alternative, bool isAsync, bool isQueryable, bool hasPredicate, ITypeSymbol receiverType, ITypeSymbol elementType)
        {
            Method = method;
            Alternative = alternative;
            IsAsync = isAsync;
            IsQueryable = isQueryable;
            HasPredicate = hasPredicate;
            ReceiverType = receiverType;
            ElementType = elementType;
        }

        internal IMethodSymbol Method { get; }

        /// <summary>The extension method with the same parameters that returns whether there is a match (<c>Any</c>, <c>AnyAsync</c>).</summary>
        internal IMethodSymbol Alternative { get; }

        internal bool IsAsync { get; }

        /// <summary>The source is an <c>IQueryable</c>: the call is translated by a provider, typically into SQL.</summary>
        internal bool IsQueryable { get; }

        internal bool HasPredicate { get; }

        /// <summary>The static type of the expression the method is called on.</summary>
        internal ITypeSymbol ReceiverType { get; }

        /// <summary>The type of the elements of the sequence.</summary>
        internal ITypeSymbol ElementType { get; }
    }

    /// <summary>One comparison of the value that is an existence test: the node to rewrite, and whether it is true when the element (or a count above zero) exists.</summary>
    internal readonly struct ExistenceComparison
    {
        internal ExistenceComparison(SyntaxNode node, bool present)
        {
            Node = node;
            Present = present;
        }

        internal SyntaxNode Node { get; }

        internal bool Present { get; }
    }

    /// <summary>A value that is produced by a call and is used for nothing else than an existence test.</summary>
    internal sealed class ExistenceUse
    {
        internal ExistenceUse(InvocationExpressionSyntax invocation, ExpressionSyntax value, ExistenceCall call, ExistenceShape shape, IReadOnlyList<ExistenceComparison> comparisons, LocalDeclarationStatementSyntax declaration)
        {
            Invocation = invocation;
            Value = value;
            Call = call;
            Shape = shape;
            Comparisons = comparisons;
            Declaration = declaration;
        }

        internal InvocationExpressionSyntax Invocation { get; }

        /// <summary>The call with what surrounds it as a value: parentheses, <c>await</c>, <c>ConfigureAwait</c>.</summary>
        internal ExpressionSyntax Value { get; }

        internal ExistenceCall Call { get; }

        internal ExistenceShape Shape { get; }

        /// <summary>Direct: the only comparison. Local: all of them, in order of appearance.</summary>
        internal IReadOnlyList<ExistenceComparison> Comparisons { get; }

        /// <summary>The declaration of the local (Local only).</summary>
        internal LocalDeclarationStatementSyntax Declaration { get; }

        internal string LocalName => Declaration?.Declaration.Variables[0].Identifier.ValueText ?? string.Empty;
    }

    /// <summary>
    /// Finds out whether the result of a call like <c>FirstOrDefault</c> or <c>Count</c> is only tested for existence: compared with null or with zero,
    /// right away or through a local. A result that is returned, assigned, passed, captured or otherwise used is not an existence test.
    /// </summary>
    internal static class ExistenceAnalysis
    {
        // the values of a count that tell the existence tests from the other comparisons (> 1, == 1...)
        private static readonly long[] CountSamples = { 0, 1, 2, 5, 100, 1000000, int.MaxValue, long.MaxValue };

        // ---- the call -------------------------------------------------------------------------------------------------

        /// <summary>The extension method called by the invocation and the one that answers the existence question instead; null when there is none.</summary>
        internal static ExistenceCall ResolveCall(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
                return null;

            var definition = method.ReducedFrom ?? method;
            if (!definition.IsStatic || !definition.IsExtensionMethod || definition.Parameters.IsEmpty)
                return null;

            var isAsync = definition.Name.EndsWith("Async", StringComparison.Ordinal) && IsTask(definition.ReturnType);
            var alternative = FindAlternative(definition, isAsync ? "AnyAsync" : "Any");
            if (alternative == null || (!isAsync && IsTask(definition.ReturnType)))
                return null;

            var receiver = GetReceiver(invocation, method);
            return new ExistenceCall(
                method,
                alternative,
                isAsync,
                definition.Parameters[0].Type is INamedTypeSymbol { Name: "IQueryable" },
                definition.Parameters.Skip(1).Any(parameter => parameter.Type is INamedTypeSymbol { Name: "Func" or "Expression" }),
                receiver == null ? null : model.GetTypeInfo(receiver, cancellationToken).Type,
                method.TypeArguments.FirstOrDefault());
        }

        private static ExpressionSyntax GetReceiver(InvocationExpressionSyntax invocation, IMethodSymbol method) =>
            method.ReducedFrom != null && invocation.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.Expression : invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;

        private static bool IsTask(ITypeSymbol type) => type is INamedTypeSymbol { Name: "Task" or "ValueTask" };

        /// <summary>A static extension method of the same class, with the same parameters, that returns a bool (or a task of it).</summary>
        private static IMethodSymbol FindAlternative(IMethodSymbol definition, string name)
        {
            return definition.ContainingType.GetMembers(name).OfType<IMethodSymbol>().FirstOrDefault(candidate =>
                candidate.IsStatic && candidate.IsExtensionMethod && candidate.Parameters.Length == definition.Parameters.Length &&
                candidate.TypeParameters.Length == definition.TypeParameters.Length &&
                candidate.Parameters.Zip(definition.Parameters, (left, right) => HasSameShape(left.Type, right.Type)).All(same => same));
        }

        // the parameter of the other method is, for example, an Expression<Func<TSource, bool>> too, whatever the name of its type parameter
        private static bool HasSameShape(ITypeSymbol left, ITypeSymbol right) =>
            left.TypeKind == TypeKind.TypeParameter || right.TypeKind == TypeKind.TypeParameter
                ? left.TypeKind == right.TypeKind && ((ITypeParameterSymbol)left).Ordinal == ((ITypeParameterSymbol)right).Ordinal
                : SymbolEqualityComparer.Default.Equals(left.OriginalDefinition, right.OriginalDefinition);

        // ---- the use of the result --------------------------------------------------------------------------------------

        /// <summary>The existence test the result of the call is used for, or null when it is used for anything else.</summary>
        internal static ExistenceUse Analyze(InvocationExpressionSyntax invocation, ExistenceCall call, ExistenceKind kind, SemanticModel model, CancellationToken cancellationToken)
        {
            var value = PeelUp(invocation, out var awaited);
            if (call.IsAsync != awaited)
                return null;

            if (value.Parent is EqualsValueClauseSyntax clause && clause.Value == value &&
                clause.Parent is VariableDeclaratorSyntax declarator && declarator.Parent is VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax { Modifiers.Count: 0, UsingKeyword.RawKind: 0 } declaration })
            {
                var comparisons = FindLocalComparisons(declarator, declaration, kind, model, cancellationToken);
                return comparisons == null ? null : new ExistenceUse(invocation, value, call, ExistenceShape.Local, comparisons, declaration);
            }

            var comparison = Classify(value, kind, model);
            return comparison == null ? null : new ExistenceUse(invocation, value, call, ExistenceShape.Direct, new[] { comparison.Value }, null);
        }

        /// <summary>The expression the call is part of as a value: through parentheses, <c>await</c> and <c>ConfigureAwait</c>.</summary>
        private static ExpressionSyntax PeelUp(ExpressionSyntax node, out bool awaited)
        {
            awaited = false;
            while (true)
            {
                switch (node.Parent)
                {
                    case ParenthesizedExpressionSyntax parenthesized:
                        node = parenthesized;
                        break;
                    case AwaitExpressionSyntax await:
                        awaited = true;
                        node = await;
                        break;
                    case MemberAccessExpressionSyntax { Name.Identifier.ValueText: "ConfigureAwait", Parent: InvocationExpressionSyntax configure } memberAccess when memberAccess.Expression == node && configure.Expression == memberAccess:
                        node = configure;
                        break;
                    default:
                        return node;
                }
            }
        }

        private static List<ExistenceComparison> FindLocalComparisons(VariableDeclaratorSyntax declarator, LocalDeclarationStatementSyntax declaration, ExistenceKind kind, SemanticModel model, CancellationToken cancellationToken)
        {
            if (declaration.Parent is not (BlockSyntax or SwitchSectionSyntax) || model.GetDeclaredSymbol(declarator, cancellationToken) is not ILocalSymbol local)
                return null;

            var comparisons = new List<ExistenceComparison>();
            var name = declarator.Identifier.ValueText;
            foreach (var identifier in declaration.Parent.DescendantNodes().OfType<IdentifierNameSyntax>().Where(identifier => identifier.Identifier.ValueText == name))
            {
                if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier, cancellationToken).Symbol, local))
                    continue;

                // a use in a lambda or a local function runs when, and as many times as, the caller decides
                var comparison = IsInNestedFunction(identifier, declaration.Parent) ? null : Classify(PeelParentheses(identifier), kind, model);
                if (comparison == null)
                    return null;

                comparisons.Add(comparison.Value);
            }

            return comparisons.Count == 0 ? null : comparisons;
        }

        private static bool IsInNestedFunction(SyntaxNode node, SyntaxNode scope) =>
            node.Ancestors().TakeWhile(ancestor => ancestor != scope).Any(ancestor => ancestor is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax);

        private static ExpressionSyntax PeelParentheses(ExpressionSyntax node)
        {
            while (node.Parent is ParenthesizedExpressionSyntax parenthesized)
                node = parenthesized;

            return node;
        }

        // ---- the comparison -------------------------------------------------------------------------------------------

        /// <summary>The comparison of the value with null (or zero) that tells whether it exists, or null when the parent of the value is something else.</summary>
        private static ExistenceComparison? Classify(ExpressionSyntax value, ExistenceKind kind, SemanticModel model)
        {
            bool? present;
            switch (value.Parent)
            {
                case BinaryExpressionSyntax binary:
                    present = EvaluateBinary(binary, value, kind, model);
                    break;
                case IsPatternExpressionSyntax isPattern when isPattern.Expression == value:
                    present = Polarity(kind, sample => EvaluatePattern(isPattern.Pattern, sample, model));
                    break;
                default:
                    return null;
            }

            return present == null ? null : new ExistenceComparison(value.Parent, present.Value);
        }

        private static bool? EvaluateBinary(BinaryExpressionSyntax binary, ExpressionSyntax value, ExistenceKind kind, SemanticModel model)
        {
            var valueIsLeft = binary.Left == value;
            var other = valueIsLeft ? binary.Right : binary.Left;
            return Polarity(kind, sample => EvaluateComparison(binary.Kind(), valueIsLeft, other, sample, model));
        }

        private static bool? EvaluateComparison(SyntaxKind operatorKind, bool valueIsLeft, ExpressionSyntax other, Sample sample, SemanticModel model)
        {
            if (sample.Kind == ExistenceKind.Item)
            {
                // only a comparison with the null literal is understood
                if (!other.IsKind(SyntaxKind.NullLiteralExpression) || (operatorKind != SyntaxKind.EqualsExpression && operatorKind != SyntaxKind.NotEqualsExpression))
                    return null;

                return sample.IsNull == (operatorKind == SyntaxKind.EqualsExpression);
            }

            var constant = GetNumber(other, model);
            if (constant == null)
                return null;

            return valueIsLeft ? Compare(operatorKind, sample.Number, constant.Value) : Compare(operatorKind, constant.Value, sample.Number);
        }

        private static bool? Compare(SyntaxKind operatorKind, long left, long right)
        {
            switch (operatorKind)
            {
                case SyntaxKind.EqualsExpression:
                    return left == right;
                case SyntaxKind.NotEqualsExpression:
                    return left != right;
                case SyntaxKind.GreaterThanExpression:
                    return left > right;
                case SyntaxKind.GreaterThanOrEqualExpression:
                    return left >= right;
                case SyntaxKind.LessThanExpression:
                    return left < right;
                case SyntaxKind.LessThanOrEqualExpression:
                    return left <= right;
                default:
                    return null;
            }
        }

        private static long? GetNumber(ExpressionSyntax expression, SemanticModel model)
        {
            var constant = model.GetConstantValue(expression);
            return constant.HasValue ? constant.Value switch { int number => number, long number => number, _ => (long?)null } : null;
        }

        /// <summary>One state of the value: an element that exists or not, a count of some number.</summary>
        private readonly struct Sample
        {
            internal Sample(ExistenceKind kind, bool isNull, long number)
            {
                Kind = kind;
                IsNull = isNull;
                Number = number;
            }

            internal ExistenceKind Kind { get; }

            internal bool IsNull { get; }

            internal long Number { get; }
        }

        /// <summary>
        /// True when the test is true exactly when the element exists (or the count is above zero), false when it is true exactly when it does not,
        /// null when it is anything else (or is not understood): the test is evaluated for the states of the value that tell the cases apart.
        /// </summary>
        private static bool? Polarity(ExistenceKind kind, Func<Sample, bool?> test)
        {
            var existing = kind == ExistenceKind.Item ? new[] { new Sample(kind, false, 0) } : CountSamples.Skip(1).Select(number => new Sample(kind, false, number)).ToArray();
            var missing = kind == ExistenceKind.Item ? new[] { new Sample(kind, true, 0) } : new[] { new Sample(kind, false, CountSamples[0]) };

            var whenExisting = existing.Select(test).ToArray();
            var whenMissing = missing.Select(test).ToArray();
            if (whenExisting.Concat(whenMissing).Any(result => result == null))
                return null;

            if (whenExisting.All(result => result == true) && whenMissing.All(result => result == false))
                return true;

            return whenExisting.All(result => result == false) && whenMissing.All(result => result == true) ? false : null;
        }

        private static bool? EvaluatePattern(PatternSyntax pattern, Sample sample, SemanticModel model)
        {
            switch (pattern)
            {
                case ParenthesizedPatternSyntax parenthesized:
                    return EvaluatePattern(parenthesized.Pattern, sample, model);
                case UnaryPatternSyntax unary when unary.IsKind(SyntaxKind.NotPattern):
                    return !EvaluatePattern(unary.Pattern, sample, model);
                case BinaryPatternSyntax binary:
                    return EvaluateBinaryPattern(binary, sample, model);
                case ConstantPatternSyntax constant when sample.Kind == ExistenceKind.Item:
                    return constant.Expression.IsKind(SyntaxKind.NullLiteralExpression) ? sample.IsNull : null;
                case ConstantPatternSyntax constant:
                    return EvaluateNumber(SyntaxKind.EqualsExpression, constant.Expression, sample, model);
                case RelationalPatternSyntax relational when sample.Kind == ExistenceKind.Count:
                    return EvaluateNumber(RelationalKind(relational.OperatorToken.Kind()), relational.Expression, sample, model);
                case RecursivePatternSyntax { Type: null, PositionalPatternClause: null, Designation: null, PropertyPatternClause.Subpatterns.Count: 0 } when sample.Kind == ExistenceKind.Item:
                    return !sample.IsNull;
                default:
                    return null;
            }
        }

        private static bool? EvaluateBinaryPattern(BinaryPatternSyntax binary, Sample sample, SemanticModel model)
        {
            var left = EvaluatePattern(binary.Left, sample, model);
            var right = EvaluatePattern(binary.Right, sample, model);
            if (left == null || right == null)
                return null;

            return binary.IsKind(SyntaxKind.AndPattern) ? left.Value && right.Value : left.Value || right.Value;
        }

        private static bool? EvaluateNumber(SyntaxKind operatorKind, ExpressionSyntax expression, Sample sample, SemanticModel model)
        {
            var constant = GetNumber(expression, model);
            return constant == null ? null : Compare(operatorKind, sample.Number, constant.Value);
        }

        private static SyntaxKind RelationalKind(SyntaxKind token) =>
            token switch
            {
                SyntaxKind.GreaterThanToken => SyntaxKind.GreaterThanExpression,
                SyntaxKind.GreaterThanEqualsToken => SyntaxKind.GreaterThanOrEqualExpression,
                SyntaxKind.LessThanToken => SyntaxKind.LessThanExpression,
                _ => SyntaxKind.LessThanOrEqualExpression,
            };
    }
}
