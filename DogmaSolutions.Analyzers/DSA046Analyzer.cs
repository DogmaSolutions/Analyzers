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
    /// Flags a read-modify-write of a <c>volatile</c> field: <c>x++</c>, <c>x += v</c>, <c>x = f(x)</c>. <c>volatile</c> makes each single read and write
    /// visible to the other threads, it does not make the sequence of the two indivisible: two threads read the same value and an update is lost.
    /// Every scenario (operation, and kind of field: <c>Interlocked</c>-capable, other value type, reference) has its own message.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA046Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA046";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA046AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA046AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA046AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA046.md");

        private static readonly ImmutableArray<SyntaxKind> _compoundKinds =
        [
            SyntaxKind.AddAssignmentExpression, SyntaxKind.SubtractAssignmentExpression, SyntaxKind.MultiplyAssignmentExpression,
            SyntaxKind.DivideAssignmentExpression, SyntaxKind.ModuloAssignmentExpression, SyntaxKind.AndAssignmentExpression,
            SyntaxKind.OrAssignmentExpression, SyntaxKind.ExclusiveOrAssignmentExpression, SyntaxKind.LeftShiftAssignmentExpression,
            SyntaxKind.RightShiftAssignmentExpression, SyntaxKind.UnsignedRightShiftAssignmentExpression,
        ];

        private static readonly ImmutableArray<SyntaxKind> _incrementKinds =
        [
            SyntaxKind.PreIncrementExpression, SyntaxKind.PreDecrementExpression, SyntaxKind.PostIncrementExpression, SyntaxKind.PostDecrementExpression,
        ];

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeIncrement, _incrementKinds);
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, _compoundKinds.Add(SyntaxKind.SimpleAssignmentExpression));
        }

        private static void AnalyzeIncrement(SyntaxNodeAnalysisContext context)
        {
            var (operand, operatorText) = context.Node switch
            {
                PrefixUnaryExpressionSyntax prefix => (prefix.Operand, prefix.OperatorToken.ValueText),
                _ => (((PostfixUnaryExpressionSyntax)context.Node).Operand, ((PostfixUnaryExpressionSyntax)context.Node).OperatorToken.ValueText),
            };

            Analyze(context, operand, DSA046Operation.Increment, operatorText);
        }

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignment = (AssignmentExpressionSyntax)context.Node;
            if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                // syntactic prefilter: the value has to mention the name of the field
                var name = GetFieldName(assignment.Left);
                if (name == null || !Mentions(assignment.Right, name))
                    return;

                Analyze(context, assignment.Left, DSA046Operation.Assignment, "=");
            }
            else
            {
                Analyze(context, assignment.Left, DSA046Operation.Compound, assignment.OperatorToken.ValueText);
            }
        }

        private static bool Mentions(ExpressionSyntax value, string name)
        {
            foreach (var token in value.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText == name)
                    return true;
            }

            return false;
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, ExpressionSyntax target, DSA046Operation operation, string operatorText)
        {
            if (GetFieldName(target) == null)
                return;

            var settings = DSA046Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree));
            if (!IsOperationEnabled(operation, settings))
                return;

            // the file is looked at only for the operations on a volatile field: nearly all of them are on locals
            if (context.SemanticModel.GetSymbolInfo(Unparenthesize(target), context.CancellationToken).Symbol is not IFieldSymbol { IsVolatile: true } field ||
                IsExcluded(field, settings) || AnalyzersUtils.IsFileExcluded(context.Node.SyntaxTree.FilePath, settings.ExcludedFilePatterns))
                return;

            var kind = GetKind(field.Type);
            if (!IsKindEnabled(kind, settings) || IsExemptedContext(context.Node, settings))
                return;

            // x = f(x) is a read-modify-write only when the value really is computed from the same field of the same instance
            if (operation == DSA046Operation.Assignment && !ReadsTheSameField(((AssignmentExpressionSyntax)context.Node).Right, Unparenthesize(target), field, context))
                return;

            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(DSA046Scenario.GetMessageResourceName(operation, kind), Resources.Culture), field.Name, operatorText);
            context.ReportDiagnostic(Diagnostic.Create(_rule, context.Node.GetLocation(), message));
        }

        // ---- the target ---------------------------------------------------------------------------------------------

        private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            return expression;
        }

        /// <summary>The simple name a field is accessed with (<c>x</c>, <c>this.x</c>, <c>other.x</c>), or null for any other expression.</summary>
        private static string GetFieldName(ExpressionSyntax expression) => Unparenthesize(expression) switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax { Name: IdentifierNameSyntax name } member when member.IsKind(SyntaxKind.SimpleMemberAccessExpression) => name.Identifier.ValueText,
            _ => null,
        };

        private static string GetReceiver(ExpressionSyntax access) =>
            access is MemberAccessExpressionSyntax { Expression: not ThisExpressionSyntax } member ? member.Expression.ToString() : string.Empty;

        private static bool ReadsTheSameField(ExpressionSyntax value, ExpressionSyntax target, IFieldSymbol field, SyntaxNodeAnalysisContext context)
        {
            var receiver = GetReceiver(target);
            foreach (var identifier in value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
            {
                if (identifier.Identifier.ValueText != field.Name)
                    continue;

                var access = identifier.Parent is MemberAccessExpressionSyntax member && member.Name == identifier ? member : (ExpressionSyntax)identifier;
                if (GetReceiver(access) == receiver &&
                    SymbolEqualityComparer.Default.Equals(context.SemanticModel.GetSymbolInfo(identifier, context.CancellationToken).Symbol, field))
                    return true;
            }

            return false;
        }

        // ---- the options --------------------------------------------------------------------------------------------

        private static bool IsOperationEnabled(DSA046Operation operation, DSA046Settings settings) => operation switch
        {
            DSA046Operation.Increment => settings.ReportIncrements,
            DSA046Operation.Compound => settings.ReportCompoundAssignments,
            _ => settings.ReportSelfAssignments,
        };

        private static bool IsKindEnabled(DSA046FieldKind kind, DSA046Settings settings) => kind switch
        {
            DSA046FieldKind.Interlocked => settings.ReportInterlockedTypes,
            DSA046FieldKind.Value => settings.ReportOtherValueTypes,
            _ => settings.ReportReferenceTypes,
        };

        /// <summary>The types <c>Interlocked</c> works on directly, among the ones a field can be volatile for: <c>int</c> and <c>uint</c>.</summary>
        internal static DSA046FieldKind GetKind(ITypeSymbol type)
        {
            if (type.IsReferenceType)
                return DSA046FieldKind.Reference;

            return type.SpecialType is SpecialType.System_Int32 or SpecialType.System_UInt32 ? DSA046FieldKind.Interlocked : DSA046FieldKind.Value;
        }

        private static bool IsExemptedContext(SyntaxNode node, DSA046Settings settings)
        {
            // the nearest boundary decides: a lambda or a local function can run later, from anywhere
            var boundary = node.Ancestors().FirstOrDefault(static a => a is LockStatementSyntax or AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax);
            return (settings.SkipInsideLock && boundary is LockStatementSyntax) ||
                   (settings.SkipConstructors && node.Ancestors().FirstOrDefault(static a => a is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or MemberDeclarationSyntax) is ConstructorDeclarationSyntax);
        }

        private static bool IsExcluded(IFieldSymbol field, DSA046Settings settings)
        {
            var type = field.ContainingType;
            return Matches(field.Name, settings.ExcludedFieldNamePatterns) ||
                   Matches(type.Name, settings.ExcludedTypeNamePatterns) ||
                   (settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns)) ||
                   (settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes));
        }

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }

    internal enum DSA046Operation
    {
        Increment,
        Compound,
        Assignment,
    }

    internal enum DSA046FieldKind
    {
        Interlocked,
        Value,
        Reference,
    }

    internal static class DSA046Scenario
    {
        internal static string GetMessageResourceName(DSA046Operation operation, DSA046FieldKind kind) => "DSA046Message" + operation + kind;
    }
}
