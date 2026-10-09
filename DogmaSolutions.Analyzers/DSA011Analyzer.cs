using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Avoid lazily initialized, self-contained, static singleton properties
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA011Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA011";
    private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA011AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA011AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA011AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Design;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/DSA011.md");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzePropertyDeclaration, SyntaxKind.PropertyDeclaration);
    }

    private static void AnalyzePropertyDeclaration(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;

        // not a static property ?
        if (!propertyDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword))
            return;

        if (propertyDeclaration.ExpressionBody != null)
        {
            // Check for expression-bodied property (??=)
            AnalyzeExpressionBody(context, propertyDeclaration);
        }
        else if (propertyDeclaration.AccessorList != null)
        {
            // Check for get accessor with if (_instance == null)
            AnalyzeGetAccessor(context, propertyDeclaration);
            AnalyzeGetAccessor2(context, propertyDeclaration);
        }
    }

    private static void AnalyzeExpressionBody(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        var expression = propertyDeclaration.ExpressionBody?.Expression;

        // not an assignment ?
        if (expression is not AssignmentExpressionSyntax assignmentExpression || assignmentExpression.Kind() != SyntaxKind.CoalesceAssignmentExpression)
            return;

        // not an identifier on the left side ?
        if (assignmentExpression.Left is not IdentifierNameSyntax leftIdentifier)
            return;

        // get the referred field
        var symbol = context.SemanticModel.GetSymbolInfo(leftIdentifier).Symbol;

        // not a private static field ?
        if (symbol is not IFieldSymbol { IsStatic: true } fieldSymbol)
            return;

        // get the class type
        var containingType = fieldSymbol.ContainingType;
        var containingProperty = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

        // not the same class containing the field ?
        if (containingType == null || containingProperty == null || !SymbolEqualityComparer.Default.Equals(containingType, containingProperty.ContainingType))
            return;

        // Matched
        var diagnostic = Diagnostic.Create(_rule, propertyDeclaration.GetLocation(), propertyDeclaration.Identifier.ToString());
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeGetAccessor(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        var getAccessor = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        if (getAccessor?.Body?.Statements == null)
            return;

        // get { if (_instance == null) _instance = ...; ... }
        foreach (var ifStatement in getAccessor.Body.Statements.OfType<IfStatementSyntax>())
        {
            if (TryGetStaticFieldComparedWithNull(ifStatement, SyntaxKind.EqualsExpression, context.SemanticModel, out var fieldSymbol) &&
                IsAssignmentToField(ifStatement.Statement, fieldSymbol, context.SemanticModel))
            {
                ReportDiagnosticIfMatchingType(context, propertyDeclaration, fieldSymbol);
            }
        }
    }

    /// <summary>
    /// <c>if (_field == null)</c> or <c>if (_field != null)</c> (depending on the kind of comparison) on a static field.
    /// </summary>
    private static bool TryGetStaticFieldComparedWithNull(
        IfStatementSyntax ifStatement,
        SyntaxKind comparisonKind,
        SemanticModel semanticModel,
        out IFieldSymbol fieldSymbol)
    {
        fieldSymbol = null;

        if (ifStatement.Condition is not BinaryExpressionSyntax binaryExpression ||
            binaryExpression.Kind() != comparisonKind ||
            binaryExpression.Right is not LiteralExpressionSyntax literalExpression ||
            literalExpression.Kind() != SyntaxKind.NullLiteralExpression ||
            binaryExpression.Left is not IdentifierNameSyntax leftIdentifier)
            return false;

        fieldSymbol = semanticModel.GetSymbolInfo(leftIdentifier).Symbol as IFieldSymbol;
        if (fieldSymbol is { IsStatic: true })
            return true;

        fieldSymbol = null;
        return false;
    }

    /// <summary>
    /// The statement is <c>_field = ...;</c> for the given field.
    /// </summary>
    private static bool IsAssignmentToField(StatementSyntax statement, IFieldSymbol fieldSymbol, SemanticModel semanticModel)
    {
        if (statement is not ExpressionStatementSyntax { Expression: AssignmentExpressionSyntax { Left: IdentifierNameSyntax assignmentLeftIdentifier } })
            return false;

        var assignmentLeftSymbol = semanticModel.GetSymbolInfo(assignmentLeftIdentifier).Symbol;
        return assignmentLeftSymbol != null && SymbolEqualityComparer.Default.Equals(assignmentLeftSymbol, fieldSymbol);
    }

    private static void AnalyzeGetAccessor2(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        var getAccessor = propertyDeclaration.AccessorList?.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        if (!(getAccessor?.Body?.Statements.Count >= 2) || getAccessor.Body.Statements[0] is not IfStatementSyntax ifStatement)
            return;

        // get { if (_instance != null) return _instance; _instance = ...; ... }
        if (TryGetStaticFieldComparedWithNull(ifStatement, SyntaxKind.NotEqualsExpression, context.SemanticModel, out var fieldSymbol) &&
            IsAssignmentToField(getAccessor.Body.Statements[1], fieldSymbol, context.SemanticModel))
        {
            ReportDiagnosticIfMatchingType(context, propertyDeclaration, fieldSymbol);
        }
    }

    private static void ReportDiagnosticIfMatchingType(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration, IFieldSymbol fieldSymbol)
    {
        var containingType = fieldSymbol.ContainingType;
        var containingProperty = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

        if (containingType == null || containingProperty == null || !SymbolEqualityComparer.Default.Equals(containingType, containingProperty.ContainingType))
            return;

        var diagnostic = Diagnostic.Create(_rule, propertyDeclaration.GetLocation(), propertyDeclaration.Identifier.ToString());
        context.ReportDiagnostic(diagnostic);
    }
}