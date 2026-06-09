using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(DSR002RefactoringProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSR002RefactoringProvider : CodeRefactoringProvider
{
   internal const string LocalConstEquivalenceKey = "DSR002.LocalConst";
   internal const string ClassFieldEquivalenceKey = "DSR002.ClassField";
   internal const string UseReplacementEquivalenceKey = "DSR002.UseReplacement";

   public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
   {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
      if (root == null)
         return;

      var literal = root.FindToken(context.Span.Start).Parent?
         .AncestorsAndSelf()
         .OfType<LiteralExpressionSyntax>()
         .FirstOrDefault(l => l.IsKind(SyntaxKind.StringLiteralExpression));
      if (literal == null)
         return;

      var stringValue = literal.Token.ValueText;
      if (string.IsNullOrEmpty(stringValue))
         return;

      var constName = DSA032Analyzer.GenerateConstantName(stringValue);

      var displayValue = stringValue;
      if (displayValue.Length > 40)
         displayValue = displayValue.Substring(0, 37) + "...";

      var methodBody = StringConstantHelper.FindContainingMethodBody(literal);
      if (methodBody != null)
      {
         context.RegisterRefactoring(
            CodeAction.Create(
               title: $"Extract \"{displayValue}\" to local constant '{constName}'",
               createChangedDocument: ct => StringConstantHelper.ExtractToLocalConstAsync(context.Document, literal, stringValue, ct),
               equivalenceKey: LocalConstEquivalenceKey));

         context.RegisterRefactoring(
            CodeAction.Create(
               title: $"Extract \"{displayValue}\" to class field constant '{constName}'",
               createChangedDocument: ct => StringConstantHelper.ExtractToClassFieldAsync(context.Document, literal, stringValue, ct),
               equivalenceKey: ClassFieldEquivalenceKey));
      }
      else
      {
         var containingType = literal.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
         if (containingType != null)
         {
            context.RegisterRefactoring(
               CodeAction.Create(
                  title: $"Extract \"{displayValue}\" to class field constant '{constName}'",
                  createChangedDocument: ct => StringConstantHelper.ExtractSingleToClassFieldAsync(context.Document, literal, stringValue, ct),
                  equivalenceKey: ClassFieldEquivalenceKey));
         }
      }

      var replacements = await StringConstantHelper.LoadReplacementsAsync(context.Document.Project, stringValue, context.CancellationToken).ConfigureAwait(false);
      foreach (var replacement in replacements)
      {
         var displayReplacement = replacement;
         if (displayReplacement.Length > 60)
            displayReplacement = displayReplacement.Substring(0, 57) + "...";

         context.RegisterRefactoring(
            CodeAction.Create(
               title: $"Replace \"{displayValue}\" with '{displayReplacement}'",
               createChangedDocument: ct => StringConstantHelper.ReplaceWithKnownConstantAsync(context.Document, literal, stringValue, replacement, ct),
               equivalenceKey: UseReplacementEquivalenceKey + ":" + replacement));
      }
   }
}
