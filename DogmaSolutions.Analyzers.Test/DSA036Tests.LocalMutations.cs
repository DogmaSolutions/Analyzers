using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA036 when the regex pattern is a local variable: it is considered a constant only if the local is never mutated
/// in its scope (<c>DSA036Analyzer.IsLocalNeverReassigned</c>), whatever the way of mutating it.
/// </summary>
public partial class DSA036Tests
{
    /// <summary>
    /// A method with a counter local ('n'), a few other mutable things, the given statements and then the regex creation.
    /// </summary>
    private static string LocalMutationSource(string statements, string regexCreation) => @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Holder { public string Text; public int Number; }
                public class Validator
                {
                    private static void Fill(ref int value) { value = 1; }
                    private static void Produce(out int value) { value = 1; }
                    public bool Check(string input, Holder holder, int[] array, bool flag)
                    {
                        var n = 1;
                        var other = 0;
                        var otherText = ""x"";
" + statements + @"
                        var regex = " + regexCreation + @";
                        return regex.IsMatch(input);
                    }
                }
            }";

    private const string CounterRegexCreation = @"new Regex(""a{"" + n + ""}"")";
    private const string MarkedCounterRegexCreation = @"{|#0:new Regex(""a{"" + n + ""}"")|}";

    private static IEnumerable<object[]> GetLocalMutationNotMatchedCases =>
    [
        ["Local counter mutated by assignment", LocalMutationSource("n = 2;", CounterRegexCreation)],
        ["Local counter mutated by compound assignment", LocalMutationSource("n += 2;", CounterRegexCreation)],
        ["Local counter mutated by postfix increment", LocalMutationSource("n++;", CounterRegexCreation)],
        ["Local counter mutated by postfix decrement", LocalMutationSource("n--;", CounterRegexCreation)],
        ["Local counter mutated by prefix increment", LocalMutationSource("++n;", CounterRegexCreation)],
        ["Local counter mutated by prefix decrement", LocalMutationSource("--n;", CounterRegexCreation)],
        ["Local counter passed by ref", LocalMutationSource("Fill(ref n);", CounterRegexCreation)],
        ["Local counter passed by out", LocalMutationSource("Produce(out n);", CounterRegexCreation)],
        ["Local counter mutated by tuple deconstruction", LocalMutationSource("(n, other) = (2, 3);", CounterRegexCreation)],
        ["Local counter mutated by nested tuple deconstruction", LocalMutationSource("((n, other), otherText) = ((2, 3), \"y\");", CounterRegexCreation)],
        ["Local counter mutated by tuple deconstruction, after another target", LocalMutationSource("(other, n) = (3, 2);", CounterRegexCreation)],
        ["Local counter mutated by tuple deconstruction, after a member target", LocalMutationSource("(holder.Number, n) = (3, 2);", CounterRegexCreation)],
        ["Local counter mutated by nested tuple deconstruction, after another target", LocalMutationSource("(otherText, (other, n)) = (\"y\", (3, 2));", CounterRegexCreation)],
    ];

    [TestMethod]
    [DynamicData(nameof(GetLocalMutationNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task LocalMutation_NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }

    private static IEnumerable<object[]> GetLocalMutationMatchedCases =>
    [
        ["Local counter never mutated", LocalMutationSource(string.Empty, MarkedCounterRegexCreation)],
        ["Other local assigned", LocalMutationSource("other = 2;", MarkedCounterRegexCreation)],
        ["Other local assigned in a declaration", LocalMutationSource("var chained = other = 2;", MarkedCounterRegexCreation)],
        ["Other local incremented and decremented (postfix)", LocalMutationSource("other++; other--;", MarkedCounterRegexCreation)],
        ["Other local incremented and decremented (prefix)", LocalMutationSource("++other; --other;", MarkedCounterRegexCreation)],
        ["Array element incremented (postfix)", LocalMutationSource("array[0]++;", MarkedCounterRegexCreation)],
        ["Array element incremented (prefix)", LocalMutationSource("++array[0];", MarkedCounterRegexCreation)],
        ["Other local negated and inverted", LocalMutationSource("var negated = -other; var inverted = !flag;", MarkedCounterRegexCreation)],
        ["Other local passed by ref", LocalMutationSource("Fill(ref other);", MarkedCounterRegexCreation)],
        ["Other local passed by out", LocalMutationSource("Produce(out other);", MarkedCounterRegexCreation)],
        ["Array element passed by ref", LocalMutationSource("Fill(ref array[0]);", MarkedCounterRegexCreation)],
        ["Member of another object assigned", LocalMutationSource("holder.Number = 2; holder.Text = otherText;", MarkedCounterRegexCreation)],
        ["Other locals mutated by tuple deconstruction", LocalMutationSource("(other, otherText) = (2, \"y\");", MarkedCounterRegexCreation)],
        ["Tuple deconstruction into members", LocalMutationSource("(holder.Number, holder.Text) = (2, \"y\");", MarkedCounterRegexCreation)],
    ];

    [TestMethod]
    [DynamicData(nameof(GetLocalMutationMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task LocalMutation_Matched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(@"new Regex(""a{"" + n + ""}"")"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
