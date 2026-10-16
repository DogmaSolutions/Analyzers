using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags a <c>lock</c> on an object that is not a private, never reassigned lock object: a field or an auto-property that is replaced after
    /// construction (two threads then lock two different objects and exclude nobody), or that code outside the class can reach (and lock on too,
    /// entangling its critical sections with the ones of the class). Every scenario has its own message.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA047Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA047";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA047AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA047AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA047AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA047.md");

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

            // syntactic prefilter: only a lock on a name (x, this.x) can be a field or a property of the class
            var target = Unparenthesize(lockStatement.Expression);
            if (!IsOwnMemberName(target))
                return;

            var settings = DSA047Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(lockStatement.SyntaxTree));
            if (AnalyzersUtils.IsFileExcluded(lockStatement.SyntaxTree.FilePath, settings.ExcludedFilePatterns))
                return;

            var symbol = context.SemanticModel.GetSymbolInfo(target, context.CancellationToken).Symbol;
            var finding = symbol switch
            {
                IFieldSymbol field when settings.ReportFields => EvaluateField(field, context, settings),
                IPropertySymbol property when settings.ReportProperties => EvaluateProperty(property, settings),
                _ => null,
            };

            if (finding == null || IsExcluded(symbol, settings))
                return;

            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(finding.Value.ResourceName, Resources.Culture), symbol.Name, finding.Value.Reason);
            context.ReportDiagnostic(Diagnostic.Create(_rule, GetHeaderLocation(lockStatement), message));
        }

        /// <summary>The <c>lock (...)</c> part of the statement: the critical section itself is not the issue.</summary>
        internal static Location GetHeaderLocation(LockStatementSyntax lockStatement) =>
            Location.Create(lockStatement.SyntaxTree, TextSpan.FromBounds(lockStatement.LockKeyword.SpanStart, lockStatement.CloseParenToken.Span.End));

        private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            return expression;
        }

        private static bool IsOwnMemberName(ExpressionSyntax target) =>
            target is IdentifierNameSyntax || target is MemberAccessExpressionSyntax { Expression: ThisExpressionSyntax, Name: IdentifierNameSyntax };

        // ---- the scenarios ------------------------------------------------------------------------------------------

        private static (string ResourceName, string Reason)? EvaluateField(IFieldSymbol field, SyntaxNodeAnalysisContext context, DSA047Settings settings)
        {
            // a lock on a member of another assembly, or on a constant, is not a lock object of this code
            if (field.DeclaringSyntaxReferences.IsEmpty || field.IsConst)
                return null;

            var reassigned = false;
            var reach = DSA047Reaches.Of(field.DeclaredAccessibility);
            var reason = Resources.DSA047ReasonField;
            if (!field.IsReadOnly || reach == DSA047Reach.Private)
            {
                var usage = DSA047UsageScanner.Get(field, context.Compilation, context.CancellationToken);
                reassigned = !field.IsReadOnly && usage.Reassigned;
                if (usage.ReturnedReach > reach)
                {
                    reach = usage.ReturnedReach;
                    reason = string.Format(Resources.Culture, Resources.DSA047ReasonReturned, usage.ReturnedBy);
                }
            }

            return Choose(reassigned, reach, reason, "Field", settings);
        }

        private static (string ResourceName, string Reason)? EvaluateProperty(IPropertySymbol property, DSA047Settings settings)
        {
            // only an auto-property is a stable object: any other property can compute a new one on every call
            if (property.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is not PropertyDeclarationSyntax { ExpressionBody: null } declaration ||
                declaration.AccessorList == null || declaration.AccessorList.Accessors.Any(static accessor => accessor.Body != null || accessor.ExpressionBody != null))
                return null;

            var reassigned = property.SetMethod is { IsInitOnly: false };
            var reach = DSA047Reaches.Of(property.GetMethod?.DeclaredAccessibility ?? Accessibility.Private);
            return Choose(reassigned, reach, Resources.DSA047ReasonProperty, "Property", settings);
        }

        private static (string ResourceName, string Reason)? Choose(bool reassigned, DSA047Reach reach, string reason, string kind, DSA047Settings settings)
        {
            var swapped = reassigned && settings.ReportSwappedLocks;
            var exposed = settings.ReportExposedLocks && (reach == DSA047Reach.External || (reach == DSA047Reach.Assembly && settings.ReportInternalLocks));
            if (!swapped && !exposed)
                return null;

            var scenario = swapped ? exposed ? "SwappedAndExposed" : "Swapped" : "Exposed";
            return ("DSA047Message" + kind + scenario, reason);
        }

        // ---- the exclusions -----------------------------------------------------------------------------------------

        private static bool IsExcluded(ISymbol symbol, DSA047Settings settings)
        {
            var type = symbol.ContainingType;
            return Matches(symbol.Name, settings.ExcludedLockNamePatterns) ||
                   Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
                   (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
                   (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));
        }

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
