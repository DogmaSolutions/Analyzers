using System;
using System.Collections.Generic;
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
    /// Flags a loop that waits for the state of its own class to change by sleeping (<c>Thread.Sleep</c>, <c>Task.Delay</c>) and checking again.
    /// Polling adds latency to every change, burns wake-ups, and, when nothing in the loop gives up, waits forever if the other side fails.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA048Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA048";

        private const string ThreadType = "System.Threading.Thread";
        private const string TaskType = "System.Threading.Tasks.Task";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA048AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA048AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA048AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA048.md");

        /// <summary>The names that show that a loop looks at the clock or at a cancellation request.</summary>
        private static readonly ImmutableHashSet<string> _giveUpNames = ImmutableHashSet.Create(
            "Now", "UtcNow", "Elapsed", "ElapsedMilliseconds", "ElapsedTicks", "TickCount", "TickCount64", "GetTimestamp", "IsCancellationRequested", "ThrowIfCancellationRequested");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeLoop, SyntaxKind.WhileStatement, SyntaxKind.DoStatement);
        }

        private static void AnalyzeLoop(SyntaxNodeAnalysisContext context)
        {
            var (condition, body, headerStart) = context.Node switch
            {
                WhileStatementSyntax loop => (loop.Condition, loop.Statement, loop.WhileKeyword.SpanStart),
                _ => (((DoStatementSyntax)context.Node).Condition, ((DoStatementSyntax)context.Node).Statement, ((DoStatementSyntax)context.Node).WhileKeyword.SpanStart),
            };

            // syntactic prefilter: nearly every loop of a code base stops here
            if (!HasWaitCall(body))
                return;

            var settings = DSA048Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree));
            if ((context.Node is DoStatementSyntax && !settings.IncludeDoLoops) || AnalyzersUtils.IsFileExcluded(context.Node.SyntaxTree.FilePath, settings.ExcludedFilePatterns))
                return;

            var wait = FindWait(body, settings, context);
            if (wait == null)
                return;

            var enclosing = context.SemanticModel.GetEnclosingSymbol(context.Node.SpanStart, context.CancellationToken);
            if (enclosing?.ContainingType is not { } type || IsExcluded(enclosing, type, settings))
                return;

            var shared = FindSharedState(condition, type, context);
            if (shared.Count == 0 || WritesAny(body, shared, context))
                return;

            var hasGiveUp = HasGiveUp(condition);
            if (hasGiveUp && !settings.ReportLoopsWithTimeout)
                return;

            var resource = hasGiveUp ? nameof(Resources.DSA048MessageWithTimeout) : nameof(Resources.DSA048MessageNoTimeout);
            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(resource, Resources.Culture), shared[0].Name, wait);
            var location = Location.Create(context.Node.SyntaxTree, TextSpan.FromBounds(headerStart, condition.Parent is WhileStatementSyntax w ? w.CloseParenToken.Span.End : ((DoStatementSyntax)condition.Parent).CloseParenToken.Span.End));
            context.ReportDiagnostic(Diagnostic.Create(_rule, location, message));
        }

        // ---- the waits ----------------------------------------------------------------------------------------------

        /// <summary>Whether the loop itself calls something named <c>Sleep</c> or <c>Delay</c>: a lambda or a local function runs somewhere else.</summary>
        private static bool HasWaitCall(StatementSyntax body)
        {
            foreach (var node in body.DescendantNodes(static node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)))
            {
                if (node is InvocationExpressionSyntax call && GetCalledName(call) is "Sleep" or "Delay")
                    return true;
            }

            return false;
        }

        /// <summary>The first call of the loop that really is <c>Thread.Sleep</c> (or <c>Task.Delay</c>, if the options say so).</summary>
        private static string FindWait(StatementSyntax body, DSA048Settings settings, SyntaxNodeAnalysisContext context)
        {
            foreach (var node in body.DescendantNodes(static node => node is not (AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)))
            {
                if (node is InvocationExpressionSyntax call && GetCalledName(call) is "Sleep" or "Delay" && GetWaitName(call, context) is { } name && (settings.IncludeTaskDelay || name == "Thread.Sleep"))
                    return name;
            }

            return null;
        }

        private static string GetCalledName(InvocationExpressionSyntax call) => call.Expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            _ => null,
        };

        private static string GetWaitName(InvocationExpressionSyntax call, SyntaxNodeAnalysisContext context)
        {
            var method = context.SemanticModel.GetSymbolInfo(call, context.CancellationToken).Symbol as IMethodSymbol;
            var ownerName = method?.ContainingType == null ? null : SymbolNames.GetFullName(method.ContainingType);
            return ownerName == ThreadType && method.Name == "Sleep" ? "Thread.Sleep" : ownerName == TaskType && method.Name == "Delay" ? "Task.Delay" : null;
        }

        // ---- the state the loop waits for ---------------------------------------------------------------------------

        /// <summary>The members of the class (or of its base classes) that the condition reads: what the loop is waiting for.</summary>
        private static List<ISymbol> FindSharedState(ExpressionSyntax condition, INamedTypeSymbol type, SyntaxNodeAnalysisContext context)
        {
            var result = new List<ISymbol>();
            foreach (var name in condition.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            {
                // x.Name: only the x counts (it is what belongs to the class), unless the member is reached through this
                if (name.Parent is MemberAccessExpressionSyntax member && member.Name == name && member.Expression is not ThisExpressionSyntax)
                    continue;

                var symbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
                if (IsMutableMember(symbol, name) && IsMemberOf(symbol, type))
                    result.Add(symbol);
            }

            return result;
        }

        private static bool IsMutableMember(ISymbol symbol, IdentifierNameSyntax name) => symbol switch
        {
            IFieldSymbol field => !field.IsConst && (!field.IsReadOnly || name.Parent is MemberAccessExpressionSyntax { Expression: var receiver } && receiver == name),
            IPropertySymbol => true,
            IMethodSymbol method => method.MethodKind == MethodKind.Ordinary,
            _ => false,
        };

        private static bool IsMemberOf(ISymbol symbol, INamedTypeSymbol type)
        {
            for (var current = type; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, symbol.ContainingType?.OriginalDefinition))
                    return true;
            }

            return false;
        }

        /// <summary>The loop changes what it waits for itself (a retry counter, a cursor): it makes progress, it does not wait for another thread.</summary>
        private static bool WritesAny(StatementSyntax body, List<ISymbol> shared, SyntaxNodeAnalysisContext context)
        {
            foreach (var name in body.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!IsWritten(name))
                    continue;

                var symbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol;
                if (symbol != null && shared.Any(s => SymbolEqualityComparer.Default.Equals(s, symbol)))
                    return true;
            }

            return false;
        }

        private static bool IsWritten(IdentifierNameSyntax name)
        {
            SyntaxNode current = name.Parent is MemberAccessExpressionSyntax member && member.Name == name ? member : name;
            return current.Parent switch
            {
                AssignmentExpressionSyntax assignment => assignment.Left == current,
                PrefixUnaryExpressionSyntax prefix => prefix.IsKind(SyntaxKind.PreIncrementExpression) || prefix.IsKind(SyntaxKind.PreDecrementExpression),
                PostfixUnaryExpressionSyntax => true,
                ArgumentSyntax argument => argument.RefKindKeyword.IsKind(SyntaxKind.RefKeyword) || argument.RefKindKeyword.IsKind(SyntaxKind.OutKeyword),
                _ => false,
            };
        }

        // ---- giving up ----------------------------------------------------------------------------------------------

        /// <summary>The loop looks at the clock, at a cancellation request, or at something that is called a timeout or a deadline.</summary>
        private static bool HasGiveUp(ExpressionSyntax condition) =>
            condition.Parent.DescendantNodes().OfType<IdentifierNameSyntax>().Any(static name =>
                _giveUpNames.Contains(name.Identifier.ValueText) ||
                name.Identifier.ValueText.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.Identifier.ValueText.IndexOf("deadline", StringComparison.OrdinalIgnoreCase) >= 0);

        // ---- the exclusions -----------------------------------------------------------------------------------------

        private static bool IsExcluded(ISymbol member, INamedTypeSymbol type, DSA048Settings settings) =>
            Matches(member.Name, settings.ExcludedMemberNamePatterns) ||
            Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
            (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
            (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
