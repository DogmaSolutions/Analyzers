using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Don't use <see cref="string.IsNullOrEmpty"/>. Use <see cref="string.IsNullOrWhiteSpace"/> instead
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA003Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA003";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA003AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA003AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA003AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

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
            context.RegisterSyntaxNodeAction(OnMethod, SyntaxKind.InvocationExpression);
        }

        /*
                private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
                {
                    var invocationExpression = (InvocationExpressionSyntax)context.Node;

                    // Check if it's a call to String.IsNullOrEmpty
                    if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess)
                        if(memberAccess.Expression is TypeOfExpressionSyntax typeOfExpression )
                            if (((PredefinedTypeSyntax)typeOfExpression.Type).Keyword.Kind() == SyntaxKind.StringKeyword) || (((IdentifierNameSyntax)typeOfExpression.Type).Identifier.ValueText == "String")) &&
                    memberAccess.Name.ToString() == "IsNullOrEmpty")
                    {
                        var diagnostic = Diagnostic.Create(Rule, invocationExpression.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
                */


        private static void OnMethod(SyntaxNodeAnalysisContext ctx)
        {
            var invocationExpression = ctx.Node as InvocationExpressionSyntax;
            if (invocationExpression == null)
                return;

            var matched = false;
            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess) // match "xxx.yyy"
            {
                if (memberAccess.Name.Identifier.ValueText != nameof(string.IsNullOrEmpty)) // match "xxx.IsNullOrEmpty"
                    return;

                if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "String" } || // String.IsNullOrEmpty()
                    memberAccess.Expression is PredefinedTypeSyntax { Keyword.ValueText: "string" } || // string.IsNullOrEmpty()
                    (memberAccess.Expression is MemberAccessExpressionSyntax && memberAccess.Expression.ToString() == "System.String") // string.IsNullOrEmpty()
                   )
                {
                    matched = true;
                }
            }
            else if (invocationExpression.Expression is IdentifierNameSyntax
                     {
                         Identifier.ValueText: "IsNullOrEmpty"
                     }) // Maybe "String" has been imported using a "using static"
            {
                matched = invocationExpression.Ancestors().
                              OfType<CompilationUnitSyntax>().
                              FirstOrDefault()?. // navigate "up" and find the root
                              DescendantNodes().
                              OfType<UsingDirectiveSyntax>(). // navigate "down" and search the "using" directives
                              Any(
                                  u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && // consider only "using static" directives
                                       (u.NamespaceOrType.ToString() == "String" ||
                                        u.NamespaceOrType.ToString() == "System.String") // consider only "using static System.String"  or "using static String"
                              ) ==
                          true;
            }


            if (!matched)
                return;

            var severity = _rule.DefaultSeverity;
            var config = ctx.Options.AnalyzerConfigOptionsProvider.GetOptions(ctx.Node.SyntaxTree);
            if (config.TryGetValue($"dotnet_diagnostic.{DiagnosticId}.severity", out var configValue) &&
                !string.IsNullOrWhiteSpace(configValue) &&
                Enum.TryParse<DiagnosticSeverity>(configValue, out var configuredSeverity))
            {
                severity = configuredSeverity;
            }


            var diagnostic = Diagnostic.Create(
                descriptor: _rule,
                location: invocationExpression.GetLocation(),
                effectiveSeverity: severity,
                additionalLocations: null,
                properties: null);

            ctx.ReportDiagnostic(diagnostic);
        }
    }
}