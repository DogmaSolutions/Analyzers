using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Do not use the RequiredAttribute for a non-nullable value type property
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA029Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA029";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA029AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA029AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA029AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Bug;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA029.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
        {
            var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

            // Check if the property type is a non-nullable value type not already covered by DSA008/DSA009
            if (IsApplicableValueType(propertyDeclaration, context.SemanticModel))
            {
                // Check if the property is not nullable
                if (IsNonNullable(propertyDeclaration))
                {
                    // Check if the property is  decorated with RequiredAttribute
                    if (IsDecoratedWithRequiredAttribute(propertyDeclaration, context.SemanticModel))
                    {
                        var diagnostic = Diagnostic.Create(
                            _rule,
                            propertyDeclaration.GetLocation(),
                            effectiveSeverity:  context.GetDiagnosticSeverity(_rule),
                            additionalLocations: null,
                            properties: null);

                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsApplicableValueType(PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(propertyDeclaration.Type);
            if (typeInfo.Type == null)
                return false;

            if (!typeInfo.Type.IsValueType)
                return false;

            // Exclude Nullable<T> (covers both T? and Nullable<T> syntax)
            if (typeInfo.Type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                return false;

            // Exclude DateTime and DateTimeOffset, already covered by DSA008 and DSA009
            var dateTimeType = semanticModel.Compilation.GetTypeByMetadataName("System.DateTime");
            if (dateTimeType != null && SymbolEqualityComparer.Default.Equals(typeInfo.Type, dateTimeType))
                return false;

            var dateTimeOffsetType = semanticModel.Compilation.GetTypeByMetadataName("System.DateTimeOffset");
            if (dateTimeOffsetType != null && SymbolEqualityComparer.Default.Equals(typeInfo.Type, dateTimeOffsetType))
                return false;

            return true;
        }

        private static bool IsNonNullable(PropertyDeclarationSyntax propertyDeclaration)
        {
            return !propertyDeclaration.Type.IsNotNull;
        }

        private static bool IsDecoratedWithRequiredAttribute(PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel)
        {
            var attributes = semanticModel.GetDeclaredSymbol(propertyDeclaration)?.GetAttributes();
            return attributes?.Any(a => a.AttributeClass?.Name == "RequiredAttribute") ?? false;
        }
    }
}
