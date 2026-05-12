using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA025CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA025CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA025Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        var interpolated = root.FindToken(diagnosticSpan.Start).Parent?
            .AncestorsAndSelf()
            .OfType<InterpolatedStringExpressionSyntax>()
            .FirstOrDefault(i => i.Span == diagnosticSpan);

        if (interpolated == null)
            return;

        context.RegisterCodeFix(
            CodeAction.Create(
                title: "Convert to structured logging template",
                createChangedDocument: ct => ConvertToStructuredLoggingAsync(context.Document, interpolated, ct),
                equivalenceKey: DSA025Analyzer.DiagnosticId),
            diagnostic);
    }

    private static async Task<Document> ConvertToStructuredLoggingAsync(
        Document document,
        InterpolatedStringExpressionSyntax interpolated,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var argument = interpolated.Parent as ArgumentSyntax;
        var argumentList = argument?.Parent as ArgumentListSyntax;
        var invocation = argumentList?.Parent as InvocationExpressionSyntax;
        if (invocation == null)
            return document;

        var templateBuilder = new StringBuilder();
        var extractedArgs = new List<ExpressionSyntax>();
        var usedNames = new Dictionary<string, int>();

        var isVerbatim = interpolated.StringStartToken.Text.Contains("@");

        templateBuilder.Append('"');

        foreach (var content in interpolated.Contents)
        {
            if (content is InterpolatedStringTextSyntax text)
            {
                var textValue = text.TextToken.Text;
                if (isVerbatim)
                    textValue = textValue.Replace("\\", "\\\\").Replace("\"\"", "\\\"");
                templateBuilder.Append(textValue);
            }
            else if (content is InterpolationSyntax interpolation)
            {
                var placeholderName = GeneratePlaceholderName(interpolation.Expression);
                placeholderName = DeduplicateName(placeholderName, usedNames);

                templateBuilder.Append('{');
                templateBuilder.Append(placeholderName);

                if (interpolation.FormatClause != null)
                {
                    templateBuilder.Append(':');
                    templateBuilder.Append(interpolation.FormatClause.FormatStringToken.Text);
                }

                templateBuilder.Append('}');

                extractedArgs.Add(interpolation.Expression.WithoutTrivia());
            }
        }

        templateBuilder.Append('"');

        var templateLiteral = SyntaxFactory.ParseExpression(templateBuilder.ToString());

        var argIndex = argumentList.Arguments.IndexOf(argument);
        var newArguments = new List<ArgumentSyntax>();

        for (var i = 0; i < argumentList.Arguments.Count; i++)
        {
            if (i == argIndex)
            {
                var nameColon = argument.NameColon;
                newArguments.Add(SyntaxFactory.Argument(nameColon, default, templateLiteral)
                    .WithLeadingTrivia(argument.GetLeadingTrivia())
                    .WithTrailingTrivia(argument.GetTrailingTrivia()));
            }
            else
            {
                newArguments.Add(argumentList.Arguments[i]);
            }
        }

        foreach (var extractedArg in extractedArgs)
            newArguments.Add(SyntaxFactory.Argument(extractedArg));

        var newArgumentList = argumentList
            .WithArguments(SyntaxFactory.SeparatedList(newArguments));

        var newRoot = root.ReplaceNode(argumentList, newArgumentList);
        return document.WithSyntaxRoot(newRoot);
    }

    internal static string GeneratePlaceholderName(ExpressionSyntax expression)
    {
        var chunks = new List<string>();

        foreach (var token in expression.DescendantTokens())
        {
            if (token.IsKind(SyntaxKind.IdentifierToken) ||
                token.IsKind(SyntaxKind.NumericLiteralToken))
            {
                chunks.Add(token.ValueText);
            }
            else if (token.IsKind(SyntaxKind.StringLiteralToken))
            {
                var text = token.ValueText;
                if (!string.IsNullOrWhiteSpace(text))
                    chunks.Add(text);
            }
            else if (SyntaxFacts.IsPredefinedType(token.Kind()))
            {
                chunks.Add(token.ValueText);
            }
        }

        if (chunks.Count == 0)
            return "Param";

        var sb = new StringBuilder();
        foreach (var chunk in chunks)
        {
            if (chunk.Length == 0)
                continue;
            sb.Append(char.ToUpperInvariant(chunk[0]));
            if (chunk.Length > 1)
                sb.Append(chunk.Substring(1));
        }

        if (sb.Length == 0)
            return "Param";

        return sb.ToString();
    }

    private static string DeduplicateName(string name, Dictionary<string, int> usedNames)
    {
        if (!usedNames.TryGetValue(name, out var count))
        {
            usedNames[name] = 1;
            return name;
        }

        usedNames[name] = count + 1;
        return name + (count + 1);
    }
}
