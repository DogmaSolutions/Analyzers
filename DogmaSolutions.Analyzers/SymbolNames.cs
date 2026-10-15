using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The fully qualified names of symbols, computed once per symbol: <c>ToDisplayString()</c> builds a new string each time, and the same
    /// few types (the console, the loggers, the attributes...) are asked for over and over.
    /// </summary>
    internal static class SymbolNames
    {
        private static readonly ConditionalWeakTable<ISymbol, string> _names = new();

        internal static string GetFullName(ISymbol symbol) => _names.GetValue(symbol, static key => key.ToDisplayString());
    }
}
