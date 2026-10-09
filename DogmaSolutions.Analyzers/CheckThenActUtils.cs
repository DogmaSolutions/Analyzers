using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Shared pattern detection for "if not exists, then insert" check-then-act patterns.
/// Used by DSA012 (database TOCTOU), DSA017 (collections with atomic alternatives),
/// and DSA018 (collections without atomic alternatives).
/// </summary>
internal static class CheckThenActUtils
{
    private static class Namespaces
    {
        internal const string GenericCollections = "System.Collections.Generic";
        internal const string ConcurrentCollections = "System.Collections.Concurrent";
        internal const string ImmutableCollections = "System.Collections.Immutable";
        internal const string Linq = "System.Linq";
        internal const string EntityFrameworkCore = "Microsoft.EntityFrameworkCore";
    }

    private static class TypeNames
    {
        internal const string Dictionary = "Dictionary";
        internal const string HashSet = "HashSet";
        internal const string SortedSet = "SortedSet";
        internal const string SortedDictionary = "SortedDictionary";
        internal const string SortedList = "SortedList";
        internal const string ConcurrentDictionary = "ConcurrentDictionary";
        internal const string ImmutableHashSet = "ImmutableHashSet";
        internal const string ImmutableSortedSet = "ImmutableSortedSet";
        internal const string ImmutableDictionary = "ImmutableDictionary";
        internal const string ImmutableSortedDictionary = "ImmutableSortedDictionary";
        internal const string DbSet = "DbSet";
        internal const string IQueryable = "IQueryable";
    }

    private static class MethodNames
    {
        internal const string Any = "Any";
        internal const string AnyAsync = "AnyAsync";
        internal const string Exists = "Exists";
        internal const string ExistsAsync = "ExistsAsync";
        internal const string Contains = "Contains";
        internal const string ContainsAsync = "ContainsAsync";
        internal const string ContainsKey = "ContainsKey";
        internal const string TryGetValue = "TryGetValue";
        internal const string Count = "Count";
        internal const string CountAsync = "CountAsync";
        internal const string FirstOrDefault = "FirstOrDefault";
        internal const string FirstOrDefaultAsync = "FirstOrDefaultAsync";
        internal const string SingleOrDefault = "SingleOrDefault";
        internal const string SingleOrDefaultAsync = "SingleOrDefaultAsync";
        internal const string Find = "Find";
        internal const string FindAsync = "FindAsync";
        internal const string Add = "Add";
        internal const string AddAsync = "AddAsync";
        internal const string AddRange = "AddRange";
        internal const string AddRangeAsync = "AddRangeAsync";
    }

    private const string ZeroLiteralText = "0";

    internal static readonly string[] BooleanExistenceMethods =
    {
        MethodNames.Any, MethodNames.AnyAsync, MethodNames.Exists, MethodNames.ExistsAsync,
        MethodNames.Contains, MethodNames.ContainsAsync, MethodNames.ContainsKey, MethodNames.TryGetValue
    };

    internal static readonly string[] CountMethods = { MethodNames.Count, MethodNames.CountAsync };

    internal static readonly string[] FindMethods =
    {
        MethodNames.FirstOrDefault, MethodNames.FirstOrDefaultAsync,
        MethodNames.SingleOrDefault, MethodNames.SingleOrDefaultAsync,
        MethodNames.Find, MethodNames.FindAsync
    };

    internal static readonly string[] InsertMethods = { MethodNames.Add, MethodNames.AddAsync, MethodNames.AddRange, MethodNames.AddRangeAsync };

    /// <summary>
    /// Maps (namespace, type name) of the collection types that offer an atomic alternative to the check-then-act pattern
    /// to the name of the RESX entry holding the user-facing suggestion.
    /// </summary>
    private static readonly Dictionary<(string Namespace, string TypeName), string> AtomicAlternativeSuggestionResources = new()
    {
        [(Namespaces.GenericCollections, TypeNames.Dictionary)] = nameof(Resources.CheckThenActSuggestionTryAddOrIndexer),
        [(Namespaces.GenericCollections, TypeNames.SortedDictionary)] = nameof(Resources.CheckThenActSuggestionTryAddOrIndexer),
        [(Namespaces.GenericCollections, TypeNames.SortedList)] = nameof(Resources.CheckThenActSuggestionIndexer),
        [(Namespaces.GenericCollections, TypeNames.HashSet)] = nameof(Resources.CheckThenActSuggestionAddReturnsBool),
        [(Namespaces.GenericCollections, TypeNames.SortedSet)] = nameof(Resources.CheckThenActSuggestionAddReturnsBool),
        [(Namespaces.ConcurrentCollections, TypeNames.ConcurrentDictionary)] = nameof(Resources.CheckThenActSuggestionConcurrentDictionary),
        [(Namespaces.ImmutableCollections, TypeNames.ImmutableHashSet)] = nameof(Resources.CheckThenActSuggestionImmutableSetAdd),
        [(Namespaces.ImmutableCollections, TypeNames.ImmutableSortedSet)] = nameof(Resources.CheckThenActSuggestionImmutableSetAdd),
        [(Namespaces.ImmutableCollections, TypeNames.ImmutableDictionary)] = nameof(Resources.CheckThenActSuggestionImmutableDictionarySetItem),
        [(Namespaces.ImmutableCollections, TypeNames.ImmutableSortedDictionary)] = nameof(Resources.CheckThenActSuggestionImmutableDictionarySetItem),
    };

    /// <summary>
    /// Detects a check-then-act pattern in the given if statement and returns
    /// the receiver expression of the existence check (for type resolution).
    /// </summary>
    internal static bool TryMatchCheckThenAct(IfStatementSyntax ifStatement, out ExpressionSyntax existenceCheckReceiver)
    {
        var matched = TryMatchInsertInsideNegatedCheck(ifStatement, out var receiver) ||
                      TryMatchInsertAfterPositiveCheck(ifStatement, out receiver);

        existenceCheckReceiver = matched ? receiver : null;
        return matched;
    }

    // Pattern A: if (!collection.Any(...)) { collection.Add(...); }
    private static bool TryMatchInsertInsideNegatedCheck(IfStatementSyntax ifStatement, out ExpressionSyntax receiver)
    {
        return IsNegatedExistenceCheck(ifStatement.Condition, out receiver) &&
               ContainsMatchingInsertInvocation(ifStatement.Statement, NormalizeReceiver(receiver));
    }

    // Pattern B and C, both guarded by a positive check.
    private static bool TryMatchInsertAfterPositiveCheck(IfStatementSyntax ifStatement, out ExpressionSyntax receiver)
    {
        if (!IsPositiveExistenceCheck(ifStatement.Condition, out receiver))
            return false;

        var receiverText = NormalizeReceiver(receiver);
        return IsThrowGuardFollowedByInsert(ifStatement, receiverText) ||
               IsInsertInElseBranch(ifStatement, receiverText);
    }

    // Pattern B: if (collection.Any(...)) { throw; } ... collection.Add(...);
    private static bool IsThrowGuardFollowedByInsert(IfStatementSyntax ifStatement, string receiverText)
    {
        return ifStatement.Else == null &&
               ContainsThrowStatement(ifStatement.Statement) &&
               HasSubsequentMatchingInsertInvocation(ifStatement, receiverText);
    }

    // Pattern C: if (collection.Any(...)) { ... } else { collection.Add(...); }
    private static bool IsInsertInElseBranch(IfStatementSyntax ifStatement, string receiverText)
    {
        return ifStatement.Else != null &&
               ContainsMatchingInsertInvocation(ifStatement.Else.Statement, receiverText);
    }

    /// <summary>
    /// Categorizes the receiver type to determine which analyzer should handle it.
    /// </summary>
    internal enum ReceiverCategory
    {
        Database,
        AtomicAlternative,
        NoAtomicAlternative
    }

    internal static ReceiverCategory CategorizeReceiverType(ITypeSymbol type)
    {
        if (type == null)
            return ReceiverCategory.NoAtomicAlternative;

        if (ImplementsIQueryable(type))
            return ReceiverCategory.Database;

        return HasAtomicAlternative(type, out _)
            ? ReceiverCategory.AtomicAlternative
            : ReceiverCategory.NoAtomicAlternative;
    }

    internal static bool HasAtomicAlternative(ITypeSymbol type, out string suggestion)
    {
        suggestion = null;
        if (type == null)
            return false;

        if (!AtomicAlternativeSuggestionResources.TryGetValue((GetNamespaceName(type), type.Name), out var resourceName))
            return false;

        suggestion = Resources.ResourceManager.GetString(resourceName, Resources.Culture);
        return true;
    }

    internal static ITypeSymbol ResolveReceiverType(ExpressionSyntax receiver, SemanticModel semanticModel)
    {
        return semanticModel.GetTypeInfo(receiver).Type;
    }

    private static bool ImplementsIQueryable(ITypeSymbol type)
    {
        return IsType(type, Namespaces.EntityFrameworkCore, TypeNames.DbSet) ||
               type.AllInterfaces.Any(iface => IsType(iface, Namespaces.Linq, TypeNames.IQueryable));
    }

    private static bool IsType(ITypeSymbol type, string namespaceName, string typeName)
    {
        return type.Name == typeName && GetNamespaceName(type) == namespaceName;
    }

    private static string GetNamespaceName(ITypeSymbol type)
    {
        return type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
    }

    internal static bool IsNegatedExistenceCheck(ExpressionSyntax condition, out ExpressionSyntax receiver)
    {
        receiver = null;

        switch (condition)
        {
            // Handle: !collection.Any(...)
            case PrefixUnaryExpressionSyntax prefixUnary when prefixUnary.IsKind(SyntaxKind.LogicalNotExpression):
                return IsExistenceCheckInvocation(prefixUnary.Operand, BooleanExistenceMethods, out receiver);

            // Handle: collection.Any(...) == false, collection.Count(...) == 0, collection.FirstOrDefault(...) == null (either operand order)
            case BinaryExpressionSyntax binary when binary.IsKind(SyntaxKind.EqualsExpression):
                return IsInvocationComparedTo(binary, BooleanExistenceMethods, IsFalseLiteral, out receiver) ||
                       IsInvocationComparedTo(binary, CountMethods, IsZeroLiteral, out receiver) ||
                       IsInvocationComparedTo(binary, FindMethods, IsNullLiteral, out receiver);

            default:
                return false;
        }
    }

    internal static bool IsPositiveExistenceCheck(ExpressionSyntax condition, out ExpressionSyntax receiver)
    {
        // Handle: collection.Any(...)
        if (IsExistenceCheckInvocation(condition, BooleanExistenceMethods, out receiver))
            return true;

        receiver = null;
        if (condition is not BinaryExpressionSyntax binary)
            return false;

        switch (binary.Kind())
        {
            // Handle: collection.Count(...) > 0
            case SyntaxKind.GreaterThanExpression:
                return IsExistenceCheckInvocation(binary.Left, CountMethods, out receiver) && IsZeroLiteral(binary.Right);

            // Handle: 0 < collection.Count(...)
            case SyntaxKind.LessThanExpression:
                return IsZeroLiteral(binary.Left) && IsExistenceCheckInvocation(binary.Right, CountMethods, out receiver);

            // Handle: collection.Count(...) != 0, collection.FirstOrDefault(...) != null (either operand order)
            case SyntaxKind.NotEqualsExpression:
                return IsInvocationComparedTo(binary, CountMethods, IsZeroLiteral, out receiver) ||
                       IsInvocationComparedTo(binary, FindMethods, IsNullLiteral, out receiver);

            default:
                return false;
        }
    }

    /// <summary>
    /// Matches a binary expression made of an existence-check invocation on one side and a literal on the other, in either order.
    /// </summary>
    private static bool IsInvocationComparedTo(
        BinaryExpressionSyntax binary,
        string[] methodNames,
        Func<ExpressionSyntax, bool> isExpectedLiteral,
        out ExpressionSyntax receiver)
    {
        return (IsExistenceCheckInvocation(binary.Left, methodNames, out receiver) && isExpectedLiteral(binary.Right)) ||
               (IsExistenceCheckInvocation(binary.Right, methodNames, out receiver) && isExpectedLiteral(binary.Left));
    }

    private static bool IsExistenceCheckInvocation(ExpressionSyntax expression, string[] methodNames, out ExpressionSyntax receiver)
    {
        receiver = null;

        if (UnwrapAwaitAndParentheses(expression) is InvocationExpressionSyntax invocation &&
            TryGetCalledMember(invocation, methodNames, out var memberAccess))
        {
            receiver = memberAccess.Expression;
            return true;
        }

        return false;
    }

    private static ExpressionSyntax UnwrapAwaitAndParentheses(ExpressionSyntax expression)
    {
        if (expression is AwaitExpressionSyntax awaitExpression)
            expression = awaitExpression.Expression;

        while (expression is ParenthesizedExpressionSyntax paren)
            expression = paren.Expression;

        return expression;
    }

    /// <summary>
    /// Checks whether the invocation is a member-access call (<c>receiver.Method(...)</c>) to one of the given method names.
    /// </summary>
    private static bool TryGetCalledMember(InvocationExpressionSyntax invocation, string[] methodNames, out MemberAccessExpressionSyntax memberAccess)
    {
        memberAccess = invocation.Expression as MemberAccessExpressionSyntax;
        return memberAccess != null && Array.IndexOf(methodNames, memberAccess.Name.Identifier.ValueText) >= 0;
    }

    /// <summary>
    /// Checks whether the node contains an insert invocation (Add, AddAsync, etc.)
    /// whose receiver matches the given normalized receiver text from the existence check.
    /// </summary>
    private static bool ContainsMatchingInsertInvocation(SyntaxNode node, string expectedReceiverText)
    {
        return node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv => TryGetCalledMember(inv, InsertMethods, out var memberAccess) &&
                        NormalizeReceiver(memberAccess.Expression) == expectedReceiverText);
    }

    internal static bool ContainsThrowStatement(StatementSyntax statement)
    {
        return statement is ThrowStatementSyntax ||
               (statement is BlockSyntax block && block.Statements.Any(s => s is ThrowStatementSyntax));
    }

    private static bool HasSubsequentMatchingInsertInvocation(IfStatementSyntax ifStatement, string expectedReceiverText)
    {
        return ifStatement.Parent is BlockSyntax block &&
               block.Statements
                   .Skip(block.Statements.IndexOf(ifStatement) + 1)
                   .Any(statement => ContainsMatchingInsertInvocation(statement, expectedReceiverText));
    }

    /// <summary>
    /// Normalizes a receiver expression to a comparable string by collapsing whitespace.
    /// </summary>
    private static string NormalizeReceiver(ExpressionSyntax receiver)
    {
        return SyntaxUtils.NormalizeWhitespace(receiver.ToString());
    }

    private static bool IsFalseLiteral(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.FalseLiteralExpression);
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
               literal.Token.ValueText == ZeroLiteralText;
    }

    private static bool IsNullLiteral(ExpressionSyntax expression)
    {
        return expression.IsKind(SyntaxKind.NullLiteralExpression);
    }
}
