using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers.Test;

/// <summary>An in-memory set of editorconfig entries.</summary>
internal sealed class FakeAnalyzerConfigOptions : AnalyzerConfigOptions
{
    private readonly Dictionary<string, string> _values = new();

    internal FakeAnalyzerConfigOptions(params (string Key, string Value)[] entries)
    {
        foreach (var (key, value) in entries)
            _values[key] = value;
    }

    public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);
}
