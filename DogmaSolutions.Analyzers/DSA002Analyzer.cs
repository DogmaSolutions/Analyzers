using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// HTTP REST API methods should not directly use Entity Framework DbContext DbSet through a LINQ fluent query
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA002Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA002";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA002AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA002AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA002AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Design;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA002");

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
                            descriptor: _rule,
                            location: maes.GetLocation(),
                            effectiveSeverity:  ctx.GetDiagnosticSeverity(_rule),
                            additionalLocations: null,
                            properties: null,
                            classDeclarationSyntax.Identifier.Text + "." + methodDeclarationSyntax.Identifier.Text,
                            methodName,
                            identifier.Identifier.ValueText);

                        if (reportedDiagnostics.All(ddd => ddd.GetMessage(CultureInfo.CurrentCulture) != diagnostic.GetMessage(CultureInfo.CurrentCulture)))
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