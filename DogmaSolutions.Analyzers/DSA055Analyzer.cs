#nullable disable
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
    /// Flags an integral variable, parameter, field or property that is named like an identifier or a key and is compared with zero by
    /// equality (<c>id != 0</c>, <c>0 == key</c>, <c>id is 0</c>). An identifier is not expected to be zero or negative: a validity check
    /// written with <c>!= 0</c> accepts every negative value, and one written with <c>== 0</c> rejects only zero.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA055Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA055";

        /// <summary>Property of the diagnostic: the operator that replaces the equality (<c>&gt;</c> or <c>&lt;=</c>).</summary>
        internal const string ReplacementProperty = "Replacement";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA055AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        // the message of each scenario is a resource of its own, and arrives here already formatted
        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA055AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA055AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA055.md");

        private static readonly ImmutableDictionary<string, string> _greater = ImmutableDictionary<string, string>.Empty.Add(ReplacementProperty, ">");
        private static readonly ImmutableDictionary<string, string> _lessOrEqual = ImmutableDictionary<string, string>.Empty.Add(ReplacementProperty, "<=");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeComparison, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
            context.RegisterSyntaxNodeAction(AnalyzePattern, SyntaxKind.IsPatternExpression);
        }

        private static void AnalyzeComparison(SyntaxNodeAnalysisContext context)
        {
            var comparison = (BinaryExpressionSyntax)context.Node;
            var operand = IsZero(comparison.Right) ? comparison.Left : IsZero(comparison.Left) ? comparison.Right : null;
            if (operand != null)
                Analyze(context, operand, comparison.IsKind(SyntaxKind.EqualsExpression));
        }

        private static void AnalyzePattern(SyntaxNodeAnalysisContext context)
        {
            var isPattern = (IsPatternExpressionSyntax)context.Node;
            switch (isPattern.Pattern)
            {
                case ConstantPatternSyntax constant when IsZero(constant.Expression):
                    Analyze(context, isPattern.Expression, isEquals: true);
                    break;
                case UnaryPatternSyntax { Pattern: ConstantPatternSyntax negated } when IsZero(negated.Expression):
                    Analyze(context, isPattern.Expression, isEquals: false);
                    break;
            }
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, ExpressionSyntax operand, bool isEquals)
        {
            // syntactic first: the name tells nearly all the comparisons with zero that are not about a key
            operand = Unparenthesize(operand);
            var name = GetName(operand);
            if (name == null)
                return;

            var settings = DSA055Settings.Cache.Get(context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree));
            if ((isEquals && !settings.ReportEquality) || !settings.IsKeyName(name) || Matches(name, settings.ExcludedNamePatterns))
                return;

            var type = GetIntegralType(operand, settings, context);
            if (type == null || !IsReported(type, settings) || IsExcluded(context.ContainingSymbol, settings, context.Node.SyntaxTree))
                return;

            var unsigned = IsUnsigned(type);
            var resource = (isEquals, unsigned) switch
            {
                (false, false) => nameof(Resources.DSA055MessageNotEqualsSigned),
                (true, false) => nameof(Resources.DSA055MessageEqualsSigned),
                (false, true) => nameof(Resources.DSA055MessageNotEqualsUnsigned),
                _ => nameof(Resources.DSA055MessageEqualsUnsigned),
            };

            var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString(resource, Resources.Culture), operand.ToString(), type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            context.ReportDiagnostic(Diagnostic.Create(_rule, context.Node.GetLocation(), isEquals ? _lessOrEqual : _greater, message));
        }

        // ---- the syntax -------------------------------------------------------------------------------------------------------

        private static ExpressionSyntax Unparenthesize(ExpressionSyntax expression)
        {
            while (expression is ParenthesizedExpressionSyntax parenthesized)
                expression = parenthesized.Expression;

            return expression;
        }

        /// <summary>The literal 0 of any integral type and base: <c>0</c>, <c>0L</c>, <c>0u</c>, <c>0x0</c>, <c>0b0</c>.</summary>
        internal static bool IsZero(ExpressionSyntax expression) =>
            Unparenthesize(expression) is LiteralExpressionSyntax { RawKind: (int)SyntaxKind.NumericLiteralExpression } literal &&
            literal.Token.Value switch
            {
                int value => value == 0,
                uint value => value == 0,
                long value => value == 0,
                ulong value => value == 0,
                _ => false,
            };

        /// <summary>The name of a variable, a parameter or a member: <c>id</c> in <c>id</c>, <c>this.id</c> or <c>order.id</c>.</summary>
        private static string GetName(ExpressionSyntax operand) => operand switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax { RawKind: (int)SyntaxKind.SimpleMemberAccessExpression, Name: IdentifierNameSyntax member } => member.Identifier.ValueText,
            _ => null,
        };

        // ---- the symbol -------------------------------------------------------------------------------------------------------

        /// <summary>The type of the operand when it is a variable, a parameter or (if enabled) a field or a property of an integral type, null otherwise.</summary>
        private static ITypeSymbol GetIntegralType(ExpressionSyntax operand, DSA055Settings settings, SyntaxNodeAnalysisContext context)
        {
            var type = context.SemanticModel.GetSymbolInfo(operand, context.CancellationToken).Symbol switch
            {
                ILocalSymbol { IsConst: false } local => local.Type,
                IParameterSymbol parameter => parameter.Type,
                IFieldSymbol { IsConst: false } field when settings.ReportMembers => field.Type,
                IPropertySymbol { IsIndexer: false } property when settings.ReportMembers => property.Type,
                _ => null,
            };

            return type != null && IsIntegral(type) ? type : null;
        }

        /// <summary>The signed and unsigned integers of 8 to 64 bits (not <c>char</c>, not the native-sized integers whose width depends on the platform, not the nullable ones).</summary>
        private static bool IsIntegral(ITypeSymbol type) => type.SpecialType is
            SpecialType.System_SByte or SpecialType.System_Byte or SpecialType.System_Int16 or SpecialType.System_UInt16 or
            SpecialType.System_Int32 or SpecialType.System_UInt32 or SpecialType.System_Int64 or SpecialType.System_UInt64;

        private static bool IsUnsigned(ITypeSymbol type) => type.SpecialType is SpecialType.System_Byte or SpecialType.System_UInt16 or SpecialType.System_UInt32 or SpecialType.System_UInt64;

        private static bool IsReported(ITypeSymbol type, DSA055Settings settings) => settings.ReportUnsignedTypes || !IsUnsigned(type);

        // ---- the exclusions ---------------------------------------------------------------------------------------------------

        private static bool IsExcluded(ISymbol containing, DSA055Settings settings, SyntaxTree tree)
        {
            var type = containing?.ContainingType;
            return AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns) ||
                   (containing != null && Matches(containing.Name, settings.ExcludedMemberNamePatterns)) ||
                   (type != null && (Matches(type.Name, settings.ExcludedTypeNamePatterns) || IsNamespaceExcluded(type, settings) || IsBaseTypeExcluded(type, settings)));
        }

        /// <summary>The name of the namespace is built only if there is a pattern to match it with.</summary>
        private static bool IsNamespaceExcluded(INamedTypeSymbol type, DSA055Settings settings) =>
            settings.ExcludedNamespacePatterns.Length > 0 && Matches(type.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(type.ContainingNamespace), settings.ExcludedNamespacePatterns);

        private static bool IsBaseTypeExcluded(INamedTypeSymbol type, DSA055Settings settings) =>
            settings.ExcludedBaseTypes.Length > 0 && AnalyzersUtils.InheritsFromAny(type, settings.ExcludedBaseTypes);

        private static bool Matches(string text, string[] patterns) => patterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(text, pattern));
    }
}
