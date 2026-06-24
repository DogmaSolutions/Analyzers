using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA017CodeFixTests
{

    [TestMethod]
    public async Task FixDictionaryMemberAccessReceiver()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class Container
    {
        public Dictionary<string, int> Items { get; } = new();
    }
    public class MyClass
    {
        public void Add(Container c, string key, int value)
        {
            {|#0:if (!c.Items.ContainsKey(key))
            {
                c.Items.Add(key, value);
            }|}
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
namespace TestApp
{
    public class Container
    {
        public Dictionary<string, int> Items { get; } = new();
    }
    public class MyClass
    {
        public void Add(Container c, string key, int value)
        {
            c.Items.TryAdd(key, value);
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
    public async Task FixDictionaryContainsKeyEqualsFalseAddToTryAdd()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            {|#0:if (dict.ContainsKey(key) == false)
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
    public async Task FixDictionaryPatternCContainsKeyElseAdd()
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
    public async Task FixDictionaryPatternCNoBraces()
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
    public async Task FixHashSetPatternCContainsElseAdd()
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
            {
                System.Console.WriteLine(""exists"");
            }
            else
            {
                set.Add(item);
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
        public void MyMethod(HashSet<string> set, string item)
        {
            if (!(set.Add(item)))
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
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedDictionaryPatternCElseAdd()
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
            {
                System.Console.WriteLine(""exists"");
            }
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
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
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
                .WithArguments("SortedDictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixHashSetContainsThrowAddToAddThrow()
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
                throw new InvalidOperationException();|}
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
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedDictionaryContainsKeyThrowAddToTryAddThrow()
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
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
        {
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
                .WithArguments("SortedDictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixForNonAdjacentThrowAdd()
    {
        var source = @"
using System;
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key)
        {
            {|#0:if (dict.ContainsKey(key))
                throw new InvalidOperationException();|}
            var value = ComputeValue(key);
            dict.Add(key, value);
        }
        private int ComputeValue(string key) => 42;
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
    public async Task FixDictionaryThisFieldReceiver()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        private readonly Dictionary<string, int> _cache = new();
        public void Add(string key, int value)
        {
            {|#0:if (!this._cache.ContainsKey(key))
            {
                this._cache.Add(key, value);
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
        private readonly Dictionary<string, int> _cache = new();
        public void Add(string key, int value)
        {
            this._cache.TryAdd(key, value);
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
    public async Task NoFixForPatternCComplexElseBody()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key)
        {
            {|#0:if (dict.ContainsKey(key))
            {
                System.Console.WriteLine(""exists"");
            }
            else
            {
                var value = ComputeValue(key);
                dict.Add(key, value);
            }|}
        }
        private int ComputeValue(string key) => 42;
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
    public async Task FixSortedSetContainsAddWithElse()
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
            {
                set.Add(item);
            }
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
        public void MyMethod(SortedSet<string> set, string item)
        {
            if (!(set.Add(item)))
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
                .WithArguments("SortedSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixForSortedListContainsKeyAdd()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedList<string, int> list, string key, int value)
        {
            {|#0:if (!list.ContainsKey(key))
            {
                list.Add(key, value);
            }|}
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
                .WithArguments("SortedList", "indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixForDictionaryComplexBody()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key)
        {
            {|#0:if (!dict.ContainsKey(key))
            {
                var value = ComputeExpensiveValue(key);
                dict.Add(key, value);
            }|}
        }
        private int ComputeExpensiveValue(string key) => 42;
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
    // Pattern A no-braces counterparts: !check + Add (no else)
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixSortedDictionaryContainsKeyAddNoBraces()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(SortedDictionary<string, int> dict, string key, int value)
        {
            {|#0:if (!dict.ContainsKey(key))
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
                .WithArguments("SortedDictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedSetContainsAddNoBraces()
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
        public void MyMethod(SortedSet<string> set, string item)
        {
            set.Add(item);
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

    [TestMethod]
    public async Task FixDictionaryTryGetValueAddNoBracesWhenOutVarUnused()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            {|#0:if (!dict.TryGetValue(key, out var existing))
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

    // ---------------------------------------------------------------
    // Pattern A+else no-braces counterparts: !check + Add + else
    // ---------------------------------------------------------------

    [TestMethod]
    public async Task FixDictionaryContainsKeyAddWithElseNoBraces()
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
}
