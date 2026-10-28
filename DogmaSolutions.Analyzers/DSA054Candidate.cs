using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>A field of a type that might hold no state: the declarator and whether it is static.</summary>
    internal sealed class DSA054Candidate
    {
        internal DSA054Candidate(VariableDeclaratorSyntax declarator, bool isStatic)
        {
            Declarator = declarator;
            IsStatic = isStatic;
            Name = declarator.Identifier.ValueText;
        }

        internal VariableDeclaratorSyntax Declarator { get; }

        internal bool IsStatic { get; }

        internal string Name { get; }
    }

    /// <summary>One declaration of a type (a type has several when it is partial) and its members.</summary>
    internal sealed class DSA054Part
    {
        internal DSA054Part(TypeDeclarationSyntax root)
        {
            Root = root;
            Members = root.Members;
        }

        internal TypeDeclarationSyntax Root { get; }

        internal SyntaxList<MemberDeclarationSyntax> Members { get; }
    }

    /// <summary>A place of the text of a type where a word has the name of a candidate: it can still be a comment, a string, a keyword...</summary>
    internal readonly struct DSA054Hit
    {
        internal DSA054Hit(DSA054Part part, int position)
        {
            Part = part;
            Position = position;
        }

        internal DSA054Part Part { get; }

        internal int Position { get; }
    }

    /// <summary>What is known of a candidate: the places where its name appears, and, once they are resolved, the identifiers that use it.</summary>
    internal sealed class DSA054Uses
    {
        /// <summary>The position of the identifier in the declaration of the field.</summary>
        internal int DeclarationPosition { get; set; }

        internal List<DSA054Hit> Hits { get; set; }

        /// <summary>The identifiers that have the name of the field, in source order.</summary>
        internal List<IdentifierNameSyntax> Uses { get; set; }

        /// <summary>Something else in the type is declared with the name of the field (a local, a parameter...): its uses have to be told apart with the model.</summary>
        internal bool HasHomonym { get; set; }
    }
}
