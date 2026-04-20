using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA012Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxNotMatchedCases =>
    [
        [
            "Just Add without existence check",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "Negated Any without Add in body",
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
                        if (!items.Any(x => x == ""test""))
                        {
                            System.Console.WriteLine(""not found"");
                        }
                    }
                }
            }"
        ],
        [
            "Non-existence condition + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(bool someFlag)
                    {
                        var items = new List<string>();
                        if (someFlag)
                        {
                            items.Add(""test"");
                        }
                    }
                }
            }"
        ],
        [
            "Positive Any + throw without Add after",
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
                        if (items.Any(x => x == ""test""))
                            throw new System.InvalidOperationException();
                    }
                }
            }"
        ],
        [
            "Standalone Any without if",
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
                        var exists = items.Any(x => x == ""test"");
                    }
                }
            }"
        ],
        [
            "Existence check and insert on different collections",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public bool Validate(string value, List<string> errors)
                    {
                        var validValues = new List<string> { ""A"", ""B"" };
                        if (!validValues.Contains(value))
                        {
                            errors.Add(""Invalid value"");
                            return false;
                        }
                        return true;
                    }
                }
            }"
        ],
        [
            "List check-then-act (DSA018 territory, not DSA012)",
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
                        if (!items.Any(x => x == ""test""))
                        {
                            items.Add(""test"");
                        }
                    }
                }
            }"
        ],
        [
            "Dictionary check-then-act (DSA017 territory, not DSA012)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var dict = new Dictionary<string, int>();
                        if (!dict.ContainsKey(""test""))
                        {
                            dict.Add(""test"", 1);
                        }
                    }
                }
            }"
        ],
        [
            "HashSet check-then-act (DSA017 territory, not DSA012)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var set = new HashSet<string>();
                        if (!set.Contains(""test""))
                        {
                            set.Add(""test"");
                        }
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxNotMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_NotMatched(
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

        await test.RunAsync().ConfigureAwait(false);
    }
}
