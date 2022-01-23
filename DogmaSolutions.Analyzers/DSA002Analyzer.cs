using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// HTTP REST API methods should not directly use Entity Framework DbContext.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DSA002Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA002";
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.DSA002AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString MessageFormat =
            new LocalizableResourceString(nameof(Resources.DSA002AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString Description =
            new LocalizableResourceString(nameof(Resources.DSA002AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

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

            var severity = Rule.DefaultSeverity;
            var config = ctx.Options.AnalyzerConfigOptionsProvider.GetOptions(ctx.Node.SyntaxTree);
            if (config.TryGetValue($"dotnet_diagnostic.{DiagnosticId}.severity", out var configValue) &&
                !string.IsNullOrWhiteSpace(configValue) &&
                Enum.TryParse<DiagnosticSeverity>(configValue, out var configuredSeverity))
            {
                severity = configuredSeverity;
            }

            var reportedDiagnostics = new List<Diagnostic>();

            void ProcessIdentifier(
                IEnumerable<IdentifierNameSyntax> identifiers,
                MemberAccessExpressionSyntax maes,
                ClassDeclarationSyntax classDeclarationSyntax,
                MethodDeclarationSyntax methodDeclarationSyntax)
            {
                bool ProcessAnchestors(IEnumerable<MemberAccessExpressionSyntax> anchestors, IdentifierNameSyntax identifier, string methodName)
                {
                    var anchestor = anchestors.FirstOrDefault(m => m.Name?.Identifier.ValueText == methodName);
                    if (anchestor != null)
                    {
                        var diagnostic = Diagnostic.Create(
                            descriptor: Rule,
                            location: maes.GetLocation(),
                            effectiveSeverity: severity,
                            additionalLocations: null,
                            properties: null,
                            classDeclarationSyntax.Identifier.Text + "." + methodDeclarationSyntax.Identifier.Text,
                            methodName,
                            identifier.Identifier.ValueText);

                        if (reportedDiagnostics.All(ddd => ddd.GetMessage()?.ToString() != diagnostic.GetMessage()?.ToString()))
                        {
                            reportedDiagnostics.Add(diagnostic);
                            ctx.ReportDiagnostic(diagnostic);
                        }

                        return true;
                    }

                    return false;
                }

                foreach (var identifier in identifiers)
                {
                    if (identifier.IsEfDbSet(ctx))
                    {
                        var anchestors = identifier.Ancestors().OfType<MemberAccessExpressionSyntax>().ToArray();
                        ProcessAnchestors(anchestors, identifier, "Where");
                        ProcessAnchestors(anchestors, identifier, "Select");
                        ProcessAnchestors(anchestors, identifier, "OrderBy");
                        ProcessAnchestors(anchestors, identifier, "GroupBy");
                    }
                }
            }

            foreach (var maes in method.DescendantNodes().OfType<MemberAccessExpressionSyntax>())
            {
                var identifiers = maes.DescendantNodes().OfType<IdentifierNameSyntax>();
                ProcessIdentifier(identifiers, maes, parentClass, method);
            }
        }
    }
}