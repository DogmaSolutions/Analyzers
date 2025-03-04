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
    /// HTTP REST API methods should not directly use Entity Framework DbContext through a LINQ query expression
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA001Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA001";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA001AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA001AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA001AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Design;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize([NotNull] AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.ParenthesizedLambdaExpression);
            context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.SimpleLambdaExpression);
            context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.AnonymousMethodExpression);
            context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.MethodDeclaration);
        }


        private static void OnMethod(SyntaxNodeAnalysisContext ctx)
        {
            var method = ctx.Node as MethodDeclarationSyntax;
            if (method == null)
                return;

            var parentClass = method.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (parentClass == null)
                return;

            if (!parentClass.IsWebApiControllerClass(ctx))
                return;

            foreach (var qes in method.DescendantNodes().OfType<QueryExpressionSyntax>())
            {
                var fromClauses = qes.DescendantNodes().OfType<FromClauseSyntax>();
                foreach (var fromClause in fromClauses)
                {
                    var identifiers = fromClause.DescendantNodes().OfType<IdentifierNameSyntax>();
                    foreach (var identifier in identifiers)
                    {
                        if (identifier.IsEfDbContext(ctx))
                        {
                            var diagnostic = Diagnostic.Create(
                                descriptor: _rule,
                                location: qes.GetLocation(),
                                effectiveSeverity:  ctx.GetDiagnosticSeverity(_rule),
                                additionalLocations: null,
                                properties: null,
                                messageArgs: parentClass.Identifier.Text + "." + method.Identifier.Text);
                            ctx.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}