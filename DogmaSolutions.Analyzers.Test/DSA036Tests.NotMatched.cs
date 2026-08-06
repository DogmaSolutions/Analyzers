using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA036Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        // ── Already in a private static readonly field ──────────────────

        [
            "new Regex already in private static readonly field",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Regex _regex = new Regex(@""\d+"");
                    public bool Check(string input) => _regex.IsMatch(input);
                }
            }"
        ],
        [
            "new Regex already in internal static readonly field",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    internal static readonly Regex Pattern = new Regex(@""\d+"", RegexOptions.Compiled);
                    public bool Check(string input) => Pattern.IsMatch(input);
                }
            }"
        ],

        // ── Pattern is a method parameter (not constant) ───────────────

        [
            "new Regex with pattern from method parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, string pattern)
                    {
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],
        [
            "Regex.IsMatch with pattern from method parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, string pattern)
                    {
                        return Regex.IsMatch(input, pattern);
                    }
                }
            }"
        ],

        // ── Pattern is a method call result ────────────────────────────

        [
            "new Regex with pattern from method call",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private string GetPattern() => @""\d+"";
                    public bool Check(string input)
                    {
                        var regex = new Regex(GetPattern());
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],
        [
            "Regex.IsMatch with pattern from method call",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private string GetPattern() => @""\d+"";
                    public bool Check(string input)
                    {
                        return Regex.IsMatch(input, GetPattern());
                    }
                }
            }"
        ],

        // ── Pattern local variable is mutated ──────────────────────────

        [
            "new Regex with reassigned local pattern variable",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, bool strict)
                    {
                        var pattern = @""\d+"";
                        if (strict)
                            pattern = @""^\d+$"";
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],
        [
            "new Regex with local pattern concatenated via +=",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var pattern = @""\d"";
                        pattern += ""+"";
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Options from parameter (not constant) ──────────────────────

        [
            "new Regex with options from method parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, RegexOptions options)
                    {
                        var regex = new Regex(@""\d+"", options);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Pattern from instance field (not constant) ─────────────────

        [
            "new Regex with pattern from instance field",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private readonly string _pattern = @""\d+"";
                    public bool Check(string input)
                    {
                        var regex = new Regex(_pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Pattern from property (not constant) ───────────────────────

        [
            "new Regex with pattern from property",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public string Pattern { get; set; } = @""\d+"";
                    public bool Check(string input)
                    {
                        var regex = new Regex(Pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Parameterless Regex constructor ─────────────────────────────

        [
            "Parameterless Regex constructor (no pattern)",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class MyRegex : Regex
                {
                    public MyRegex() : base() { }
                }
            }"
        ],

        // ── Conditional pattern (depends on parameter) ──────────────────

        [
            "new Regex with pattern from ternary depending on parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, bool strict)
                    {
                        var regex = new Regex(strict ? @""^\d+$"" : @""\d+"");
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Local variable initialized from parameter ───────────────────

        [
            "new Regex with local initialized from parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, string rawPattern)
                    {
                        var pattern = rawPattern;
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Pattern from interpolated string with non-constant part ─────

        [
            "new Regex with interpolated string containing parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, string prefix)
                    {
                        var regex = new Regex($""{prefix}\\d+"");
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Non-static Regex method call (instance method) ──────────────

        [
            "Instance Regex.IsMatch is not a static call",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Regex _regex = new Regex(@""\d+"");
                    public bool Check(string input) => _regex.IsMatch(input);
                }
            }"
        ],

        // ── Local passed by ref ─────────────────────────────────────────

        [
            "new Regex with local variable passed by ref before use",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private void Transform(ref string s) { s = s + ""+""; }
                    public bool Check(string input)
                    {
                        var pattern = @""\d"";
                        Transform(ref pattern);
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Public static readonly field (already hoisted) ─────────────

        [
            "new Regex in public static readonly field",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public static readonly Regex Pattern = new Regex(@""\d+"", RegexOptions.Compiled);
                    public bool Check(string input) => Pattern.IsMatch(input);
                }
            }"
        ],
        [
            "new Regex in protected static readonly field",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    protected static readonly Regex Pattern = new Regex(@""\d+"");
                    public bool Check(string input) => Pattern.IsMatch(input);
                }
            }"
        ],

        // ── Pattern from array element ─────────────────────────────────

        [
            "new Regex with pattern from array element",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly string[] Patterns = { @""\d+"", @""[a-z]+"" };
                    public bool Check(string input)
                    {
                        var regex = new Regex(Patterns[0]);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Pattern from dictionary ────────────────────────────────────

        [
            "new Regex with pattern from dictionary lookup",
            @"
            using System.Collections.Generic;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Dictionary<string, string> Patterns = new();
                    public bool Check(string input)
                    {
                        var regex = new Regex(Patterns[""key""]);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Pattern from null-coalescing with non-constant ─────────────

        [
            "new Regex with null-coalescing pattern involving parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, string pattern)
                    {
                        var regex = new Regex(pattern ?? @""\d+"");
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── out var declaration ─────────────────────────────────────────

        [
            "new Regex with local from out parameter",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private bool TryGetPattern(out string p) { p = @""\d+""; return true; }
                    public bool Check(string input)
                    {
                        TryGetPattern(out var pattern);
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── foreach iteration variable ─────────────────────────────────

        [
            "new Regex with foreach iteration variable",
            @"
            using System.Collections.Generic;
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public void CheckAll(string input, List<string> patterns)
                    {
                        foreach (var pattern in patterns)
                        {
                            var regex = new Regex(pattern);
                            regex.IsMatch(input);
                        }
                    }
                }
            }"
        ],

        // ── Pattern from static property (not field) ───────────────────

        [
            "new Regex with pattern from static property",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public static string Pattern => @""\d+"";
                    public bool Check(string input)
                    {
                        var regex = new Regex(Pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Pattern from switch expression ─────────────────────────────

        [
            "new Regex with pattern from switch expression",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, int mode)
                    {
                        var regex = new Regex(mode switch { 1 => @""\d+"", _ => @""[a-z]+"" });
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Local assigned in conditional branches ─────────────────────

        [
            "new Regex with local variable assigned in if-else",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input, bool strict)
                    {
                        string pattern;
                        if (strict)
                            pattern = @""^\d+$"";
                        else
                            pattern = @""\d+"";
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Local passed by out ────────────────────────────────────────

        [
            "new Regex with local variable passed by out before use",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private void GetPattern(out string s) { s = @""\d+""; }
                    public bool Check(string input)
                    {
                        var pattern = @""\d"";
                        GetPattern(out pattern);
                        var regex = new Regex(pattern);
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],

        // ── Regex subclass constructor ─────────────────────────────────

        [
            "Regex subclass constructor is not System.Text.RegularExpressions.Regex",
            @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class CustomRegex : Regex
                {
                    public CustomRegex(string pattern) : base(pattern) { }
                }
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = new CustomRegex(@""\d+"");
                        return regex.IsMatch(input);
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA036Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
