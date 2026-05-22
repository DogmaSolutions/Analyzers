using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

internal static class RequiredAttributeRemover
{
    public static PropertyDeclarationSyntax RemoveRequiredAttribute(
        PropertyDeclarationSyntax propertyDeclaration,
        SemanticModel semanticModel)
    {
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);
        if (propertySymbol == null)
            return null;

        var requiredAttrData = propertySymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "RequiredAttribute");
        if (requiredAttrData?.ApplicationSyntaxReference == null)
            return null;

        var attrSyntax = (AttributeSyntax)requiredAttrData.ApplicationSyntaxReference.GetSyntax();
        var attrList = (AttributeListSyntax)attrSyntax.Parent;

        PropertyDeclarationSyntax newProperty;
        if (attrList.Attributes.Count == 1)
        {
            var removedIndex = propertyDeclaration.AttributeLists.IndexOf(attrList);
            var newAttributeLists = propertyDeclaration.AttributeLists.Remove(attrList);
            newProperty = propertyDeclaration.WithAttributeLists(newAttributeLists);

            if (newProperty.AttributeLists.Count == 0)
            {
                newProperty = newProperty.WithLeadingTrivia(attrList.GetLeadingTrivia());
            }
            else if (removedIndex == 0)
            {
                var firstAttrList = newProperty.AttributeLists[0];
                var newFirstAttrList = firstAttrList.WithLeadingTrivia(attrList.GetLeadingTrivia());
                newProperty = newProperty.ReplaceNode(firstAttrList, newFirstAttrList);
            }
            else
            {
                var firstToken = newProperty.Modifiers.Any()
                    ? newProperty.Modifiers.First()
                    : newProperty.Type.GetFirstToken();
                var newFirstToken = firstToken.WithLeadingTrivia(attrList.GetLeadingTrivia());
                newProperty = newProperty.ReplaceToken(firstToken, newFirstToken);
            }
        }
        else
        {
            var newAttrList = attrList.RemoveNode(attrSyntax, SyntaxRemoveOptions.KeepNoTrivia);
            newProperty = propertyDeclaration.ReplaceNode(attrList, newAttrList);
        }

        return newProperty;
    }
}
