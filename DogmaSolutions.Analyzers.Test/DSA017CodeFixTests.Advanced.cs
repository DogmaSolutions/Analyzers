using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA017CodeFixTests
{

    [TestMethod]
    public async Task FixHashSetContainsAddWithElseNoBraces()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            {|#0:if (!set.Contains(item))
                set.Add(item);
            else
                System.Console.WriteLine(""exists"");|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            if (!(set.Add(item)))
                System.Console.WriteLine(""exists"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedSetContainsAddWithElseNoBraces()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedSet<string> set, string item)
        {
            {|#0:if (!set.Contains(item))
                set.Add(item);
            else
                System.Console.WriteLine(""exists"");|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedSet<string> set, string item)
        {
            if (!(set.Add(item)))
                System.Console.WriteLine(""exists"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("SortedSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Pattern B block-throw counterparts: check + throw { } + Add
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixHashSetContainsThrowBlockAddToAddThrow()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            {|#0:if (set.Contains(item))
            {
                throw new InvalidOperationException(""duplicate"");
            }|}
            set.Add(item);
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            if (!(set.Add(item)))
            {
                throw new InvalidOperationException(""duplicate"");
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedDictionaryContainsKeyThrowBlockAddToTryAddThrow()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
        {
            {|#0:if (dict.ContainsKey(key))
            {
                throw new InvalidOperationException(""duplicate"");
            }|}
            dict.Add(key, value);
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
        {
            if (!(dict.TryAdd(key, value)))
            {
                throw new InvalidOperationException(""duplicate"");
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("SortedDictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Pattern C no-braces counterparts: check + else Add
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixHashSetPatternCNoBraces()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            {|#0:if (set.Contains(item))
                System.Console.WriteLine(""exists"");
            else
                set.Add(item);|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            if (!(set.Add(item)))
                System.Console.WriteLine(""exists"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedDictionaryPatternCNoBraces()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
        {
            {|#0:if (dict.ContainsKey(key))
                System.Console.WriteLine(""exists"");
            else
                dict.Add(key, value);|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
        {
            if (!(dict.TryAdd(key, value)))
                System.Console.WriteLine(""exists"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("SortedDictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // TryGetValue no-braces counterparts: out-var used → no fix
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task NoFixForDictionaryTryGetValueNoBracesWhenOutVarUsedInElse()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int newValue)
        {
            {|#0:if (!dict.TryGetValue(key, out var existing))
                dict.Add(key, newValue);
            else
                dict[key] = existing + newValue;|}
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixForDictionaryTryGetValueNoBracesWhenOutVarUsedAfterIf()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int newValue)
        {
            {|#0:if (!dict.TryGetValue(key, out var existing))
                dict.Add(key, newValue);|}
            System.Console.WriteLine(existing);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // TryGetValue with pre-declared out variable (not inline var)
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixDictionaryTryGetValuePreDeclaredOutVarUnused()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            int existing;
            {|#0:if (!dict.TryGetValue(key, out existing))
            {
                dict.Add(key, value);
            }|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            int existing;
            dict.TryAdd(key, value);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixForDictionaryTryGetValuePreDeclaredOutVarUsed()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int newValue)
        {
            int existing;
            {|#0:if (!dict.TryGetValue(key, out existing))
            {
                dict.Add(key, newValue);
            }|}
            System.Console.WriteLine(existing);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Trivia edge cases: mixed braces, comments, blank lines
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixDictionaryPatternCMixedBraces_IfBracedElseNot()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            {|#0:if (dict.ContainsKey(key))
            {
                System.Console.WriteLine(""exists"");
            }
            else
                dict.Add(key, value);|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            if (!(dict.TryAdd(key, value)))
            {
                System.Console.WriteLine(""exists"");
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDictionaryPatternCMixedBraces_IfNotBracedElseBraced()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            {|#0:if (dict.ContainsKey(key))
                System.Console.WriteLine(""exists"");
            else
            {
                dict.Add(key, value);
            }|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            if (!(dict.TryAdd(key, value)))
                System.Console.WriteLine(""exists"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDictionaryPatternAMixedBraces_IfNotBracedElseBraced()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            {|#0:if (!dict.ContainsKey(key))
                dict.Add(key, value);
            else
            {
                System.Console.WriteLine(""exists"");
            }|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            if (!(dict.TryAdd(key, value)))
            {
                System.Console.WriteLine(""exists"");
            }
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDictionaryWithLeadingComment()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            // Ensure key is present
            {|#0:if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            // Ensure key is present
            dict.TryAdd(key, value);
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixDictionaryThrowPatternWithLeadingComment()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            // Guard against duplicates
            {|#0:if (dict.ContainsKey(key))
                throw new InvalidOperationException();|}
            dict.Add(key, value);
        }
    }
}";

        var fixedSource = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            // Guard against duplicates
            if (!(dict.TryAdd(key, value)))
                throw new InvalidOperationException();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixHashSetPatternAMixedBraces_IfBracedElseNot()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            {|#0:if (!set.Contains(item))
            {
                set.Add(item);
            }
            else
                System.Console.WriteLine(""exists"");|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(HashSet<string> set, string item)
        {
            if (!(set.Add(item)))
                System.Console.WriteLine(""exists"");
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA017Analyzer, DSA017CodeFixProvider>
                .Diagnostic(DSA017Analyzer.DiagnosticId).WithLocation(0)
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
