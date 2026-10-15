using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The size and complexity of a class, all its parts (partial declarations) included, nested types excluded.</summary>
    internal readonly struct TypeMetrics
    {
        internal TypeMetrics(int cyclomaticComplexity, int maxMethodComplexity, int lines, int methods, int maxConstructorParameters, ActivityKinds syntacticActivity)
        {
            CyclomaticComplexity = cyclomaticComplexity;
            MaxMethodComplexity = maxMethodComplexity;
            Lines = lines;
            Methods = methods;
            MaxConstructorParameters = maxConstructorParameters;
            SyntacticActivity = syntacticActivity;
        }

        /// <summary>Sum of the cyclomatic complexity of every method, constructor, operator and accessor.</summary>
        internal int CyclomaticComplexity { get; }

        /// <summary>Cyclomatic complexity of the most complex of them.</summary>
        internal int MaxMethodComplexity { get; }

        internal int Lines { get; }

        /// <summary>Methods, operators and conversion operators (constructors and accessors excluded).</summary>
        internal int Methods { get; }

        internal int MaxConstructorParameters { get; }

        /// <summary>The activity kinds (catch, await, throw) found in the code without any semantic query.</summary>
        internal ActivityKinds SyntacticActivity { get; }
    }

    /// <summary>
    /// Measures a class with the syntax only: no semantic model is needed, so it costs a single walk over the bodies of the class.
    /// </summary>
    internal static class DSA043TypeMetrics
    {
        // each of these adds one path through the code
        private static readonly HashSet<SyntaxKind> DecisionKinds = new()
        {
            SyntaxKind.IfStatement, SyntaxKind.ForStatement, SyntaxKind.ForEachStatement, SyntaxKind.ForEachVariableStatement,
            SyntaxKind.WhileStatement, SyntaxKind.DoStatement, SyntaxKind.CaseSwitchLabel, SyntaxKind.CasePatternSwitchLabel,
            SyntaxKind.SwitchExpressionArm, SyntaxKind.CatchClause, SyntaxKind.CatchFilterClause, SyntaxKind.ConditionalExpression,
            SyntaxKind.LogicalAndExpression, SyntaxKind.LogicalOrExpression, SyntaxKind.CoalesceExpression,
            SyntaxKind.CoalesceAssignmentExpression, SyntaxKind.AndPattern, SyntaxKind.OrPattern,
        };

        private static readonly Dictionary<SyntaxKind, ActivityKinds> ActivityByKind = new()
        {
            [SyntaxKind.CatchClause] = ActivityKinds.Catch,
            [SyntaxKind.AwaitExpression] = ActivityKinds.Await,
            [SyntaxKind.ThrowStatement] = ActivityKinds.Throw,
            [SyntaxKind.ThrowExpression] = ActivityKinds.Throw,
        };

        internal static TypeMetrics Compute(INamedTypeSymbol type, bool countBlankLines, CancellationToken cancellationToken)
        {
            var declarations = GetDeclarations(type, cancellationToken);
            var units = declarations.SelectMany(GetExecutableUnits).Select(Measure).ToList();

            return new TypeMetrics(
                cyclomaticComplexity: units.Sum(unit => unit.Complexity),
                maxMethodComplexity: units.Select(unit => unit.Complexity).DefaultIfEmpty(0).Max(),
                lines: declarations.Sum(declaration => CountLines(declaration, countBlankLines)),
                methods: declarations.Sum(declaration => declaration.Members.Count(IsMethodLike)),
                maxConstructorParameters: declarations.Select(GetMaxConstructorParameters).DefaultIfEmpty(0).Max(),
                syntacticActivity: units.Aggregate(ActivityKinds.None, (found, unit) => found | unit.Activity));
        }

        /// <summary>The parts of the class: one declaration per partial.</summary>
        internal static IReadOnlyList<TypeDeclarationSyntax> GetDeclarations(INamedTypeSymbol type, CancellationToken cancellationToken)
        {
            return type.DeclaringSyntaxReferences.Select(reference => reference.GetSyntax(cancellationToken)).OfType<TypeDeclarationSyntax>().ToArray();
        }

        // ---- executable units -------------------------------------------------------------------------------------

        private static IEnumerable<SyntaxNode> GetExecutableUnits(TypeDeclarationSyntax declaration)
        {
            return declaration.Members.SelectMany(GetUnitsOfMember);
        }

        /// <summary>A method-like member is one unit; a property or indexer has one unit per accessor (or for its expression body).</summary>
        private static IEnumerable<SyntaxNode> GetUnitsOfMember(MemberDeclarationSyntax member)
        {
            switch (member)
            {
                case BaseMethodDeclarationSyntax method:
                    return new[] { method };
                case BasePropertyDeclarationSyntax property:
                    return GetUnitsOfProperty(property);
                default:
                    return Enumerable.Empty<SyntaxNode>();
            }
        }

        private static IEnumerable<SyntaxNode> GetUnitsOfProperty(BasePropertyDeclarationSyntax property)
        {
            var expressionBody = (property as PropertyDeclarationSyntax)?.ExpressionBody ?? (property as IndexerDeclarationSyntax)?.ExpressionBody;
            if (expressionBody != null)
                return new SyntaxNode[] { expressionBody };

            return property.AccessorList?.Accessors.Cast<SyntaxNode>() ?? Enumerable.Empty<SyntaxNode>();
        }

        private static (int Complexity, ActivityKinds Activity) Measure(SyntaxNode unit)
        {
            var complexity = 1;
            var activity = ActivityKinds.None;
            foreach (var node in unit.DescendantNodes())
            {
                var kind = node.Kind();
                complexity += DecisionKinds.Contains(kind) ? 1 : 0;
                activity |= ActivityByKind.TryGetValue(kind, out var found) ? found : ActivityKinds.None;
            }

            return (complexity, activity);
        }

        // ---- counts -----------------------------------------------------------------------------------------------

        private static bool IsMethodLike(MemberDeclarationSyntax member) =>
            member is MethodDeclarationSyntax or OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax;

        private static int GetMaxConstructorParameters(TypeDeclarationSyntax declaration)
        {
            var primary = declaration.ParameterList?.Parameters.Count ?? 0;
            var explicitConstructors = declaration.Members.OfType<ConstructorDeclarationSyntax>().Select(ctor => ctor.ParameterList.Parameters.Count);
            return explicitConstructors.DefaultIfEmpty(0).Max(count => Math.Max(count, primary));
        }

        /// <summary>The lines of the declaration, from its first to its last token; blank lines only when asked.</summary>
        private static int CountLines(TypeDeclarationSyntax declaration, bool countBlankLines)
        {
            var text = declaration.SyntaxTree.GetText();
            var span = declaration.Span;
            var first = text.Lines.GetLineFromPosition(span.Start).LineNumber;
            var last = text.Lines.GetLineFromPosition(Math.Max(span.Start, span.End - 1)).LineNumber;

            return Enumerable.Range(first, last - first + 1).Count(number => countBlankLines || !IsBlank(text, text.Lines[number].Span));
        }

        private static bool IsBlank(SourceText text, TextSpan span) => Enumerable.Range(span.Start, span.Length).All(position => char.IsWhiteSpace(text[position]));
    }
}
