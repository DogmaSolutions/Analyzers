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
        private readonly ExpressionSyntax _receiver;
        private readonly SemanticModel _model;
        private ITypeSymbol _receiverType;
        private bool _receiverTypeKnown;

        internal ExistenceCall(IMethodSymbol method, IMethodSymbol alternative, bool isAsync, bool isQueryable, bool hasPredicate, ExpressionSyntax receiver, SemanticModel model)
        {
            Method = method;
            Alternative = alternative;
            IsAsync = isAsync;
            IsQueryable = isQueryable;
            HasPredicate = hasPredicate;
            _receiver = receiver;
            _model = model;
        }

        internal IMethodSymbol Method { get; }

        /// <summary>The extension method with the same parameters that returns whether there is a match (<c>Any</c>, <c>AnyAsync</c>).</summary>
        internal IMethodSymbol Alternative { get; }

        internal bool IsAsync { get; }

        /// <summary>The source is an <c>IQueryable</c>: the call is translated by a provider, typically into SQL.</summary>
        internal bool IsQueryable { get; }

        internal bool HasPredicate { get; }

        /// <summary>The static type of the expression the method is called on (asked to the semantic model when it is needed: not every rule needs it).</summary>
        internal ITypeSymbol ReceiverType
        {
            get
            {
                if (!_receiverTypeKnown)
                {
                    _receiverType = _receiver == null ? null : _model.GetTypeInfo(_receiver).Type;
                    _receiverTypeKnown = true;
                }

                return _receiverType;
            }
        }

        /// <summary>The type of the elements of the sequence.</summary>
        internal ITypeSymbol ElementType => Method.TypeArguments.FirstOrDefault();
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

    /// <summary>The use of the result of a call, as far as the syntax tells: compared right away, or kept in a variable that is only compared.</summary>
    internal sealed class ExistenceShapeInfo
    {
        internal ExistenceShapeInfo(InvocationExpressionSyntax invocation, ExpressionSyntax value, bool awaited, ExistenceShape shape, LocalDeclarationStatementSyntax declaration, VariableDeclaratorSyntax declarator, IReadOnlyList<IdentifierNameSyntax> uses)
        {
            Invocation = invocation;
            Value = value;
            Awaited = awaited;
            Shape = shape;
            Declaration = declaration;
            Declarator = declarator;
            Uses = uses;
        }

        internal InvocationExpressionSyntax Invocation { get; }

        internal ExpressionSyntax Value { get; }

        /// <summary>The call is awaited (with <c>ConfigureAwait</c> or not).</summary>
        internal bool Awaited { get; }

        internal ExistenceShape Shape { get; }

        internal LocalDeclarationStatementSyntax Declaration { get; }

        internal VariableDeclaratorSyntax Declarator { get; }

        /// <summary>The places the variable is used at, all of them comparisons in shape.</summary>
        internal IReadOnlyList<IdentifierNameSyntax> Uses { get; }
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
        private static readonly long[] CountSamples = { 1, 2, 5, 100, 1000000, int.MaxValue, long.MaxValue };

        // the states the tests are evaluated for, made once
        private static readonly Sample[] ItemExisting = { new(ExistenceKind.Item, false, 0) };
        private static readonly Sample ItemMissing = new(ExistenceKind.Item, true, 0);
        private static readonly Sample[] CountExisting = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(CountSamples, number => new Sample(ExistenceKind.Count, false, number)));
        private static readonly Sample CountMissing = new(ExistenceKind.Count, false, 0);

        /// <summary>The method that answers the existence question instead of the method (null: there is none), remembered: the same few methods are called thousands of times.</summary>
        private sealed class Alternative
        {
            internal Alternative(IMethodSymbol definition)
            {
                var isAsync = definition.Name.EndsWith("Async", StringComparison.Ordinal) && IsTask(definition.ReturnType);
                IsAsync = isAsync;
                IsValid = isAsync || !IsTask(definition.ReturnType);
                Method = FindAlternative(definition, isAsync ? "AnyAsync" : "Any");
                IsQueryable = definition.Parameters[0].Type is INamedTypeSymbol { Name: "IQueryable" };
                HasPredicate = HasPredicateParameter(definition);
            }

            internal IMethodSymbol Method { get; }

            internal bool IsAsync { get; }

            /// <summary>A method that returns a task is called by its async name only.</summary>
            internal bool IsValid { get; }

            internal bool IsQueryable { get; }

            internal bool HasPredicate { get; }
        }

        private static bool HasPredicateParameter(IMethodSymbol definition)
        {
            for (var index = 1; index < definition.Parameters.Length; index++)
            {
                if (definition.Parameters[index].Type is INamedTypeSymbol { Name: "Func" or "Expression" })
                    return true;
            }

            return false;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IMethodSymbol, Alternative> _alternatives = new();

        // ---- the call -------------------------------------------------------------------------------------------------

        /// <summary>The extension method called by the invocation and the one that answers the existence question instead; null when there is none.</summary>
        internal static ExistenceCall ResolveCall(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
                return null;

            var definition = method.ReducedFrom ?? method;
            if (!definition.IsStatic || !definition.IsExtensionMethod || definition.Parameters.IsEmpty)
                return null;

            var alternative = _alternatives.GetValue(definition.OriginalDefinition, key => new Alternative(key));
            if (alternative.Method == null || !alternative.IsValid)
                return null;

            return new ExistenceCall(method, alternative.Method, alternative.IsAsync, alternative.IsQueryable, alternative.HasPredicate, GetReceiver(invocation, method), model);
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

        /// <summary>
        /// What can be told about the use of the result of the call without asking the semantic model: it is compared right away, or kept in a local variable
        /// whose every use is a comparison. Most results are returned, passed or used, and are rejected here.
        /// </summary>
        internal static ExistenceShapeInfo GetShape(InvocationExpressionSyntax invocation, ExistenceKind kind)
        {
            var value = PeelUp(invocation, out var awaited);
            if (value.Parent is EqualsValueClauseSyntax clause && clause.Value == value &&
                clause.Parent is VariableDeclaratorSyntax declarator && declarator.Parent is VariableDeclarationSyntax { Variables.Count: 1, Parent: LocalDeclarationStatementSyntax { Modifiers.Count: 0, UsingKeyword.RawKind: 0 } declaration })
            {
                var uses = FindLocalUses(declarator, declaration, kind);
                return uses == null ? null : new ExistenceShapeInfo(invocation, value, awaited, ExistenceShape.Local, declaration, declarator, uses);
            }

            return IsComparisonShaped(value, kind) ? new ExistenceShapeInfo(invocation, value, awaited, ExistenceShape.Direct, null, null, null) : null;
        }

        /// <summary>The existence test the result of the call is used for, or null when it is used for anything else.</summary>
        internal static ExistenceUse Analyze(InvocationExpressionSyntax invocation, ExistenceCall call, ExistenceKind kind, SemanticModel model, CancellationToken cancellationToken)
        {
            var shape = GetShape(invocation, kind);
            return shape == null ? null : Complete(shape, call, kind, model, cancellationToken);
        }

        /// <summary>The shape checked against the semantic model: the variable is the one that is compared, and the numbers compared with are zero and one.</summary>
        internal static ExistenceUse Complete(ExistenceShapeInfo shape, ExistenceCall call, ExistenceKind kind, SemanticModel model, CancellationToken cancellationToken)
        {
            if (call.IsAsync != shape.Awaited)
                return null;

            if (shape.Shape == ExistenceShape.Direct)
            {
                var comparison = Classify(shape.Value, kind, model);
                return comparison == null ? null : new ExistenceUse(shape.Invocation, shape.Value, call, ExistenceShape.Direct, new[] { comparison.Value }, null);
            }

            var comparisons = ClassifyLocalUses(shape, kind, model, cancellationToken);
            return comparisons == null ? null : new ExistenceUse(shape.Invocation, shape.Value, call, ExistenceShape.Local, comparisons, shape.Declaration);
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

        // ---- the variable ------------------------------------------------------------------------------------------------

        /// <summary>The names used in a block, with the places they are used at: built once and used by all the variables of the block.</summary>
        private sealed class IdentifierIndex
        {
            internal IdentifierIndex(SyntaxNode scope)
            {
                foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
                {
                    var name = identifier.Identifier.ValueText;
                    if (!ByName.TryGetValue(name, out var list))
                        ByName[name] = list = new List<IdentifierNameSyntax>();

                    list.Add(identifier);
                }
            }

            internal Dictionary<string, List<IdentifierNameSyntax>> ByName { get; } = new(StringComparer.Ordinal);
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<SyntaxNode, IdentifierIndex> _indexes = new();

        /// <summary>
        /// The places the variable is used at, when every one of them is a comparison (null when one is not, or there is none). A name that is only the member
        /// of something else (<c>x.Id</c>) is not a use.
        /// </summary>
        private static List<IdentifierNameSyntax> FindLocalUses(VariableDeclaratorSyntax declarator, LocalDeclarationStatementSyntax declaration, ExistenceKind kind)
        {
            var scope = declaration.Parent;
            if (scope is not (BlockSyntax or SwitchSectionSyntax) || !_indexes.GetValue(scope, key => new IdentifierIndex(key)).ByName.TryGetValue(declarator.Identifier.ValueText, out var candidates))
                return null;

            var uses = new List<IdentifierNameSyntax>();
            foreach (var identifier in candidates)
            {
                if (identifier.SpanStart < declarator.Span.End || IsMemberName(identifier))
                    continue;

                // a use in a lambda or a local function runs when, and as many times as, the caller decides
                if (IsInNestedFunction(identifier, scope) || !IsComparisonShaped(PeelParentheses(identifier), kind))
                    return null;

                uses.Add(identifier);
            }

            return uses.Count == 0 ? null : uses;
        }

        /// <summary>The name is not a reference to a variable: it follows a dot, names an argument, a member of an initializer or a property of an anonymous type.</summary>
        private static bool IsMemberName(IdentifierNameSyntax identifier) =>
            identifier.Parent switch
            {
                MemberAccessExpressionSyntax memberAccess => memberAccess.Name == identifier,
                MemberBindingExpressionSyntax => true,
                QualifiedNameSyntax qualified => qualified.Right == identifier,
                NameColonSyntax or NameEqualsSyntax => true,
                AssignmentExpressionSyntax { Parent: InitializerExpressionSyntax } assignment => assignment.Left == identifier,
                _ => false,
            };

        private static List<ExistenceComparison> ClassifyLocalUses(ExistenceShapeInfo shape, ExistenceKind kind, SemanticModel model, CancellationToken cancellationToken)
        {
            var local = model.GetDeclaredSymbol(shape.Declarator, cancellationToken);
            var comparisons = new List<ExistenceComparison>();

            // the name can stand for something else than the variable
            foreach (var identifier in shape.Uses.Where(identifier => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier, cancellationToken).Symbol, local)))
            {
                var comparison = Classify(PeelParentheses(identifier), kind, model);
                if (comparison == null)
                    return null;

                comparisons.Add(comparison.Value);
            }

            return comparisons.Count == 0 ? null : comparisons;
        }

        // ---- the shape of the comparison, without the semantic model -----------------------------------------------------------

        /// <summary>The value is compared with null (or with something that can be a number) right where it is: the question that decides is asked to the model later.</summary>
        private static bool IsComparisonShaped(ExpressionSyntax value, ExistenceKind kind)
        {
            switch (value.Parent)
            {
                case BinaryExpressionSyntax binary:
                    var other = binary.Left == value ? binary.Right : binary.Left;
                    return kind == ExistenceKind.Item
                        ? (binary.IsKind(SyntaxKind.EqualsExpression) || binary.IsKind(SyntaxKind.NotEqualsExpression)) && other.IsKind(SyntaxKind.NullLiteralExpression)
                        : IsComparisonOperator(binary) && MayBeNumber(other);
                case IsPatternExpressionSyntax isPattern:
                    return isPattern.Expression == value && IsPatternShaped(isPattern.Pattern, kind);
                default:
                    return false;
            }
        }

        private static bool IsComparisonOperator(BinaryExpressionSyntax binary) =>
            binary.Kind() is SyntaxKind.EqualsExpression or SyntaxKind.NotEqualsExpression or SyntaxKind.GreaterThanExpression or SyntaxKind.GreaterThanOrEqualExpression or SyntaxKind.LessThanExpression or SyntaxKind.LessThanOrEqualExpression;

        private static bool MayBeNumber(ExpressionSyntax expression) =>
            expression switch
            {
                LiteralExpressionSyntax literal => literal.IsKind(SyntaxKind.NumericLiteralExpression),
                IdentifierNameSyntax or MemberAccessExpressionSyntax => true,
                ParenthesizedExpressionSyntax parenthesized => MayBeNumber(parenthesized.Expression),
                PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.UnaryMinusExpression) && MayBeNumber(prefix.Operand),
                _ => false,
            };

        private static bool IsPatternShaped(PatternSyntax pattern, ExistenceKind kind) =>
            pattern switch
            {
                ParenthesizedPatternSyntax parenthesized => IsPatternShaped(parenthesized.Pattern, kind),
                UnaryPatternSyntax unary => unary.IsKind(SyntaxKind.NotPattern) && IsPatternShaped(unary.Pattern, kind),
                BinaryPatternSyntax binary => IsPatternShaped(binary.Left, kind) && IsPatternShaped(binary.Right, kind),
                ConstantPatternSyntax constant => kind == ExistenceKind.Item ? constant.Expression.IsKind(SyntaxKind.NullLiteralExpression) : MayBeNumber(constant.Expression),
                RelationalPatternSyntax relational => kind == ExistenceKind.Count && MayBeNumber(relational.Expression),
                RecursivePatternSyntax recursive => kind == ExistenceKind.Item && recursive.Type == null && recursive.PositionalPatternClause == null && recursive.Designation == null && recursive.PropertyPatternClause?.Subpatterns.Count == 0,
                _ => false,
            };

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
            var present = value.Parent switch
            {
                BinaryExpressionSyntax binary => EvaluateBinary(binary, value, kind, model),
                IsPatternExpressionSyntax isPattern when isPattern.Expression == value => Polarity(kind, sample => EvaluatePattern(isPattern.Pattern, sample, model)),
                _ => null,
            };

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
                return other.IsKind(SyntaxKind.NullLiteralExpression) && (operatorKind == SyntaxKind.EqualsExpression || operatorKind == SyntaxKind.NotEqualsExpression) ? sample.IsNull == (operatorKind == SyntaxKind.EqualsExpression) : null;
            }

            var constant = GetNumber(other, model);
            if (constant == null)
                return null;

            return valueIsLeft ? Compare(operatorKind, sample.Number, constant.Value) : Compare(operatorKind, constant.Value, sample.Number);
        }

        private static bool? Compare(SyntaxKind operatorKind, long left, long right) =>
            operatorKind switch
            {
                SyntaxKind.EqualsExpression => left == right,
                SyntaxKind.NotEqualsExpression => left != right,
                SyntaxKind.GreaterThanExpression => left > right,
                SyntaxKind.GreaterThanOrEqualExpression => left >= right,
                SyntaxKind.LessThanExpression => left < right,
                SyntaxKind.LessThanOrEqualExpression => left <= right,
                _ => null,
            };

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
            // every state with an element (or a count above zero) must give the same answer, and the state without one the opposite
            bool? existing = null;
            foreach (var sample in kind == ExistenceKind.Item ? ItemExisting : CountExisting)
            {
                var result = test(sample);
                if (result == null || (existing != null && existing != result))
                    return null;

                existing = result;
            }

            var missing = test(kind == ExistenceKind.Item ? ItemMissing : CountMissing);
            return missing == null || missing == existing ? null : existing;
        }

        private static bool? EvaluatePattern(PatternSyntax pattern, Sample sample, SemanticModel model) =>
            pattern switch
            {
                ParenthesizedPatternSyntax parenthesized => EvaluatePattern(parenthesized.Pattern, sample, model),
                UnaryPatternSyntax unary when unary.IsKind(SyntaxKind.NotPattern) => !EvaluatePattern(unary.Pattern, sample, model),
                BinaryPatternSyntax binary => EvaluateBinaryPattern(binary, sample, model),
                ConstantPatternSyntax constant when sample.Kind == ExistenceKind.Item => constant.Expression.IsKind(SyntaxKind.NullLiteralExpression) ? sample.IsNull : null,
                ConstantPatternSyntax constant => EvaluateNumber(SyntaxKind.EqualsExpression, constant.Expression, sample, model),
                RelationalPatternSyntax relational when sample.Kind == ExistenceKind.Count => EvaluateNumber(RelationalKind(relational.OperatorToken.Kind()), relational.Expression, sample, model),
                RecursivePatternSyntax { Type: null, PositionalPatternClause: null, Designation: null, PropertyPatternClause.Subpatterns.Count: 0 } when sample.Kind == ExistenceKind.Item => !sample.IsNull,
                _ => null,
            };

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
