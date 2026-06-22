using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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

        internal static readonly ImmutableArray<string> DefaultExcludedFilePatterns = ImmutableArray.Create(
            "*.Designer.cs",
            "*.generated.cs",
            "*.g.cs",
            "*.g.i.cs");

        internal static readonly ImmutableArray<string> DefaultExcludedBaseTypes = ImmutableArray.Create(
            "Microsoft.EntityFrameworkCore.Migrations.Migration",
            "Microsoft.EntityFrameworkCore.Infrastructure.ModelSnapshot",
            "System.Data.Entity.Migrations.DbMigration",
            "Microsoft.EntityFrameworkCore.DbContext");

        internal static bool MatchesGlobPattern(string text, string pattern)
        {
            var textIdx = 0;
            var patIdx = 0;
            var starTextIdx = -1;
            var starPatIdx = -1;

            while (textIdx < text.Length)
            {
                if (patIdx < pattern.Length &&
                    pattern[patIdx] != '*' &&
                    char.ToUpperInvariant(pattern[patIdx]) == char.ToUpperInvariant(text[textIdx]))
                {
                    textIdx++;
                    patIdx++;
                }
                else if (patIdx < pattern.Length && pattern[patIdx] == '*')
                {
                    starTextIdx = textIdx;
                    starPatIdx = patIdx;
                    patIdx++;
                }
                else if (starPatIdx >= 0)
                {
                    starTextIdx++;
                    textIdx = starTextIdx;
                    patIdx = starPatIdx + 1;
                }
                else
                {
                    return false;
                }
            }

            while (patIdx < pattern.Length && pattern[patIdx] == '*')
                patIdx++;

            return patIdx == pattern.Length;
        }

        internal static bool IsFileExcluded(string filePath, IReadOnlyList<string> patterns)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            var fileName = Path.GetFileName(filePath);
            foreach (var pattern in patterns)
            {
                if (MatchesGlobPattern(fileName, pattern))
                    return true;
            }

            return false;
        }

        internal static bool InheritsFromAny(INamedTypeSymbol typeSymbol, IReadOnlyCollection<string> excludedBaseTypes)
        {
            var baseType = typeSymbol.BaseType;
            while (baseType != null)
            {
                var fullName = baseType.ToDisplayString();
                foreach (var excludedType in excludedBaseTypes)
                {
                    if (string.Equals(fullName, excludedType, StringComparison.Ordinal))
                        return true;
                }

                baseType = baseType.BaseType;
            }

            return false;
        }

        internal static IReadOnlyList<string> ParseExcludedFilePatterns(AnalyzerConfigOptions options, string optionKey)
        {
            if (options.TryGetValue(optionKey, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
            }

            return DefaultExcludedFilePatterns;
        }

        internal static IReadOnlyList<string> ParseExcludedBaseTypes(AnalyzerConfigOptions options, string optionKey)
        {
            if (options.TryGetValue(optionKey, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => s.Length > 0)
                    .ToList();
            }

            return DefaultExcludedBaseTypes;
        }

        internal static List<BaseTypeDeclarationSyntax> GetTopLevelTypeDeclarations(SyntaxNode root)
        {
            var types = new List<BaseTypeDeclarationSyntax>();

            foreach (var member in root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (member.Parent is BaseNamespaceDeclarationSyntax || member.Parent is CompilationUnitSyntax)
                    types.Add(member);
            }

            return types;
        }
    }
}