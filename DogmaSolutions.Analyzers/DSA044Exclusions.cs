using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>Where a console or debug write is accepted even though a logger is available.</summary>
    internal static class DSA044Exclusions
    {
        /// <summary>Excluded by the name of the type, of its namespace, of the member, or by the file: no semantic query needed.</summary>
        internal static bool IsExcludedByName(ISymbol member, SyntaxTree tree, DSA044Settings settings)
        {
            return settings.ExcludedTypeNamePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(member.ContainingType.Name, pattern)) ||
                   settings.ExcludedMemberNamePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(member.Name, pattern)) ||
                   IsNamespaceExcluded(member, settings) ||
                   AnalyzersUtils.IsFileExcluded(tree.FilePath, settings.ExcludedFilePatterns);
        }

        /// <summary>The name of the namespace is built only if there is a pattern to match it with.</summary>
        private static bool IsNamespaceExcluded(ISymbol member, DSA044Settings settings)
        {
            if (settings.ExcludedNamespacePatterns.Count == 0)
                return false;

            var typeNamespace = member.ContainingNamespace == null ? string.Empty : SymbolNames.GetFullName(member.ContainingNamespace);
            return settings.ExcludedNamespacePatterns.Any(pattern => AnalyzersUtils.MatchesGlobPattern(typeNamespace, pattern));
        }

        /// <summary>The member also reads from the console (<c>Console.ReadLine</c>, <c>ReadKey</c>, ...): it talks to a user, it does not log.</summary>
        internal static bool IsInteractive(SyntaxNode node, SemanticModel model, DSA044Settings settings, CancellationToken cancellationToken)
        {
            if (!settings.SkipInteractiveMembers)
                return false;

            var member = node.Ancestors().OfType<MemberDeclarationSyntax>().FirstOrDefault();
            return member != null && _readingMembers.GetValue(member, key => new StrongBox<bool>(ReadsTheConsole(key, model, cancellationToken))).Value;
        }

        // every flagged call of a member asks the same question about the member: it is answered once
        private static readonly ConditionalWeakTable<MemberDeclarationSyntax, StrongBox<bool>> _readingMembers = new();

        private static bool ReadsTheConsole(MemberDeclarationSyntax member, SemanticModel model, CancellationToken cancellationToken) =>
            member.DescendantNodes().OfType<InvocationExpressionSyntax>().Any(invocation => ReadsTheConsole(invocation, model, cancellationToken));

        private static bool ReadsTheConsole(InvocationExpressionSyntax invocation, SemanticModel model, CancellationToken cancellationToken)
        {
            var name = SyntaxUtils.GetInvokedName(invocation);
            if (name == null || !name.StartsWith("Read", System.StringComparison.Ordinal))
                return false;

            if (model.GetSymbolInfo(invocation, cancellationToken).Symbol?.ContainingType is { } containing && SymbolNames.GetFullName(containing) == FlaggedCall.ConsoleType)
                return true;

            // Console.In.ReadLine(): the method belongs to TextReader, but the receiver is the input stream of the console
            return invocation.Expression is MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax stream } &&
                   model.GetSymbolInfo(stream, cancellationToken).Symbol is IPropertySymbol { ContainingType: { } owner } &&
                   SymbolNames.GetFullName(owner) == FlaggedCall.ConsoleType;
        }
    }
}
