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
    /// Do not use the RequiredAttribute for a non-nullable DateTime property
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA008Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA008";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA008AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA008AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA008AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Bug;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: _description);

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

            // Check if the property type is DateTime
            if (IsDateTimeType(propertyDeclaration, context.SemanticModel))
            {
                // Check if the property is not nullable
                if (IsNonNullable(propertyDeclaration))
                {
                    // Check if the property is  decorated with RequiredAttribute
                    if (IsDecoratedWithRequiredAttribute(propertyDeclaration, context.SemanticModel))
                    {
                        var diagnostic = Diagnostic.Create(_rule, propertyDeclaration.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsDateTimeType(PropertyDeclarationSyntax propertyDeclaration, SemanticModel semanticModel)
        {
            var typeInfo = semanticModel.GetTypeInfo(propertyDeclaration.Type);
            if (typeInfo.Type == null)
                return false;
            
            var dtT = semanticModel.Compilation.GetTypeByMetadataName("System.DateTime");
            if (dtT == null)
                return false;
            
            return SymbolEqualityComparer.Default.Equals(typeInfo.Type, dtT);
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