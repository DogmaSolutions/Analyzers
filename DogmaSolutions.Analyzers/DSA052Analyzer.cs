#nullable disable
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a <c>FirstOrDefault</c> or <c>SingleOrDefault</c> (and their async versions) whose element is never used but to test whether it is null: the
    /// element is fetched (and, in a database query, loaded, materialized and possibly tracked) to be thrown away, and <c>Any</c> asks the real question.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA052Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA052";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA052AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA052AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA052AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Performance;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA052.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            var settings = DSA052Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree));
            var use = ExistenceAnalyzerCore.Find(context, settings, ExistenceKind.Item);
            if (use == null)
                return;

            context.ReportDiagnostic(Diagnostic.Create(_rule, context.Node.GetLocation(), ExistenceAnalyzerCore.Describe("052", use, Resources.DSA052SingleNote)));
        }
    }
}
