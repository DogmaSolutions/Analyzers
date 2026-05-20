using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA017CodeFixTests
{
    [TestMethod]
    public async Task FixDictionaryContainsKeyAddToTryAdd()
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
    public async Task FixDictionaryContainsKeyAddNoBraces()
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

    [TestMethod]
    public async Task FixDictionaryTryGetValueAddToTryAddWhenOutVarUnused()
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
    public async Task NoFixForDictionaryTryGetValueWithElseBracesWhenOutVarUnused()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void Merge(Dictionary<string, string> target, Dictionary<string, string> source)
        {
            foreach (var entry in source)
            {
                {|#0:if (!target.TryGetValue(entry.Key, out var value))
                {
                    target.Add(entry.Key, entry.Value);
                }
                else
                {
                    target[entry.Key] = entry.Value;
                }|}
            }
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
    public async Task NoFixForDictionaryTryGetValueWithElseNoBracesWhenOutVarUnused()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public void Merge(Dictionary<string, string> target, Dictionary<string, string> source)
        {
            foreach (var entry in source)
            {
                {|#0:if (!target.TryGetValue(entry.Key, out var value))
                    target.Add(entry.Key, entry.Value);
                else
                    target[entry.Key] = entry.Value;|}
            }
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
    public async Task NoFixForDictionaryTryGetValueWhenOutVarUsedInElse()
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
            {
                dict.Add(key, newValue);
            }
            else
            {
                dict[key] = existing + newValue;
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
                .WithArguments("Dictionary", "TryAdd or indexer assignment [key] = value"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixForDictionaryTryGetValueWhenOutVarUsedAfterIf()
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

    [TestMethod]
    public async Task FixSortedDictionaryContainsKeyAddToTryAdd()
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
    public async Task FixDictionaryIndexerReceiver()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        private readonly Dictionary<string, Dictionary<string, int>> _nested = new();
        public void Add(string outerKey, string innerKey, int value)
        {
            {|#0:if (!_nested[outerKey].ContainsKey(innerKey))
            {
                _nested[outerKey].Add(innerKey, value);
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
        private readonly Dictionary<string, Dictionary<string, int>> _nested = new();
        public void Add(string outerKey, string innerKey, int value)
        {
            _nested[outerKey].TryAdd(innerKey, value);
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
    public async Task FixHashSetContainsAddToAdd()
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
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixHashSetContainsAddNoBraces()
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
                .WithArguments("HashSet", "Add (already returns a bool indicating whether the element was added)"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSortedSetContainsAddToAdd()
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
    public async Task FixDictionaryContainsKeyThrowAddToTryAddThrow()
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
    public async Task FixDictionaryContainsKeyThrowBlockAddToTryAddThrow()
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
            {|#0:if (dict.ContainsKey(key))
            {
                throw new InvalidOperationException(""Key already exists"");
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
        public void MyMethod(Dictionary<string, int> dict, string key, int value)
        {
            if (!(dict.TryAdd(key, value)))
            {
                throw new InvalidOperationException(""Key already exists"");
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
    public async Task FixDictionaryContainsKeyAddWithElseToTryAddWithBody()
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
            {
                dict.Add(key, value);
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
    public async Task FixHashSetContainsAddWithElseToAddWithBody()
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
    public async Task FixDictionaryFieldReceiver()
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
            {|#0:if (!_cache.ContainsKey(key))
            {
                _cache.Add(key, value);
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
            _cache.TryAdd(key, value);
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
    public async Task FixDictionaryPropertyReceiver()
    {
        var source = @"
using System.Collections.Generic;
namespace TestApp
{
    public class MyClass
    {
        public Dictionary<string, int> Items { get; } = new();
        public void Add(string key, int value)
        {
            {|#0:if (!Items.ContainsKey(key))
            {
                Items.Add(key, value);
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
        public Dictionary<string, int> Items { get; } = new();
        public void Add(string key, int value)
        {
            Items.TryAdd(key, value);
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
}
