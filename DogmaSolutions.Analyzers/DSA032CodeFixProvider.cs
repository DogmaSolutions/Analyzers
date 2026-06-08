using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.IO;
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

      var replacements = await LoadReplacementsAsync(context.Document.Project, stringValue, context.CancellationToken).ConfigureAwait(false);
      foreach (var replacement in replacements)
      {
         var displayReplacement = replacement;
         if (displayReplacement.Length > 60)
            displayReplacement = displayReplacement.Substring(0, 57) + "...";

         context.RegisterCodeFix(
            CodeAction.Create(
               title: $"Replace \"{displayValue}\" with '{displayReplacement}'",
               createChangedDocument: ct => ReplaceWithKnownConstantAsync(context.Document, literal, stringValue, replacement, ct),
               equivalenceKey: UseReplacementEquivalenceKey + ":" + replacement),
            diagnostic);
      }

      context.RegisterCodeFix(
         CodeAction.Create(
            title: $"Extract \"{displayValue}\" to local constant '{constName}'",
            createChangedDocument: ct => ExtractToLocalConstAsync(context.Document, literal, stringValue, ct),
            equivalenceKey: LocalConstEquivalenceKey),
         diagnostic);

      context.RegisterCodeFix(
         CodeAction.Create(
            title: $"Extract \"{displayValue}\" to class field constant '{constName}'",
            createChangedDocument: ct => ExtractToClassFieldAsync(context.Document, literal, stringValue, ct),
            equivalenceKey: ClassFieldEquivalenceKey),
         diagnostic);

      ReviewCommentCodeFix.Register(context, diagnostic, literal, DSA032Analyzer.DiagnosticId, nameof(Resources.DSA032ReviewComment));
   }

   private static async Task<ImmutableArray<string>> LoadReplacementsAsync(Project project, string stringValue, CancellationToken cancellationToken)
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

   private static async Task<Document> ReplaceWithKnownConstantAsync(
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
      if (methodBody == null)
         return document;

      var allLiterals = FindMatchingLiterals(methodBody, stringValue);
      if (allLiterals.Count < 2)
         return document;

      var replacement = SyntaxFactory.ParseExpression(replacementExpression);

      var newRoot = root.ReplaceNodes(allLiterals, (original, _) =>
         replacement
            .WithLeadingTrivia(original.GetLeadingTrivia())
            .WithTrailingTrivia(original.GetTrailingTrivia()));

      return document.WithSyntaxRoot(newRoot);
   }

   private static async Task<Document> ExtractToLocalConstAsync(
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
      if (allLiterals.Count < 2)
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

      var eolTrivia = GetEndOfLineTrivia(finalInsertion);
      var constDecl = CreateLocalConstDeclaration(constName, stringValue, finalInsertion, eolTrivia);

      var idx = containingBlock.Statements.IndexOf(finalInsertion);
      var newStatements = containingBlock.Statements.Insert(idx, constDecl);
      newRoot = newRoot.ReplaceNode(containingBlock, containingBlock.WithStatements(newStatements));

      return document.WithSyntaxRoot(newRoot);
   }

   private static async Task<Document> ExtractToClassFieldAsync(
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
      if (allLiterals.Count < 2)
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

      var eolTrivia = GetEndOfLineTrivia(updatedType);
      var fieldDecl = CreateClassFieldDeclaration(constName, stringValue, updatedType, eolTrivia);

      var newType = InsertFieldBeforeFirstMethod(updatedType, fieldDecl);
      newRoot = newRoot.ReplaceNode(updatedType, newType);

      return document.WithSyntaxRoot(newRoot);
   }

   private static BlockSyntax FindContainingMethodBody(SyntaxNode node)
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

   private static List<LiteralExpressionSyntax> FindMatchingLiterals(SyntaxNode scope, string stringValue)
   {
      return scope.DescendantNodes()
         .OfType<LiteralExpressionSyntax>()
         .Where(lit => lit.IsKind(SyntaxKind.StringLiteralExpression) &&
                       lit.Token.ValueText == stringValue)
         .ToList();
   }

   private static StatementSyntax FindEarliestContainingStatement(
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

   private static string ResolveNameConflicts(string baseName, SyntaxNode scope)
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

   private static string ResolveFieldNameConflicts(string baseName, TypeDeclarationSyntax type)
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

   private static LocalDeclarationStatementSyntax CreateLocalConstDeclaration(
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

   private static FieldDeclarationSyntax CreateClassFieldDeclaration(
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

   private static TypeDeclarationSyntax InsertFieldBeforeFirstMethod(
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

   private static SyntaxTriviaList GetIndentationTrivia(SyntaxNode node)
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

   private static SyntaxTriviaList GetMemberIndentationTrivia(SyntaxNode node)
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

   private static SyntaxTrivia GetEndOfLineTrivia(SyntaxNode node)
   {
      for (var current = node; current != null; current = current.Parent)
      {
         foreach (var trivia in current.GetLeadingTrivia())
         {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
               return trivia;
         }

         foreach (var trivia in current.GetTrailingTrivia())
         {
            if (trivia.IsKind(SyntaxKind.EndOfLineTrivia))
               return trivia;
         }
      }

      var firstEol = node.SyntaxTree.GetRoot().DescendantTokens()
         .SelectMany(t => t.TrailingTrivia)
         .FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
      if (firstEol != default)
         return firstEol;

      return SyntaxFactory.LineFeed;
   }

   private static string Unquote(string value)
   {
      if (value.Length >= 2 && value[0] == '`' && value[value.Length - 1] == '`')
         return value.Substring(1, value.Length - 2);
      return value;
   }
}
