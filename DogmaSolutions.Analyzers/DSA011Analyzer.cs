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
        description: _description);

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
        if (containingType == null || containingProperty == null || !containingType.Equals(containingProperty.ContainingType))
            return;

        // Matched
        var diagnostic = Diagnostic.Create(_rule, propertyDeclaration.GetLocation(), propertyDeclaration.Identifier.ToString());
        context.ReportDiagnostic(diagnostic);
    }

    private static void AnalyzeGetAccessor(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        var getAccessor = propertyDeclaration.AccessorList.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);

        if (getAccessor?.Body?.Statements != null)
        {
            foreach (var statement in getAccessor.Body.Statements)
            {
                if (statement is IfStatementSyntax ifStatement)
                {
                    if (ifStatement.Condition is BinaryExpressionSyntax binaryExpression &&
                        binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                        binaryExpression.Right is LiteralExpressionSyntax literalExpression &&
                        literalExpression.Kind() == SyntaxKind.NullLiteralExpression &&
                        binaryExpression.Left is IdentifierNameSyntax leftIdentifier)
                    {
                        var leftSymbol = context.SemanticModel.GetSymbolInfo(leftIdentifier).Symbol;

                        if (leftSymbol is IFieldSymbol fieldSymbol && fieldSymbol.IsStatic)
                        {
                            var assignment = ifStatement.Statement as ExpressionStatementSyntax;
                            if (assignment?.Expression is AssignmentExpressionSyntax assignmentExpression)
                            {
                                if (assignmentExpression.Left is IdentifierNameSyntax assignmentLeftIdentifier)
                                {
                                    var assignmentLeftSymbol = context.SemanticModel.GetSymbolInfo(assignmentLeftIdentifier).Symbol;
                                    if (assignmentLeftSymbol != null && assignmentLeftSymbol.Equals(leftSymbol))
                                    {
                                        var containingType = fieldSymbol.ContainingType;
                                        var containingProperty = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);
                                        if (containingType != null && containingProperty != null && containingType.Equals(containingProperty.ContainingType))
                                        {
                                            var diagnostic = Diagnostic.Create(_rule, propertyDeclaration.GetLocation(), propertyDeclaration.Identifier.ToString());
                                            context.ReportDiagnostic(diagnostic);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    private static void AnalyzeGetAccessor2(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration)
    {
        var getAccessor = propertyDeclaration.AccessorList?.Accessors.FirstOrDefault(a => a.Kind() == SyntaxKind.GetAccessorDeclaration);
        if (!(getAccessor?.Body?.Statements.Count >= 2) || getAccessor.Body.Statements[0] is not IfStatementSyntax ifStatement) 
            return;
        
        if (ifStatement.Condition is not BinaryExpressionSyntax binaryExpression ||
            binaryExpression.Kind() != SyntaxKind.NotEqualsExpression ||
            binaryExpression.Right is not LiteralExpressionSyntax literalExpression ||
            literalExpression.Kind() != SyntaxKind.NullLiteralExpression ||
            binaryExpression.Left is not IdentifierNameSyntax leftIdentifier)
            return;

        var leftSymbol = context.SemanticModel.GetSymbolInfo(leftIdentifier).Symbol;

        if (leftSymbol is not IFieldSymbol fieldSymbol || !fieldSymbol.IsStatic)
            return;

        if (getAccessor.Body.Statements[1] is not ExpressionStatementSyntax expressionStatement ||
            expressionStatement.Expression is not AssignmentExpressionSyntax assignmentExpression ||
            assignmentExpression.Left is not IdentifierNameSyntax assignmentLeftIdentifier)
            return;

        var assignmentLeftSymbol = context.SemanticModel.GetSymbolInfo(assignmentLeftIdentifier).Symbol;

        if (assignmentLeftSymbol != null && assignmentLeftSymbol.Equals(fieldSymbol))
        {
            ReportDiagnosticIfMatchingType(context, propertyDeclaration, fieldSymbol);
        }
    }

    private static void ReportDiagnosticIfMatchingType(SyntaxNodeAnalysisContext context, PropertyDeclarationSyntax propertyDeclaration, IFieldSymbol fieldSymbol)
    {
        var containingType = fieldSymbol.ContainingType;
        var containingProperty = context.SemanticModel.GetDeclaredSymbol(propertyDeclaration);

        if (containingType == null || containingProperty == null || !containingType.Equals(containingProperty.ContainingType))
            return;

        var diagnostic = Diagnostic.Create(_rule, propertyDeclaration.GetLocation(), propertyDeclaration.Identifier.ToString());
        context.ReportDiagnostic(diagnostic);
    }
}