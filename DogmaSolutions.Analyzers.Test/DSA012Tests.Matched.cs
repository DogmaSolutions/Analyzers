using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA012Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        [
            "Negated Any + Add (block body)",
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
            "Negated Any + Add (single statement)",
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
                            items.Add(""test"");|}
                    }
                }
            }"
        ],
        [
            "Positive Any + throw + Add after (single statement throw)",
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
            "Positive Any + throw + Add after (block throw)",
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
                            throw new System.InvalidOperationException();
                        }|}
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "Count == 0 + Add",
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
            "FirstOrDefault == null + Add",
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
            "Positive Any + else Add",
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
            "Negated Contains + Add",
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
    ];


    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA012Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA012Analyzer>.Diagnostic(DSA012Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
