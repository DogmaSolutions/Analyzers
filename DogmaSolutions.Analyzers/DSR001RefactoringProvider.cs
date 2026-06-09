using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

[ExportCodeRefactoringProvider(LanguageNames.CSharp, Name = nameof(DSR001RefactoringProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSR001RefactoringProvider : CodeRefactoringProvider
{
   internal const string TagWithCallSiteEquivalenceKey = "DSR001.TagWithCallSite";
   internal const string TagWithEquivalenceKey = "DSR001.TagWith";

   public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
   {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
      if (root == null)
         return;

      var node = root.FindNode(context.Span);
      var invocation = node.AncestorsAndSelf()
         .OfType<InvocationExpressionSyntax>()
         .FirstOrDefault();
      if (invocation == null)
         return;

      if (!DSA021Analyzer.TryGetTerminalMethodName(invocation, out _))
         return;

      var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
      if (semanticModel == null)
         return;

      if (!DSA021Analyzer.IsEntityFrameworkChain(invocation, semanticModel))
         return;

      if (DSA021Analyzer.IsInsideSubquery(invocation, semanticModel))
         return;

      if (DSA021Analyzer.HasTagInChain(invocation, semanticModel, semanticModel.Compilation))
         return;

      context.RegisterRefactoring(
         CodeAction.Create(
            title: "Insert `.TagWithCallSite()` before terminal method",
            createChangedDocument: ct => EfQueryTagHelper.AddTagToQueryAsync(context.Document, invocation, useCallSite: true, ct),
            equivalenceKey: TagWithCallSiteEquivalenceKey));

      context.RegisterRefactoring(
         CodeAction.Create(
            title: "Insert `.TagWith(\"...\")` before terminal method",
            createChangedDocument: ct => EfQueryTagHelper.AddTagToQueryAsync(context.Document, invocation, useCallSite: false, ct),
            equivalenceKey: TagWithEquivalenceKey));
   }
}
