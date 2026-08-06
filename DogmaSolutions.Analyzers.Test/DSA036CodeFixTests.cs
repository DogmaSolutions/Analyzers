using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA036CodeFixTests
{
    [TestMethod]
    public async Task ExtractsNewRegexToStaticReadonlyField()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new Regex(@""\d+"");
                    public bool Check(string input)
                    {
                        var regex = _regex;
                        return regex.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0).WithArguments(@"new Regex(@""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsNewRegexWithOptionsToStaticReadonlyField()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input)
                    {
                        var regex = {|#0:new Regex(@""\d+"", RegexOptions.Compiled | RegexOptions.IgnoreCase)|};
                        return regex.IsMatch(input);
                    }
                }
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new Regex(@""\d+"", RegexOptions.Compiled | RegexOptions.IgnoreCase);
                    public bool Check(string input)
                    {
                        var regex = _regex;
                        return regex.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"", RegexOptions.Compiled | RegexOptions.IgnoreCase)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsStaticIsMatchToFieldAndInstanceCall()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\d+"");
                    public bool Check(string input)
                    {
                        return _regex.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"Regex.IsMatch(input, @""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsStaticReplaceToFieldAndInstanceCall()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Sanitizer
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\d+"");
                    public string Clean(string input)
                    {
                        return _regex.Replace(input, ""X"");
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"Regex.Replace(input, @""\d+"", ""X"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FieldNameConflictResolution()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Regex _regex = new Regex(""existing"");
                    public bool Check(string input)
                    {
                        return {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                    }
                }
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Regex _regex = new Regex(""existing"");
                    private static readonly System.Text.RegularExpressions.Regex _regex1 = new Regex(@""\d+"");
                    public bool Check(string input)
                    {
                        return _regex1.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DoesNotFlagStaticReadonlyField()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly Regex _regex = new Regex(@""\d+"", RegexOptions.Compiled);
                    public bool Check(string input) => _regex.IsMatch(input);
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsStaticIsMatchWithOptionsToFieldAndInstanceCall()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\d+"", RegexOptions.IgnoreCase);
                    public bool Check(string input)
                    {
                        return _regex.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"Regex.IsMatch(input, @""\d+"", RegexOptions.IgnoreCase)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsStaticSplitToFieldAndInstanceCall()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Parser
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\s+"");
                    public string[] Tokenize(string input)
                    {
                        return _regex.Split(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"Regex.Split(input, @""\s+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsStaticMatchToFieldAndInstanceCall()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Extractor
                {
                    public string Extract(string input)
                    {
                        return {|#0:Regex.Match(input, @""\d+"")|}.Value;
                    }
                }
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Extractor
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\d+"");
                    public string Extract(string input)
                    {
                        return _regex.Match(input).Value;
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"Regex.Match(input, @""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsStaticMatchesToFieldAndInstanceCall()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Scanner
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\d+"");
                    public int CountDigits(string input)
                    {
                        return _regex.Matches(input).Count;
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"Regex.Matches(input, @""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsImplicitNewRegexToFieldWithFullType()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@""\d+"");
                    public bool Check(string input)
                    {
                        Regex regex = _regex;
                        return regex.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"new(@""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsNewRegexInExpressionBodiedMethod()
    {
        var source = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    public bool Check(string input) => {|#0:new Regex(@""\d+"")|}.IsMatch(input);
                }
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new Regex(@""\d+"");
                    public bool Check(string input) => _regex.IsMatch(input);
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsNewRegexInStruct()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public struct Validator
                {
                    private static readonly System.Text.RegularExpressions.Regex _regex = new Regex(@""\d+"");
                    public bool Check(string input)
                    {
                        var regex = _regex;
                        return regex.IsMatch(input);
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtractsNewRegexInNestedClass()
    {
        var source = @"
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
            }";

        var fixedSource = @"
            using System.Text.RegularExpressions;
            namespace TestApp
            {
                public class Outer
                {
                    public class Inner
                    {
                        private static readonly System.Text.RegularExpressions.Regex _regex = new Regex(@""\d+"");
                        public bool Check(string input)
                        {
                            var regex = _regex;
                            return regex.IsMatch(input);
                        }
                    }
                }
            }";

        var test = new CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA036Analyzer, DSA036CodeFixProvider>
                .Diagnostic(DSA036Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments(@"new Regex(@""\d+"")"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
