using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a <c>lock</c> whose critical section is nothing but a single plain read or assignment of a field. Locking each access to a
    /// field separately does not make the logic that uses the field thread-safe: races live between operations (check-then-act,
    /// read-modify-write), not inside a single access. Every scenario (read or write, dedicated, shared or <c>this</c> lock, plain,
    /// tearable, volatile or readonly field) has its own message, which explains what is wrong with that specific lock.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA010Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA010";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA010AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA010AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA010AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA010.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLockStatement, SyntaxKind.LockStatement);
        }

        private static void AnalyzeLockStatement(SyntaxNodeAnalysisContext context)
        {
            var lockStatement = (LockStatementSyntax)context.Node;

            // syntactic prefilter: nearly every lock statement of a code base stops here
            if (!DSA010Pattern.HasSingleAccessShape(lockStatement))
                return;

            var settings = DSA010Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(lockStatement.SyntaxTree));
            var finding = DSA010Evaluator.Evaluate(lockStatement, context.SemanticModel, settings, context.CancellationToken);
            if (finding == null)
                return;

            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(finding.MessageResourceName, Resources.Culture), finding.FieldName, finding.LockName);
            context.ReportDiagnostic(Diagnostic.Create(_rule, GetHeaderLocation(lockStatement), message));
        }

        /// <summary>The <c>lock (...)</c> part of the statement: the critical section itself is not the issue.</summary>
        internal static Location GetHeaderLocation(LockStatementSyntax lockStatement) =>
            Location.Create(lockStatement.SyntaxTree, TextSpan.FromBounds(lockStatement.LockKeyword.SpanStart, lockStatement.CloseParenToken.Span.End));
    }
}
