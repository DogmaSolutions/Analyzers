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
    /// Flags a <c>switch</c> (statement or expression) whose governing value is of an enum type that declares more
    /// than <c>max_enum_members</c> members (default 4). Beyond a handful of cases a switch stops being the right
    /// tool: the decisional complexity invites CWE-670 (always-incorrect control flow), CWE-840 (business-logic
    /// errors) and CWE-478 (missing default / non-exhaustive) defects. The remedy is usually the Strategy pattern.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA041Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA041";

        internal const string MaxEnumMembersOptionKey = "dotnet_diagnostic.DSA041.max_enum_members";
        internal const int DefaultMaxEnumMembers = 4;

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA041AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA041AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA041AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA041.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSwitchStatement, SyntaxKind.SwitchStatement);
            context.RegisterSyntaxNodeAction(AnalyzeSwitchExpression, SyntaxKind.SwitchExpression);
        }

        private static void AnalyzeSwitchStatement(SyntaxNodeAnalysisContext context)
        {
            var switchStatement = (SwitchStatementSyntax)context.Node;
            Analyze(context, switchStatement.Expression, switchStatement.SwitchKeyword);
        }

        private static void AnalyzeSwitchExpression(SyntaxNodeAnalysisContext context)
        {
            var switchExpression = (SwitchExpressionSyntax)context.Node;
            Analyze(context, switchExpression.GoverningExpression, switchExpression.SwitchKeyword);
        }

        private static void Analyze(SyntaxNodeAnalysisContext context, ExpressionSyntax governingExpression, SyntaxToken switchKeyword)
        {
            // [fail-open] No resolvable type, or not an enum -> this rule does not apply.
            var type = context.SemanticModel.GetTypeInfo(governingExpression, context.CancellationToken).Type;
            var enumType = UnwrapEnum(type);
            if (enumType == null)
                return;

            var memberCount = CountEnumMembers(enumType);
            var maxEnumMembers = ReadMaxEnumMembers(context);
            if (memberCount <= maxEnumMembers)
                return;

            var diagnostic = Diagnostic.Create(
                _rule,
                switchKeyword.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                messageArgs: new object[] { enumType.Name, memberCount, maxEnumMembers });

            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>Returns the enum type of <paramref name="type"/> (unwrapping <c>Nullable&lt;TEnum&gt;</c>), else null.</summary>
        internal static INamedTypeSymbol UnwrapEnum(ITypeSymbol type)
        {
            if (type is not INamedTypeSymbol named)
                return null;

            if (named.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T &&
                named.TypeArguments.Length == 1)
            {
                named = named.TypeArguments[0] as INamedTypeSymbol;
            }

            return named is { TypeKind: TypeKind.Enum } ? named : null;
        }

        /// <summary>Counts the declared members of an enum (its const fields; excludes the synthetic value__ field).</summary>
        internal static int CountEnumMembers(INamedTypeSymbol enumType) =>
            enumType.GetMembers().Count(m => m is IFieldSymbol { IsConst: true });

        private static int ReadMaxEnumMembers(SyntaxNodeAnalysisContext context)
        {
            var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
            return AnalyzerOptionsReader.ReadInt(options, MaxEnumMembersOptionKey, DefaultMaxEnumMembers, minInclusive: 1);
        }
    }
}
