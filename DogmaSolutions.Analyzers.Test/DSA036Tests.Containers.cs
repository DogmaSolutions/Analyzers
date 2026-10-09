using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA036 for every kind of container a regex creation can live in (constructor, accessor, local function, lambda,
/// anonymous method, initializer, top-level statement): the enclosing body is where a constant pattern is searched
/// (<c>DSA036Analyzer.GetEnclosingMethodBody</c>).
/// </summary>
public partial class DSA036Tests
{
    private static string Container(string members) => @"
            using System;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Holder
                {
" + members + @"
                }
            }";

    private static IEnumerable<object[]> GetContainerCases =>
    [
        ["Constructor with a block body", Container(@"
                    private Regex _r;
                    public Holder() { _r = {|#0:new Regex(@""\d+"")|}; }")],
        ["Constructor with an expression body", Container(@"
                    private Regex _r;
                    public Holder() => _r = {|#0:new Regex(@""\d+"")|};")],
        ["Property getter with a block body", Container(@"
                    public Regex R { get { return {|#0:new Regex(@""\d+"")|}; } }")],
        ["Property getter with an expression body", Container(@"
                    public Regex R { get => {|#0:new Regex(@""\d+"")|}; }")],
        ["Local function with a block body", Container(@"
                    public Regex M()
                    {
                        Regex Local() { return {|#0:new Regex(@""\d+"")|}; }
                        return Local();
                    }")],
        ["Local function with an expression body", Container(@"
                    public Regex M()
                    {
                        Regex Local() => {|#0:new Regex(@""\d+"")|};
                        return Local();
                    }")],
        ["Simple lambda", Container(@"
                    public Func<int, Regex> M() => n => {|#0:new Regex(@""\d+"")|};")],
        ["Parenthesized lambda", Container(@"
                    public Func<int, int, Regex> M() => (a, b) => {|#0:new Regex(@""\d+"")|};")],
        ["Anonymous method", Container(@"
                    public Func<Regex> M() => delegate { return {|#0:new Regex(@""\d+"")|}; };")],
        ["Instance field initializer", Container(@"
                    private readonly Regex _r = {|#0:new Regex(@""\d+"")|};")],
        ["Property initializer", Container(@"
                    public Regex R { get; } = {|#0:new Regex(@""\d+"")|};")],
        ["Regex.IsMatch in a constructor", Container(@"
                    public Holder(string input) { var ok = {|#0:Regex.IsMatch(input, @""\d+"")|}; }")],
        ["Regex.IsMatch in a lambda", Container(@"
                    public Func<string, bool> M() => s => {|#0:Regex.IsMatch(s, @""\d+"")|};")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetContainerCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Container_Matched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        // The reported expression is the text between the {|#0: and |} markers.
#pragma warning disable CA1062
        var markerStart = sourceCode.IndexOf("{|#0:", System.StringComparison.Ordinal) + "{|#0:".Length;
        var expectedExpression = sourceCode.Substring(markerStart, sourceCode.IndexOf("|}", markerStart, System.StringComparison.Ordinal) - markerStart);
#pragma warning restore CA1062
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(expectedExpression));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Container_TopLevelStatement_Matched()
    {
        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = @"
using System.Text.RegularExpressions;
var regex = {|#0:new Regex(@""\d+"")|};
System.Console.WriteLine(regex.IsMatch(""1""));";
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.OutputKind = Microsoft.CodeAnalysis.OutputKind.ConsoleApplication;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
