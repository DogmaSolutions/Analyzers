using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

public partial class DSA034CodeFixProvider
{

    internal static bool IsCtorsGroupMember(MemberDeclarationSyntax member)
    {
        return member is ConstructorDeclarationSyntax
            || member is DestructorDeclarationSyntax
            || member is FieldDeclarationSyntax
            || member is PropertyDeclarationSyntax
            || member is EventFieldDeclarationSyntax
            || IsDisposeMethod(member);
    }

    internal static bool IsCtorsGroupMemberForTopic(MemberDeclarationSyntax member)
    {
        return member is ConstructorDeclarationSyntax
            || member is DestructorDeclarationSyntax
            || IsDisposeMethod(member);
    }

    private static void AssignMembersToTopics(
        List<MemberDeclarationSyntax> classifiableMembers,
        Dictionary<MemberDeclarationSyntax, List<string>> memberWords,
        List<string> topTopics,
        Dictionary<string, int> wordFrequency,
        Dictionary<string, List<MemberDeclarationSyntax>> topicGroups,
        List<MemberDeclarationSyntax> miscGroup)
    {
        foreach (var member in classifiableMembers)
        {
            var words = memberWords[member];
            string bestTopic = null;
            var bestFrequency = 0;

            foreach (var word in words)
            {
                foreach (var topic in topTopics)
                {
                    if (string.Equals(word, topic, StringComparison.OrdinalIgnoreCase) &&
                        wordFrequency[topic] > bestFrequency)
                    {
                        bestTopic = topic;
                        bestFrequency = wordFrequency[topic];
                    }
                }
            }

            if (bestTopic != null)
                topicGroups[bestTopic].Add(member);
            else
                miscGroup.Add(member);
        }
    }

    private static void PruneAndRefillTopics(
        List<string> topTopics,
        Dictionary<string, List<MemberDeclarationSyntax>> topicGroups,
        List<string> allCandidates,
        ref int candidateIndex,
        int maxTopics,
        List<MemberDeclarationSyntax> miscGroup,
        Dictionary<MemberDeclarationSyntax, List<string>> memberWords,
        Dictionary<string, int> wordFrequency)
    {
        var emptyTopics = topTopics.Where(t => topicGroups[t].Count == 0).ToList();
        if (emptyTopics.Count == 0)
            return;

        foreach (var empty in emptyTopics)
        {
            topTopics.Remove(empty);
            topicGroups.Remove(empty);
        }

        var added = false;
        while (topTopics.Count < maxTopics && candidateIndex < allCandidates.Count)
        {
            var candidate = allCandidates[candidateIndex++];
            if (topicGroups.ContainsKey(candidate))
                continue;

            topTopics.Add(candidate);
            topicGroups[candidate] = new List<MemberDeclarationSyntax>();
            added = true;
        }

        if (!added)
            return;

        var remainingMisc = new List<MemberDeclarationSyntax>();
        foreach (var member in miscGroup)
        {
            var words = memberWords[member];
            string bestTopic = null;
            var bestFrequency = 0;

            foreach (var word in words)
            {
                foreach (var topic in topTopics)
                {
                    if (string.Equals(word, topic, StringComparison.OrdinalIgnoreCase) &&
                        wordFrequency[topic] > bestFrequency)
                    {
                        bestTopic = topic;
                        bestFrequency = wordFrequency[topic];
                    }
                }
            }

            if (bestTopic != null)
                topicGroups[bestTopic].Add(member);
            else
                remainingMisc.Add(member);
        }

        miscGroup.Clear();
        miscGroup.AddRange(remainingMisc);
    }

    internal static bool IsViableTopicGroup(List<MemberDeclarationSyntax> group)
    {
        var methods = 0;
        var nonMethods = 0;
        foreach (var member in group)
        {
            if (member is MethodDeclarationSyntax)
                methods++;
            else
                nonMethods++;
        }

        return methods >= 1 || nonMethods >= 2;
    }

    private static bool IsDisposeMethod(MemberDeclarationSyntax member)
    {
        if (member is not MethodDeclarationSyntax method)
            return false;

        var name = method.Identifier.ValueText;
        return name == "Dispose" || name == "DisposeAsync";
    }

    internal static string GetEffectiveVisibility(MemberDeclarationSyntax member)
    {
        var modifiers = member.Modifiers;

        if (modifiers.Any(SyntaxKind.PublicKeyword))
            return "Public";
        if (modifiers.Any(SyntaxKind.InternalKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
            return "ProtectedInternal";
        if (modifiers.Any(SyntaxKind.PrivateKeyword) && modifiers.Any(SyntaxKind.ProtectedKeyword))
            return "PrivateProtected";
        if (modifiers.Any(SyntaxKind.InternalKeyword))
            return "Internal";
        if (modifiers.Any(SyntaxKind.ProtectedKeyword))
            return "Protected";
        if (modifiers.Any(SyntaxKind.PrivateKeyword))
            return "Private";

        return "Private";
    }

    internal static string GetMemberName(MemberDeclarationSyntax member)
    {
        return member switch
        {
            MethodDeclarationSyntax m => m.Identifier.ValueText,
            PropertyDeclarationSyntax p => p.Identifier.ValueText,
            FieldDeclarationSyntax f => f.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? string.Empty,
            EventDeclarationSyntax e => e.Identifier.ValueText,
            EventFieldDeclarationSyntax ef => ef.Declaration.Variables.FirstOrDefault()?.Identifier.ValueText ?? string.Empty,
            IndexerDeclarationSyntax => "Indexer",
            OperatorDeclarationSyntax o => o.OperatorToken.ValueText,
            ConversionOperatorDeclarationSyntax => "ConversionOperator",
            DelegateDeclarationSyntax d => d.Identifier.ValueText,
            _ => string.Empty
        };
    }

    internal static List<string> SplitPascalCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return new List<string>();

        if (name.StartsWith("_", StringComparison.Ordinal))
            name = name.TrimStart('_');

        if (name.Length == 0)
            return new List<string>();

        var words = new List<string>();
        var current = new StringBuilder();

        for (var i = 0; i < name.Length; i++)
        {
            if (name[i] == '_')
            {
                if (current.Length > 0)
                {
                    words.Add(current.ToString());
                    current.Clear();
                }

                continue;
            }

            if (char.IsUpper(name[i]) && current.Length > 0)
            {
                words.Add(current.ToString());
                current.Clear();
            }

            current.Append(name[i]);
        }

        if (current.Length > 0)
            words.Add(current.ToString());

        return words;
    }

    private static readonly Dictionary<string, string> IrregularPlurals = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Children", "Child" },
        { "Men", "Man" },
        { "Women", "Woman" },
        { "People", "Person" },
        { "Mice", "Mouse" },
        { "Geese", "Goose" },
        { "Teeth", "Tooth" },
        { "Feet", "Foot" },
        { "Indices", "Index" },
        { "Vertices", "Vertex" },
        { "Matrices", "Matrix" },
        { "Appendices", "Appendix" },
        { "Criteria", "Criterion" },
        { "Phenomena", "Phenomenon" },
        { "Aliases", "Alias" },
        { "Statuses", "Status" },
        { "Buses", "Bus" },
        { "Viruses", "Virus" },
        { "Bonuses", "Bonus" },
        { "Focuses", "Focus" },
        { "Campuses", "Campus" },
        { "Heroes", "Hero" },
        { "Potatoes", "Potato" },
        { "Tomatoes", "Tomato" },
        { "Echoes", "Echo" },
        { "Vetoes", "Veto" },
        { "Caches", "Cache" },
        { "Niches", "Niche" },
        { "Aches", "Ache" },
    };

    internal static string NormalizeWord(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length <= 2)
            return word;

        foreach (var kvp in IrregularPlurals)
        {
            if (string.Equals(word, kvp.Key, StringComparison.OrdinalIgnoreCase))
            {
                if (char.IsLower(word[0]) && char.IsUpper(kvp.Value[0]))
                    return char.ToLowerInvariant(kvp.Value[0]) + kvp.Value.Substring(1);
                return kvp.Value;
            }
        }

        if (!word.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return word;

        var len = word.Length;

        if (EndsWith(word, "ss") || EndsWith(word, "us") || EndsWith(word, "is"))
            return word;

        if (len > 3 && EndsWith(word, "ies"))
        {
            var lower = word.ToLowerInvariant();
            if (lower == "series" || lower == "species")
                return word;
            return word.Substring(0, len - 3) + (char.IsUpper(word[len - 3]) ? "Y" : "y");
        }

        if (len > 4 && EndsWith(word, "sses"))
            return word.Substring(0, len - 2);

        if (len > 4 && EndsWith(word, "shes"))
            return word.Substring(0, len - 2);

        if (len > 5 && EndsWith(word, "ches"))
            return word.Substring(0, len - 2);

        if (len > 3 && EndsWith(word, "xes"))
            return word.Substring(0, len - 2);

        if (len > 3 && EndsWith(word, "zes") && !EndsWith(word, "zzes"))
            return word.Substring(0, len - 1);

        return word.Substring(0, len - 1);
    }

    private static bool EndsWith(string word, string suffix)
    {
        return word.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
    }
}
