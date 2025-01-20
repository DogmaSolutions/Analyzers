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
            location: invocationExpression.GetLocation(),
            effectiveSeverity: severity,
            additionalLocations: null,
            properties: null);

        ctx.ReportDiagnostic(diagnostic);
    }

    protected virtual bool IsMatched([NotNull] InvocationExpressionSyntax invocationExpression, [NotNull] DiagnosticDescriptor rule)
    {
        if (invocationExpression == null) throw new ArgumentNullException(nameof(invocationExpression));

        if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccess) // match "xxx.yyy"
        {
            if (memberAccess.Name.Identifier.ValueText != MemberName) // match "xxx.Now"
                return false;

            if ((memberAccess.Expression is IdentifierNameSyntax syntax && syntax.Identifier.ValueText == TypeName) ||
                (memberAccess.Expression is MemberAccessExpressionSyntax && memberAccess.Expression.ToString() == TypeFullName)
               )
            {
                return true;
            }
        }
        else if (invocationExpression.Expression is IdentifierNameSyntax syntax &&
                 syntax.Identifier is var token &&
                 token.ValueText == MemberName) // Maybe "TypeName" has been imported using a "using static"
        {
            return invocationExpression.Ancestors().
                       OfType<CompilationUnitSyntax>().
                       FirstOrDefault()?. // navigate "up" and find the root
                       DescendantNodes().
                       OfType<UsingDirectiveSyntax>(). // navigate "down" and search the "using" directives
                       Any(
                           u => u.StaticKeyword.IsKind(SyntaxKind.StaticKeyword) && // consider only "using static" directives
                                (u.NamespaceOrType.ToString() == TypeName ||
                                 u.NamespaceOrType.ToString() == TypeFullName ||
                                 u.NamespaceOrType.ToString() == GlobalTypeFullName) // consider only "using static TypeFullName"  or "using static TypeName"
                       ) ==
                   true;
        }

        return false;
    }
}