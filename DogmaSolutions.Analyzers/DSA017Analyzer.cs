using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Detects "if not exists, then insert" check-then-act patterns on collection types
/// that offer an atomic alternative (e.g., Dictionary.TryAdd, HashSet.Add returning bool,
/// ConcurrentDictionary.GetOrAdd). The check is redundant and the pattern is prone to
/// TOCTOU race conditions in multithreaded code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA017Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA017";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA017AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA017AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA017AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Design;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA017");

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

        // Only fire for collection types with atomic alternatives
        var receiverType = CheckThenActUtils.ResolveReceiverType(receiver, context.SemanticModel);
        if (CheckThenActUtils.CategorizeReceiverType(receiverType) != CheckThenActUtils.ReceiverCategory.AtomicAlternative)
            return;

        CheckThenActUtils.HasAtomicAlternative(receiverType, out var suggestion);
        var typeName = receiverType?.Name ?? "collection";

        // For set-like types (HashSet, SortedSet, etc.) whose only atomic alternative is
        // "Add returns bool", suppress when the body contains logic beyond the Add call.
        // The check-then-act pattern is often used as a cache guard to avoid redundant
        // computation, and these types don't offer a "get-or-add" strategy.
        if (IsSetLikeType(typeName) && HasComplexInsertBody(ifStatement))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: ifStatement.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null,
            typeName,
            suggestion ?? "the type's atomic operation");
        context.ReportDiagnostic(diagnostic);
    }

    private static bool IsSetLikeType(string typeName)
    {
        return typeName == "HashSet" || typeName == "SortedSet" ||
               typeName == "ImmutableHashSet" || typeName == "ImmutableSortedSet";
    }

    /// <summary>
    /// Returns true if the if-body (or else-body) that contains the insert call also
    /// contains other executable statements beyond the insert itself. This indicates a
    /// cache/guard pattern where the check gates expensive computation, not just deduplication.
    /// </summary>
    private static bool HasComplexInsertBody(IfStatementSyntax ifStatement)
    {
        // Pattern A: if (!contains) { add; possibly other logic; }
        if (HasInsertWithOtherLogic(ifStatement.Statement))
            return true;

        // Pattern C: if (contains) { ... } else { add; possibly other logic; }
        if (ifStatement.Else != null && HasInsertWithOtherLogic(ifStatement.Else.Statement))
            return true;

        return false;
    }

    private static bool HasInsertWithOtherLogic(StatementSyntax body)
    {
        // Single statement (no block) — always simple (just the Add call)
        if (body is not BlockSyntax block)
            return false;

        // Check if the block has more than one statement
        // A block with only the Add call is simple; anything else means complex logic
        return block.Statements.Count > 1;
    }
}
