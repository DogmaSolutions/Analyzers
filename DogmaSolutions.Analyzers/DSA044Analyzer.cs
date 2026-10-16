using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a call to <c>Console.Write*</c> or <c>Debug.Write*</c> in code that has a logger available (Microsoft.Extensions.Logging,
    /// Serilog, NLog, log4net, Castle, Common.Logging, NServiceBus, Akka.NET, TraceSource). The text written to the console is lost, or
    /// worse interleaved with other output, and carries no level, category, timestamp or structure: a log entry does. Which calls, which
    /// loggers and which places are exempt are configurable, see the options of <see cref="DSA044Settings"/>.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA044Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA044";

        /// <summary>Properties of the diagnostic, for the code fix.</summary>
        internal const string LoggerNameProperty = "LoggerName";
        internal const string IsMicrosoftLoggerProperty = "IsMicrosoftLogger";
        internal const string LevelProperty = "Level";
        internal const string HasCategoryProperty = "HasCategory";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA044AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA044AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA044AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.BestPractice;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA044.md");

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
            var invocation = (InvocationExpressionSyntax)context.Node;
            var settings = DSA044Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(invocation.SyntaxTree));

            // cheap, syntactic: only the calls named like a flagged member are bound
            var name = SyntaxUtils.GetInvokedName(invocation);
            if (name == null || !settings.FlaggedMembers.Any(pattern => AnalyzersUtils.MatchesGlobPattern(name, pattern)))
                return;

            var flagged = DSA044FlaggedCallFinder.Find(invocation, context.SemanticModel, settings, context.CancellationToken);
            var enclosing = context.SemanticModel.GetEnclosingSymbol(invocation.SpanStart, context.CancellationToken);
            var member = enclosing == null ? null : DSA044LoggerAvailability.GetEnclosingMember(enclosing);
            if (flagged == null || member?.ContainingType == null || DSA044Exclusions.IsExcludedByName(member, invocation.SyntaxTree, settings))
                return;

            var logger = DSA044LoggerAvailability.Find(enclosing, context.Compilation, settings, context.CancellationToken);
            if (logger == null || DSA044Exclusions.IsInteractive(invocation, context.SemanticModel, settings, context.CancellationToken) ||
                DSA044ExplicitLoggerChecks.IsFallbackOfAnExplicitCheck(invocation, enclosing, context.SemanticModel, settings, context.CancellationToken))
                return;

            Report(context, invocation, flagged, logger);
        }

        private static void Report(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, FlaggedCall flagged, AvailableLogger logger)
        {
            var properties = ImmutableDictionary<string, string>.Empty
                .Add(LoggerNameProperty, logger.Name)
                .Add(IsMicrosoftLoggerProperty, logger.IsMicrosoftLogger ? "true" : "false")
                .Add(LevelProperty, flagged.Level)
                .Add(HasCategoryProperty, flagged.HasCategory ? "true" : "false");

            context.ReportDiagnostic(Diagnostic.Create(_rule, invocation.GetLocation(), properties, flagged.DisplayName, logger.Name));
        }
    }
}
