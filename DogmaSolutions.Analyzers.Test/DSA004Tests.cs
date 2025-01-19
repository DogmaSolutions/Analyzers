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
public class DSA004Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;

        await CSharpAnalyzerVerifier<DSA004Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
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

        public DateTime UtcNow()
        {
            return DateTime.UtcNow;
        }

        public DateTime Now()
        {
            return DateTime.UtcNow;
        }

        public DateTime UtcNow3()
        {
            return System.DateTime.UtcNow;
        }
    }
}
";
        var test = new CSharpAnalyzerVerifier<DSA004Analyzer>.Test();
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
            "System.DateTime.Now", @"
using System;
namespace WebApplication1
{      
    public class MyEntity
    {
        public DateTime Now1()
        {
            return {|#0:System.DateTime.Now|};
        }
    }
}
"
        ],
        [
            "DateTime.Now",     @"
using System;
namespace WebApplication1
{      
    public class MyEntity
    {
        public DateTime Now1()
        {
            return {|#0:DateTime.Now|};
        }
    }
}
"
        ],
        [
            "using static System.DateTime; Now",    @"
using System;
using static System.DateTime;
namespace WebApplication1
{      
    public class MyEntity
    {
        public DateTime Now1()
        {
            return {|#0:Now|};
        }
    }
}
"
        ],
        [
            "using static global::System.DateTime; Now",    @"
using System;
using static global::System.DateTime;
namespace WebApplication1
{      
    public class MyEntity
    {
        public DateTime Now1()
        {
            return {|#0:Now|};
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
        var test = new CSharpAnalyzerVerifier<DSA004Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
            [
                ..new PackageIdentity[]
                {
                    new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                    new("Microsoft.EntityFrameworkCore", "3.1.22")
                }
            ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA004Analyzer>.Diagnostic(DSA004Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}