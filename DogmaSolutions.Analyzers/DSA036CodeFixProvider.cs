using System.Collections.Generic;
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA036CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA036CodeFixProvider : CodeFixProvider
{
    public override ImmutableArray<string> FixableDiagnosticIds => [DSA036Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        var diagnostic = context.Diagnostics[0];
        var diagnosticSpan = diagnostic.Location.SourceSpan;
        var node = root.FindNode(diagnosticSpan);

        var kind = diagnostic.Properties.GetValueOrDefault("Kind");

        if (kind == "Constructor")
        {
            var creation = node as ObjectCreationExpressionSyntax
                           ?? node.DescendantNodesAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();

            ExpressionSyntax target = creation;
            if (target == null)
            {
                var implicitCreation = node as ImplicitObjectCreationExpressionSyntax
                                       ?? node.DescendantNodesAndSelf().OfType<ImplicitObjectCreationExpressionSyntax>().FirstOrDefault();
                target = implicitCreation;
            }

            if (target == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Extract to private static readonly field",
                    createChangedDocument: ct => ExtractConstructorToFieldAsync(context.Document, target, ct),
                    equivalenceKey: DSA036Analyzer.DiagnosticId),
                diagnostic);

            ReviewCommentCodeFix.Register(context, diagnostic, target, DSA036Analyzer.DiagnosticId, nameof(Resources.DSA036ReviewComment));
        }
        else if (kind == "StaticMethod")
        {
            var invocation = node as InvocationExpressionSyntax
                             ?? node.DescendantNodesAndSelf().OfType<InvocationExpressionSyntax>().FirstOrDefault();
            if (invocation == null)
                return;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Extract to private static readonly field",
                    createChangedDocument: ct => ExtractStaticMethodToFieldAsync(context.Document, invocation, ct),
                    equivalenceKey: DSA036Analyzer.DiagnosticId),
                diagnostic);

            ReviewCommentCodeFix.Register(context, diagnostic, invocation, DSA036Analyzer.DiagnosticId, nameof(Resources.DSA036ReviewComment));
        }
    }

    private static async Task<Document> ExtractConstructorToFieldAsync(
        Document document,
        ExpressionSyntax creation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var typeDecl = creation.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDecl == null)
            return document;

        ExpressionSyntax fieldInitializer;
        if (creation is ImplicitObjectCreationExpressionSyntax implicitCreation)
        {
            fieldInitializer = SyntaxFactory.ObjectCreationExpression(
                    SyntaxFactory.ParseTypeName("System.Text.RegularExpressions.Regex"),
                    implicitCreation.ArgumentList,
                    null)
                .NormalizeWhitespace();
        }
        else
        {
            fieldInitializer = creation.WithoutTrivia();
        }

        var fieldName = GenerateFieldName(typeDecl);

        var fieldDecl = CreateFieldDeclaration(fieldName, fieldInitializer, typeDecl);

        var fieldReference = SyntaxFactory.IdentifierName(fieldName)
            .WithLeadingTrivia(creation.GetLeadingTrivia())
            .WithTrailingTrivia(creation.GetTrailingTrivia());

        var newTypeDecl = typeDecl.ReplaceNode(creation, fieldReference);
        newTypeDecl = InsertField(newTypeDecl, fieldDecl);

        var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
        return document.WithSyntaxRoot(newRoot);
    }

    private static async Task<Document> ExtractStaticMethodToFieldAsync(
        Document document,
        InvocationExpressionSyntax invocation,
        CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null)
            return document;

        var model = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (model == null)
            return document;

        var symbolInfo = model.GetSymbolInfo(invocation, cancellationToken);
        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            return document;

        var typeDecl = invocation.Ancestors().OfType<TypeDeclarationSyntax>().FirstOrDefault();
        if (typeDecl == null)
            return document;

        var constructorArgs = new List<ArgumentSyntax>();
        var instanceArgs = new List<ArgumentSyntax>();

        for (var i = 0; i < invocation.ArgumentList.Arguments.Count; i++)
        {
            var arg = invocation.ArgumentList.Arguments[i];
            var paramName = GetParameterName(arg, i, methodSymbol);

            if (paramName is "input" or "replacement" or "evaluator")
                instanceArgs.Add(arg.WithoutTrivia());
            else
                constructorArgs.Add(arg.WithoutTrivia());
        }

        var regexType = SyntaxFactory.ParseTypeName("System.Text.RegularExpressions.Regex");
        var constructorArgList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(constructorArgs));
        var fieldInitializer = SyntaxFactory.ObjectCreationExpression(regexType, constructorArgList, null)
            .NormalizeWhitespace();

        var fieldName = GenerateFieldName(typeDecl);
        var fieldDecl = CreateFieldDeclaration(fieldName, fieldInitializer, typeDecl);

        var methodName = ((MemberAccessExpressionSyntax)invocation.Expression).Name;
        var instanceCall = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName(fieldName),
                    methodName.WithoutTrivia()),
                SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(instanceArgs)))
            .NormalizeWhitespace()
            .WithLeadingTrivia(invocation.GetLeadingTrivia())
            .WithTrailingTrivia(invocation.GetTrailingTrivia());

        var newTypeDecl = typeDecl.ReplaceNode(invocation, instanceCall);
        newTypeDecl = InsertField(newTypeDecl, fieldDecl);

        var newRoot = root.ReplaceNode(typeDecl, newTypeDecl);
        return document.WithSyntaxRoot(newRoot);
    }

    private static string GetParameterName(ArgumentSyntax arg, int index, IMethodSymbol method)
    {
        if (arg.NameColon != null)
            return arg.NameColon.Name.Identifier.ValueText;

        if (index < method.Parameters.Length)
            return method.Parameters[index].Name;

        return "unknown";
    }

    private static string GenerateFieldName(TypeDeclarationSyntax typeDecl)
    {
        var existingNames = new HashSet<string>(
            typeDecl.Members
                .OfType<FieldDeclarationSyntax>()
                .SelectMany(f => f.Declaration.Variables)
                .Select(v => v.Identifier.ValueText));

        var baseName = "_regex";
        if (!existingNames.Contains(baseName))
            return baseName;

        var suffix = 1;
        while (existingNames.Contains(baseName + suffix))
            suffix++;

        return baseName + suffix;
    }

    private static FieldDeclarationSyntax CreateFieldDeclaration(
        string fieldName,
        ExpressionSyntax initializer,
        TypeDeclarationSyntax typeDecl)
    {
        var eolTrivia = SyntaxUtils.GetEndOfLineTrivia(typeDecl);
        var memberIndent = DetectMemberIndentation(typeDecl);

        return SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName("System.Text.RegularExpressions.Regex"),
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.VariableDeclarator(fieldName)
                            .WithInitializer(SyntaxFactory.EqualsValueClause(initializer)))))
            .WithModifiers(SyntaxFactory.TokenList(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.StaticKeyword),
                SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword)))
            .NormalizeWhitespace()
            .WithLeadingTrivia(memberIndent)
            .WithTrailingTrivia(eolTrivia);
    }

    private static TypeDeclarationSyntax InsertField(
        TypeDeclarationSyntax typeDecl,
        FieldDeclarationSyntax fieldDecl)
    {
        var lastFieldIndex = -1;
        for (var i = 0; i < typeDecl.Members.Count; i++)
        {
            if (typeDecl.Members[i] is FieldDeclarationSyntax)
                lastFieldIndex = i;
        }

        var insertIndex = lastFieldIndex >= 0 ? lastFieldIndex + 1 : 0;
        return typeDecl.WithMembers(typeDecl.Members.Insert(insertIndex, fieldDecl));
    }

    private static SyntaxTriviaList DetectMemberIndentation(TypeDeclarationSyntax typeDecl)
    {
        foreach (var member in typeDecl.Members)
        {
            var leading = member.GetLeadingTrivia();
            for (var i = leading.Count - 1; i >= 0; i--)
            {
                if (leading[i].IsKind(SyntaxKind.WhitespaceTrivia))
                    return SyntaxFactory.TriviaList(leading[i]);
            }
        }

        return SyntaxFactory.TriviaList(SyntaxFactory.Whitespace("        "));
    }
}
