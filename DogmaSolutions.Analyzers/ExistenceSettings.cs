#nullable disable
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The options that DSA052 and DSA053 share: the methods they look at, the kinds of source, and the exclusions.</summary>
    internal abstract class ExistenceSettings
    {
        protected ExistenceSettings(AnalyzerConfigOptions options, string ruleId, string[] defaultMethods)
        {
            IncludedMethods = new HashSet<string>(AnalyzerOptionsReader.ReadList(options, Key(ruleId, "included_methods"), defaultMethods), StringComparer.Ordinal);
            ReportLocals = AnalyzerOptionsReader.ReadBool(options, Key(ruleId, "report_locals"), true);
            ReportQueryableSources = AnalyzerOptionsReader.ReadBool(options, Key(ruleId, "report_queryable_sources"), true);
            ReportEnumerableSources = AnalyzerOptionsReader.ReadBool(options, Key(ruleId, "report_enumerable_sources"), true);

            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, Key(ruleId, "excluded_type_name_patterns"), Array.Empty<string>());
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, Key(ruleId, "excluded_namespace_patterns"), Array.Empty<string>());
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, Key(ruleId, "excluded_base_types"), Array.Empty<string>());
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, Key(ruleId, "excluded_member_name_patterns"), Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, Key(ruleId, "excluded_file_patterns"));
        }

        internal static string Key(string ruleId, string name) => "dotnet_diagnostic." + ruleId + "." + name;

        /// <summary>The methods whose result is looked at.</summary>
        internal HashSet<string> IncludedMethods { get; }

        /// <summary>Report the result that is kept in a local variable and only tested.</summary>
        internal bool ReportLocals { get; }

        /// <summary>Report the calls on an <c>IQueryable</c> (translated by a provider, typically into SQL).</summary>
        internal bool ReportQueryableSources { get; }

        /// <summary>Report the calls on an <c>IEnumerable</c> (in memory).</summary>
        internal bool ReportEnumerableSources { get; }

        internal string[] ExcludedTypeNamePatterns { get; }

        internal string[] ExcludedNamespacePatterns { get; }

        internal string[] ExcludedBaseTypes { get; }

        internal string[] ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }

        /// <summary>The kind of source is one of the reported ones.</summary>
        internal bool IsSourceReported(ExistenceCall call) => call.IsQueryable ? ReportQueryableSources : ReportEnumerableSources;

        /// <summary>The options say whether this call, used in this way, is reported (the call is known by now: its source, its element type...).</summary>
        internal abstract bool IsCallReported(ExistenceShape shape, ExistenceCall call);

        /// <summary>The options say whether a use of this shape is reported at all, before anything about the call is known.</summary>
        internal abstract bool IsShapeReported(ExistenceShape shape);

        /// <summary>The node is in a file or a member that the options exclude: nothing but the syntax tells.</summary>
        internal bool IsExcludedBySyntax(SyntaxNode node) =>
            AnalyzersUtils.IsFileExcluded(node.SyntaxTree.FilePath, ExcludedFilePatterns) ||
            (ExcludedMemberNamePatterns.Length > 0 && Matches(GetMemberName(node), ExcludedMemberNamePatterns));

        /// <summary>The node is in a type that the options exclude: the semantic model is asked only if there is a pattern to match.</summary>
        internal bool IsExcludedBySymbol(SyntaxNode node, SemanticModel model)
        {
            if (ExcludedTypeNamePatterns.Length == 0 && ExcludedNamespacePatterns.Length == 0 && ExcludedBaseTypes.Length == 0)
                return false;

            var type = model.GetEnclosingSymbol(node.SpanStart)?.ContainingType;
            return type != null &&
                   (Matches(type.Name, ExcludedTypeNamePatterns) ||
                    (ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), ExcludedNamespacePatterns)) ||
                    (ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, ExcludedBaseTypes)));
        }

        private static string GetMemberName(SyntaxNode node) =>
            node.Ancestors().FirstOrDefault(static ancestor => ancestor is MemberDeclarationSyntax) switch
            {
                MethodDeclarationSyntax method => method.Identifier.ValueText,
                ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText,
                PropertyDeclarationSyntax property => property.Identifier.ValueText,
                _ => string.Empty,
            };

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
