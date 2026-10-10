using System;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// Reads the rule-specific entries of the .editorconfig (<c>dotnet_diagnostic.DSAxxx.name = value</c>).
    /// A missing, blank, malformed or out-of-range entry never breaks the analysis: the documented default is used instead.
    /// </summary>
    internal static class AnalyzerOptionsReader
    {
        /// <summary>An integer entry; values that are not plain integers or are lower than <paramref name="minInclusive"/> yield the default.</summary>
        internal static int ReadInt(AnalyzerConfigOptions options, string key, int defaultValue, int minInclusive)
        {
            if (options.TryGetValue(key, out var value) &&
                int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) &&
                parsed >= minInclusive)
            {
                return parsed;
            }

            return defaultValue;
        }

        /// <summary>A boolean entry (<c>true</c> / <c>false</c>, case-insensitive); anything else yields the default.</summary>
        internal static bool ReadBool(AnalyzerConfigOptions options, string key, bool defaultValue)
        {
            if (options.TryGetValue(key, out var value) && bool.TryParse(value, out var parsed))
                return parsed;

            return defaultValue;
        }

        /// <summary>A comma separated list; items are trimmed and empty items dropped. A missing or blank entry yields the default.</summary>
        internal static string[] ReadList(AnalyzerConfigOptions options, string key, string[] defaultValue)
        {
            if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Split(',')
                    .Select(item => item.Trim())
                    .Where(item => item.Length > 0)
                    .ToArray();
            }

            return defaultValue;
        }
    }

    /// <summary>
    /// Caches a value derived from the <see cref="AnalyzerConfigOptions"/> of a syntax tree. The entries are held weakly, so they
    /// never outlive the options they were computed from (which matters in long-lived hosts such as the IDE).
    /// </summary>
    internal sealed class AnalyzerOptionsCache<T>
        where T : class
    {
        private readonly ConditionalWeakTable<AnalyzerConfigOptions, T> _table = new();
        private readonly Func<AnalyzerConfigOptions, T> _factory;

        internal AnalyzerOptionsCache(Func<AnalyzerConfigOptions, T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        internal T Get(AnalyzerConfigOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return _table.GetValue(options, _factory.Invoke);
        }
    }
}
