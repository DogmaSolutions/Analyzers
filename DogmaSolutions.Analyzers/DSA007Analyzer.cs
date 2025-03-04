using System;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Potential non-deterministic point-in-time execution
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    // ReSharper disable once InconsistentNaming
    public sealed class DSA007Analyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "DSA007";
        private static readonly LocalizableString _title = new LocalizableResourceString(nameof(Resources.DSA007AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _messageFormat =
            new LocalizableResourceString(nameof(Resources.DSA007AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

        private static readonly LocalizableString _description =
            new LocalizableResourceString(nameof(Resources.DSA007AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

        private const string Category = RuleCategories.CodeSmell;

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
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeMethodDeclaration, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethodDeclaration(SyntaxNodeAnalysisContext context)
        {
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;

            // Find all field assignments within the method body
            var fieldAssignments = methodDeclaration.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(a => IsFieldAccess(a.Left, context.SemanticModel));

            foreach (var assignment in fieldAssignments) // i.e.: _theField = "A value";
            {
                // i.e.: if(_theField == null)
                var innerIfStatement = SearchTheFirstConditionalStatementCheckingIfTheFieldIsNull(assignment, context.SemanticModel);
                if (innerIfStatement != null) // if this is a conditional assignment of a class field, we must ensure that it's properly locked
                {
                    // Check if the field assignment is not within a lock statement
                    if (!IsWithinGuardedLockStatement(
                            innerIfStatement,
                            assignment,
                            context.SemanticModel)) // i.e. NOT   if(_theField == null) lock(_theLock) { if(_theField == null) { ... } }
                    {
                        var diagnostic = Diagnostic.Create(
                            _rule,
                            assignment.GetLocation(),
                            effectiveSeverity:  context.GetDiagnosticSeverity(_rule),
                            additionalLocations: null,
                            properties: null);
                        
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }

        private static bool IsFieldAccess(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression is MemberAccessExpressionSyntax memberAccessExpression)
            {
                var symbol = semanticModel.GetSymbolInfo(memberAccessExpression.Name).Symbol;
                return symbol is IFieldSymbol;
            }
            else if (expression is IdentifierNameSyntax identifierName)
            {
                var symbol = semanticModel.GetSymbolInfo(identifierName).Symbol;
                return symbol is IFieldSymbol;
            }

            return false;
        }

        private static IfStatementSyntax? SearchTheFirstConditionalStatementCheckingIfTheFieldIsNull(AssignmentExpressionSyntax assignment, SemanticModel semanticModel)
        {
            // Simplified check for null check before assignment
            // This can be further refined based on specific needs
            var parent = assignment.Parent;

            while (parent != null)
            {
                if (parent is IfStatementSyntax ifStatement)
                {
                    if (ifStatement.Condition is BinaryExpressionSyntax binaryExpression &&
                        binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                        IsFieldAccess(binaryExpression.Left, semanticModel) &&
                        SymbolEqualityComparer.Default.Equals(GetFieldSymbol(binaryExpression.Left, semanticModel), GetFieldSymbol(assignment.Left, semanticModel)) &&
                        binaryExpression.Right is LiteralExpressionSyntax &&
                        ((LiteralExpressionSyntax)binaryExpression.Right).Token.ValueText == "null")
                    {
                        return ifStatement;
                    }
                }

                parent = parent.Parent;
            }

            return null;
        }

        private static IFieldSymbol? GetFieldSymbol(ExpressionSyntax expression, SemanticModel semanticModel)
        {
            if (expression is MemberAccessExpressionSyntax memberAccess)
            {
                return semanticModel.GetSymbolInfo(memberAccess.Name).Symbol as IFieldSymbol;
            }

            if (expression is IdentifierNameSyntax identifierName)
            {
                return semanticModel.GetSymbolInfo(identifierName).Symbol as IFieldSymbol;
            }

            return null;
        }
/*
        private static bool IsWithinLockStatement( AssignmentExpressionSyntax assignment, SemanticModel semanticModel)
        {
            var ancestor = assignment.AncestorsAndSelf().OfType<LockStatementSyntax>().FirstOrDefault();
            return ancestor != null;
        }*/

        private static bool IsWithinGuardedLockStatement(IfStatementSyntax innerIfStatement, AssignmentExpressionSyntax assignment, SemanticModel semanticModel)
        {
            var ancestor = innerIfStatement.AncestorsAndSelf().OfType<LockStatementSyntax>().FirstOrDefault();
            if (ancestor == null)
            {
                return false;
            }

            // Check if the LockStatement is within an IfStatement with a null check
            var lockStatementParent = ancestor.Parent;
            while (lockStatementParent != null)
            {
                if (lockStatementParent is IfStatementSyntax ifStatement)
                {
                    if (ifStatement.Condition is BinaryExpressionSyntax binaryExpression &&
                        binaryExpression.Kind() == SyntaxKind.EqualsExpression &&
                        IsFieldAccess(binaryExpression.Left, semanticModel) &&
                        SymbolEqualityComparer.Default.Equals(GetFieldSymbol(binaryExpression.Left, semanticModel), GetFieldSymbol(assignment.Left, semanticModel)) &&
                        binaryExpression.Right is LiteralExpressionSyntax &&
                        ((LiteralExpressionSyntax)binaryExpression.Right).Token.ValueText == "null")
                    {
                        return true;
                    }
                }

                lockStatementParent = lockStatementParent.Parent;
            }

            return false;
        }
    }
}