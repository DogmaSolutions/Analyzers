using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
// ReSharper disable once InconsistentNaming
public class DSA003Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;

        await CSharpAnalyzerVerifier<DSA003Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task QueryExpressionSyntax_NotMatched()
    {
        var sourceCode = @"
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication1
{      

    public class MyEntity
    {
        public long Id { get; set; }

        public bool IsNullOrWhiteSpace1(string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public bool IsNullOrWhiteSpace2(string s)
        {
            return String.IsNullOrWhiteSpace(s);
        }

        public bool IsNullOrWhiteSpace3(string s)
        {
            return System.String.IsNullOrWhiteSpace(s);
        }
    }
}
";
        var test = new CSharpAnalyzerVerifier<DSA003Analyzer>.Test();
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


    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        [
            "System.String.IsNullOrEmpty", @"
using System;
namespace WebApplication1
{      
    public class MyEntity
    {
        public bool IsNullOrEmpty3(string s)
        {
            return {|#0:System.String.IsNullOrEmpty(s)|};
        }
    }
}
"
        ],
        [
            "String.IsNullOrEmpty",     @"
using System;
namespace WebApplication1
{      
    public class MyEntity
    {
        public bool IsNullOrEmpty3(string s)
        {
            return {|#0:String.IsNullOrEmpty(s)|};
        }
    }
}
"
        ],
        [
            "string.IsNullOrEmpty",     @"
using System;
namespace WebApplication1
{      
    public class MyEntity
    {
        public bool IsNullOrEmpty3(string s)
        {
            return {|#0:string.IsNullOrEmpty(s)|};
        }
    }
}
"
        ],
        [
            "using static System.String; IsNullOrEmpty",    @"
using System;
using static System.String;
namespace WebApplication1
{      
    public class MyEntity
    {
        public bool IsNullOrEmpty3(string s)
        {
            return {|#0:IsNullOrEmpty(s)|};
        }
    }
}
"
        ] ,
        [
            "using static global::System.String; IsNullOrEmpty",    @"
using System;
using static global::System.String;
namespace WebApplication1
{      
    public class MyEntity
    {
        public bool IsNullOrEmpty3(string s)
        {
            return {|#0:IsNullOrEmpty(s)|};
        }
    }
}
"
        ]
    ];

    public static string GetQueryExpressionSyntaxMatchedCasesDisplayName(MethodInfo methodInfo, object[] data)
    {
#pragma warning disable CA1062
        return (string)data[0];
#pragma warning restore CA1062
    }

    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxMatchedCasesDisplayName))]
    public async Task QueryExpressionSyntax_Matched(string title, string sourceCode)
    {
        var test = new CSharpAnalyzerVerifier<DSA003Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
            [
                ..new PackageIdentity[]
                {
                    new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                    new("Microsoft.EntityFrameworkCore", "3.1.22")
                }
            ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA003Analyzer>.Diagnostic(DSA003Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}