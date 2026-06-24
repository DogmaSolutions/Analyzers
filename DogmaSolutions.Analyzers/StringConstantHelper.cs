using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class StringConstantHelper
{
   internal static BlockSyntax FindContainingMethodBody(SyntaxNode node)
   {
      var current = node.Parent;
      while (current != null)
      {
         if (current is MethodDeclarationSyntax method)
            return method.Body;
         if (current is ConstructorDeclarationSyntax ctor)
            return ctor.Body;
         current = current.Parent;
      }

      return null;
   }

   internal static List<LiteralExpressionSyntax> FindMatchingLiterals(SyntaxNode scope, string stringValue)
   {
      return scope.DescendantNodes()
         .OfType<LiteralExpressionSyntax>()
         .Where(lit => lit.IsKind(SyntaxKind.StringLiteralExpression) &&
                       lit.Token.ValueText == stringValue)
         .ToList();
   }

   internal static StatementSyntax FindEarliestContainingStatement(
      List<LiteralExpressionSyntax> literals,
      BlockSyntax block)
   {
      StatementSyntax earliest = null;
      var earliestStart = int.MaxValue;

      foreach (var literal in literals)
      {
         var statement = literal.Ancestors().OfType<StatementSyntax>()
            .LastOrDefault(s => s.Parent == block);

         if (statement != null && statement.SpanStart < earliestStart)
         {
            earliest = statement;
            earliestStart = statement.SpanStart;
         }
      }

      return earliest;
   }

   internal static string ResolveNameConflicts(string baseName, SyntaxNode scope)
   {
      var existingNames = new HashSet<string>(
         scope.DescendantNodes()
            .OfType<VariableDeclaratorSyntax>()
            .Select(v => v.Identifier.ValueText));

      foreach (var param in scope.DescendantNodes().OfType<ParameterSyntax>())
         existingNames.Add(param.Identifier.ValueText);

      if (!existingNames.Contains(baseName))
         return baseName;

      var suffix = 1;
      while (existingNames.Contains(baseName + suffix))
         suffix++;

      return baseName + suffix;
   }

   internal static string ResolveFieldNameConflicts(string baseName, TypeDeclarationSyntax type)
   {
      var existingNames = new HashSet<string>();

      foreach (var member in type.Members)
      {
         if (member is FieldDeclarationSyntax field)
         {
            foreach (var variable in field.Declaration.Variables)
               existingNames.Add(variable.Identifier.ValueText);
         }
         else if (member is PropertyDeclarationSyntax prop)
         {
            existingNames.Add(prop.Identifier.ValueText);
         }
      }

      if (!existingNames.Contains(baseName))
         return baseName;

      var suffix = 1;
      while (existingNames.Contains(baseName + suffix))
         suffix++;

      return baseName + suffix;
   }

   internal static LocalDeclarationStatementSyntax CreateLocalConstDeclaration(
      string constName,
      string stringValue,
      StatementSyntax insertBefore,
      SyntaxTrivia eolTrivia)
   {
      var eolStr = eolTrivia.ToFullString();
      if (string.IsNullOrEmpty(eolStr))
         eolStr = "\r\n";

      return SyntaxFactory.LocalDeclarationStatement(
            SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ConstKeyword)),
            SyntaxFactory.VariableDeclaration(
               SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
               SyntaxFactory.SingletonSeparatedList(
                  SyntaxFactory.VariableDeclarator(constName)
                     .WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.LiteralExpression(
                           SyntaxKind.StringLiteralExpression,
                           SyntaxFactory.Literal(stringValue)))))))
         .NormalizeWhitespace(eol: eolStr)
         .WithLeadingTrivia(GetIndentationTrivia(insertBefore))
         .WithTrailingTrivia(eolTrivia);
   }

   internal static FieldDeclarationSyntax CreateClassFieldDeclaration(
      string fieldName,
      string stringValue,
      TypeDeclarationSyntax containingType,
      SyntaxTrivia eolTrivia)
   {
      var eolStr = eolTrivia.ToFullString();
      if (string.IsNullOrEmpty(eolStr))
         eolStr = "\r\n";

      var firstMember = containingType.Members.FirstOrDefault();
      var indentationSource = firstMember ?? (SyntaxNode)containingType;

      return SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(
               SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)),
               SyntaxFactory.SingletonSeparatedList(
                  SyntaxFactory.VariableDeclarator(fieldName)
                     .WithInitializer(SyntaxFactory.EqualsValueClause(
                        SyntaxFactory.LiteralExpression(
                           SyntaxKind.StringLiteralExpression,
                           SyntaxFactory.Literal(stringValue)))))))
         .WithModifiers(SyntaxFactory.TokenList(
            SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
            SyntaxFactory.Token(SyntaxKind.ConstKeyword)))
         .NormalizeWhitespace(eol: eolStr)
         .WithLeadingTrivia(GetMemberIndentationTrivia(indentationSource))
         .WithTrailingTrivia(eolTrivia);
   }

   internal static TypeDeclarationSyntax InsertFieldBeforeFirstMethod(
      TypeDeclarationSyntax type,
      FieldDeclarationSyntax field)
   {
      var firstMethodIndex = -1;
      for (var i = 0; i < type.Members.Count; i++)
      {
         if (type.Members[i] is MethodDeclarationSyntax ||
             type.Members[i] is ConstructorDeclarationSyntax)
         {
            firstMethodIndex = i;
            break;
         }
      }

      var insertIndex = firstMethodIndex >= 0 ? firstMethodIndex : type.Members.Count;
      var newMembers = type.Members.Insert(insertIndex, field);

      if (type is ClassDeclarationSyntax classDecl)
         return classDecl.WithMembers(newMembers);
      if (type is StructDeclarationSyntax structDecl)
         return structDecl.WithMembers(newMembers);
      if (type is RecordDeclarationSyntax recordDecl)
         return recordDecl.WithMembers(newMembers);

      return type;
   }

   internal static SyntaxTriviaList GetIndentationTrivia(SyntaxNode node)
   {
      var leading = node.GetLeadingTrivia();
      var result = new List<SyntaxTrivia>();

      for (var i = leading.Count - 1; i >= 0; i--)
      {
         if (leading[i].IsKind(SyntaxKind.WhitespaceTrivia))
            result.Insert(0, leading[i]);
         else
            break;
      }

      return result.Count > 0 ? SyntaxFactory.TriviaList(result) : SyntaxTriviaList.Empty;
   }

   internal static SyntaxTriviaList GetMemberIndentationTrivia(SyntaxNode node)
   {
      var leading = node.GetLeadingTrivia();
      var startIndex = leading.Count;

      for (var i = leading.Count - 1; i >= 0; i--)
      {
         var kind = leading[i].Kind();
         if (kind == SyntaxKind.WhitespaceTrivia || kind == SyntaxKind.EndOfLineTrivia)
            startIndex = i;
         else
            break;
      }

      if (startIndex >= leading.Count)
         return SyntaxTriviaList.Empty;

      var result = new List<SyntaxTrivia>();
      for (var i = startIndex; i < leading.Count; i++)
         result.Add(leading[i]);

      return SyntaxFactory.TriviaList(result);
   }

   internal static async Task<Document> ExtractToLocalConstAsync(
      Document document,
      LiteralExpressionSyntax targetLiteral,
      string stringValue,
      CancellationToken cancellationToken)
   {
      var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
      if (root == null)
         return document;

      var methodBody = FindContainingMethodBody(targetLiteral);
      if (methodBody == null)
         return document;

      var allLiterals = FindMatchingLiterals(methodBody, stringValue);
      if (allLiterals.Count == 0)
         return document;

      var constName = DSA032Analyzer.GenerateConstantName(stringValue);
      constName = ResolveNameConflicts(constName, methodBody);

      var identifierName = SyntaxFactory.IdentifierName(constName);
      var replaceAnnotation = new SyntaxAnnotation("DSA032_replace");
      var insertAnnotation = new SyntaxAnnotation("DSA032_insert");

      var insertionStatement = FindEarliestContainingStatement(allLiterals, methodBody);
      if (insertionStatement == null)
         return document;

      var literalSet = new HashSet<SyntaxNode>(allLiterals);
      var allNodesToAnnotate = new HashSet<SyntaxNode>(allLiterals) { insertionStatement };

      var annotatedRoot = root.ReplaceNodes(allNodesToAnnotate, (original, rewritten) =>
      {
         var result = rewritten;
         if (original == insertionStatement)
            result = result.WithAdditionalAnnotations(insertAnnotation);
         if (literalSet.Contains(original))
            result = result.WithAdditionalAnnotations(replaceAnnotation);
         return result;
      });

      var newRoot = annotatedRoot;
      SyntaxNode nodeToReplace;
      while ((nodeToReplace = newRoot.GetAnnotatedNodes(replaceAnnotation).FirstOrDefault()) != null)
      {
         newRoot = newRoot.ReplaceNode(
            nodeToReplace,
            identifierName
               .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
               .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia()));
      }

      var finalInsertion = newRoot.GetAnnotatedNodes(insertAnnotation).First() as StatementSyntax;
      if (finalInsertion == null)
         return document;

      var containingBlock = finalInsertion.Parent as BlockSyntax;
      if (containingBlock == null)
         return document.WithSyntaxRoot(newRoot);

      var eolTrivia = SyntaxUtils.GetEndOfLineTrivia(finalInsertion);
      var constDecl = CreateLocalConstDeclaration(constName, stringValue, finalInsertion, eolTrivia);

      var idx = containingBlock.Statements.IndexOf(finalInsertion);
      var newStatements = containingBlock.Statements.Insert(idx, constDecl);
      newRoot = newRoot.ReplaceNode(containingBlock, containingBlock.WithStatements(newStatements));

      return document.WithSyntaxRoot(newRoot);
   }

   internal static async Task<Document> ExtractToClassFieldAsync(
      Document document,
      LiteralExpressionSyntax targetLiteral,
      string stringValue,
      CancellationToken cancellationToken)
   {
      var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
      if (root == null)
         return document;

      var methodBody = FindContainingMethodBody(targetLiteral);
      if (methodBody == null)
         return document;

      var containingType = targetLiteral.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
      if (containingType == null)
         return document;

      var allLiterals = FindMatchingLiterals(methodBody, stringValue);
      if (allLiterals.Count == 0)
         return document;

      var constName = DSA032Analyzer.GenerateConstantName(stringValue);
      constName = ResolveFieldNameConflicts(constName, containingType);

      var identifierName = SyntaxFactory.IdentifierName(constName);
      var replaceAnnotation = new SyntaxAnnotation("DSA032_replace");
      var typeAnnotation = new SyntaxAnnotation("DSA032_type");

      var literalSet = new HashSet<SyntaxNode>(allLiterals);
      var allNodesToAnnotate = new HashSet<SyntaxNode>(allLiterals) { containingType };

      var annotatedRoot = root.ReplaceNodes(allNodesToAnnotate, (original, rewritten) =>
      {
         var result = rewritten;
         if (original == containingType)
            result = result.WithAdditionalAnnotations(typeAnnotation);
         if (literalSet.Contains(original))
            result = result.WithAdditionalAnnotations(replaceAnnotation);
         return result;
      });

      var newRoot = annotatedRoot;
      SyntaxNode nodeToReplace;
      while ((nodeToReplace = newRoot.GetAnnotatedNodes(replaceAnnotation).FirstOrDefault()) != null)
      {
         newRoot = newRoot.ReplaceNode(
            nodeToReplace,
            identifierName
               .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
               .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia()));
      }

      var updatedType = newRoot.GetAnnotatedNodes(typeAnnotation).First() as TypeDeclarationSyntax;
      if (updatedType == null)
         return document;

      var eolTrivia = SyntaxUtils.GetEndOfLineTrivia(updatedType);
      var fieldDecl = CreateClassFieldDeclaration(constName, stringValue, updatedType, eolTrivia);

      var newType = InsertFieldBeforeFirstMethod(updatedType, fieldDecl);
      newRoot = newRoot.ReplaceNode(updatedType, newType);

      return document.WithSyntaxRoot(newRoot);
   }

   internal static async Task<Document> ExtractSingleToClassFieldAsync(
      Document document,
      LiteralExpressionSyntax targetLiteral,
      string stringValue,
      CancellationToken cancellationToken)
   {
      var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
      if (root == null)
         return document;

      var containingType = targetLiteral.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
      if (containingType == null)
         return document;

      var constName = DSA032Analyzer.GenerateConstantName(stringValue);
      constName = ResolveFieldNameConflicts(constName, containingType);

      var identifierName = SyntaxFactory.IdentifierName(constName);
      var replaceAnnotation = new SyntaxAnnotation("DSR002_replace");
      var typeAnnotation = new SyntaxAnnotation("DSR002_type");

      var annotatedRoot = root.ReplaceNodes(
         new SyntaxNode[] { targetLiteral, containingType },
         (original, rewritten) =>
         {
            var result = rewritten;
            if (original == containingType)
               result = result.WithAdditionalAnnotations(typeAnnotation);
            if (original == targetLiteral)
               result = result.WithAdditionalAnnotations(replaceAnnotation);
            return result;
         });

      var newRoot = annotatedRoot;
      var nodeToReplace = newRoot.GetAnnotatedNodes(replaceAnnotation).FirstOrDefault();
      if (nodeToReplace != null)
      {
         newRoot = newRoot.ReplaceNode(
            nodeToReplace,
            identifierName
               .WithLeadingTrivia(nodeToReplace.GetLeadingTrivia())
               .WithTrailingTrivia(nodeToReplace.GetTrailingTrivia()));
      }

      var updatedType = newRoot.GetAnnotatedNodes(typeAnnotation).First() as TypeDeclarationSyntax;
      if (updatedType == null)
         return document;

      var eolTrivia = SyntaxUtils.GetEndOfLineTrivia(updatedType);
      var fieldDecl = CreateClassFieldDeclaration(constName, stringValue, updatedType, eolTrivia);

      var newType = InsertFieldBeforeFirstMethod(updatedType, fieldDecl);
      newRoot = newRoot.ReplaceNode(updatedType, newType);

      return document.WithSyntaxRoot(newRoot);
   }

   internal static async Task<Document> ReplaceWithKnownConstantAsync(
      Document document,
      LiteralExpressionSyntax targetLiteral,
      string stringValue,
      string replacementExpression,
      CancellationToken cancellationToken)
   {
      var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
      if (root == null)
         return document;

      var methodBody = FindContainingMethodBody(targetLiteral);
      var scope = (SyntaxNode)methodBody ?? targetLiteral.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
      if (scope == null)
         return document;

      var allLiterals = FindMatchingLiterals(scope, stringValue);
      if (allLiterals.Count == 0)
         return document;

      var replacement = SyntaxFactory.ParseExpression(replacementExpression);

      var newRoot = root.ReplaceNodes(allLiterals, (original, _) =>
         replacement
            .WithLeadingTrivia(original.GetLeadingTrivia())
            .WithTrailingTrivia(original.GetTrailingTrivia()));

      return document.WithSyntaxRoot(newRoot);
   }

   internal static async Task<ImmutableArray<string>> LoadReplacementsAsync(
      Project project,
      string stringValue,
      CancellationToken cancellationToken)
   {
      var builder = ImmutableArray.CreateBuilder<string>();
      foreach (var doc in project.AdditionalDocuments)
      {
         if (!Path.GetFileName(doc.Name).Equals(DSA032Analyzer.StringReplacementsFileName, StringComparison.OrdinalIgnoreCase))
            continue;

         var text = await doc.GetTextAsync(cancellationToken).ConfigureAwait(false);
         if (text == null)
            continue;

         foreach (var line in text.Lines)
         {
            var lineText = line.ToString().Trim();
            if (lineText.Length == 0 || lineText.StartsWith("#", StringComparison.Ordinal))
               continue;

            var arrowIndex = lineText.LastIndexOf("->", StringComparison.Ordinal);
            if (arrowIndex < 0)
               continue;

            var left = Unquote(lineText.Substring(0, arrowIndex).Trim());
            var right = Unquote(lineText.Substring(arrowIndex + 2).Trim());

            if (left == stringValue && right.Length > 0)
               builder.Add(right);
         }
      }

      return builder.ToImmutable();
   }

   internal static string Unquote(string value)
   {
      if (value.Length >= 2 && value[0] == '`' && value[value.Length - 1] == '`')
         return value.Substring(1, value.Length - 2);
      return value;
   }
}
