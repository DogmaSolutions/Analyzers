using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Detects "if not exists, then insert" check-then-act patterns on collection types
/// that do not offer an atomic alternative (e.g., List, ICollection, LinkedList, Queue).
/// The pattern is prone to TOCTOU race conditions in multithreaded code and should be
/// protected with a lock or SemaphoreSlim, or the collection should be replaced with a
/// type that provides built-in duplicate handling.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA018Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA018";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA018AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA018AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA018AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Design;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA018");

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

        // Only fire for collection types without atomic alternatives (and unknown types)
        var receiverType = CheckThenActUtils.ResolveReceiverType(receiver, context.SemanticModel);
        if (CheckThenActUtils.CategorizeReceiverType(receiverType) != CheckThenActUtils.ReceiverCategory.NoAtomicAlternative)
            return;

        // Skip if already protected by a lock statement — this is exactly the fix DSA018 recommends
        if (IsInsideLockStatement(ifStatement))
            return;

        var diagnostic = Diagnostic.Create(
            descriptor: _rule,
            location: ifStatement.GetLocation(),
            effectiveSeverity: context.GetDiagnosticSeverity(_rule),
            additionalLocations: null,
            properties: null);
        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Checks whether the if statement is already inside a lock block, which provides
    /// the thread-safety that DSA018 recommends. In that case, the diagnostic is suppressed.
    /// </summary>
    private static bool IsInsideLockStatement(IfStatementSyntax ifStatement)
    {
        var current = ifStatement.Parent;
        while (current != null)
        {
            if (current is LockStatementSyntax)
                return true;

            // Stop at method/function boundaries
            if (current is MethodDeclarationSyntax ||
                current is LocalFunctionStatementSyntax ||
                current is SimpleLambdaExpressionSyntax ||
                current is ParenthesizedLambdaExpressionSyntax ||
                current is AnonymousMethodExpressionSyntax ||
                current is ConstructorDeclarationSyntax ||
                current is AccessorDeclarationSyntax)
                break;

            current = current.Parent;
        }

        return false;
    }
}
