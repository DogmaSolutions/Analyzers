using System;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    public static class AnalyzersUtils
    {
        public static DiagnosticSeverity GetDiagnosticSeverity(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            return GetDiagnosticSeverity(context, rule, rule.Id);
        }

        public static DiagnosticSeverity GetDiagnosticSeverity(this SyntaxNodeAnalysisContext context, DiagnosticDescriptor rule, string diagnosticId)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            var config = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Node.SyntaxTree);
            var severity = rule.DefaultSeverity;
            if (config.TryGetValue($"dotnet_diagnostic.{diagnosticId}.severity", out var configValue) &&
                !string.IsNullOrWhiteSpace(configValue) &&
                Enum.TryParse<DiagnosticSeverity>(configValue, out var configuredSeverity))
            {
                severity = configuredSeverity;
            }

            return severity;
        }


        public static bool IsEfDbContext(this IdentifierNameSyntax identifier, SyntaxNodeAnalysisContext ctx)
        {
            var fromSymbolInfo = ctx.SemanticModel.GetSymbolInfo(identifier);
            var typeSymbol = GetTypeSymbol(fromSymbolInfo);
            if (typeSymbol != null)
            {
                var bt = typeSymbol.BaseType;
                if (bt != null)
                {
                    var cn = bt.Name;
                    var ns = bt.ContainingNamespace?.ToDisplayString();
                    if (cn == "DbContext" && ns == "Microsoft.EntityFrameworkCore")
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public static bool IsEfDbSet(this IdentifierNameSyntax identifier, SyntaxNodeAnalysisContext ctx)
        {
            var fromSymbolInfo = ctx.SemanticModel.GetSymbolInfo(identifier);
            var typeSymbol = GetTypeSymbol(fromSymbolInfo);
            if (typeSymbol == null)
                return false;

            var ds = typeSymbol.ToDisplayString();

            if (ds.StartsWith("Microsoft.EntityFrameworkCore.DbSet<", StringComparison.InvariantCulture) && ds.EndsWith(">", StringComparison.InvariantCulture))
                return true;

            return false;
        }


        public static ITypeSymbol GetTypeSymbol(this SymbolInfo symbolInfo)
        {
            if (symbolInfo.Symbol is IFieldSymbol fs)
                return fs.Type;

            if (symbolInfo.Symbol is ILocalSymbol ls)
                return ls.Type;

            if (symbolInfo.Symbol is IParameterSymbol ps)
                return ps.Type;

            if (symbolInfo.Symbol is INamedTypeSymbol nts)
                return nts;

            if (symbolInfo.Symbol is IPropertySymbol prs)
                return prs.Type;

            return null;
        }


        public static bool IsWebApiControllerClass([NotNull] this ClassDeclarationSyntax classDeclaration, SyntaxNodeAnalysisContext ctx)
        {
            if (classDeclaration == null) throw new ArgumentNullException(nameof(classDeclaration));
            if (classDeclaration.BaseList?.Types.Any(
                    t =>
                    {
                        var baseType = ctx.SemanticModel.GetSymbolInfo(t.Type);
                        var typeSymbol = GetTypeSymbol(baseType);
                        if (typeSymbol != null)
                            return IsWebApiControllerClass(typeSymbol);

                        return false;
                    }) ==
                true)
                return true;

            return false;
        }


        public static bool IsWebApiControllerClass(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol == null)
                return false;

            if (typeSymbol.Name == "ControllerBase" && typeSymbol.ContainingNamespace?.ToDisplayString() == "Microsoft.AspNetCore.Mvc")
                return true;

            if (typeSymbol.BaseType != null)
                return IsWebApiControllerClass(typeSymbol.BaseType);

            return false;
        }
    }
}