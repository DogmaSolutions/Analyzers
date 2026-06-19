using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA033Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA033";
    internal const string MaxLinesOptionKey = "dotnet_diagnostic.DSA033.max_lines";
    internal const int DefaultMaxLines = 500;

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
        context.RegisterSyntaxTreeAction(AnalyzeSyntaxTree);
    }

    private static void AnalyzeSyntaxTree(SyntaxTreeAnalysisContext context)
    {
        var text = context.Tree.GetText(context.CancellationToken);
        var lineCount = text.Lines.Count;

        var options = context.Options.AnalyzerConfigOptionsProvider.GetOptions(context.Tree);
        var maxLines = DefaultMaxLines;
        if (options.TryGetValue(MaxLinesOptionKey, out var value) &&
            int.TryParse(value, out var parsed) &&
            parsed > 0)
        {
            maxLines = parsed;
        }

        if (lineCount <= maxLines)
            return;

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