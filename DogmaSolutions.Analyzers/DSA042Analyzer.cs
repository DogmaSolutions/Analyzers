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
            var repeated = FindRepeatedSegment(fullName);
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

        /// <summary>Returns the first dot-separated segment of <paramref name="fullName"/> that occurs more than once (ordinal), else null.</summary>
        internal static string FindRepeatedSegment(string fullName)
        {
            if (string.IsNullOrEmpty(fullName))
                return null;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var segment in fullName.Split('.'))
            {
                if (segment.Length > 0 && !seen.Add(segment))
                    return segment;
            }

            return null;
        }
    }
}
