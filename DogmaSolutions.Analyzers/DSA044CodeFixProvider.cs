using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Replaces the console or debug write with a call of the available logger when it is a Microsoft.Extensions.Logging one
/// (<c>logger.LogInformation</c>, <c>LogError</c> for <c>Console.Error</c>, <c>LogDebug</c> for <c>Debug</c>), and always offers the
/// review comment. The other logging libraries have too many different APIs for a safe mechanical fix.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA044CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA044CodeFixProvider : CodeFixProvider
{
    internal const string ReplaceEquivalenceKey = DSA044Analyzer.DiagnosticId + "_ReplaceWithLogger";
    private const string LoggingNamespace = "Microsoft.Extensions.Logging";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA044Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];

        if (root?.FindNode(diagnostic.Location.SourceSpan) is InvocationExpressionSyntax invocation)
            RegisterFixes(context, diagnostic, invocation);
    }

    private static void RegisterFixes(CodeFixContext context, Diagnostic diagnostic, InvocationExpressionSyntax invocation)
    {
        if (diagnostic.Properties[DSA044Analyzer.IsMicrosoftLoggerProperty] == "true" && diagnostic.Properties[DSA044Analyzer.HasCategoryProperty] == "false" && DSA044LogCallBuilder.Build(invocation, diagnostic.Properties[DSA044Analyzer.LoggerNameProperty], diagnostic.Properties[DSA044Analyzer.LevelProperty]) != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Replace with a call of the logger",
                    createChangedDocument: ct => ReplaceAsync(context.Document, invocation, diagnostic, ct),
                    equivalenceKey: ReplaceEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, invocation, DSA044Analyzer.DiagnosticId, nameof(Resources.DSA044ReviewComment));
    }

    private static async Task<Document> ReplaceAsync(Document document, InvocationExpressionSyntax invocation, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        var replacement = DSA044LogCallBuilder.Build(invocation, diagnostic.Properties[DSA044Analyzer.LoggerNameProperty], diagnostic.Properties[DSA044Analyzer.LevelProperty]);

        var newRoot = root.ReplaceNode(invocation, replacement.WithAdditionalAnnotations(Formatter.Annotation));
        return document.WithSyntaxRoot(AddLoggingUsing((CompilationUnitSyntax)newRoot));
    }

    /// <summary>The <c>LogInformation</c>, <c>LogError</c>... methods are extension methods of the Microsoft.Extensions.Logging namespace.</summary>
    private static CompilationUnitSyntax AddLoggingUsing(CompilationUnitSyntax root)
    {
        if (root.DescendantNodes().OfType<UsingDirectiveSyntax>().Any(directive => directive.Name?.ToString() == LoggingNamespace))
            return root;

        var directive = SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(LoggingNamespace)).WithTrailingTrivia(SyntaxUtils.GetEndOfLineTrivia(root));
        return root.AddUsings(directive);
    }
}
