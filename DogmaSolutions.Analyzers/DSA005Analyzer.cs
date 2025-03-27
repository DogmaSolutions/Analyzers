using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Potential non-deterministic point-in-time execution
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA005Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA005";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA005AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA005AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA005AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA005");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Find all invocations of DateTime.Now within the method body
            var dateTimeNowInvocations = methodDeclaration.DescendantNodes().
                OfType<MemberAccessExpressionSyntax>().
                Where(
                    m => m.Expression is IdentifierNameSyntax { Identifier.ValueText: "DateTime" or "DateTimeOffset" } &&
                         m.Name?.Identifier.ValueText is "Now" or "UtcNow");

            // Check if there are multiple occurrences
            if (dateTimeNowInvocations.Count() > 1)
            {
                var diagnostic = Diagnostic.Create(
                    _rule,
                    methodDeclaration.GetLocation(),
                    effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                    additionalLocations: null,
                    properties: null);
                
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}