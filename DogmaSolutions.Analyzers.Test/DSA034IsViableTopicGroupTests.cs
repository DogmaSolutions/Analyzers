using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA034IsViableTopicGroupTests
{
   private static MemberDeclarationSyntax Method(string name = "DoWork") =>
      SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), name)
         .WithBody(SyntaxFactory.Block());

   private static MemberDeclarationSyntax Field(string name = "_value") =>
      SyntaxFactory.FieldDeclaration(
         SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(name))));

   private static MemberDeclarationSyntax Property(string name = "Value") =>
      SyntaxFactory.PropertyDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)), name)
         .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[]
         {
            SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
            SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
               .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
         })));

   private static MemberDeclarationSyntax EventField(string name = "Changed") =>
      SyntaxFactory.EventFieldDeclaration(
         SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("System.Action"))
            .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(name))));

   // --- Case A: at least 1 method ---

   [TestMethod]
   public void OneMethod_ZeroNonMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Method() }));

   [TestMethod]
   public void TwoMethods_ZeroNonMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Method("A"), Method("B") }));

   [TestMethod]
   public void OneMethod_OneField_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Method(), Field() }));

   [TestMethod]
   public void OneMethod_OneProperty_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Method(), Property() }));

   [TestMethod]
   public void OneMethod_OneEvent_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Method(), EventField() }));

   // --- Case B: at least 2 non-method items ---

   [TestMethod]
   public void TwoFields_ZeroMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Field("_a"), Field("_b") }));

   [TestMethod]
   public void TwoProperties_ZeroMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Property("A"), Property("B") }));

   [TestMethod]
   public void FieldAndProperty_ZeroMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Field(), Property() }));

   [TestMethod]
   public void FieldAndEvent_ZeroMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Field(), EventField() }));

   [TestMethod]
   public void PropertyAndEvent_ZeroMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Property(), EventField() }));

   [TestMethod]
   public void ThreeNonMethods_ZeroMethods_IsViable() =>
      Assert.IsTrue(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Field("_a"), Field("_b"), Property() }));

   // --- Not viable: 0 methods and fewer than 2 non-methods ---

   [TestMethod]
   public void Empty_NotViable() =>
      Assert.IsFalse(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax>()));

   [TestMethod]
   public void SingleField_NotViable() =>
      Assert.IsFalse(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Field() }));

   [TestMethod]
   public void SingleProperty_NotViable() =>
      Assert.IsFalse(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { Property() }));

   [TestMethod]
   public void SingleEvent_NotViable() =>
      Assert.IsFalse(DSA034CodeFixProvider.IsViableTopicGroup(new List<MemberDeclarationSyntax> { EventField() }));
}
