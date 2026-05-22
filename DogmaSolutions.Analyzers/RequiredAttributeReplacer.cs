using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class RequiredAttributeReplacer
{
    public static PropertyDeclarationSyntax ReplaceRequiredWithRange(
        PropertyDeclarationSyntax propertyDeclaration,
        SemanticModel semanticModel,
        SyntaxKind typeKeyword)
    {
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);
        if (propertySymbol == null)
            return null;

        var requiredAttrData = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RequiredAttribute");
        if (requiredAttrData?.ApplicationSyntaxReference == null)
            return null;

        var attrSyntax = (AttributeSyntax)requiredAttrData.ApplicationSyntaxReference.GetSyntax();

        var oneLiteral = SyntaxFactory.AttributeArgument(
            SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(1)));

        var maxValueAccess = SyntaxFactory.AttributeArgument(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(typeKeyword)),
                SyntaxFactory.IdentifierName("MaxValue")));

        NameSyntax rangeName = IsFullyQualified(attrSyntax)
            ? SyntaxFactory.QualifiedName(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("System"),
                        SyntaxFactory.IdentifierName("ComponentModel")),
                    SyntaxFactory.IdentifierName("DataAnnotations")),
                SyntaxFactory.IdentifierName("Range"))
            : SyntaxFactory.IdentifierName("Range");

        var rangeAttr = SyntaxFactory.Attribute(
            rangeName,
            SyntaxFactory.AttributeArgumentList(
                SyntaxFactory.SeparatedList(new[] { oneLiteral, maxValueAccess })));

        return propertyDeclaration.ReplaceNode(attrSyntax, rangeAttr.WithTriviaFrom(attrSyntax));
    }

    private static bool IsFullyQualified(AttributeSyntax attrSyntax)
    {
        return attrSyntax.Name is QualifiedNameSyntax;
    }
}
