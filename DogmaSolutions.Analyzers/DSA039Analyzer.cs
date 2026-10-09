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
    /// Reducing the output of <see cref="System.Security.Cryptography.RandomNumberGenerator"/> into a range
    /// with the remainder operator (<c>%</c>) introduces modulo bias: unless the range divides the operand
    /// domain evenly, the lower residues occur more often, skewing a value that was meant to be uniform and
    /// defeating the point of using a cryptographic generator. Use
    /// <c>RandomNumberGenerator.GetInt32(fromInclusive, toExclusive)</c> instead.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA039Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA039";

        private const string RandomNumberGeneratorMetadataName = "System.Security.Cryptography.RandomNumberGenerator";
        private const string BitConverterMetadataName = "System.BitConverter";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA039AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA039AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA039AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Security;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA039.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeModulo, SyntaxKind.ModuloExpression);
        }

        private static void AnalyzeModulo(SyntaxNodeAnalysisContext context)
        {
            var modulo = (BinaryExpressionSyntax)context.Node;

            // Only the integral modulo-bias case is targeted; a floating-point remainder is not this defect.
            var leftType = context.SemanticModel.GetTypeInfo(modulo.Left, context.CancellationToken).Type;
            if (leftType == null || !IsIntegral(leftType))
                return;

            // [CySec] Detects CWE-1241 "Use of Predictable Algorithm in Random Number Generator" / modulo bias
            // that weakens a value produced by a CSPRNG (CWE-338 "Use of Cryptographically Weak PRNG" family).
            // [RiskMitigation RA-01] Flags biased reduction of RandomNumberGenerator output.
            if (IsRandomNumberGeneratorDerivedInteger(modulo.Left, context))
            {
                var diagnostic = Diagnostic.Create(
                    _rule,
                    modulo.GetLocation(),
                    effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                    additionalLocations: null,
                    properties: null);

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static bool IsIntegral(ITypeSymbol type) =>
            type.SpecialType is SpecialType.System_Byte or SpecialType.System_SByte
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64;

        /// <summary>
        /// True when <paramref name="expression"/> yields an integer whose value comes from RNG bytes:
        /// <c>BitConverter.ToIntXX(&lt;rngBytes&gt;, ...)</c>, an element access on <c>&lt;rngBytes&gt;</c>,
        /// or a local integer initialized (one hop) from either of those.
        /// </summary>
        private static bool IsRandomNumberGeneratorDerivedInteger(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            expression = SyntaxUtils.UnwrapParenthesesCastsAndChecked(expression);

            switch (expression)
            {
                case InvocationExpressionSyntax invocation
                    when context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method
                         && IsDeclaredOn(method.ContainingType, BitConverterMetadataName, context)
                         && method.Name.StartsWith("To", StringComparison.Ordinal)
                         && invocation.ArgumentList.Arguments.Count >= 1:
                    return IsRandomNumberGeneratorByteSource(invocation.ArgumentList.Arguments[0].Expression, context);

                case ElementAccessExpressionSyntax elementAccess:
                    return IsRandomNumberGeneratorByteSource(elementAccess.Expression, context);

                case IdentifierNameSyntax when context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol is ILocalSymbol local:
                    return LocalInitializerMatches(local, initializer => IsRandomNumberGeneratorDerivedInteger(initializer, context));

                default:
                    return false;
            }
        }

        /// <summary>
        /// True when <paramref name="expression"/> is a <c>byte[]</c> produced or filled by a RandomNumberGenerator:
        /// a <c>GetBytes(...)</c> call, or a local buffer initialized from <c>GetBytes(...)</c> or passed to
        /// <c>Fill(...)</c> / an instance <c>GetBytes(buffer)</c> within the same method (one hop).
        /// </summary>
        private static bool IsRandomNumberGeneratorByteSource(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            expression = SyntaxUtils.UnwrapParenthesesCastsAndChecked(expression);

            if (expression is InvocationExpressionSyntax invocation
                && context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method
                && IsRandomNumberGeneratorType(method.ContainingType, context)
                && method.Name is "GetBytes" or "GetNonZeroBytes")
            {
                return true;
            }

            if (expression is IdentifierNameSyntax
                && context.SemanticModel.GetSymbolInfo(expression, context.CancellationToken).Symbol is ILocalSymbol local)
            {
                if (LocalInitializerMatches(local, initializer => IsRandomNumberGeneratorByteSource(initializer, context)))
                    return true;

                return BufferIsFilledByRandomNumberGenerator(local, expression, context);
            }

            return false;
        }

        /// <summary>Scans the enclosing member for a RandomNumberGenerator call that fills <paramref name="local"/>.</summary>
        private static bool BufferIsFilledByRandomNumberGenerator(ILocalSymbol local, SyntaxNode origin, SyntaxNodeAnalysisContext context)
        {
            var scope = origin.Ancestors().FirstOrDefault(a =>
                            a is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax)
                        ?? origin.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (scope == null)
                return false;

            return scope.DescendantNodes()
                .OfType<InvocationExpressionSyntax>()
                .Any(invocation => IsRandomFillOf(invocation, local, context));
        }

        /// <summary>RandomNumberGenerator.Fill / GetBytes / GetNonZeroBytes called with <paramref name="local"/> as an argument.</summary>
        private static bool IsRandomFillOf(InvocationExpressionSyntax invocation, ILocalSymbol local, SyntaxNodeAnalysisContext context)
        {
            return context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol is IMethodSymbol method &&
                   IsRandomNumberGeneratorType(method.ContainingType, context) &&
                   (method.Name is "Fill" or "GetBytes" or "GetNonZeroBytes") &&
                   invocation.ArgumentList.Arguments.Any(argument =>
                       SymbolEqualityComparer.Default.Equals(
                           context.SemanticModel.GetSymbolInfo(argument.Expression, context.CancellationToken).Symbol,
                           local));
        }

        private static bool LocalInitializerMatches(ILocalSymbol local, Func<ExpressionSyntax, bool> predicate)
        {
            foreach (var reference in local.DeclaringSyntaxReferences)
            {
                if (reference.GetSyntax() is VariableDeclaratorSyntax { Initializer.Value: { } value } && predicate(value))
                    return true;
            }

            return false;
        }

        private static bool IsRandomNumberGeneratorType(ITypeSymbol? type, SyntaxNodeAnalysisContext context)
        {
            var rngType = context.SemanticModel.Compilation.GetTypeByMetadataName(RandomNumberGeneratorMetadataName);
            if (rngType == null)
                return false;

            for (var current = type; current != null; current = current.BaseType)
            {
                if (SymbolEqualityComparer.Default.Equals(current, rngType))
                    return true;
            }

            return false;
        }

        private static bool IsDeclaredOn(ITypeSymbol? type, string metadataName, SyntaxNodeAnalysisContext context) =>
            SymbolEqualityComparer.Default.Equals(type, context.SemanticModel.Compilation.GetTypeByMetadataName(metadataName));
    }
}
