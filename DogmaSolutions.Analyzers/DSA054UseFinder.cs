#nullable disable
using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Finds the identifiers with the name of a candidate field in the code of a type. Walking the tokens of the type costs a red node for each of them
    /// (most of the cost of the rule); the text of the type is scanned instead, in one pass that looks up each word in a table of the candidate names, and
    /// only the few hits are turned into tokens. A type whose text has a unicode escape (<c>_</c> can spell a name) is walked token by token.
    /// </summary>
    internal sealed class DSA054UseFinder
    {
        private readonly Dictionary<string, DSA054Uses> _byName;
        private readonly string[] _names;
        private readonly DSA054Uses[] _uses;
        private readonly bool[] _firstChars = new bool[128];
        private readonly char[] _starts;
        private readonly int _mask;
        private readonly int _minLength = int.MaxValue;
        private readonly int _maxLength;

        internal DSA054UseFinder(Dictionary<string, DSA054Uses> byName)
        {
            _byName = byName;
            var size = 8;
            while (size < byName.Count * 2)
                size <<= 1;

            _names = new string[size];
            _uses = new DSA054Uses[size];
            _mask = size - 1;
            foreach (var pair in byName)
            {
                var name = pair.Key;
                var slot = Hash(name, 0, name.Length) & _mask;
                while (_names[slot] != null)
                    slot = (slot + 1) & _mask;

                _names[slot] = name;
                _uses[slot] = pair.Value;
                _minLength = Math.Min(_minLength, name.Length);
                _maxLength = Math.Max(_maxLength, name.Length);
                if (name[0] < 128)
                    _firstChars[name[0]] = true;
            }

            // the words of the code that can be a candidate start with a few characters: the text is searched for those, instead of being read char by char
            var starts = new List<char>();
            foreach (var name in byName.Keys)
            {
                if (!starts.Contains(name[0]))
                    starts.Add(name[0]);
            }

            _starts = starts.Count <= MaxSearchedStarts ? starts.ToArray() : null;
        }

        private const int MaxSearchedStarts = 4;

        internal void Find(TypeDeclarationSyntax root, CancellationToken cancellationToken)
        {
            var text = root.SyntaxTree.GetText(cancellationToken).ToString(root.Span);
            var part = new DSA054Part(root);
            if (text.IndexOf("\\u", StringComparison.Ordinal) >= 0 || text.IndexOf("\\U", StringComparison.Ordinal) >= 0)
                FindByTokens(part);
            else
                FindByText(part, text);
        }

        internal static int Hash(string text, int start, int length)
        {
            var hash = 0;
            for (var i = start; i < start + length; i++)
                hash = unchecked((hash * 31) + text[i]);

            return hash & int.MaxValue;
        }

        internal static bool IsPart(char c) => c == '_' || c >= 0x80 || (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');

        private void FindByText(DSA054Part part, string text)
        {
            var next = 0;
            while (TryFindWord(text, ref next, out var start, out var length))
            {
                var uses = Lookup(text, start, length, Hash(text, start, length));
                if (uses != null)
                    AddHit(uses, part, part.Root.SpanStart + (start > 0 && text[start - 1] == '@' ? start - 1 : start));
            }
        }

        /// <summary>The next word of the text that can be the name of a candidate: it is not a number, and its first character and its length fit.</summary>
        private bool TryFindWord(string text, ref int next, out int start, out int length)
        {
            start = length = 0;
            int found;
            while ((found = NextStart(text, next)) >= 0)
            {
                next = found + 1;

                // a character that is in the middle of a word does not start one
                if (found > 0 && IsPart(text[found - 1]))
                    continue;

                var end = found + 1;
                while (end < text.Length && IsPart(text[end]))
                    end++;

                next = end;
                if (end - found >= _minLength && end - found <= _maxLength && (text[found] >= 128 || _firstChars[text[found]]))
                {
                    start = found;
                    length = end - found;
                    return true;
                }
            }

            return false;
        }

        /// <summary>The next character that can start a candidate: the text is searched for the few characters the candidates start with, instead of being read char by char.</summary>
        private int NextStart(string text, int from)
        {
            if (_starts == null)
            {
                while (from < text.Length && !IsPart(text[from]))
                    from++;

                return from < text.Length ? from : -1;
            }

            return _starts.Length == 1 ? text.IndexOf(_starts[0], from) : text.IndexOfAny(_starts, from);
        }

        private DSA054Uses Lookup(string text, int start, int length, int hash)
        {
            for (var slot = hash & _mask; _names[slot] != null; slot = (slot + 1) & _mask)
            {
                var name = _names[slot];
                if (name.Length == length && string.CompareOrdinal(text, start, name, 0, length) == 0)
                    return _uses[slot];
            }

            return null;
        }

        /// <summary>The declaration of the field is not one of its uses.</summary>
        private static void AddHit(DSA054Uses uses, DSA054Part part, int position)
        {
            if (position != uses.DeclarationPosition)
                (uses.Hits ??= new List<DSA054Hit>()).Add(new DSA054Hit(part, position));
        }

        private void FindByTokens(DSA054Part part)
        {
            foreach (var token in part.Root.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.IdentifierToken) && _byName.TryGetValue(token.ValueText, out var uses))
                    AddHit(uses, part, token.SpanStart);
            }
        }

        /// <summary>
        /// Turns the hits of a candidate into identifiers, member by member, and stops as soon as the field is certainly used by two members (the
        /// common case of a field that is state): the rest of the hits are never looked at. Returns false for such a field.
        /// </summary>
        internal static bool Resolve(DSA054Uses uses, bool rejectEarly)
        {
            SyntaxNode group = null;
            var groupIsUse = false;
            var verified = 0;
            foreach (var hit in uses.Hits)
            {
                // the token is looked for from the member that holds it: a shorter way down than from the type
                var member = FindMember(hit.Part.Members, hit.Position) ?? hit.Part.Root;
                if (member != group)
                {
                    if (rejectEarly && groupIsUse && !uses.HasHomonym && ++verified == 2)
                        return false;

                    group = member;
                    groupIsUse = false;
                }

                var token = member.FindToken(hit.Position);
                if (token.SpanStart != hit.Position || !token.IsKind(SyntaxKind.IdentifierToken))
                    continue;

                if (token.Parent is IdentifierNameSyntax name)
                {
                    (uses.Uses ??= new List<IdentifierNameSyntax>()).Add(name);
                    groupIsUse = true;
                }
                else if (!IsFieldOf(token.Parent, hit.Part.Root))
                {
                    uses.HasHomonym = true;
                }
            }

            return !(rejectEarly && groupIsUse && !uses.HasHomonym && verified == 1);
        }

        private static SyntaxNode FindMember(SyntaxList<MemberDeclarationSyntax> members, int position)
        {
            int low = 0, high = members.Count - 1;
            while (low <= high)
            {
                var middle = (low + high) >> 1;
                var span = members[middle].FullSpan;
                if (position < span.Start)
                    high = middle - 1;
                else if (position >= span.End)
                    low = middle + 1;
                else
                    return members[middle];
            }

            return null;
        }

        /// <summary>Whether the declarator is the one of a field of the type (and not of a local, a nested type or a member that has the same name).</summary>
        private static bool IsFieldOf(SyntaxNode declarator, SyntaxNode root) => declarator is VariableDeclaratorSyntax { Parent.Parent: FieldDeclarationSyntax field } && field.Parent == root;
    }
}
