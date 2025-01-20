using System;
using System.Data.Common;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

// ReSharper disable once InconsistentNaming
public abstract class ProhibitSimpleMemberAccessExpressionDiagnosticAnalyzer<T> : DiagnosticAnalyzer
{
    protected abstract string MemberName { get; }
    protected string TypeName => typeof(T).Name;
    protected string TypeFullName => typeof(T).FullName;
    protected string GlobalTypeFullName => "global::" + TypeFullName;


    protected virtual void RegisterHandler(AnalysisContext context, DiagnosticDescriptor rule)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.RegisterSyntaxNodeAction(ctx => OnMemberAccessExpression(ctx, rule), SyntaxKind.SimpleMemberAccessExpression);
        context.RegisterSyntaxNodeAction(ctx => OnIdentifierName(ctx, rule), SyntaxKind.IdentifierName);
    }

#pragma warning disable CA1062
  

    protected virtual void HandleMatched(SyntaxNodeAnalysisContext ctx, DiagnosticDescriptor rule, CSharpSyntaxNode identifierNameSyntax)
    {
        var severity = rule.DefaultSeverity;
        var config = ctx.Options.AnalyzerConfigOptionsProvider.GetOptions(ctx.Node.SyntaxTree);
        if (config.TryGetValue($"dotnet_diagnostic.{rule.Id}.severity", out var configValue) &&
            !string.IsNullOrWhiteSpace(configValue) &&
            Enum.TryParse<DiagnosticSeverity>(configValue, out var configuredSeverity))
        {
            severity = configuredSeverity;
        }


        var diagnostic = Diagnostic.Create(
            descriptor: rule,
            location: identifierNameSyntax.GetLocation(),
            effectiveSeverity: severity,
            additionalLocations: null,
            properties: null);

        ctx.ReportDiagnostic(diagnostic);
    }

    protected virtual void OnMemberAccessExpression(SyntaxNodeAnalysisContext ctx, [NotNull] DiagnosticDescriptor rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        var memberAccessExpression = ctx.Node as MemberAccessExpressionSyntax;
        if (memberAccessExpression == null)
            return;

        if (!IsMemberAccessExpressionMatched(memberAccessExpression, rule))
            return;

        HandleMatched(ctx, rule, memberAccessExpression);
    }

    protected virtual bool IsMemberAccessExpressionMatched([NotNull] MemberAccessExpressionSyntax memberAccessExpression, [NotNull] DiagnosticDescriptor rule)
    {
        if (memberAccessExpression == null) throw new ArgumentNullException(nameof(memberAccessExpression));

        if (memberAccessExpression.Name?.Identifier.ValueText != MemberName)
            return false;

        // match "TypeFullName.Member" or "TypeFullName.Member"
        var str = memberAccessExpression.Expression.ToString();
        if (str == TypeFullName || str == TypeName)
        {
            return true;
        }


        // Maybe "TypeName" has been imported using a "using static"
        var importedByStaticUsing = memberAccessExpression.Ancestors().
                       OfType<CompilationUnitSyntax>().
                       FirstOrDefault()?. // navigate "up" and find the root
                       DescendantNodes().
                       OfType<UsingDirectiveSyntax>(). // navigate "down" and search the "using" directives
                       Any(
                           u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && // consider only "using static" directives
                                (u.NamespaceOrType.ToString() == TypeFullName ||   // consider only "using static TypeFullName" ?
                                 u.NamespaceOrType.ToString() == GlobalTypeFullName  // consider only "using static global::TypeFullName" ?
                                 )
                       ) ==
                   true;

        return importedByStaticUsing;
    }

    protected virtual void OnIdentifierName(SyntaxNodeAnalysisContext ctx, [NotNull] DiagnosticDescriptor rule)
    {
        if (rule == null) throw new ArgumentNullException(nameof(rule));

        var identifierNameSyntax = ctx.Node as IdentifierNameSyntax;
        if (identifierNameSyntax == null)
            return;

        if (!IsIdentifierNameMatched(identifierNameSyntax, rule))
            return;

        HandleMatched(ctx, rule, identifierNameSyntax);
    }

    protected virtual bool IsIdentifierNameMatched([NotNull] IdentifierNameSyntax identifierNameSyntax, [NotNull] DiagnosticDescriptor rule)
    {
        if (identifierNameSyntax == null) throw new ArgumentNullException(nameof(identifierNameSyntax));

        if (identifierNameSyntax.Identifier.ValueText != MemberName)
            return false;

        // Maybe "TypeName" has been imported using a "using static"
        var importedByStaticUsing = identifierNameSyntax.Ancestors().
                                        OfType<CompilationUnitSyntax>().
                                        FirstOrDefault()?. // navigate "up" and find the root
                                        DescendantNodes().
                                        OfType<UsingDirectiveSyntax>(). // navigate "down" and search the "using" directives
                                        Any(
                                            u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && // consider only "using static" directives
                                                 (u.NamespaceOrType.ToString() == TypeFullName ||   // consider only "using static TypeFullName" ?
                                                  u.NamespaceOrType.ToString() == GlobalTypeFullName  // consider only "using static global::TypeFullName" ?
                                                 )
                                        ) ==
                                    true;

        return importedByStaticUsing;
    }

#pragma warning restore CA1062
}