using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>DSA056 end to end: a test fixture that is not excluded from the code coverage.</summary>
[TestClass]
public partial class DSA056Tests
{
    /// <summary>The three frameworks at once: a scenario uses the attributes it needs by their full names.</summary>
    internal static readonly ReferenceAssemblies WithFrameworks = ReferenceAssemblies.Net.Net80.AddPackages(
    [
        new PackageIdentity("NUnit", "3.14.0"),
        new PackageIdentity("xunit.core", "2.9.2"),
        new PackageIdentity("MSTest.TestFramework", "3.6.4"),
    ]);

    [TestMethod]
    public async Task Empty()
    {
        await CSharpAnalyzerVerifier<DSA056Analyzer>.VerifyAnalyzerAsync(string.Empty).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        return CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);
    }

    internal static string ToEditorConfig(string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic.DSA056." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    /// <summary>The source has <c>{|#n:Name|}</c> on the identifier of each class expected, in order; <paramref name="expected"/> are the names.</summary>
    internal static CSharpAnalyzerVerifier<DSA056Analyzer>.Test CreateTest(string source, string settings, string expected, ReferenceAssemblies references = null)
    {
        var test = new CSharpAnalyzerVerifier<DSA056Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = references ?? WithFrameworks;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", ToEditorConfig(settings)));

        var names = (expected ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        for (var i = 0; i < names.Length; i++)
        {
            var message = string.Format(Resources.Culture, Resources.DSA056Message, names[i]);
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA056Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i).WithMessage(message));
        }

        return test;
    }
}
