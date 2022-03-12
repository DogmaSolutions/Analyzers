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
    /// HTTP REST API methods should not directly use Entity Framework DbContext through a LINQ query expression
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DSA001Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA001";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.DSA001AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.DSA001AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.DSA001AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Naming;

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning,
            isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
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

            var config = ctx.Options.AnalyzerConfigOptionsProvider.GetOptions(ctx.Node.SyntaxTree);
            var severity = Rule.DefaultSeverity;
            if (config.TryGetValue($"dotnet_diagnostic.{DiagnosticId}.severity", out var configValue) &&
                !string.IsNullOrWhiteSpace(configValue) &&
                Enum.TryParse<DiagnosticSeverity>(configValue, out var configuredSeverity))
            {
                severity = configuredSeverity;
            }


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
                                descriptor: Rule,
                                location: qes.GetLocation(),
                                effectiveSeverity: severity,
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