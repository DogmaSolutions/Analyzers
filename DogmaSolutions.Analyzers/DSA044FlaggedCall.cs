using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DogmaSolutions.Analyzers
{
    /// <summary>A call to a console or debug output method: what is called and the log level it would map to.</summary>
    internal sealed class FlaggedCall
    {
        internal FlaggedCall(string displayName, string level, bool hasCategory = false)
        {
            HasCategory = hasCategory;
            DisplayName = displayName;
            Level = level;
        }

        /// <summary>For example <c>Console.WriteLine</c>, <c>Console.Error.WriteLine</c> or <c>Debug.Write</c>.</summary>
        internal string DisplayName { get; }

        /// <summary>The Microsoft.Extensions.Logging level that fits: Debug, Information or Error.</summary>
        internal string Level { get; }

        /// <summary>The overload takes a <c>category</c> (<c>Debug.WriteLine(message, category)</c>): its second argument is not a value of the message.</summary>
        internal bool HasCategory { get; }

        internal const string ConsoleType = "System.Console";
        internal const string DebugType = "System.Diagnostics.Debug";
    }

    /// <summary>Recognizes the calls DSA044 is about: a flagged member of a flagged type, or of <c>Console.Out</c> / <c>Console.Error</c>.</summary>
    internal static class DSA044FlaggedCallFinder
    {
        internal static FlaggedCall Find(InvocationExpressionSyntax invocation, SemanticModel model, DSA044Settings settings, CancellationToken cancellationToken)
        {
            if (model.GetSymbolInfo(invocation, cancellationToken).Symbol is not IMethodSymbol method)
                return null;

            var typeName = SymbolNames.GetFullName(method.ContainingType);
            if (settings.FlaggedTypes.Contains(typeName))
                return new FlaggedCall(method.ContainingType.Name + "." + method.Name, typeName == FlaggedCall.DebugType ? "Debug" : "Information", method.Parameters.Any(parameter => parameter.Name == "category"));

            return FindConsoleStreamCall(invocation, method, model, settings, cancellationToken);
        }

        /// <summary><c>Console.Error.WriteLine(...)</c>: the method belongs to <c>TextWriter</c>, but the receiver is a stream of the console.</summary>
        private static FlaggedCall FindConsoleStreamCall(InvocationExpressionSyntax invocation, IMethodSymbol method, SemanticModel model, DSA044Settings settings, CancellationToken cancellationToken)
        {
            if (!settings.IncludeConsoleStreams || !settings.FlaggedTypes.Contains(FlaggedCall.ConsoleType))
                return null;

            if (invocation.Expression is not MemberAccessExpressionSyntax { Expression: MemberAccessExpressionSyntax stream })
                return null;

            if (model.GetSymbolInfo(stream, cancellationToken).Symbol is not IPropertySymbol { ContainingType: { } owner } property || SymbolNames.GetFullName(owner) != FlaggedCall.ConsoleType)
                return null;

            return new FlaggedCall("Console." + property.Name + "." + method.Name, property.Name == "Error" ? "Error" : "Information");
        }
    }
}
