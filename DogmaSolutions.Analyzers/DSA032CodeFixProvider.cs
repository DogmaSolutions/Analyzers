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

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA032CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA032CodeFixProvider : CodeFixProvider
{
   internal const string LocalConstEquivalenceKey = DSA032Analyzer.DiagnosticId + ".LocalConst";
   internal const string ClassFieldEquivalenceKey = DSA032Analyzer.DiagnosticId + ".ClassField";
   internal const string UseReplacementEquivalenceKey = DSA032Analyzer.DiagnosticId + ".UseReplacement";

   public override ImmutableArray<string> FixableDiagnosticIds => [DSA032Analyzer.DiagnosticId];

   public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

   public override async Task RegisterCodeFixesAsync(CodeFixContext context)
   {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
      if (root == null)
         return;

      var diagnostic = context.Diagnostics[0];
      var diagnosticSpan = diagnostic.Location.SourceSpan;

      var literal = root.FindToken(diagnosticSpan.Start).Parent?
         .AncestorsAndSelf()
         .OfType<LiteralExpressionSyntax>()
         .FirstOrDefault(l => l.IsKind(SyntaxKind.StringLiteralExpression) && l.Span == diagnosticSpan);
      if (literal == null)
         return;

      if (!diagnostic.Properties.TryGetValue(DSA032Analyzer.StringValueProperty, out var stringValue))
         return;

      var constName = DSA032Analyzer.GenerateConstantName(stringValue);

      var displayValue = stringValue;
      if (displayValue.Length > 40)
         displayValue = displayValue.Substring(0, 37) + "...";

      var replacements = await StringConstantHelper.LoadReplacementsAsync(context.Document.Project, stringValue, context.CancellationToken).ConfigureAwait(false);
      foreach (var replacement in replacements)
      {
         var displayReplacement = replacement;
         if (displayReplacement.Length > 60)
            displayReplacement = displayReplacement.Substring(0, 57) + "...";

         context.RegisterCodeFix(
            CodeAction.Create(
               title: $"Replace \"{displayValue}\" with '{displayReplacement}'",
               createChangedDocument: ct => StringConstantHelper.ReplaceWithKnownConstantAsync(context.Document, literal, stringValue, replacement, ct),
               equivalenceKey: UseReplacementEquivalenceKey + ":" + replacement),
            diagnostic);
      }

      context.RegisterCodeFix(
         CodeAction.Create(
            title: $"Extract \"{displayValue}\" to local constant '{constName}'",
            createChangedDocument: ct => StringConstantHelper.ExtractToLocalConstAsync(context.Document, literal, stringValue, ct),
            equivalenceKey: LocalConstEquivalenceKey),
         diagnostic);

      context.RegisterCodeFix(
         CodeAction.Create(
            title: $"Extract \"{displayValue}\" to class field constant '{constName}'",
            createChangedDocument: ct => StringConstantHelper.ExtractToClassFieldAsync(context.Document, literal, stringValue, ct),
            equivalenceKey: ClassFieldEquivalenceKey),
         diagnostic);

      ReviewCommentCodeFix.Register(context, diagnostic, literal, DSA032Analyzer.DiagnosticId, nameof(Resources.DSA032ReviewComment));
   }
}
