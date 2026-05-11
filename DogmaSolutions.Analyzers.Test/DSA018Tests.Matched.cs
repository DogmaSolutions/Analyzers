using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA018Tests
{
    private static IEnumerable<object[]> GetMatchedCases =>
    [
        [
            "List field: negated Any + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (!_items.Any(x => x == ""test""))
                        {
                            _items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List field: negated Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (!_items.Contains(""test""))
                        {
                            _items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List field: positive Any + throw + Add after",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (_items.Any(x => x == ""test""))
                            throw new System.InvalidOperationException();|}
                        _items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "List field: positive Any + else Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (_items.Any(x => x == ""test""))
                        {
                            System.Console.WriteLine(""exists"");
                        }
                        else
                        {
                            _items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List field: Count == 0 + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (_items.Count(x => x == ""test"") == 0)
                        {
                            _items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List field: FirstOrDefault == null + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (_items.FirstOrDefault(x => x == ""test"") == null)
                        {
                            _items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List parameter: negated Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(List<string> items)
                    {
                        {|#0:if (!items.Contains(""test""))
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List parameter: negated Any + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(List<string> items)
                    {
                        {|#0:if (!items.Any(x => x == ""test""))
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "ICollection parameter: Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(ICollection<string> items, string item)
                    {
                        {|#0:if (!items.Contains(item))
                        {
                            items.Add(item);
                        }|}
                    }
                }
            }"
        ],
        [
            "IList parameter: Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(IList<string> items, string item)
                    {
                        {|#0:if (!items.Contains(item))
                        {
                            items.Add(item);
                        }|}
                    }
                }
            }"
        ],
        [
            "List property: negated Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public List<string> Items { get; } = new List<string>();
                    public void MyMethod()
                    {
                        {|#0:if (!Items.Contains(""test""))
                        {
                            Items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA018Analyzer>.Diagnostic(DSA018Analyzer.DiagnosticId)
                .WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
