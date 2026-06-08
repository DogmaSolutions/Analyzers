using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace DogmaSolutions.Analyzers;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA018CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA018CodeFixProvider : CodeFixProvider
{
   public override ImmutableArray<string> FixableDiagnosticIds => [DSA018Analyzer.DiagnosticId];

   public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

   public override async Task RegisterCodeFixesAsync(CodeFixContext context)
   {
      var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
      if (root == null)
         return;

      var diagnostic = context.Diagnostics[0];
      var node = root.FindNode(diagnostic.Location.SourceSpan);

      ReviewCommentCodeFix.Register(context, diagnostic, node, DSA018Analyzer.DiagnosticId, nameof(Resources.DSA018ReviewComment));
   }
}
