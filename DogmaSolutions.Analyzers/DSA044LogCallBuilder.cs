using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Builds the Microsoft.Extensions.Logging call that replaces a console or debug write: <c>Console.WriteLine("x {0}", y)</c> becomes
    /// <c>logger.LogInformation("x {0}", y)</c>, <c>Console.WriteLine($"x {y}")</c> becomes <c>logger.LogInformation("x {Y}", y)</c> and
    /// any other single argument becomes a value of a template. Syntax only: it knows nothing of the types.
    /// </summary>
    internal static class DSA044LogCallBuilder
    {
        /// <summary>The replacement of <paramref name="invocation"/>, or null when its arguments have no obvious logging equivalent.</summary>
        internal static InvocationExpressionSyntax Build(InvocationExpressionSyntax invocation, string loggerName, string level)
        {
            var arguments = CreateArguments(invocation.ArgumentList.Arguments);
            if (arguments == null)
                return null;

            var method = SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName(loggerName),
                SyntaxFactory.IdentifierName("Log" + level));

            return SyntaxFactory.InvocationExpression(method, SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)))
                .WithTriviaFrom(invocation);
        }

        private static IEnumerable<ArgumentSyntax> CreateArguments(SeparatedSyntaxList<ArgumentSyntax> original)
        {
            if (original.Count == 0 || original.Any(argument => argument.NameColon != null || !argument.RefKindKeyword.IsKind(SyntaxKind.None)))
                return null;

            var first = original[0].Expression;
            if (original.Count > 1)
                return IsStringLiteral(first) ? original : null;

            return first switch
            {
                LiteralExpressionSyntax literal when literal.IsKind(SyntaxKind.StringLiteralExpression) => original,
                InterpolatedStringExpressionSyntax interpolated => FromInterpolatedString(interpolated),
                _ => ValueOfTemplate("{Value}", first)
            };
        }

        private static bool IsStringLiteral(ExpressionSyntax expression) =>
            expression is LiteralExpressionSyntax literal && literal.IsKind(SyntaxKind.StringLiteralExpression);

        // ---- interpolated strings ---------------------------------------------------------------------------------

        /// <summary>
        /// <c>$"read {count:N0} of {file.Name}"</c> becomes the template <c>"read {Count:N0} of {Name}"</c> and the values
        /// <c>count</c> and <c>file.Name</c>. A string that is not a plain <c>$"..."</c> one (verbatim, raw), or that has an alignment, keeps
        /// its text as a single value.
        /// </summary>
        private static IEnumerable<ArgumentSyntax> FromInterpolatedString(InterpolatedStringExpressionSyntax interpolated)
        {
            var interpolations = interpolated.Contents.OfType<InterpolationSyntax>().ToList();
            if (!interpolated.StringStartToken.IsKind(SyntaxKind.InterpolatedStringStartToken) || interpolations.Any(interpolation => interpolation.AlignmentClause != null))
                return ValueOfTemplate("{Value}", interpolated);

            var template = new StringBuilder();
            foreach (var content in interpolated.Contents)
                template.Append(content is InterpolationSyntax interpolation ? ToPlaceholder(interpolation) : ((InterpolatedStringTextSyntax)content).TextToken.Text);

            var values = interpolations.Select(interpolation => SyntaxFactory.Argument(interpolation.Expression.WithoutTrivia()));
            // the text of the strings is copied as it is written in the source, escapes included
            return new[] { SyntaxFactory.Argument(SyntaxFactory.ParseExpression("\"" + template + "\"")) }.Concat(values);
        }

        /// <summary>The placeholder of a hole: <c>{Name}</c> or <c>{Name:format}</c>, named after the last identifier of the expression.</summary>
        private static string ToPlaceholder(InterpolationSyntax interpolation)
        {
            var name = GetPlaceholderName(interpolation.Expression);
            var format = interpolation.FormatClause == null ? string.Empty : ":" + interpolation.FormatClause.FormatStringToken.Text;
            return "{" + name + format + "}";
        }

        private static string GetPlaceholderName(ExpressionSyntax expression)
        {
            var identifier = expression switch
            {
                IdentifierNameSyntax name => name.Identifier.ValueText,
                MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
                _ => null
            };

            return string.IsNullOrEmpty(identifier) ? "Value" : char.ToUpperInvariant(identifier[0]) + identifier.Substring(1);
        }

        private static IEnumerable<ArgumentSyntax> ValueOfTemplate(string template, ExpressionSyntax value) => new[]
        {
            SyntaxFactory.Argument(SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal("\"" + template + "\"", template))),
            SyntaxFactory.Argument(value.WithoutTrivia()),
        };
    }
}
