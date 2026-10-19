#nullable disable
using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a <c>Count</c> (or <c>LongCount</c>, <c>CountAsync</c>...) of a sequence that has to be enumerated, whose number is never used but to test
    /// whether it is zero: <c>Any</c> stops at the first element. It leaves alone what CA1827 and CA1828 report already, and the sequences whose size is
    /// known without enumerating them (arrays, collections), where <c>Length</c> and <c>Count</c> are the right tool.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA053Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA053";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA053AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA053AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA053AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Performance;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA053.md");

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
            var settings = DSA053Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree));
            var use = ExistenceAnalyzerCore.Find(context, settings, ExistenceKind.Count);
            if (use == null)
                return;

            context.ReportDiagnostic(Diagnostic.Create(_rule, context.Node.GetLocation(), ExistenceAnalyzerCore.Describe("053", use)));
        }
    }
}
