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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA034CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public partial class DSA034CodeFixProvider : CodeFixProvider
{
    internal const string VisibilityEquivalenceKey = DSA034Analyzer.DiagnosticId + ".SplitByVisibility";
    internal const string TopicEquivalenceKey = DSA034Analyzer.DiagnosticId + ".SplitByTopic";

    internal const string MaxTopicsOptionKey = "dotnet_diagnostic.DSA034.max_topics";
    internal const string ExcludedTopicWordsOptionKey = "dotnet_diagnostic.DSA034.excluded_topic_words";
    internal const int DefaultMaxTopics = 5;

    internal static readonly HashSet<string> DefaultExcludedTopicWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "The", "Get", "Set", "Save", "Load", "Print", "Async", "Sync",
        "Open", "Close", "File", "Add", "Remove", "Delete", "Create",
        "Update", "Find", "Check", "Is", "Has", "Can", "Try", "On",
        "Handle", "Process", "Execute", "Run", "Do", "Make", "To",
        "From", "With", "For", "By", "In", "Of", "And", "Or", "Not",
        "Begin", "End", "Start", "Stop", "Init", "Initialize",
        "Read", "Write", "Send", "Receive", "Input", "Output",
        "Invoke", "Call", "Raise", "Fire", "Trigger", "Emit",
        "Parse", "Format", "Convert", "Transform", "Map",
        "Validate", "Verify", "Ensure", "Assert",
        "Log", "Trace", "Debug", "Info", "Warn", "Error",
        "Enable", "Disable", "Register", "Unregister",
        "Subscribe", "Unsubscribe", "Attach", "Detach",
        "Lock", "Unlock", "Acquire", "Release",
        "Enter", "Exit", "Return", "Yield",
        "Test", "Mock", "Stub", "Fake", "Spy",
        "Before", "After", "Pre", "Post",
        "Internal", "Default", "Current", "New", "Old",
        "First", "Last", "Next", "Previous", "All", "Any", "Each",
        "Count", "Index", "Item", "Value", "Key", "Name", "Type",
        "Data", "Result", "Status", "State", "Info", "Context",
        "Null", "Empty", "True", "False"
    };

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA034Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count != 1)
            return;

        var typeDecl = topLevelTypes[0];
        if (typeDecl is not TypeDeclarationSyntax)
            return;

        var diagnostic = context.Diagnostics[0];

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Split into partial files by member visibility",
                createChangedSolution: ct => SplitByVisibilityAsync(context.Document, ct),
                equivalenceKey: VisibilityEquivalenceKey),
            diagnostic);

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Split into partial files by topic",
                createChangedSolution: ct => SplitByTopicAsync(context.Document, ct),
                equivalenceKey: TopicEquivalenceKey),
            diagnostic);
    }

    internal static async Task<Solution> SplitByVisibilityAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document.Project.Solution;

        var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count != 1 || topLevelTypes[0] is not TypeDeclarationSyntax typeDecl)
            return document.Project.Solution;

        var typeName = typeDecl.Identifier.ValueText;
        var members = typeDecl.Members;

        var ctorsGroup = new List<MemberDeclarationSyntax>();
        var visibilityGroups = new Dictionary<string, List<MemberDeclarationSyntax>>(StringComparer.Ordinal);

        foreach (var member in members)
        {
            if (IsCtorsGroupMember(member))
            {
                ctorsGroup.Add(member);
            }
            else if (member is BaseTypeDeclarationSyntax)
            {
                // nested types get their own files, handled separately
                continue;
            }
            else
            {
                var visibility = GetEffectiveVisibility(member);
                if (!visibilityGroups.TryGetValue(visibility, out var list))
                {
                    list = new List<MemberDeclarationSyntax>();
                    visibilityGroups[visibility] = list;
                }
                list.Add(member);
            }
        }

        var nestedTypes = members.OfType<BaseTypeDeclarationSyntax>().ToList();

        var partialFiles = new Dictionary<string, List<MemberDeclarationSyntax>>(StringComparer.Ordinal);

        if (ctorsGroup.Count > 0)
            partialFiles[$"{typeName}.Ctors"] = ctorsGroup;

        foreach (var kvp in visibilityGroups.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (kvp.Value.Count > 0)
                partialFiles[$"{typeName}.{kvp.Key}"] = kvp.Value;
        }

        return BuildPartialSolution(document, root, typeDecl, partialFiles, nestedTypes, includeBaseTypes: true);
    }

    internal static async Task<Solution> SplitByTopicAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document.Project.Solution;

        var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
        if (topLevelTypes.Count != 1 || topLevelTypes[0] is not TypeDeclarationSyntax typeDecl)
            return document.Project.Solution;

        var options = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider
            .GetOptions(root.SyntaxTree);

        var maxTopics = DefaultMaxTopics;
        if (options.TryGetValue(MaxTopicsOptionKey, out var maxTopicsValue) &&
            int.TryParse(maxTopicsValue, out var parsedMax) && parsedMax > 0)
        {
            maxTopics = parsedMax;
        }

        var excludedWords = DefaultExcludedTopicWords;
        if (options.TryGetValue(ExcludedTopicWordsOptionKey, out var excludedValue) &&
            !string.IsNullOrWhiteSpace(excludedValue))
        {
            excludedWords = new HashSet<string>(
                excludedValue.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }

        var typeName = typeDecl.Identifier.ValueText;
        var members = typeDecl.Members;

        var ctorsGroup = new List<MemberDeclarationSyntax>();
        var classifiableMembers = new List<MemberDeclarationSyntax>();
        var nestedTypes = new List<BaseTypeDeclarationSyntax>();

        foreach (var member in members)
        {
            if (member is BaseTypeDeclarationSyntax nested)
            {
                nestedTypes.Add(nested);
            }
            else if (IsCtorsGroupMemberForTopic(member))
            {
                ctorsGroup.Add(member);
            }
            else
            {
                classifiableMembers.Add(member);
            }
        }

        var wordFrequency = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var memberWords = new Dictionary<MemberDeclarationSyntax, List<string>>();

        foreach (var member in classifiableMembers)
        {
            var name = GetMemberName(member);
            var words = SplitPascalCase(name)
                .Select(NormalizeWord)
                .Where(w => w.Length > 1 && !excludedWords.Contains(w))
                .ToList();
            memberWords[member] = words;

            foreach (var word in words)
            {
                wordFrequency.TryGetValue(word, out var count);
                wordFrequency[word] = count + 1;
            }
        }

        var allCandidates = wordFrequency
            .Where(kvp => kvp.Value >= 2)
            .OrderByDescending(kvp => kvp.Value)
            .ThenBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kvp => kvp.Key)
            .ToList();

        var candidateIndex = Math.Min(maxTopics, allCandidates.Count);
        var topTopics = allCandidates.Take(candidateIndex).ToList();

        var topicGroups = new Dictionary<string, List<MemberDeclarationSyntax>>(StringComparer.OrdinalIgnoreCase);
        foreach (var topic in topTopics)
            topicGroups[topic] = new List<MemberDeclarationSyntax>();

        var miscGroup = new List<MemberDeclarationSyntax>();

        AssignMembersToTopics(classifiableMembers, memberWords, topTopics, wordFrequency, topicGroups, miscGroup);

        PruneAndRefillTopics(
            topTopics,
            topicGroups,
            allCandidates,
            ref candidateIndex,
            maxTopics,
            miscGroup,
            memberWords,
            wordFrequency);

        foreach (var topic in topTopics.ToList())
        {
            var group = topicGroups[topic];
            if (IsViableTopicGroup(group))
                continue;

            miscGroup.AddRange(group);
            topicGroups[topic].Clear();
        }

        var partialFiles = new Dictionary<string, List<MemberDeclarationSyntax>>(StringComparer.Ordinal);

        if (ctorsGroup.Count > 0)
            partialFiles[$"{typeName}.Ctors"] = ctorsGroup;

        foreach (var topic in topTopics)
        {
            if (topicGroups[topic].Count > 0)
            {
                var titleCaseTopic = char.ToUpper(topic[0], CultureInfo.InvariantCulture) + topic.Substring(1);
                partialFiles[$"{typeName}.{titleCaseTopic}"] = topicGroups[topic];
            }
        }

        if (miscGroup.Count > 0)
            partialFiles[$"{typeName}.Misc"] = miscGroup;

        return BuildPartialSolution(document, root, typeDecl, partialFiles, nestedTypes, includeBaseTypes: true);
    }

    internal static Solution BuildPartialSolution(
        Document document,
        SyntaxNode root,
        TypeDeclarationSyntax typeDecl,
        Dictionary<string, List<MemberDeclarationSyntax>> partialFiles,
        List<BaseTypeDeclarationSyntax> nestedTypes,
        bool includeBaseTypes)
    {
        var solution = document.Project.Solution;
        var project = document.Project;
        var folders = document.Folders;
        var sourceDir = !string.IsNullOrEmpty(document.FilePath)
            ? Path.GetDirectoryName(document.FilePath)
            : null;
        var typeName = typeDecl.Identifier.ValueText;

        var usingsNodes = root.DescendantNodes().OfType<UsingDirectiveSyntax>().ToList();
        var namespaceDecl = typeDecl.Parent as BaseNamespaceDeclarationSyntax;
        var eol = root.SyntaxTree.GetText().ToString().Contains("\r\n") ? "\r\n" : "\n";

        var isFirst = true;
        foreach (var kvp in partialFiles)
        {
            var fileName = kvp.Key + ".cs";
            var partialMembers = kvp.Value;

            var partialType = CreatePartialType(
                typeDecl,
                partialMembers,
                includeBaseTypes && isFirst);

            var compilationUnit = BuildCompilationUnit(usingsNodes, namespaceDecl, partialType, eol);

            if (isFirst)
            {
                solution = solution.WithDocumentSyntaxRoot(document.Id, compilationUnit);
                solution = solution.WithDocumentName(document.Id, fileName);
                isFirst = false;
            }
            else
            {
                var newFilePath = !string.IsNullOrEmpty(sourceDir)
                    ? Path.Combine(sourceDir, fileName)
                    : null;

                solution = solution.AddDocument(
                    DocumentId.CreateNewId(project.Id),
                    fileName,
                    compilationUnit,
                    folders: folders,
                    filePath: newFilePath);
            }
        }

        foreach (var nested in nestedTypes)
        {
            var nestedName = nested switch
            {
                TypeDeclarationSyntax tds => tds.Identifier.ValueText,
                EnumDeclarationSyntax eds => eds.Identifier.ValueText,
                _ => nested.ToString().GetHashCode().ToString(CultureInfo.InvariantCulture)
            };

            var fileName = $"{typeName}.{nestedName}.cs";

            var partialType = CreatePartialTypeWithNestedOnly(typeDecl, nested);
            var compilationUnit = BuildCompilationUnit(usingsNodes, namespaceDecl, partialType, eol);

            if (isFirst)
            {
                solution = solution.WithDocumentSyntaxRoot(document.Id, compilationUnit);
                solution = solution.WithDocumentName(document.Id, fileName);
                isFirst = false;
            }
            else
            {
                var newFilePath = !string.IsNullOrEmpty(sourceDir)
                    ? Path.Combine(sourceDir, fileName)
                    : null;

                solution = solution.AddDocument(
                    DocumentId.CreateNewId(project.Id),
                    fileName,
                    compilationUnit,
                    folders: folders,
                    filePath: newFilePath);
            }
        }

        return solution;
    }

    private static TypeDeclarationSyntax CreatePartialType(
        TypeDeclarationSyntax original,
        List<MemberDeclarationSyntax> members,
        bool includeBaseList)
    {
        var modifiers = EnsurePartialModifier(original.Modifiers);

        TypeDeclarationSyntax partial = original switch
        {
            ClassDeclarationSyntax c => SyntaxFactory.ClassDeclaration(c.Identifier)
                .WithModifiers(modifiers)
                .WithTypeParameterList(c.TypeParameterList)
                .WithMembers(SyntaxFactory.List(members)),
            StructDeclarationSyntax s => SyntaxFactory.StructDeclaration(s.Identifier)
                .WithModifiers(modifiers)
                .WithTypeParameterList(s.TypeParameterList)
                .WithMembers(SyntaxFactory.List(members)),
            InterfaceDeclarationSyntax i => SyntaxFactory.InterfaceDeclaration(i.Identifier)
                .WithModifiers(modifiers)
                .WithTypeParameterList(i.TypeParameterList)
                .WithMembers(SyntaxFactory.List(members)),
            RecordDeclarationSyntax r => SyntaxFactory.RecordDeclaration(
                    r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? SyntaxKind.RecordStructDeclaration : SyntaxKind.RecordDeclaration,
                    SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                    r.Identifier)
                .WithModifiers(modifiers)
                .WithClassOrStructKeyword(r.ClassOrStructKeyword)
                .WithTypeParameterList(r.TypeParameterList)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                .WithMembers(SyntaxFactory.List(members)),
            _ => null
        };

        if (partial == null)
            return original;

        if (includeBaseList && original.BaseList != null)
            partial = partial.WithBaseList(original.BaseList);

        return partial
            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }

    private static TypeDeclarationSyntax CreatePartialTypeWithNestedOnly(
        TypeDeclarationSyntax original,
        BaseTypeDeclarationSyntax nested)
    {
        var modifiers = EnsurePartialModifier(original.Modifiers);

        var memberList = SyntaxFactory.List<MemberDeclarationSyntax>(new[] { nested });

        TypeDeclarationSyntax partial = original switch
        {
            ClassDeclarationSyntax c => SyntaxFactory.ClassDeclaration(c.Identifier)
                .WithModifiers(modifiers)
                .WithTypeParameterList(c.TypeParameterList)
                .WithMembers(memberList),
            StructDeclarationSyntax s => SyntaxFactory.StructDeclaration(s.Identifier)
                .WithModifiers(modifiers)
                .WithTypeParameterList(s.TypeParameterList)
                .WithMembers(memberList),
            InterfaceDeclarationSyntax i => SyntaxFactory.InterfaceDeclaration(i.Identifier)
                .WithModifiers(modifiers)
                .WithTypeParameterList(i.TypeParameterList)
                .WithMembers(memberList),
            RecordDeclarationSyntax r => SyntaxFactory.RecordDeclaration(
                    r.ClassOrStructKeyword.IsKind(SyntaxKind.StructKeyword) ? SyntaxKind.RecordStructDeclaration : SyntaxKind.RecordDeclaration,
                    SyntaxFactory.Token(SyntaxKind.RecordKeyword),
                    r.Identifier)
                .WithModifiers(modifiers)
                .WithClassOrStructKeyword(r.ClassOrStructKeyword)
                .WithTypeParameterList(r.TypeParameterList)
                .WithOpenBraceToken(SyntaxFactory.Token(SyntaxKind.OpenBraceToken))
                .WithCloseBraceToken(SyntaxFactory.Token(SyntaxKind.CloseBraceToken))
                .WithMembers(memberList),
            _ => null
        };

        if (partial == null)
            return original;

        return partial
            .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
            .WithTrailingTrivia(SyntaxFactory.CarriageReturnLineFeed);
    }

    private static SyntaxTokenList EnsurePartialModifier(SyntaxTokenList modifiers)
    {
        if (modifiers.Any(SyntaxKind.PartialKeyword))
            return modifiers;

        return modifiers.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword).WithTrailingTrivia(SyntaxFactory.Space));
    }

    private static CompilationUnitSyntax BuildCompilationUnit(
        List<UsingDirectiveSyntax> usings,
        BaseNamespaceDeclarationSyntax namespaceDecl,
        TypeDeclarationSyntax partialType,
        string eol)
    {
        MemberDeclarationSyntax memberToAdd;

        if (namespaceDecl != null)
        {
            var ns = namespaceDecl switch
            {
                FileScopedNamespaceDeclarationSyntax fs => (BaseNamespaceDeclarationSyntax)SyntaxFactory
                    .FileScopedNamespaceDeclaration(fs.Name)
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[] { partialType }))
                    .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed)
                    .WithNamespaceKeyword(SyntaxFactory.Token(SyntaxKind.NamespaceKeyword).WithTrailingTrivia(SyntaxFactory.Space)),
                NamespaceDeclarationSyntax ns2 => SyntaxFactory
                    .NamespaceDeclaration(ns2.Name)
                    .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(new[] { partialType }))
                    .WithLeadingTrivia(SyntaxFactory.CarriageReturnLineFeed),
                _ => namespaceDecl
            };
            memberToAdd = ns;
        }
        else
        {
            memberToAdd = partialType;
        }

        var compilationUnit = SyntaxFactory.CompilationUnit()
            .WithUsings(SyntaxFactory.List(usings))
            .WithMembers(SyntaxFactory.List(new[] { memberToAdd }))
            .NormalizeWhitespace(eol: eol);

        return compilationUnit;
    }
}
