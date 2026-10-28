using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA036Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        // ── new Regex with string literal ──────────────────────────────

        [
            "new Regex with string literal pattern in method body",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"")|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex with pattern and RegexOptions",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"", RegexOptions.Compiled)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(@""\d+"", RegexOptions.Compiled)"
        ],
        [
            "new Regex with combined RegexOptions flags",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(""pattern"", RegexOptions.IgnoreCase | RegexOptions.Compiled)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(""pattern"", RegexOptions.IgnoreCase | RegexOptions.Compiled)"
        ],

        // ── new Regex with const field ─────────────────────────────────

        [
            "new Regex with const field pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private const string Pattern = @""\d+"";
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(Pattern)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            "new Regex(Pattern)"
        ],

        // ── new Regex with non-mutated local variable ──────────────────

        [
            "new Regex with non-mutated local variable pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var pattern = @""\d+"";
                        var regex = {|#0:new Regex(pattern)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            "new Regex(pattern)"
        ],

        // ── new Regex with static readonly field pattern ───────────────

        [
            "new Regex with static readonly field as pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly string Pattern = @""\d+"";
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(Pattern)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            "new Regex(Pattern)"
        ],

        // ── new Regex in various method types ──────────────────────────

        [
            "new Regex in constructor",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private readonly Regex _regex;
                    public Validator()
                    {
                        _regex = {|#0:new Regex(@""\d+"")|};
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in property getter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public Regex NumberPattern
                    {
                        get { return {|#0:new Regex(@""\d+"")|};  }
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in lambda inside method",
            @"
            using System;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public Func<string, bool> GetChecker()
                    {
                        return input => {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex inside a loop",
            @"
            using System.Text.RegularExpressions;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class Validator
                {
                    public void CheckAll(List<string> inputs)
                    {
                        foreach (var input in inputs)
                        {
                            var match = {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                        }
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── Static Regex method calls ──────────────────────────────────

        [
            "Regex.IsMatch with string literal pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        return {|#0:Regex.IsMatch(input, @""\d+"")|};
                    }
                }
            }",
            @"Regex.IsMatch(input, @""\d+"")"
        ],
        [
            "Regex.IsMatch with pattern and options",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        return {|#0:Regex.IsMatch(input, @""\d+"", RegexOptions.IgnoreCase)|};
                    }
                }
            }",
            @"Regex.IsMatch(input, @""\d+"", RegexOptions.IgnoreCase)"
        ],
        [
            "Fully qualified static Regex call",
            @"
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        return {|#0:System.Text.RegularExpressions.Regex.IsMatch(input, @""\d+"")|};
                    }
                }
            }",
            @"System.Text.RegularExpressions.Regex.IsMatch(input, @""\d+"")"
        ],
        [
            "Fully qualified Regex creation",
            @"
            namespace TestApp
            {
                public class Validator
                {
                    private readonly object _r = {|#0:new System.Text.RegularExpressions.Regex(@""\d+"")|};
                }
            }",
            @"new System.Text.RegularExpressions.Regex(@""\d+"")"
        ],
        [
            "Alias qualified Regex creation",
            @"
            namespace TestApp
            {
                public class Validator
                {
                    private readonly object _r = {|#0:new global::System.Text.RegularExpressions.Regex(@""\d+"")|};
                }
            }",
            @"new global::System.Text.RegularExpressions.Regex(@""\d+"")"
        ],
        [
            "Regex through a using alias",
            @"
            using Rx = System.Text.RegularExpressions.Regex;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        return {|#0:Rx.IsMatch(input, @""\d+"")|};
                    }
                }
            }",
            @"Rx.IsMatch(input, @""\d+"")"
        ],
        [
            "Regex.Match with string literal pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public string Extract(string input)
                    {
                        return {|#0:Regex.Match(input, @""\d+"")|}.Value;
                    }
                }
            }",
            @"Regex.Match(input, @""\d+"")"
        ],
        [
            "Regex.Replace with string literal pattern and replacement",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Sanitizer
                {
                    public string Clean(string input)
                    {
                        return {|#0:Regex.Replace(input, @""\d+"", ""X"")|};
                    }
                }
            }",
            @"Regex.Replace(input, @""\d+"", ""X"")"
        ],
        [
            "Regex.Split with string literal pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Parser
                {
                    public string[] Tokenize(string input)
                    {
                        return {|#0:Regex.Split(input, @""\s+"")|};
                    }
                }
            }",
            @"Regex.Split(input, @""\s+"")"
        ],
        [
            "Regex.Matches with string literal pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Scanner
                {
                    public int CountDigits(string input)
                    {
                        return {|#0:Regex.Matches(input, @""\d+"")|}.Count;
                    }
                }
            }",
            @"Regex.Matches(input, @""\d+"")"
        ],

        // ── Static Regex method with const variable pattern ────────────

        [
            "Regex.IsMatch with const local pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        const string pattern = @""\d+"";
                        return {|#0:Regex.IsMatch(input, pattern)|};
                    }
                }
            }",
            "Regex.IsMatch(input, pattern)"
        ],

        // ── Non-mutated local for static method ────────────────────────

        [
            "Regex.IsMatch with non-mutated local pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var pattern = @""\d+"";
                        return {|#0:Regex.IsMatch(input, pattern)|};
                    }
                }
            }",
            "Regex.IsMatch(input, pattern)"
        ],

        // ── Expression-bodied member ──────────────────────────────────

        [
            "new Regex in expression-bodied method",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input) => {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── Implicit object creation ──────────────────────────────────

        [
            "Implicit new Regex with pattern literal",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        Regex regex = {|#0:new(@""\d+"")|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new(@""\d+"")"
        ],

        // ── new Regex in static method ────────────────────────────────

        [
            "new Regex in static method body",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public static bool Check(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"")|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── new Regex in struct ───────────────────────────────────────

        [
            "new Regex in struct method",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public struct Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"")|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── new Regex in record ───────────────────────────────────────

        [
            "new Regex in record method",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public record Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"")|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── new Regex in nested class ─────────────────────────────────

        [
            "new Regex in nested class method",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Outer
                {
                    public class Inner
                    {
                        public bool Check(string input)
                        {
                            var regex = {|#0:new Regex(@""\d+"")|};
                            return regex.IsMatch(input);
                        }
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── new Regex in async method ─────────────────────────────────

        [
            "new Regex in async method body",
            @"
            using System.Text.RegularExpressions;
            using System.Threading.Tasks;
            namespace TestApp
            {
                public class Validator
                {
                    public async Task<bool> CheckAsync(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"")|};
                        return await Task.FromResult(regex.IsMatch(input));
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── new Regex in local function ───────────────────────────────

        [
            "new Regex in local function",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        return DoCheck(input);
                        bool DoCheck(string s) => {|#0:new Regex(@""\d+"")|}.IsMatch(s);
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── new Regex in anonymous method ─────────────────────────────

        [
            "new Regex in anonymous method (delegate)",
            @"
            using System;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public Func<string, bool> GetChecker()
                    {
                        return delegate(string input)
                        {
                            return {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                        };
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── Implicit object creation with options ──────────────────────

        [
            "Implicit new Regex with pattern and options",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        Regex regex = {|#0:new(@""\d+"", RegexOptions.Compiled)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new(@""\d+"", RegexOptions.Compiled)"
        ],

        // ── String concatenation of constants ─────────────────────────

        [
            "new Regex with constant string concatenation",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(""abc"" + ""def"")|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            @"new Regex(""abc"" + ""def"")"
        ],

        // ── new Regex with nameof() ───────────────────────────────────

        [
            "new Regex with nameof as pattern",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(nameof(Validator))|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            "new Regex(nameof(Validator))"
        ],

        // ── new Regex in switch case body ─────────────────────────────

        [
            "new Regex in switch case body",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, int mode)
                    {
                        switch (mode)
                        {
                            case 1:
                                return {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                            default:
                                return false;
                        }
                    }
                }
            }",
            @"new Regex(@""\d+"")"
        ],

        // ── Const local variable (not field) ──────────────────────────

        [
            "new Regex with const local variable",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        const string pattern = @""\d+"";
                        var regex = {|#0:new Regex(pattern)|};
                        return regex.IsMatch(input);
                    }
                }
            }",
            "new Regex(pattern)"
        ],

        // ── Code that runs more than once is still flagged ────────────

        [
            "new Regex in an instance field initializer (one per instance)",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private Regex _regex = {|#0:new Regex(@""\d+"")|};
                    public bool Check(string input) => _regex.IsMatch(input);
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in an instance property initializer (one per instance)",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public Regex Pattern { get; } = {|#0:new Regex(@""\d+"")|};
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in a lambda of a static field initializer (one per call)",
            @"
            using System;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Func<string, bool> _check = input => {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in a static method of a class with a static constructor",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    static Validator() { }
                    public static bool Check(string input) => {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in a lambda of a static constructor (one per call)",
            @"
            using System;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static Func<string, bool> _check;
                    static Validator() { _check = input => {|#0:new Regex(@""\d+"")|}.IsMatch(input); }
                }
            }",
            @"new Regex(@""\d+"")"
        ],
        [
            "new Regex in the getter of a static property (one per call)",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public static Regex Pattern { get { return {|#0:new Regex(@""\d+"")|}; } }
                }
            }",
            @"new Regex(@""\d+"")"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(string title, string sourceCode, string expectedExpression)
    {
        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(expectedExpression));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MultipleNewRegexInSameMethod()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var r1 = {|#0:new Regex(@""\d+"")|};
                        var r2 = {|#1:new Regex(@""[a-z]+"")|};
                        return r1.IsMatch(input) || r2.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(1)
                .WithArguments(@"new Regex(@""[a-z]+"")"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExpressionBodiedProperty()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public Regex Pattern => {|#0:new Regex(@""\d+"")|};
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA036Analyzer>.Diagnostic(DSA036Analyzer.DiagnosticId)
                .WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task A_call_on_the_result_of_a_call_is_not_a_static_Regex_call()
    {
        var source = @"
            using System.Linq;
            using System.Collections.Generic;
            namespace TestApp
            {
                public class Validator
                {
                    private static IEnumerable<int> Get() => new int[0];
                    public int Check() => Get().Count() + Get().Count(x => x > 0);
                }
            }";

        await CSharpAnalyzerVerifier<DSA036Analyzer>.VerifyAnalyzerAsync(source).ConfigureAwait(false);
    }
}
