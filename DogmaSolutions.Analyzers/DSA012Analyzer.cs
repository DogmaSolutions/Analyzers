using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Avoid "if not exists, then insert" check-then-act antipattern (TOCTOU) on database types.
/// Fires only when the receiver implements IQueryable (e.g., DbSet, IQueryable).
/// For in-memory collections with atomic alternatives, see DSA017.
/// For in-memory collections without atomic alternatives, see DSA018.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA012Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA012";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA012AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA012AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA012AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Design;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA012.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeIfStatement, SyntaxKind.IfStatement);
    }

    private static void AnalyzeIfStatement(SyntaxNodeAnalysisContext context)
    {
        var ifStatement = (IfStatementSyntax)context.Node;

        if (!CheckThenActUtils.TryMatchCheckThenAct(ifStatement, out var receiver))
            return;

        // Only fire for database types (IQueryable, DbSet)
        var receiverType = CheckThenActUtils.ResolveReceiverType(receiver, context.SemanticModel);
        if (CheckThenActUtils.CategorizeReceiverType(receiverType) != CheckThenActUtils.ReceiverCategory.Database)
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: ifStatement.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null);
        context.ReportDiagnostic(diagnostic);
    }
}
