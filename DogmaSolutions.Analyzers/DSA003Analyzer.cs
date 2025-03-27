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
    /// Don't use <see cref="string.IsNullOrEmpty"/>. Use <see cref="string.IsNullOrWhiteSpace"/> instead
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA003Analyzer : ProhibitInvocationExpressionDiagnosticAnalyzer<string>
    {
        public const string DiagnosticId = "DSA003";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA003AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA003AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA003AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA003");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];
        
        protected override string MemberName => nameof(string.IsNullOrEmpty);

        public override void Initialize([NotNull] AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx => OnInvocationExpression(ctx, _rule), SyntaxKind.InvocationExpression);
        }

        protected override bool IsMatched([NotNull] InvocationExpressionSyntax invocationExpression, [NotNull] DiagnosticDescriptor rule)
        {
            if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess) // match "xxx.yyy"
            {
                if (memberAccess.Name.Identifier.ValueText != nameof(string.IsNullOrEmpty)) // match "xxx.IsNullOrEmpty"
                    return false;

                if (memberAccess.Expression is IdentifierNameSyntax { Identifier.ValueText: "String" } || // String.IsNullOrEmpty()
                    memberAccess.Expression is PredefinedTypeSyntax { Keyword.ValueText: "string" } || // string.IsNullOrEmpty()
                    (memberAccess.Expression is MemberAccessExpressionSyntax && memberAccess.Expression.ToString() == "System.String") // string.IsNullOrEmpty()
                   )
                {
                    return true;
                }
            }
            else if (invocationExpression.Expression is IdentifierNameSyntax
                     {
                         Identifier.ValueText: "IsNullOrEmpty"
                     }) // Maybe "String" has been imported using a "using static"
            {
                return invocationExpression.Ancestors().
                           OfType<CompilationUnitSyntax>().
                           FirstOrDefault()?. // navigate "up" and find the root
                           DescendantNodes().
                           OfType<UsingDirectiveSyntax>(). // navigate "down" and search the "using" directives
                           Any(
                               u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && // consider only "using static" directives
                                    (u.NamespaceOrType.ToString() == "String" ||
                                     u.NamespaceOrType.ToString() == "System.String" ||
                                     u.NamespaceOrType.ToString() == "global::System.String") // consider only "using static xyz"
                           ) ==
                       true;
            }

            return false;
        }
    }
}