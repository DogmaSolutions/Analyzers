using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA033Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA033";
    internal const string MaxLinesOptionKey = "dotnet_diagnostic.DSA033.max_lines";
    internal const int DefaultMaxLines = 500;
    internal const string ExcludedFilePatternsOptionKey = "dotnet_diagnostic.DSA033.excluded_file_patterns";
    internal const string ExcludedBaseTypesOptionKey = "dotnet_diagnostic.DSA033.excluded_base_types";

    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA033AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat = new LocalizableResourceString(
        nameof(Resources.DSA033AnalyzerMessageFormat),
        Resources.ResourceManager,
        typeof(Resources));

    private static readonly LocalizableString _description = new LocalizableResourceString(
        nameof(Resources.DSA033AnalyzerDescription),
        Resources.ResourceManager,
        typeof(Resources));

    private const string Category = RuleCategories.CodeSmell;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA033.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new System.ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationStart =>
        {
            var compilation = compilationStart.Compilation;
            compilationStart.RegisterSyntaxTreeAction(treeContext =>
                AnalyzeSyntaxTree(treeContext, compilation));
        });
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context, Compilation compilation)
    {
        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);

        // 1. Check file name exclusion FIRST (cheapest)
        var excludedPatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsOptionKey);
        if (AnalyzersUtils.IsFileExcluded(context.Tree.FilePath, excludedPatterns))
            return;

        // 2. Check line count
        var text = context.Tree.GetText(context.CancellationToken);
        var lineCount = text.Lines.Count;

        var maxLines = DefaultMaxLines;
        if (options.TryGetValue(MaxLinesOptionKey, out var value) &&
            int.TryParse(value, out var parsed) &&
            parsed > 0)
        {
            maxLines = parsed;
        }

        if (lineCount <= maxLines)
            return;

        // 3. Check base type exclusion (needs semantic model)
        var excludedBaseTypes = AnalyzersUtils.ParseExcludedBaseTypes(options, ExcludedBaseTypesOptionKey);
        if (excludedBaseTypes.Count > 0)
        {
            var semanticModel = compilation.GetSemanticModel(context.Tree);
            var root = context.Tree.GetRoot(context.CancellationToken);
            var topLevelTypes = AnalyzersUtils.GetTopLevelTypeDeclarations(root);
            foreach (var typeDecl in topLevelTypes)
            {
                if (semanticModel.GetDeclaredSymbol(typeDecl, context.CancellationToken) is INamedTypeSymbol typeSymbol &&
                    AnalyzersUtils.InheritsFromAny(typeSymbol, excludedBaseTypes))
                    return;
            }
        }

        // 4. Report diagnostic
        var filePath = context.Tree.FilePath;
        var fileName = string.IsNullOrEmpty(filePath)
            ? "unknown"
            : System.IO.Path.GetFileName(filePath);

        var diagnostic = Diagnostic.Create(
            _rule,
            Location.Create(context.Tree, text.Lines[0].Span),
            fileName,
            lineCount,
            maxLines);
        context.ReportDiagnostic(diagnostic);
    }
}