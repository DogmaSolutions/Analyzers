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
    /// Potential non-deterministic point-in-time execution
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA006Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA006";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA006AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA006AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA006AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Design;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: _description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        private static readonly Type[] _exceptionTypes =
        [
            typeof(Exception),
            typeof(SystemException),
            typeof(ApplicationException),
            typeof(IndexOutOfRangeException),
            typeof(NullReferenceException),
            typeof(OutOfMemoryException),
            typeof(ExecutionEngineException)
        ];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeThrowStatement, SyntaxKind.ThrowStatement);
        }

        private static void AnalyzeThrowStatement(SyntaxNodeAnalysisContext context)
        {
            var throwStatement = (ThrowStatementSyntax)context.Node;

            // Check if the thrown expression is an object creation expression
            if (throwStatement.Expression is not ObjectCreationExpressionSyntax objectCreationExpression)
                return;

            // Check if the type being created is System.Exception or NullReferenceException
            if (IsProhibitedExceptionType(objectCreationExpression, context.SemanticModel))
            {
                var diagnostic = Diagnostic.Create(_rule, throwStatement.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }


        private static bool IsProhibitedExceptionType(ObjectCreationExpressionSyntax objectCreationExpressionSyntax, SemanticModel semanticModel)
        {
            var typeSyntax = objectCreationExpressionSyntax.Type;
            var typeInfo = semanticModel.GetTypeInfo(typeSyntax);
            var symbol = typeInfo.Type;
            if (symbol != null)
            {
                foreach (var exceptionType in _exceptionTypes)
                {
                    var excType = semanticModel.Compilation.GetTypeByMetadataName(exceptionType.AssemblyQualifiedName);
                    if (symbol.Equals(excType, SymbolEqualityComparer.Default))
                        return true;
                }
            }
            else
            {
                var qualifiedNameSyntax = typeSyntax.ToString();
                foreach (var exceptionType in _exceptionTypes)
                {
                    if (qualifiedNameSyntax.Equals(exceptionType.FullName, StringComparison.Ordinal) ||
                        qualifiedNameSyntax.Equals(exceptionType.Name, StringComparison.Ordinal))
                        return true;
                }
            }

            return false;
        }
    }
}