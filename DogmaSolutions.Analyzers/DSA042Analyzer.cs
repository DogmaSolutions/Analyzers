using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a namespace whose dotted path repeats the same segment — adjacent (<c>My.Project.Models.Models</c>) or
    /// not (<c>My.Project.Models.Abstractions.Models</c>). An echoed segment is a naming smell that makes the
    /// namespace harder to read and hints at a misplaced nesting level.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA042Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA042";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA042AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA042AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA042AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA042.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeNamespace, SyntaxKind.NamespaceDeclaration, SyntaxKind.FileScopedNamespaceDeclaration);
        }

        private static void AnalyzeNamespace(SyntaxNodeAnalysisContext context)
        {
            var declaration = (BaseNamespaceDeclarationSyntax)context.Node;

            // [fail-open] Resolve the full, effective namespace path by symbol so nested declarations are handled.
            if (context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken) is not INamespaceSymbol namespaceSymbol)
                return;

            var fullName = namespaceSymbol.ToDisplayString();

            // Only the segments THIS declaration introduces (its own dotted name) can make it the culprit. A
            // physically nested declaration (`namespace A.A { namespace B { } }`) must not re-report the "A"
            // repetition inherited from its enclosing namespace — that is reported on the ancestor `A.A` itself.
            var firstOwnSegmentIndex = CountSegments(fullName) - CountSegments(declaration.Name.ToString());
            var repeated = FindRepeatedSegment(fullName, firstOwnSegmentIndex);
            if (repeated == null)
                return;

            var diagnostic = Diagnostic.Create(
                _rule,
                declaration.Name.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                messageArgs: new object[] { fullName, repeated });

            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>Number of non-empty dot-separated segments in <paramref name="dottedName"/>.</summary>
        internal static int CountSegments(string dottedName) =>
            string.IsNullOrEmpty(dottedName) ? 0 : dottedName.Split('.').Length;

        /// <summary>
        /// Returns the first dot-separated segment of <paramref name="fullName"/> whose DUPLICATE occurrence is at
        /// or after <paramref name="firstOwnSegmentIndex"/> — the boundary between the segments inherited from the
        /// enclosing namespace and the ones this declaration itself introduces (ordinal comparison), else null. So a
        /// nested declaration does not re-report a repetition inherited from an ancestor (reported on the ancestor),
        /// while a repetition the declaration itself introduces still fires.
        /// </summary>
        internal static string FindRepeatedSegment(string fullName, int firstOwnSegmentIndex)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            if (firstOwnSegmentIndex < 0)
                firstOwnSegmentIndex = 0;

            var segments = fullName.Split('.');
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = 0; index < segments.Length; index++)
            {
                var segment = segments[index];
                if (segment.Length == 0)
                    continue;

                if (!seen.Add(segment) && index >= firstOwnSegmentIndex)
                    return segment;
            }

            return null;
        }
    }
}
