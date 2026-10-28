#nullable disable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a private field that holds nothing from one call to the next: every use of the field is in a single member, and each read comes after an
    /// assignment that always runs. It is either a memoization that never memoizes (<c>_v = Compute(); return _v;</c> instead of
    /// <c>_v ??= Compute();</c>) or a local variable that was declared as a field. A field that is assigned and never read is optional (the compiler
    /// and the IDE already report most of those).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA054Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA054";

        /// <summary>Property of the diagnostic: the code fix can turn the assignment into <c>??=</c>.</summary>
        internal const string CanMemoizeProperty = "CanMemoize";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA054AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA054AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA054AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA054.md");

        private static readonly ImmutableDictionary<string, string> _memoizable = ImmutableDictionary<string, string>.Empty.Add(CanMemoizeProperty, "true");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(AnalyzeType, SymbolKind.NamedType);
        }

        private static void AnalyzeType(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;
            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.DeclaringSyntaxReferences.IsEmpty)
                return;

            // the candidates are found on the syntax, so that a type with none (most of them) costs a loop on its members
            var candidates = GetCandidates(type, context.CancellationToken);
            if (candidates == null)
                return;

            var tree = type.DeclaringSyntaxReferences[0].SyntaxTree;
            var settings = DSA054Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(tree));
            if (IsExcluded(type, settings) || AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns))
                return;

            var names = FindUses(type, candidates, settings, context.CancellationToken);
            foreach (var candidate in candidates)
            {
                if (names.TryGetValue(candidate.Name, out var info) && info.Hits != null && DSA054UseFinder.Resolve(info, !settings.ReportNeverRead) && info.Uses != null)
                    AnalyzeField(candidate, info, settings, context);
            }
        }

        private static List<DSA054Candidate> GetCandidates(INamedTypeSymbol type, System.Threading.CancellationToken cancellationToken)
        {
            List<DSA054Candidate> result = null;
            foreach (var reference in type.DeclaringSyntaxReferences)
            {
                foreach (var member in ((TypeDeclarationSyntax)reference.GetSyntax(cancellationToken)).Members)
                {
                    if (member is FieldDeclarationSyntax field && IsCandidate(field, out var isStatic))
                    {
                        foreach (var declarator in field.Declaration.Variables)
                            (result ??= new List<DSA054Candidate>()).Add(new DSA054Candidate(declarator, isStatic));
                    }
                }
            }

            return result;
        }

        /// <summary>A private field that is not <c>readonly</c>, <c>const</c>, <c>volatile</c>, a pointer or a fixed buffer, and has no attribute.</summary>
        private static bool IsCandidate(FieldDeclarationSyntax field, out bool isStatic)
        {
            isStatic = false;
            if (field.AttributeLists.Count > 0 || field.Declaration.Type is PointerTypeSyntax)
                return false;

            foreach (var modifier in field.Modifiers)
            {
                switch (modifier.Kind())
                {
                    case SyntaxKind.StaticKeyword:
                        isStatic = true;
                        break;
                    case SyntaxKind.PublicKeyword:
                    case SyntaxKind.ProtectedKeyword:
                    case SyntaxKind.InternalKeyword:
                    case SyntaxKind.ConstKeyword:
                    case SyntaxKind.ReadOnlyKeyword:
                    case SyntaxKind.VolatileKeyword:
                    case SyntaxKind.FixedKeyword:
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The uses of the candidates, by name, in one pass over the tokens of the type. Every identifier with the name of a field is counted as a use
        /// of it, which can only add uses (a local with the same name): the rest of the analysis rejects on more uses, never on fewer.
        /// </summary>
        private static Dictionary<string, DSA054Uses> FindUses(INamedTypeSymbol type, List<DSA054Candidate> candidates, DSA054Settings settings, System.Threading.CancellationToken cancellationToken)
        {
            var result = new Dictionary<string, DSA054Uses>(candidates.Count, StringComparer.Ordinal);
            foreach (var candidate in candidates)
            {
                if ((settings.ReportStaticFields || !candidate.IsStatic) && !Matches(candidate.Name, settings.ExcludedFieldNamePatterns))
                    result[candidate.Name] = new DSA054Uses { DeclarationPosition = candidate.Declarator.Identifier.SpanStart };
            }

            if (result.Count == 0)
                return result;

            var finder = new DSA054UseFinder(result);
            foreach (var reference in type.DeclaringSyntaxReferences)
                finder.Find((TypeDeclarationSyntax)reference.GetSyntax(cancellationToken), cancellationToken);

            return result;
        }

        private static void AnalyzeField(DSA054Candidate candidate, DSA054Uses info, DSA054Settings settings, SymbolAnalysisContext context)
        {
            // a homonym (a local or a parameter with the name of the field) is told apart with the model: the rare case that needs it
            var bound = info.Uses;
            if (info.HasHomonym)
            {
                bound = DSA054Flow.Bind(info.Uses, DSA054Flow.GetField(candidate.Declarator, context.Compilation, context.CancellationToken), context.Compilation, context.CancellationToken);
                if (bound.Count == 0)
                    return;
            }

            if (settings.ReportNeverRead && DSA054Flow.AreAllPlainWrites(bound))
            {
                Report(candidate, DSA054Scenario.NeverRead, bound[0].FirstAncestorOrSelf<AssignmentExpressionSyntax>(), false, DSA054Flow.GetName(DSA054Flow.GetMember(bound[0])), context);
                return;
            }

            if (!InOneMember(bound))
                return;

            var member = DSA054Flow.GetMember(bound[0]);
            if (Matches(DSA054Flow.GetName(member), settings.ExcludedMemberNamePatterns))
                return;

            var result = DSA054Flow.Analyze(member, bound, candidate.IsStatic);
            if (result.Scenario == DSA054Scenario.None || (result.Scenario == DSA054Scenario.NeverRead && !settings.ReportNeverRead))
                return;

            Report(candidate, result.Scenario, result.Assignment, result.CanMemoize, DSA054Flow.GetName(member), context);
        }

        private static bool InOneMember(List<IdentifierNameSyntax> uses)
        {
            var member = DSA054Flow.GetMember(uses[0]);
            for (var i = 1; i < uses.Count; i++)
            {
                if (DSA054Flow.GetMember(uses[i]) != member)
                    return false;
            }

            return true;
        }

        private static void Report(DSA054Candidate candidate, DSA054Scenario scenario, AssignmentExpressionSyntax assignment, bool canMemoize, string memberName, SymbolAnalysisContext context)
        {
            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA054Message" + scenario, Resources.Culture), candidate.Name, memberName);
            var properties = canMemoize && DSA054Flow.CanHoldNull(candidate.Declarator, context.Compilation, context.CancellationToken) ? _memoizable : ImmutableDictionary<string, string>.Empty;
            context.ReportDiagnostic(Diagnostic.Create(_rule, assignment.GetLocation(), properties, message));
        }

        private static bool IsExcluded(INamedTypeSymbol type, DSA054Settings settings) =>
            Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
            (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
            (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
