using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class EfQueryTagHelper
{
   internal static async Task<Document> AddTagToQueryAsync(
      Document document,
      InvocationExpressionSyntax invocation,
      bool useCallSite,
      CancellationToken cancellationToken)
   {
      var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
      if (root == null)
         return document;

      if (invocation.Expression is not MemberAccessExpressionSyntax terminalAccess)
         return document;

      var receiver = terminalAccess.Expression;

      ExpressionSyntax tagExpression;
      if (useCallSite)
      {
         tagExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
               SyntaxKind.SimpleMemberAccessExpression,
               receiver,
               SyntaxFactory.IdentifierName("TagWithCallSite")));
      }
      else
      {
         tagExpression = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
               SyntaxKind.SimpleMemberAccessExpression,
               receiver,
               SyntaxFactory.IdentifierName("TagWith")),
            SyntaxFactory.ArgumentList(
               SyntaxFactory.SingletonSeparatedList(
                  SyntaxFactory.Argument(
                     SyntaxFactory.LiteralExpression(
                        SyntaxKind.StringLiteralExpression,
                        SyntaxFactory.Literal("TODO: describe this query"))))));
      }

      var newTerminalAccess = terminalAccess.WithExpression(tagExpression);
      var newInvocation = invocation.WithExpression(newTerminalAccess);

      var newRoot = root.ReplaceNode(invocation, newInvocation);
      return document.WithSyntaxRoot(newRoot);
   }
}
