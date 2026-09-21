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
    /// A <see cref="System.Random"/> constructed with a seed that is either a time-based value
    /// (e.g. <c>Environment.TickCount</c>, <c>DateTime.UtcNow.Ticks</c>) or a compile-time constant.
    /// A time-based seed is redundant with the parameterless constructor and, because it is derived
    /// from a low-resolution clock, several instances created within the same tick share a seed and
    /// therefore emit the identical sequence. A constant seed produces a fully deterministic sequence.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA038Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA038";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA038AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA038AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA038AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Bug;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA038.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(
                AnalyzeObjectCreation,
                SyntaxKind.ObjectCreationExpression,
                SyntaxKind.ImplicitObjectCreationExpression);
        }

        private static void AnalyzeObjectCreation(SyntaxNodeAnalysisContext context)
        {
            var creation = (BaseObjectCreationExpressionSyntax)context.Node;

            // Random(int Seed) takes exactly one argument; the parameterless ctor (the recommended form) is ignored.
            var arguments = creation.ArgumentList?.Arguments;
            if (arguments is not { Count: 1 })
                return;

            var compilation = context.SemanticModel.Compilation;

            // [fail-open] If System.Random is absent from this compilation, the covered shape cannot occur.
            var randomType = compilation.GetTypeByMetadataName("System.Random");
            if (randomType == null)
                return;

            var createdType = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken).Type;
            if (!SymbolEqualityComparer.Default.Equals(createdType, randomType))
                return;

            var seed = SyntaxUtils.UnwrapParenthesesCastsAndChecked(arguments.Value[0].Expression);

            // Order matters: a time-based value is a property/method call (never a compile-time constant),
            // so classify it first, then fall back to the constant check.
            string? seedKind = null;
            if (IsTimeBasedSeed(seed, context))
                seedKind = "a time-based value, which is redundant with the parameterless 'new Random()' and makes instances created within the same clock tick share a sequence";
            else if (context.SemanticModel.GetConstantValue(seed, context.CancellationToken).HasValue)
                seedKind = "a compile-time constant, which produces a deterministic, predictable sequence";

            if (seedKind == null)
                return;

            var diagnostic = Diagnostic.Create(
                _rule,
                creation.GetLocation(),
                effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                additionalLocations: null,
                properties: null,
                messageArgs: seedKind);

            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsTimeBasedSeed(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            var symbol = context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol;
            if (symbol?.ContainingType is not { } containingType)
                return false;

            var compilation = context.SemanticModel.Compilation;

            bool DeclaredOn(string metadataName) =>
                SymbolEqualityComparer.Default.Equals(containingType, compilation.GetTypeByMetadataName(metadataName));

            // Environment.TickCount / Environment.TickCount64
            if (DeclaredOn("System.Environment") && symbol.Name is "TickCount" or "TickCount64")
                return true;

            // DateTime.*.Ticks / DateTimeOffset.*.Ticks (e.g. DateTime.Now.Ticks, DateTime.UtcNow.Ticks)
            if ((DeclaredOn("System.DateTime") || DeclaredOn("System.DateTimeOffset")) && symbol.Name == "Ticks")
                return true;

            // Stopwatch.GetTimestamp()
            if (DeclaredOn("System.Diagnostics.Stopwatch") && symbol.Name == "GetTimestamp")
                return true;

            return false;
        }
    }
}
