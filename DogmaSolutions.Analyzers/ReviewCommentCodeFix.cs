using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class ReviewCommentCodeFix
{
   public const string EquivalenceKeySuffix = ".ReviewComment";

   public static void Register(
      CodeFixContext context,
      Diagnostic diagnostic,
      SyntaxNode node,
      string diagnosticId,
      string resourceKey)
   {
      context.RegisterCodeFix(
         CodeAction.Create(
            title: $"[{diagnosticId}] Add comment for code review",
            createChangedDocument: ct => InsertReviewCommentAsync(context.Document, node, diagnosticId, resourceKey, ct),
            equivalenceKey: diagnosticId + EquivalenceKeySuffix),
         diagnostic);
   }

   private static async Task<Document> InsertReviewCommentAsync(
      Document document,
      SyntaxNode node,
      string diagnosticId,
      string resourceKey,
      CancellationToken cancellationToken)
   {
      var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
      if (root == null)
         return document;

      var commentText = Resources.ResourceManager.GetString(resourceKey, Resources.Culture);
      if (string.IsNullOrEmpty(commentText))
         return document;

      commentText = SanitizeBlockComment(commentText);

      var anchor = FindAnchorNode(node);
      if (anchor == null)
         return document;

      var trivia = anchor.GetLeadingTrivia();

      var marker = "[" + diagnosticId + " /";
      foreach (var t in trivia)
      {
         if (t.IsKind(SyntaxKind.MultiLineCommentTrivia) && t.ToString().Contains(marker))
            return document;
      }

      var indentStr = DetectIndentation(trivia);
      var eol = DetectEndOfLine(anchor);
      var formattedComment = FormatComment(commentText, indentStr, eol);

      var newTriviaList = new List<SyntaxTrivia>();
      var insertionIndex = FindLastWhitespaceIndex(trivia);

      for (var i = 0; i < insertionIndex; i++)
         newTriviaList.Add(trivia[i]);

      newTriviaList.Add(SyntaxFactory.Whitespace(indentStr));
      newTriviaList.Add(SyntaxFactory.Comment(formattedComment));
      newTriviaList.Add(SyntaxFactory.EndOfLine(eol));

      for (var i = insertionIndex; i < trivia.Count; i++)
         newTriviaList.Add(trivia[i]);

      var newAnchor = anchor.WithLeadingTrivia(newTriviaList);
      var newRoot = root.ReplaceNode(anchor, newAnchor);
      return document.WithSyntaxRoot(newRoot);
   }

   private static SyntaxNode FindAnchorNode(SyntaxNode node)
   {
      foreach (var ancestor in node.AncestorsAndSelf())
      {
         if (ancestor is StatementSyntax)
            return ancestor;
         if (ancestor is MemberDeclarationSyntax)
            return ancestor;
         if (ancestor is BaseTypeDeclarationSyntax)
            return ancestor;
      }

      return node;
   }

   private static string DetectIndentation(SyntaxTriviaList trivia)
   {
      for (var i = trivia.Count - 1; i >= 0; i--)
      {
         if (trivia[i].IsKind(SyntaxKind.WhitespaceTrivia))
            return trivia[i].ToString();
      }

      return string.Empty;
   }

   private static string DetectEndOfLine(SyntaxNode anchor)
   {
      foreach (var t in anchor.GetLeadingTrivia())
      {
         if (t.IsKind(SyntaxKind.EndOfLineTrivia))
            return t.ToString();
      }

      var prevToken = anchor.GetFirstToken().GetPreviousToken();
      foreach (var t in prevToken.TrailingTrivia)
      {
         if (t.IsKind(SyntaxKind.EndOfLineTrivia))
            return t.ToString();
      }

      var root = anchor.SyntaxTree?.GetRoot();
      if (root != null)
      {
         foreach (var t in root.DescendantTrivia())
         {
            if (t.IsKind(SyntaxKind.EndOfLineTrivia))
               return t.ToString();
         }
      }

      return "\n";
   }

   private static int FindLastWhitespaceIndex(SyntaxTriviaList trivia)
   {
      for (var i = trivia.Count - 1; i >= 0; i--)
      {
         if (trivia[i].IsKind(SyntaxKind.WhitespaceTrivia))
            return i;
      }

      return trivia.Count;
   }

   internal static string SanitizeBlockComment(string commentText)
   {
      if (commentText.Length < 4 || !commentText.StartsWith("/*", StringComparison.Ordinal) || !commentText.EndsWith("*/", StringComparison.Ordinal))
         return commentText;

      var body = commentText.Substring(2, commentText.Length - 4);
      if (body.IndexOf("*/", StringComparison.Ordinal) < 0)
         return commentText;

      body = body.Replace("*/", "* /");
      return "/*" + body + "*/";
   }

   internal static string FormatComment(string rawComment, string indent, string eol = "\n")
   {
      var normalized = rawComment.Replace("\r\n", "\n");
      var lines = normalized.Split('\n');
      if (lines.Length <= 1)
         return rawComment;

      var sb = new StringBuilder();
      sb.Append(lines[0]);
      for (var i = 1; i < lines.Length; i++)
      {
         sb.Append(eol);
         sb.Append(indent);
         sb.Append(lines[i]);
      }

      return sb.ToString();
   }
}
