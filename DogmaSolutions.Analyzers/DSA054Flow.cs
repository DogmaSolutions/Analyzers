#nullable disable
using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    internal enum DSA054Scenario
    {
        /// <summary>The field is real state (or the code is too complex to prove it is not): nothing to report.</summary>
        None,

        /// <summary>The member assigns the field and then returns it.</summary>
        Memoization,

        /// <summary>The member assigns the field and then reads it for something else.</summary>
        Leftover,

        /// <summary>The field is assigned and never read.</summary>
        NeverRead,
    }

    internal readonly struct DSA054Result
    {
        internal DSA054Result(DSA054Scenario scenario, AssignmentExpressionSyntax assignment, bool canMemoize)
        {
            Scenario = scenario;
            Assignment = assignment;
            CanMemoize = canMemoize;
        }

        internal DSA054Scenario Scenario { get; }

        /// <summary>The first assignment of the field in the member, where the diagnostic is reported.</summary>
        internal AssignmentExpressionSyntax Assignment { get; }

        /// <summary>The assignment can be turned into <c>??=</c> without changing anything else: it is the first statement of the member and the only write.</summary>
        internal bool CanMemoize { get; }
    }

    /// <summary>
    /// Follows a field inside the one member that uses it: every read has to come after an assignment that always runs. The proof is deliberately
    /// narrow (the assignment is a statement of the body, or of a plain nested block or <c>lock</c>, that precedes every other use), so that anything
    /// more intricate is taken to be real state.
    /// </summary>
    internal sealed class DSA054Flow
    {
        private readonly List<IdentifierNameSyntax> _uses;
        private readonly bool _isStatic;
        private int _next;
        private bool _assigned;
        private int _reads;
        private int _writes;
        private bool _returned;
        private AssignmentExpressionSyntax _first;
        private StatementSyntax _firstStatement;

        private DSA054Flow(List<IdentifierNameSyntax> uses, bool isStatic)
        {
            _uses = uses;
            _isStatic = isStatic;
        }

        /// <summary>Whether the uses of a field are all plain assignments (a field written and never read).</summary>
        internal static bool AreAllPlainWrites(List<IdentifierNameSyntax> uses)
        {
            foreach (var use in uses)
            {
                var node = GetNode(use);
                if (!IsPlainWrite(node) || node.Parent.Parent is not (ExpressionStatementSyntax or InitializerExpressionSyntax))
                    return false;
            }

            return true;
        }

        internal static DSA054Result Analyze(SyntaxNode member, List<IdentifierNameSyntax> uses, bool isStatic)
        {
            var (block, expression) = GetBody(member);
            if ((block == null && expression == null) || uses.Count == 0)
                return default;

            var body = (SyntaxNode)block ?? expression;
            if (uses[0].SpanStart < body.SpanStart || CallsItself(member, body, GetName(member)))
                return default;

            var flow = new DSA054Flow(uses, isStatic);
            var ok = block != null ? flow.WalkAll(block.Statements) : flow.WalkExpressionBody(expression);
            return ok ? flow.Conclude(member) : default;
        }

        internal static IFieldSymbol GetField(VariableDeclaratorSyntax declarator, Compilation compilation, System.Threading.CancellationToken cancellationToken) =>
            (IFieldSymbol)compilation.GetSemanticModel(declarator.SyntaxTree).GetDeclaredSymbol(declarator, cancellationToken);

        /// <summary>The uses that are the field (and not a local or a parameter with the same name).</summary>
        internal static List<IdentifierNameSyntax> Bind(List<IdentifierNameSyntax> uses, IFieldSymbol field, Compilation compilation, System.Threading.CancellationToken cancellationToken)
        {
            var result = new List<IdentifierNameSyntax>(uses.Count);
            foreach (var use in uses)
            {
                var symbol = compilation.GetSemanticModel(use.SyntaxTree).GetSymbolInfo(use, cancellationToken).Symbol;
                if (symbol != null && SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, field.OriginalDefinition))
                    result.Add(use);
            }

            return result;
        }

        /// <summary>A reference type, or a <c>Nullable&lt;T&gt;</c>: the types where <c>??=</c> compiles and null means "not computed yet".</summary>
        internal static bool CanHoldNull(VariableDeclaratorSyntax declarator, Compilation compilation, System.Threading.CancellationToken cancellationToken)
        {
            var type = GetField(declarator, compilation, cancellationToken).Type;
            return (type.IsReferenceType && type.TypeKind != TypeKind.TypeParameter) || type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
        }

        internal static string GetName(SyntaxNode member) => member is AccessorDeclarationSyntax accessor
            ? GetIdentifier(accessor.Parent.Parent) + "." + accessor.Keyword.ValueText
            : GetIdentifier(member);

        internal static SyntaxNode GetMember(SyntaxNode node) => node.Parent.FirstAncestorOrSelf<SyntaxNode>(static ancestor => ancestor is MemberDeclarationSyntax or AccessorDeclarationSyntax);

        private static string GetIdentifier(SyntaxNode member) => member switch
        {
            MethodDeclarationSyntax method => method.Identifier.ValueText,
            ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
            DestructorDeclarationSyntax destructor => destructor.Identifier.ValueText,
            PropertyDeclarationSyntax property => property.Identifier.ValueText,
            EventDeclarationSyntax @event => @event.Identifier.ValueText,
            IndexerDeclarationSyntax => "this[]",
            OperatorDeclarationSyntax @operator => "operator " + @operator.OperatorToken.ValueText,
            ConversionOperatorDeclarationSyntax conversion => "operator " + conversion.Type,
            _ => member.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>().Identifier.ValueText,
        };

        private static (BlockSyntax Block, ExpressionSyntax Expression) GetBody(SyntaxNode member) => member switch
        {
            BaseMethodDeclarationSyntax method => (method.Body, method.ExpressionBody?.Expression),
            AccessorDeclarationSyntax accessor => (accessor.Body, accessor.ExpressionBody?.Expression),
            PropertyDeclarationSyntax property => (null, property.ExpressionBody?.Expression),
            IndexerDeclarationSyntax indexer => (null, indexer.ExpressionBody?.Expression),
            _ => (null, null),
        };

        /// <summary>A member that calls itself (a recursion) can see its own assignment from the inner call: the field is state shared by the calls.</summary>
        private static bool CallsItself(SyntaxNode member, SyntaxNode body, string name)
        {
            var dot = name.IndexOf('.');
            var simple = dot >= 0 ? name.Substring(0, dot) : name;

            // most bodies do not even mention the name: the text tells it without a single token
            if (body.SyntaxTree.GetText().ToString(body.Span).IndexOf(simple, StringComparison.Ordinal) < 0)
                return false;

            var owner = (member.Parent as BaseTypeDeclarationSyntax)?.Identifier.ValueText ?? (member.Parent?.Parent as BaseTypeDeclarationSyntax)?.Identifier.ValueText;
            foreach (var token in body.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.IdentifierToken) && string.Equals(token.ValueText, simple, StringComparison.Ordinal) && token.Parent is IdentifierNameSyntax use && IsOwnCall(use, owner))
                    return true;
            }

            return false;
        }

        /// <summary>The name is not qualified, or it is qualified by <c>this</c> or by the type itself (<c>other.Dispose()</c> is a call to another object).</summary>
        private static bool IsOwnCall(IdentifierNameSyntax use, string owner) =>
            use.Parent is not MemberAccessExpressionSyntax access || access.Name != use || access.Expression is ThisExpressionSyntax ||
            (access.Expression is IdentifierNameSyntax qualifier && qualifier.Identifier.ValueText == owner);

        private bool WalkExpressionBody(ExpressionSyntax expression)
        {
            if (expression is not AssignmentExpressionSyntax assignment || !assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
                return false;

            return TryAssign(assignment, _firstStatement, valueIsUsed: true);
        }

        private bool WalkAll(SyntaxList<StatementSyntax> statements)
        {
            foreach (var statement in statements)
            {
                if (!Walk(statement))
                    return false;
            }

            return true;
        }

        private bool Walk(StatementSyntax statement)
        {
            if (_next >= _uses.Count)
                return true;

            if (_assigned)
                return ClassifyInside(statement.Span.End);

            if (_uses[_next].SpanStart >= statement.Span.End)
                return true;

            switch (statement)
            {
                case BlockSyntax block:
                    return WalkAll(block.Statements);
                case LockStatementSyntax @lock:
                    return _uses[_next].SpanStart >= @lock.Expression.Span.End && Walk(@lock.Statement);
                case ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax assignment } when assignment.IsKind(SyntaxKind.SimpleAssignmentExpression):
                    return TryAssign(assignment, statement, valueIsUsed: false);
                case ReturnStatementSyntax { Expression: AssignmentExpressionSyntax returned } when returned.IsKind(SyntaxKind.SimpleAssignmentExpression):
                    return TryAssign(returned, statement, valueIsUsed: true);
                default:
                    return false;
            }
        }

        /// <summary>The first use of the field is the left side of this assignment, and nothing else in the assignment uses it.</summary>
        private bool TryAssign(AssignmentExpressionSyntax assignment, StatementSyntax statement, bool valueIsUsed)
        {
            var use = _uses[_next];
            if (!assignment.Left.Span.Contains(use.Span) || (_next + 1 < _uses.Count && _uses[_next + 1].SpanStart < assignment.Span.End) || !Accepts(use))
                return false;

            _next++;
            _assigned = true;
            _writes++;
            _first = assignment;
            _firstStatement = statement ?? assignment.FirstAncestorOrSelf<StatementSyntax>();
            if (valueIsUsed)
            {
                _reads++;
                _returned = true;
            }

            return true;
        }

        private bool ClassifyInside(int end)
        {
            while (_next < _uses.Count && _uses[_next].SpanStart < end)
            {
                if (!Classify(_uses[_next++]))
                    return false;
            }

            return true;
        }

        private bool Classify(IdentifierNameSyntax use)
        {
            if (!Accepts(use))
                return false;

            var node = GetNode(use);
            if (IsPlainWrite(node))
            {
                _writes++;
                return true;
            }

            _reads++;
            if (node.Parent is ReturnStatementSyntax)
                _returned = true;

            return true;
        }

        /// <summary>The ways of using a field that make it more than a plain variable of the member: another instance, an alias, a closure, a deconstruction.</summary>
        private bool Accepts(IdentifierNameSyntax use)
        {
            if (IsInsideNestedFunction(use) || IsOfAnotherInstance(use))
                return false;

            var node = GetNode(use);
            return node.Parent switch
            {
                ArgumentSyntax argument => argument.RefKindKeyword.IsKind(SyntaxKind.None) && !IsDeconstructionTarget(argument),
                PrefixUnaryExpressionSyntax prefix => !prefix.IsKind(SyntaxKind.AddressOfExpression),
                AssignmentExpressionSyntax assignment => assignment.Left != node || !assignment.IsKind(SyntaxKind.CoalesceAssignmentExpression),
                _ => !IsDeconstructionTarget(node),
            };
        }

        private bool IsOfAnotherInstance(IdentifierNameSyntax use) => use.Parent switch
        {
            MemberAccessExpressionSyntax access when access.Name == use => !_isStatic && access.Expression is not ThisExpressionSyntax,
            MemberBindingExpressionSyntax => true,
            _ => false,
        };

        private static bool IsInsideNestedFunction(SyntaxNode use)
        {
            for (var current = use.Parent; current is not (null or MemberDeclarationSyntax or AccessorDeclarationSyntax); current = current.Parent)
            {
                if (current is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or QueryExpressionSyntax)
                    return true;
            }

            return false;
        }

        private static bool IsDeconstructionTarget(SyntaxNode node)
        {
            var current = node;
            while (current.Parent is ArgumentSyntax or TupleExpressionSyntax or ParenthesizedExpressionSyntax)
                current = current.Parent;

            return current != node && current.Parent is AssignmentExpressionSyntax assignment && assignment.Left == current;
        }

        /// <summary>The expression that stands for the field: <c>this.f</c> or <c>f</c>.</summary>
        private static SyntaxNode GetNode(IdentifierNameSyntax use) => use.Parent is MemberAccessExpressionSyntax access && access.Name == use ? access : use;

        private static bool IsPlainWrite(SyntaxNode node) =>
            node.Parent is AssignmentExpressionSyntax assignment && assignment.Left == node && assignment.IsKind(SyntaxKind.SimpleAssignmentExpression);

        private DSA054Result Conclude(SyntaxNode member)
        {
            if (_next < _uses.Count)
                return default;

            if (_reads == 0)
                return new DSA054Result(DSA054Scenario.NeverRead, _first, false);

            var scenario = _returned ? DSA054Scenario.Memoization : DSA054Scenario.Leftover;
            return new DSA054Result(scenario, _first, scenario == DSA054Scenario.Memoization && _writes == 1 && IsFirstStatement(member) && member is not (ConstructorDeclarationSyntax or DestructorDeclarationSyntax));
        }

        private bool IsFirstStatement(SyntaxNode member)
        {
            var (block, expression) = GetBody(member);
            return block != null ? block.Statements.Count > 0 && block.Statements[0] == _firstStatement : expression == _first;
        }
    }
}
