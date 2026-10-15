using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>Where the critical section is: in which kind of member, and whether it is all that the member does.</summary>
    internal static class DSA010Scope
    {
        /// <summary>
        /// The member that contains the critical section, or null when the options exclude its place (not the only statement of the member,
        /// or a kind of member that is not looked at).
        /// </summary>
        internal static SyntaxNode FindMember(LockStatementSyntax lockStatement, DSA010Settings settings, out DSA010MemberKinds kind)
        {
            SyntaxNode member;
            if (settings.SoleStatementOnly)
                member = lockStatement.Parent is BlockSyntax { Statements.Count: 1 } block ? block.Parent : null;
            else
                member = lockStatement.Ancestors().FirstOrDefault(IsMemberLike);

            kind = Classify(member);
            return member != null && (settings.MemberKinds & kind) != 0 ? member : null;
        }

        private static bool IsMemberLike(SyntaxNode node) =>
            node is AccessorDeclarationSyntax or BaseMethodDeclarationSyntax or LocalFunctionStatementSyntax or AnonymousFunctionExpressionSyntax;

        private static DSA010MemberKinds Classify(SyntaxNode member)
        {
            switch (member)
            {
                case null:
                    return DSA010MemberKinds.None;
                case AccessorDeclarationSyntax:
                    return DSA010MemberKinds.Accessor;
                case ConstructorDeclarationSyntax or DestructorDeclarationSyntax:
                    return DSA010MemberKinds.Constructor;
                case MethodDeclarationSyntax or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax:
                    return DSA010MemberKinds.Method;
                case LocalFunctionStatementSyntax:
                    return DSA010MemberKinds.LocalFunction;
                case AnonymousFunctionExpressionSyntax:
                    return DSA010MemberKinds.Lambda;
                default:
                    return DSA010MemberKinds.Other;
            }
        }

        /// <summary>The name of the property, indexer, event or method the member belongs to (null when it has none, e.g. a lambda).</summary>
        internal static string GetMemberName(SyntaxNode member)
        {
            switch (member)
            {
                case AccessorDeclarationSyntax { Parent: { Parent: PropertyDeclarationSyntax property } }:
                    return property.Identifier.ValueText;
                case AccessorDeclarationSyntax { Parent: { Parent: EventDeclarationSyntax @event } }:
                    return @event.Identifier.ValueText;
                case AccessorDeclarationSyntax { Parent: { Parent: IndexerDeclarationSyntax } }:
                    return "this[]";
                case MethodDeclarationSyntax method:
                    return method.Identifier.ValueText;
                case LocalFunctionStatementSyntax localFunction:
                    return localFunction.Identifier.ValueText;
                default:
                    return null;
            }
        }
    }
}
