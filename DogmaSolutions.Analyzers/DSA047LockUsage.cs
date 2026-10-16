using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>How far from the class a member can be reached.</summary>
    internal enum DSA047Reach
    {
        /// <summary>Private to the class.</summary>
        Private,

        /// <summary>Visible in the assembly only.</summary>
        Assembly,

        /// <summary>Visible to the callers and to the derived types.</summary>
        External,
    }

    /// <summary>What the code of a type does with a field that is used as a lock object.</summary>
    internal sealed class DSA047FieldUsage
    {
        /// <summary>Written anywhere but in its declaration and directly in the constructors.</summary>
        internal bool Reassigned { get; set; }

        /// <summary>The widest reach of the members that return the field.</summary>
        internal DSA047Reach ReturnedReach { get; set; }

        /// <summary>The name of a member that returns the field with the widest reach.</summary>
        internal string ReturnedBy { get; set; }
    }

    internal static class DSA047Reaches
    {
        internal static DSA047Reach Of(Accessibility accessibility) => accessibility switch
        {
            Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal => DSA047Reach.External,
            Accessibility.Internal or Accessibility.ProtectedAndInternal => DSA047Reach.Assembly,
            _ => DSA047Reach.Private,
        };
    }

    /// <summary>
    /// Finds, once per field, whether the code of its type reassigns it and whether a member returns it. All the parts of the type are scanned: they are
    /// the only code that can touch a private field.
    /// </summary>
    internal static class DSA047UsageScanner
    {
        private static readonly ConditionalWeakTable<IFieldSymbol, DSA047FieldUsage> _cache = new();

        internal static DSA047FieldUsage Get(IFieldSymbol field, Compilation compilation, CancellationToken cancellationToken) =>
            _cache.GetValue(field.OriginalDefinition, key => Scan(key, compilation, cancellationToken));

        private static DSA047FieldUsage Scan(IFieldSymbol field, Compilation compilation, CancellationToken cancellationToken)
        {
            var usage = new DSA047FieldUsage();
            var index = TypeIdentifierIndex.Get(field.ContainingType, compilation);
            var roots = index.GetRoots(cancellationToken);
            foreach (var name in index.Find(field.Name, cancellationToken))
            {
                var model = index.GetModel(name.SyntaxTree);
                if (SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(name, cancellationToken).Symbol?.OriginalDefinition, field))
                    Classify(name, field, roots, model, usage, cancellationToken);
            }

            return usage;
        }

        private static void Classify(IdentifierNameSyntax name, IFieldSymbol field, SyntaxNode[] typeDeclarations, SemanticModel model, DSA047FieldUsage usage, CancellationToken cancellationToken)
        {
            var access = name.Parent is MemberAccessExpressionSyntax member && member.Name == name ? member : (ExpressionSyntax)name;
            if (IsWrite(access) && !IsAssignedByConstructor(name, access, field, typeDeclarations))
                usage.Reassigned = true;

            if (IsReturned(access))
                RecordReturn(name, model, usage, cancellationToken);
        }

        private static bool IsWrite(ExpressionSyntax access)
        {
            var current = (SyntaxNode)access;
            while (current.Parent is ParenthesizedExpressionSyntax or ArgumentSyntax { Parent: TupleExpressionSyntax } or TupleExpressionSyntax)
                current = current.Parent;

            return current.Parent switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left == current,
                ArgumentSyntax argument => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword),
                RefExpressionSyntax => true,
                _ => false,
            };
        }

        /// <summary>A plain <c>x = ...</c> directly in the body of a constructor of the type, on the instance being built.</summary>
        private static bool IsAssignedByConstructor(IdentifierNameSyntax name, ExpressionSyntax access, IFieldSymbol field, SyntaxNode[] typeDeclarations)
        {
            var isOwnInstance = access == name || access is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax };
            if (!isOwnInstance || access.Parent is not AssignmentExpressionSyntax { Parent: not InitializerExpressionSyntax } assignment || assignment.Left != access)
                return false;

            // the nearest boundary decides: a lambda or a local function can run later
            var boundary = name.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax);
            // an instance constructor writes the instance fields (a static field is written again by every instance), a static constructor the static ones
            return boundary is ConstructorDeclarationSyntax constructor && Array.IndexOf(typeDeclarations, constructor.Parent) >= 0 && field.IsStatic == constructor.Modifiers.Any(SyntaxKind.StaticKeyword);
        }

        private static bool IsReturned(ExpressionSyntax access)
        {
            SyntaxNode current = access;
            while (current.Parent is ParenthesizedExpressionSyntax or CastExpressionSyntax)
                current = current.Parent;

            return current.Parent is ReturnStatementSyntax or ArrowExpressionClauseSyntax;
        }

        private static void RecordReturn(IdentifierNameSyntax name, SemanticModel model, DSA047FieldUsage usage, CancellationToken cancellationToken)
        {
            // the member (or the accessor) that returns the field, unless it is a lambda or a local function: those return to their own caller
            var boundary = name.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax);
            if (boundary is not MemberDeclarationSyntax declaration || model.GetDeclaredSymbol(declaration, cancellationToken) is not { } symbol)
                return;

            var reach = DSA047Reaches.Of(symbol.DeclaredAccessibility);
            if (usage.ReturnedBy == null || reach > usage.ReturnedReach)
            {
                usage.ReturnedReach = reach;
                usage.ReturnedBy = symbol.Name;
            }
        }
    }
}
