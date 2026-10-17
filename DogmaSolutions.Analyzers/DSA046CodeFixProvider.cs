using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers;

/// <summary>
/// Turns <c>x++</c>, <c>x--</c>, <c>x += v</c> and <c>x -= v</c> on an <c>int</c> or <c>uint</c> field into the matching <c>Interlocked</c> call. The fix is
/// offered only when the operation is a statement of its own (when the value of the expression is used, the result of <c>Interlocked</c> is not the same),
/// the other cases get the review comment only.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DSA046CodeFixProvider))]
[Shared]
// ReSharper disable once InconsistentNaming
public sealed class DSA046CodeFixProvider : CodeFixProvider
{
    internal const string UseInterlockedEquivalenceKey = DSA046Analyzer.DiagnosticId + "_UseInterlocked";

    public override ImmutableArray<string> FixableDiagnosticIds => [DSA046Analyzer.DiagnosticId];

    public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        var diagnostic = context.Diagnostics[0];
        if (root?.FindNode(diagnostic.Location.SourceSpan) is ExpressionSyntax expression)
            await RegisterFixesAsync(context, diagnostic, expression).ConfigureAwait(false);
    }

    private static async Task RegisterFixesAsync(CodeFixContext context, Diagnostic diagnostic, ExpressionSyntax expression)
    {
        var model = await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false);
        var replacement = model == null ? null : CreateInterlockedCall(expression, model, context.CancellationToken);
        if (replacement != null)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: "Use " + replacement.Value.Name,
                    createChangedDocument: ct => ReplaceAsync(context.Document, expression, replacement.Value.Call, ct),
                    equivalenceKey: UseInterlockedEquivalenceKey),
                diagnostic);
        }

        ReviewCommentCodeFix.Register(context, diagnostic, expression, DSA046Analyzer.DiagnosticId, nameof(Resources.DSA046ReviewComment));
    }

    private static (string Name, ExpressionSyntax Call)? CreateInterlockedCall(ExpressionSyntax expression, SemanticModel model, CancellationToken cancellationToken)
    {
        if (expression.Parent is not ExpressionStatementSyntax)
            return null;

        switch (expression)
        {
            case PrefixUnaryExpressionSyntax prefix:
                return CreateIncrement(expression, prefix.Operand, prefix.IsKind(SyntaxKind.PreIncrementExpression), model, cancellationToken);
            case PostfixUnaryExpressionSyntax postfix:
                return CreateIncrement(expression, postfix.Operand, postfix.IsKind(SyntaxKind.PostIncrementExpression), model, cancellationToken);
            case AssignmentExpressionSyntax assignment when assignment.IsKind(SyntaxKind.AddAssignmentExpression) || assignment.IsKind(SyntaxKind.SubtractAssignmentExpression):
                return CreateAdd(assignment, model, cancellationToken);
            default:
                return null;
        }
    }

    private static (string Name, ExpressionSyntax Call)? CreateIncrement(ExpressionSyntax expression, ExpressionSyntax target, bool increment, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!IsInterlockedField(target, model, cancellationToken))
            return null;

        var name = increment ? "Increment" : "Decrement";
        return ("Interlocked." + name, Call(name, target));
    }

    private static (string Name, ExpressionSyntax Call)? CreateAdd(AssignmentExpressionSyntax assignment, SemanticModel model, CancellationToken cancellationToken)
    {
        if (!IsInterlockedField(assignment.Left, model, cancellationToken))
            return null;

        ExpressionSyntax amount = assignment.Right;
        if (assignment.IsKind(SyntaxKind.SubtractAssignmentExpression))
        {
            // an unsigned amount cannot be negated
            if (model.GetTypeInfo(assignment.Left, cancellationToken).Type?.SpecialType != SpecialType.System_Int32)
                return null;

            amount = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.UnaryMinusExpression, SyntaxFactory.ParenthesizedExpression(assignment.Right.WithoutTrivia()));
        }

        return ("Interlocked.Add", Call("Add", assignment.Left, amount));
    }

    private static bool IsInterlockedField(ExpressionSyntax target, SemanticModel model, CancellationToken cancellationToken) =>
        model.GetSymbolInfo(target, cancellationToken).Symbol is IFieldSymbol field && DSA046Analyzer.GetKind(field.Type) == DSA046FieldKind.Interlocked && HasInterlockedOverload(model.Compilation, field.Type);

    /// <summary>The unsigned overloads of Interlocked.Add, Increment and Decrement are not in every framework (they are not in .NET Framework nor in .NET Standard 2.0): the fix would not compile.</summary>
    private static bool HasInterlockedOverload(Compilation compilation, ITypeSymbol type) =>
        compilation.GetTypeByMetadataName("System.Threading.Interlocked")?.GetMembers("Add").OfType<IMethodSymbol>()
            .Any(method => method.Parameters.Length == 2 && SymbolEqualityComparer.Default.Equals(method.Parameters[0].Type, type)) == true;

    private static ExpressionSyntax Call(string method, ExpressionSyntax target, ExpressionSyntax amount = null)
    {
        var arguments = SyntaxFactory.SingletonSeparatedList(SyntaxFactory.Argument(null, SyntaxFactory.Token(SyntaxKind.RefKeyword), target.WithoutTrivia()));
        if (amount != null)
            arguments = arguments.Add(SyntaxFactory.Argument(amount.WithoutTrivia()));

        return SyntaxFactory.InvocationExpression(
            SyntaxFactory.ParseExpression("System.Threading.Interlocked." + method),
            SyntaxFactory.ArgumentList(arguments));
    }

    private static async Task<Document> ReplaceAsync(Document document, ExpressionSyntax expression, ExpressionSyntax call, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        return document.WithSyntaxRoot(root.ReplaceNode(expression, call.WithTriviaFrom(expression)));
    }
}
