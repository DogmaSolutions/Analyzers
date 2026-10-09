using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

// ReSharper disable once InconsistentNaming
public abstract class ProhibitInvocationExpressionDiagnosticAnalyzer<T> : DiagnosticAnalyzer
{
    protected abstract string MemberName { get; }
    protected string TypeName => typeof(T).Name;
    protected string TypeFullName => typeof(T).FullName;
    protected string GlobalTypeFullName => "global::" + TypeFullName;

    protected virtual void RegisterHandler(AnalysisContext context, DiagnosticDescriptor rule)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.RegisterSyntaxNodeAction(ctx => OnInvocationExpression(ctx, rule), SyntaxKind.InvocationExpression);
    }

    protected virtual void OnInvocationExpression(SyntaxNodeAnalysisContext ctx, [NotNull] DiagnosticDescriptor rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        var invocationExpression = ctx.Node as InvocationExpressionSyntax;
        if (invocationExpression == null)
            return;

        if (!IsMatched(invocationExpression, rule))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: rule,
            location: invocationExpression.GetLocation(),
            effectiveSeverity:  ctx.GetDiagnosticSeverity(rule),
            additionalLocations: null,
            properties: null);

        ctx.ReportDiagnostic(diagnostic);
    }

    protected virtual bool IsMatched([NotNull] InvocationExpressionSyntax invocationExpression, [NotNull] DiagnosticDescriptor rule)
    {
        if (invocationExpression == null) throw new ArgumentNullException(nameof(invocationExpression));

        switch (invocationExpression.Expression)
        {
            case MemberAccessExpressionSyntax memberAccess: // match "xxx.yyy"
                return memberAccess.Name.Identifier.ValueText == MemberName && // match "xxx.Now"
                       IsReferenceToType(memberAccess.Expression);

            case IdentifierNameSyntax identifier when identifier.Identifier.ValueText == MemberName:
                return IsTypeImportedWithUsingStatic(invocationExpression); // Maybe "TypeName" has been imported using a "using static"

            default:
                return false;
        }
    }

    private bool IsReferenceToType(ExpressionSyntax expression)
    {
        return (expression is IdentifierNameSyntax identifier && identifier.Identifier.ValueText == TypeName) ||
               (expression is MemberAccessExpressionSyntax && expression.ToString() == TypeFullName);
    }

    private bool IsTypeImportedWithUsingStatic(SyntaxNode node)
    {
        var root = node.Ancestors().OfType<CompilationUnitSyntax>().FirstOrDefault(); // navigate "up" and find the root
        return root != null &&
               root.DescendantNodes().
                   OfType<UsingDirectiveSyntax>(). // navigate "down" and search the "using" directives
                   Any(IsUsingStaticOfType);
    }

    // consider only "using static TypeFullName" or "using static TypeName"
    private bool IsUsingStaticOfType(UsingDirectiveSyntax directive)
    {
        if (!directive.StaticKeyword.IsKind(SyntaxKind.StaticKeyword))
            return false;

        var imported = directive.NamespaceOrType.ToString();
        return imported == TypeName || imported == TypeFullName || imported == GlobalTypeFullName;
    }
}
