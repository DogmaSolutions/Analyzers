using System;
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
    internal static readonly string[] BooleanExistenceMethods =
    {
        "Any", "AnyAsync", "Exists", "ExistsAsync",
        "Contains", "ContainsAsync", "ContainsKey", "TryGetValue"
    };

    internal static readonly string[] CountMethods = { "Count", "CountAsync" };

    internal static readonly string[] FindMethods =
    {
        "FirstOrDefault", "FirstOrDefaultAsync",
        "SingleOrDefault", "SingleOrDefaultAsync",
        "Find", "FindAsync"
    };

    internal static readonly string[] InsertMethods = { "Add", "AddAsync", "AddRange", "AddRangeAsync" };

    /// <summary>
    /// Detects a check-then-act pattern in the given if statement and returns
    /// the receiver expression of the existence check (for type resolution).
    /// </summary>
    internal static bool TryMatchCheckThenAct(IfStatementSyntax ifStatement, out ExpressionSyntax existenceCheckReceiver)
    {
        existenceCheckReceiver = null;

        // Pattern A: if (!collection.Any(...)) { collection.Add(...); }
        if (IsNegatedExistenceCheck(ifStatement.Condition, out var receiver))
        {
            var receiverText = NormalizeReceiver(receiver);
            if (ContainsMatchingInsertInvocation(ifStatement.Statement, receiverText))
            {
                existenceCheckReceiver = receiver;
                return true;
            }
        }

        if (IsPositiveExistenceCheck(ifStatement.Condition, out receiver))
        {
            var receiverText = NormalizeReceiver(receiver);

            // Pattern B: if (collection.Any(...)) { throw; } ... collection.Add(...);
            if (ContainsThrowStatement(ifStatement.Statement) && ifStatement.Else == null)
            {
                if (HasSubsequentMatchingInsertInvocation(ifStatement, receiverText))
                {
                    existenceCheckReceiver = receiver;
                    return true;
                }
            }

            // Pattern C: if (collection.Any(...)) { ... } else { collection.Add(...); }
            if (ifStatement.Else != null && ContainsMatchingInsertInvocation(ifStatement.Else.Statement, receiverText))
            {
                existenceCheckReceiver = receiver;
                return true;
            }
        }

        return false;
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

        if (HasAtomicAlternative(type, out _))
            return ReceiverCategory.AtomicAlternative;

        return ReceiverCategory.NoAtomicAlternative;
    }

    internal static bool HasAtomicAlternative(ITypeSymbol type, out string suggestion)
    {
        suggestion = null;
        if (type == null)
            return false;

        var name = type.Name;
        var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;

        if (ns == "System.Collections.Generic")
        {
            switch (name)
            {
                case "Dictionary":
                    suggestion = "TryAdd or indexer assignment [key] = value";
                    return true;
                case "HashSet":
                    suggestion = "Add (already returns a bool indicating whether the element was added)";
                    return true;
                case "SortedSet":
                    suggestion = "Add (already returns a bool indicating whether the element was added)";
                    return true;
                case "SortedDictionary":
                    suggestion = "TryAdd or indexer assignment [key] = value";
                    return true;
                case "SortedList":
                    suggestion = "indexer assignment [key] = value";
                    return true;
            }
        }

        if (ns == "System.Collections.Concurrent")
        {
            if (name == "ConcurrentDictionary")
            {
                suggestion = "GetOrAdd, AddOrUpdate, or TryAdd";
                return true;
            }
        }

        if (ns == "System.Collections.Immutable")
        {
            switch (name)
            {
                case "ImmutableHashSet":
                    suggestion = "Add (already handles duplicates)";
                    return true;
                case "ImmutableSortedSet":
                    suggestion = "Add (already handles duplicates)";
                    return true;
                case "ImmutableDictionary":
                    suggestion = "SetItem (upsert semantics)";
                    return true;
                case "ImmutableSortedDictionary":
                    suggestion = "SetItem (upsert semantics)";
                    return true;
            }
        }

        return false;
    }

    internal static ITypeSymbol ResolveReceiverType(ExpressionSyntax receiver, SemanticModel semanticModel)
    {
        var typeInfo = semanticModel.GetTypeInfo(receiver);
        return typeInfo.Type;
    }

    private static bool ImplementsIQueryable(ITypeSymbol type)
    {
        if (type == null)
            return false;

        // Direct check
        if (type.Name == "DbSet" &&
            type.ContainingNamespace?.ToDisplayString() == "Microsoft.EntityFrameworkCore")
            return true;

        // Interface check
        foreach (var iface in type.AllInterfaces)
        {
            if (iface.Name == "IQueryable" &&
                iface.ContainingNamespace?.ToDisplayString() == "System.Linq")
                return true;
        }

        return false;
    }

    internal static bool IsNegatedExistenceCheck(ExpressionSyntax condition, out ExpressionSyntax receiver)
    {
        receiver = null;

        // Handle: !collection.Any(...)
        if (condition is PrefixUnaryExpressionSyntax prefixUnary &&
            prefixUnary.IsKind(SyntaxKind.LogicalNotExpression) &&
            IsExistenceCheckInvocation(prefixUnary.Operand, BooleanExistenceMethods, out receiver))
        {
            return true;
        }

        if (condition is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.EqualsExpression))
        {
            // Handle: collection.Any(...) == false
            if ((IsExistenceCheckInvocation(binary.Left, BooleanExistenceMethods, out receiver) && IsFalseLiteral(binary.Right)) ||
                (IsExistenceCheckInvocation(binary.Right, BooleanExistenceMethods, out receiver) && IsFalseLiteral(binary.Left)))
                return true;

            // Handle: collection.Count(...) == 0
            if ((IsExistenceCheckInvocation(binary.Left, CountMethods, out receiver) && IsZeroLiteral(binary.Right)) ||
                (IsExistenceCheckInvocation(binary.Right, CountMethods, out receiver) && IsZeroLiteral(binary.Left)))
                return true;

            // Handle: collection.FirstOrDefault(...) == null
            if ((IsExistenceCheckInvocation(binary.Left, FindMethods, out receiver) && IsNullLiteral(binary.Right)) ||
                (IsExistenceCheckInvocation(binary.Right, FindMethods, out receiver) && IsNullLiteral(binary.Left)))
                return true;
        }

        return false;
    }

    internal static bool IsPositiveExistenceCheck(ExpressionSyntax condition, out ExpressionSyntax receiver)
    {
        receiver = null;

        // Handle: collection.Any(...)
        if (IsExistenceCheckInvocation(condition, BooleanExistenceMethods, out receiver))
            return true;

        if (condition is BinaryExpressionSyntax binary)
        {
            // Handle: collection.Count(...) > 0
            if (binary.IsKind(SyntaxKind.GreaterThanExpression) &&
                IsExistenceCheckInvocation(binary.Left, CountMethods, out receiver) && IsZeroLiteral(binary.Right))
                return true;

            // Handle: 0 < collection.Count(...)
            if (binary.IsKind(SyntaxKind.LessThanExpression) &&
                IsZeroLiteral(binary.Left) && IsExistenceCheckInvocation(binary.Right, CountMethods, out receiver))
                return true;

            // Handle: collection.Count(...) != 0
            if (binary.IsKind(SyntaxKind.NotEqualsExpression))
            {
                if ((IsExistenceCheckInvocation(binary.Left, CountMethods, out receiver) && IsZeroLiteral(binary.Right)) ||
                    (IsExistenceCheckInvocation(binary.Right, CountMethods, out receiver) && IsZeroLiteral(binary.Left)))
                    return true;

                // Handle: collection.FirstOrDefault(...) != null
                if ((IsExistenceCheckInvocation(binary.Left, FindMethods, out receiver) && IsNullLiteral(binary.Right)) ||
                    (IsExistenceCheckInvocation(binary.Right, FindMethods, out receiver) && IsNullLiteral(binary.Left)))
                    return true;
            }
        }

        return false;
    }

    private static bool IsExistenceCheckInvocation(ExpressionSyntax expression, string[] methodNames, out ExpressionSyntax receiver)
    {
        receiver = null;

        // Handle await expressions
        if (expression is AwaitExpressionSyntax awaitExpression)
            expression = awaitExpression.Expression;

        // Handle parenthesized expressions
        while (expression is ParenthesizedExpressionSyntax paren)
            expression = paren.Expression;

        if (expression is InvocationExpressionSyntax invocation &&
            invocation.Expression is MemberAccessExpressionSyntax memberAccess)
        {
            var methodName = memberAccess.Name.Identifier.ValueText;
            if (Array.IndexOf(methodNames, methodName) >= 0)
            {
                receiver = memberAccess.Expression;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether the node contains an insert invocation (Add, AddAsync, etc.)
    /// whose receiver matches the given normalized receiver text from the existence check.
    /// </summary>
    private static bool ContainsMatchingInsertInvocation(SyntaxNode node, string expectedReceiverText)
    {
        return node.DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Any(inv => inv.Expression is MemberAccessExpressionSyntax memberAccess &&
                        Array.IndexOf(InsertMethods, memberAccess.Name.Identifier.ValueText) >= 0 &&
                        NormalizeReceiver(memberAccess.Expression) == expectedReceiverText);
    }

    internal static bool ContainsThrowStatement(StatementSyntax statement)
    {
        if (statement is ThrowStatementSyntax)
            return true;

        if (statement is BlockSyntax block)
            return block.Statements.Any(s => s is ThrowStatementSyntax);

        return false;
    }

    private static bool HasSubsequentMatchingInsertInvocation(IfStatementSyntax ifStatement, string expectedReceiverText)
    {
        if (ifStatement.Parent is not BlockSyntax block)
            return false;

        var ifIndex = block.Statements.IndexOf(ifStatement);
        if (ifIndex < 0)
            return false;

        for (var i = ifIndex + 1; i < block.Statements.Count; i++)
        {
            if (ContainsMatchingInsertInvocation(block.Statements[i], expectedReceiverText))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Normalizes a receiver expression to a comparable string by collapsing whitespace.
    /// </summary>
    private static string NormalizeReceiver(ExpressionSyntax receiver)
    {
        if (receiver == null)
            return string.Empty;

        var text = receiver.ToString();
        var sb = new System.Text.StringBuilder(text.Length);
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

    private static bool IsFalseLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.FalseLiteralExpression);
    }

    private static bool IsZeroLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.NumericLiteralExpression) &&
               literal.Token.ValueText == "0";
    }

    private static bool IsNullLiteral(ExpressionSyntax expression)
    {
        return expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.NullLiteralExpression);
    }
}
