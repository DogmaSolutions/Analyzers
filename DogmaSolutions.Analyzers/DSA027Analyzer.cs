using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
// ReSharper disable once InconsistentNaming
public sealed class DSA027Analyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "DSA027";
    public const string VariableNameProperty = "VariableName";

    private static readonly LocalizableString _title =
        new LocalizableResourceString(nameof(Resources.DSA027AnalyzerTitle), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _messageFormat =
        new LocalizableResourceString(nameof(Resources.DSA027AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));

    private static readonly LocalizableString _description =
        new LocalizableResourceString(nameof(Resources.DSA027AnalyzerDescription), Resources.ResourceManager, typeof(Resources));

    private const string Category = RuleCategories.Performance;

    private static readonly DiagnosticDescriptor _rule = new(
        DiagnosticId,
        _title,
        _messageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: _description,
        helpLinkUri: "https://github.com/DogmaSolutions/Analyzers?tab=readme-ov-file#DSA027");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [_rule];

    public override void Initialize(AnalysisContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSyntaxNodeAction(AnalyzeAssignment,
            SyntaxKind.AddAssignmentExpression,
            SyntaxKind.SimpleAssignmentExpression);
    }

    private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
    {
        var assignment = (AssignmentExpressionSyntax)context.Node;

        var leftType = context.SemanticModel.GetTypeInfo(assignment.Left, context.CancellationToken).Type;
        if (leftType == null || leftType.SpecialType != SpecialType.System_String)
            return;

        var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol;
        if (leftSymbol == null)
            return;

        if (leftSymbol is not ILocalSymbol && leftSymbol is not IParameterSymbol)
            return;

        if (assignment.IsKind(SyntaxKind.SimpleAssignmentExpression))
        {
            if (!HasSelfReferenceInAddChain(assignment.Right, leftSymbol, context.SemanticModel))
                return;
        }

        var loop = FindEnclosingLoop(assignment);
        if (loop == null)
            return;

        var loopBody = GetLoopBody(loop);
        if (loopBody == null)
            return;

        if (IsDeclaredInsideNode(leftSymbol, loopBody))
            return;

        var properties = ImmutableDictionary.CreateBuilder<string, string>();
        properties.Add(VariableNameProperty, leftSymbol.Name);

        context.ReportDiagnostic(Diagnostic.Create(
            _rule,
            assignment.GetLocation(),
            properties.ToImmutable(),
            leftSymbol.Name));
    }

    private static bool HasSelfReferenceInAddChain(ExpressionSyntax expr, ISymbol selfSymbol, SemanticModel model)
    {
        if (expr is not BinaryExpressionSyntax binary || !binary.IsKind(SyntaxKind.AddExpression))
            return false;

        return ContainsSelfReference(binary, selfSymbol, model);
    }

    private static bool ContainsSelfReference(ExpressionSyntax expr, ISymbol selfSymbol, SemanticModel model)
    {
        if (expr is BinaryExpressionSyntax binary && binary.IsKind(SyntaxKind.AddExpression))
            return ContainsSelfReference(binary.Left, selfSymbol, model) ||
                   ContainsSelfReference(binary.Right, selfSymbol, model);

        if (expr is IdentifierNameSyntax identifier)
        {
            var symbol = model.GetSymbolInfo(identifier).Symbol;
            return SymbolEqualityComparer.Default.Equals(symbol, selfSymbol);
        }

        return false;
    }

    private static SyntaxNode FindEnclosingLoop(SyntaxNode node)
    {
        for (var current = node.Parent; current != null; current = current.Parent)
        {
            if (current is ParenthesizedLambdaExpressionSyntax or
                SimpleLambdaExpressionSyntax or
                AnonymousMethodExpressionSyntax or
                LocalFunctionStatementSyntax)
                return null;

            if (current is ForStatementSyntax or
                ForEachStatementSyntax or
                ForEachVariableStatementSyntax or
                WhileStatementSyntax or
                DoStatementSyntax)
                return current;
        }

        return null;
    }

    internal static SyntaxNode GetLoopBody(SyntaxNode loop)
    {
        return loop switch
        {
            ForStatementSyntax f => f.Statement,
            ForEachStatementSyntax fe => fe.Statement,
            ForEachVariableStatementSyntax fev => fev.Statement,
            WhileStatementSyntax w => w.Statement,
            DoStatementSyntax d => d.Statement,
            _ => null
        };
    }

    private static bool IsDeclaredInsideNode(ISymbol symbol, SyntaxNode container)
    {
        foreach (var syntaxRef in symbol.DeclaringSyntaxReferences)
        {
            var declNode = syntaxRef.GetSyntax();
            if (container.Span.Contains(declNode.Span))
                return true;
        }

        return false;
    }
}
