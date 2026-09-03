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
    /// A <c>[ThreadStatic]</c> field of type <see cref="System.Random"/> must not carry a field initializer.
    /// A field initializer on a thread-static field runs only for the thread that first triggers the type's
    /// static construction; every other thread observes the default value (<c>null</c>) and throws a
    /// <see cref="System.NullReferenceException"/> at first use.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA037Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA037";

        private static readonly LocalizableString _title =
            new LocalizableResourceString(nameof(Resources.DSA037AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA037AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA037AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.Bug;

        private static readonly DiagnosticDescriptor _rule = new(
            DiagnosticId,
            _title,
            _messageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: _description,
            helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA037.md");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

        public override void Initialize(AnalysisContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);
        }

        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            var fieldDeclaration = (FieldDeclarationSyntax)context.Node;

            // Cheap syntactic prefilter: a [ThreadStatic] field necessarily carries at least one attribute.
            if (fieldDeclaration.AttributeLists.Count == 0)
                return;

            var compilation = context.SemanticModel.Compilation;

            // [fail-open] If the well-known types are absent from this compilation, the covered shape cannot occur.
            var randomType = compilation.GetTypeByMetadataName("System.Random");
            if (randomType == null)
                return;

            var threadStaticType = compilation.GetTypeByMetadataName("System.ThreadStaticAttribute");
            if (threadStaticType == null)
                return;

            foreach (var variable in fieldDeclaration.Declaration.Variables)
            {
                // Only an INITIALIZED declarator carries the bug; an uninitialized one is the correct shape.
                if (variable.Initializer == null)
                    continue;

                if (context.SemanticModel.GetDeclaredSymbol(variable, context.CancellationToken) is not IFieldSymbol fieldSymbol)
                    continue;

                // Scope to the Random theme: the field type must be exactly System.Random.
                if (!SymbolEqualityComparer.Default.Equals(fieldSymbol.Type, randomType))
                    continue;

                // Resolve [ThreadStatic] by SYMBOL, not by attribute text, so `[ThreadStatic]`,
                // `[ThreadStaticAttribute]` and `[System.ThreadStatic]` all match and a user-defined
                // same-named attribute does not.
                var isThreadStatic = fieldSymbol.GetAttributes()
                    .Any(attribute => SymbolEqualityComparer.Default.Equals(attribute.AttributeClass, threadStaticType));
                if (!isThreadStatic)
                    continue;

                // Anchor the diagnostic at the initializer (the '= ...' clause) — that is the mistake to remove.
                var diagnostic = Diagnostic.Create(
                    _rule,
                    variable.Initializer.GetLocation(),
                    effectiveSeverity: context.GetDiagnosticSeverity(_rule),
                    additionalLocations: null,
                    properties: null,
                    messageArgs: fieldSymbol.Name);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
