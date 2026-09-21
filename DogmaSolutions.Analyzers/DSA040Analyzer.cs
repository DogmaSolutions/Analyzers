using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Flags output of the non-cryptographic <see cref="System.Random"/> that flows into a
    /// security-sensitive sink — a symbol whose name matches a security term such as token, salt,
    /// nonce, key, secret, password, session or otp. Such values must come from
    /// <see cref="System.Security.Cryptography.RandomNumberGenerator"/>, whose output is unpredictable.
    /// The name heuristic keeps the rule high-precision, complementing the broad CA5394.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA040Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA040";

        // [CySec] Mitigates CWE-338 "Use of Cryptographically Weak PRNG": the single source of truth for the
        // security-sensitive sink names this rule recognises. [RiskMitigation RA-01]
        private static readonly ImmutableHashSet<string> SecurityTerms = new[]
        {
            "token", "password", "passwd", "pwd", "salt", "nonce", "iv", "secret",
            "apikey", "session", "sessionid", "csrf", "otp", "key", "resetcode", "verificationcode"
        }.ToImmutableHashSet(StringComparer.Ordinal);

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA040AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA040AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA040AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Security;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA040.md");

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

            if (context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is not IMethodSymbol method)
                return;

            // Producer: any Next* method on System.Random (covers both `rnd.Next()` and `Random.Shared.Next()`).
            var randomType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Random");
            if (randomType == null || !SymbolEqualityComparer.Default.Equals(method.ContainingType, randomType))
                return;
            if (!IsNextMethodName(method.Name))
                return;

            // NextBytes(buffer) returns void; its sink is the buffer argument's name.
            if (method.Name == "NextBytes" && invocation.ArgumentList.Arguments.Count == 1)
            {
                if (IsSecuritySensitiveName(GetSymbolName(invocation.ArgumentList.Arguments[0].Expression, context)))
                    Report(context, invocation);
                return;
            }

            if (FlowsToSecuritySensitiveSink(invocation, context))
                Report(context, invocation);
        }

        private static void Report(SyntaxNodeAnalysisContext context, SyntaxNode node) =>
            context.ReportDiagnostic(Diagnostic.Create(
                _rule,
                node.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null));

        /// <summary>Is <paramref name="name"/> a <c>System.Random</c> producer method (<c>Next</c>, <c>NextBytes</c>, …)?</summary>
        internal static bool IsNextMethodName(string name) =>
            !string.IsNullOrEmpty(name) && name.StartsWith("Next", StringComparison.Ordinal);

        /// <summary>
        /// Climbs from a produced value through the value-preserving wrappers around it — parentheses, casts, and
        /// a member-access or invocation whose receiver IS the value (e.g. <c>rnd.Next().ToString()</c>) — so the
        /// caller can inspect the real consumer. Pure over syntax; no semantic model needed.
        /// </summary>
        internal static ExpressionSyntax ClimbValuePreservingWrappers(ExpressionSyntax value)
        {
            while (true)
            {
                switch (value.Parent)
                {
                    case ParenthesizedExpressionSyntax or CastExpressionSyntax:
                        value = (ExpressionSyntax)value.Parent;
                        continue;
                    case MemberAccessExpressionSyntax member when member.Expression == value:
                        value = member;
                        continue;
                    case InvocationExpressionSyntax invocation when invocation.Expression == value:
                        value = invocation;
                        continue;
                }

                return value;
            }
        }

        private static bool FlowsToSecuritySensitiveSink(ExpressionSyntax value, SyntaxNodeAnalysisContext context)
        {
            value = ClimbValuePreservingWrappers(value);

            var parent = value.Parent;

            switch (parent)
            {
                case EqualsValueClauseSyntax { Parent: VariableDeclaratorSyntax declarator }:
                    if (IsSecuritySensitiveName(declarator.Identifier.Text))
                        return true;
                    // One hop: a plain local that is later assigned to / returned as a security sink.
                    return context.SemanticModel.GetDeclaredSymbol(declarator, context.CancellationToken) is ILocalSymbol local
                           && LocalFlowsToSecuritySensitiveSink(local, declarator, context);

                case AssignmentExpressionSyntax assignment when assignment.Right == value:
                    return IsSecuritySensitiveName(GetSymbolName(assignment.Left, context) ?? NameOf(assignment.Left));

                case ReturnStatementSyntax:
                case ArrowExpressionClauseSyntax:
                    return IsSecuritySensitiveName(EnclosingMemberName(value));

                case ArgumentSyntax argument:
                    return IsSecuritySensitiveName(ParameterName(argument, context));

                default:
                    return false;
            }
        }

        private static bool LocalFlowsToSecuritySensitiveSink(ILocalSymbol local, SyntaxNode declarator, SyntaxNodeAnalysisContext context)
        {
            var scope = declarator.Ancestors().FirstOrDefault(a =>
                a is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax);
            if (scope == null)
                return false;

            foreach (var identifier in scope.DescendantNodes().OfType<IdentifierNameSyntax>())
            {
                if (!SymbolEqualityComparer.Default.Equals(
                        context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol, local))
                    continue;

                switch (identifier.Parent)
                {
                    case AssignmentExpressionSyntax assignment when assignment.Right == identifier
                        && IsSecuritySensitiveName(GetSymbolName(assignment.Left, context) ?? NameOf(assignment.Left)):
                        return true;
                    case ReturnStatementSyntax when IsSecuritySensitiveName(EnclosingMemberName(identifier)):
                        return true;
                    case ArgumentSyntax argument when IsSecuritySensitiveName(ParameterName(argument, context)):
                        return true;
                }
            }

            return false;
        }

        private static string? GetSymbolName(ExpressionSyntax expression, SyntaxNodeAnalysisContext context) =>
            context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol?.Name;

        internal static string? NameOf(ExpressionSyntax expression) => expression switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            MemberAccessExpressionSyntax member => member.Name.Identifier.Text,
            _ => null
        };

        private static string? EnclosingMemberName(SyntaxNode node)
        {
            foreach (var ancestor in node.Ancestors())
            {
                switch (ancestor)
                {
                    case MethodDeclarationSyntax method:
                        return method.Identifier.Text;
                    case LocalFunctionStatementSyntax localFunction:
                        return localFunction.Identifier.Text;
                    case PropertyDeclarationSyntax property:
                        return property.Identifier.Text;
                    case AccessorDeclarationSyntax accessor when accessor.Parent?.Parent is PropertyDeclarationSyntax owningProperty:
                        return owningProperty.Identifier.Text;
                }
            }

            return null;
        }

        private static string? ParameterName(ArgumentSyntax argument, SyntaxNodeAnalysisContext context)
        {
            if (argument.NameColon?.Name is { } named)
                return named.Identifier.Text;

            if (argument.Parent is not BaseArgumentListSyntax argumentList || argumentList.Parent is not { } invocationOrCreation)
                return null;

            if (context.SemanticModel.GetSymbolInfo(invocationOrCreation, context.CancellationToken).Symbol is not IMethodSymbol method)
                return null;

            var index = argumentList.Arguments.IndexOf(argument);
            if (index < 0 || index >= method.Parameters.Length)
                return null;

            return method.Parameters[index].Name;
        }

        internal static bool IsSecuritySensitiveName(string? name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            var words = SplitIntoWords(name!);
            if (words.Count == 0)
                return false;

            for (var i = 0; i < words.Count; i++)
            {
                if (SecurityTerms.Contains(words[i]))
                    return true;

                // Concatenate adjacent words to catch multi-word terms (session+id, reset+code, api+key).
                if (i + 1 < words.Count && SecurityTerms.Contains(words[i] + words[i + 1]))
                    return true;
            }

            return false;
        }

        /// <summary>Splits an identifier into lowercase words at underscores and camelCase / letter-digit boundaries.</summary>
        internal static List<string> SplitIntoWords(string identifier)
        {
            var words = new List<string>();
            var start = 0;

            for (var i = 1; i <= identifier.Length; i++)
            {
                var atEnd = i == identifier.Length;
                var boundary = atEnd
                    || identifier[i] == '_'
                    || (char.IsUpper(identifier[i]) && !char.IsUpper(identifier[i - 1]))
                    || (char.IsDigit(identifier[i]) != char.IsDigit(identifier[i - 1]));

                if (!boundary)
                    continue;

                if (i > start)
                {
                    var word = identifier.Substring(start, i - start).Trim('_').ToLowerInvariant();
                    if (word.Length > 0)
                        words.Add(word);
                }

                start = identifier[Math.Min(i, identifier.Length - 1)] == '_' ? i + 1 : i;
            }

            return words;
        }
    }
}
