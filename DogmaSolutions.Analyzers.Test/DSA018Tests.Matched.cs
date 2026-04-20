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
            "List: negated Any + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        {|#0:if (!items.Any(x => x == ""test""))
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List: negated Contains + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        {|#0:if (!items.Contains(""test""))
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List: positive Any + throw + Add after",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        {|#0:if (items.Any(x => x == ""test""))
                            throw new System.InvalidOperationException();|}
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "List: positive Any + else Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        {|#0:if (items.Any(x => x == ""test""))
                        {
                            System.Console.WriteLine(""exists"");
                        }
                        else
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List: Count == 0 + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        {|#0:if (items.Count(x => x == ""test"") == 0)
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "List: FirstOrDefault == null + Add",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        {|#0:if (items.FirstOrDefault(x => x == ""test"") == null)
                        {
                            items.Add(""test"");
                        }|}
                    }
                }
            }"
        ],
        [
            "ICollection: Contains + Add",
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
            "IList: Contains + Add (interface without atomic alternative)",
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
