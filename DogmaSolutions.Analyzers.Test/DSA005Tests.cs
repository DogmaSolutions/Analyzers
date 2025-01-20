using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
// ReSharper disable once InconsistentNaming
public class DSA005Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;

        await CSharpAnalyzerVerifier<DSA005Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
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
    public class MyClass 
    {
      private int DoSomething(DateTime now){ return 0;}
      private int DoOtherThings(DateTime now){ return 0;}

      public int IsOk(string s)
      {     
         var now = DateTime.UtcNow; // fixed point-in-time reference
         
         var retVal = DoSomething(now); // this WILL NOT trigger the rule
         
         for(int i = 0; i < 10; i++)
         {
           retVal += DoOtherThings(now);  // this WILL NOT trigger the rule
         }

         return retVal;
      }
    }
}
";
        var test = new CSharpAnalyzerVerifier<DSA005Analyzer>.Test();
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
            "DateTime.UtcNow", @"
using System;
namespace WebApplication1
{      
    public class MyClass
    {
        private void DoSomething(DateTime now){}
        private void DoOtherThings(DateTime now){}

        public void IsNotOk(string s)
        {   
         DoSomething(DateTime.UtcNow); // this WILL trigger the rule
         
         for(int i = 0; i < 10; i++)
         {
           DoOtherThings(DateTime.UtcNow);  // this WILL trigger the rule
         }
        }
    }
}
",
            10, 9, 18, 10
        ],
        [
            "DateTime.UtcNow mixed with DateTime.Now", @"
using System;
namespace WebApplication1
{      
    public class MyClass
    {
        private void DoSomething(DateTime now){}
        private void DoOtherThings(DateTime now){}

        public void IsNotOk(string s)
        {   
         DoSomething(DateTime.Now); // this WILL trigger the rule
         
         for(int i = 0; i < 10; i++)
         {
           DoOtherThings(DateTime.UtcNow);  // this WILL trigger the rule
         }
        }
    }
}
",
            10, 9, 18, 10
        ],
        [
            "DateTime.Now", @"
using System;
namespace WebApplication1
{      
    public class MyClass
    {
        private void DoSomething(DateTime now){}
        private void DoOtherThings(DateTime now){}

        public void IsNotOk(string s)
        {   
         DoSomething(DateTime.Now); // this WILL trigger the rule
         
         for(int i = 0; i < 10; i++)
         {
           DoOtherThings(DateTime.Now);  // this WILL trigger the rule
         }
        }
    }
}
",
            10, 9, 18, 10
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
    public async Task QueryExpressionSyntax_Matched(
        string title,
        string sourceCode,
        int startLine,
        int startColumn,
        int endLine,
        int endColumn)
    {
        var test = new CSharpAnalyzerVerifier<DSA005Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA005Analyzer>.Diagnostic(DSA005Analyzer.DiagnosticId).WithSpan(startLine, startColumn, endLine, endColumn));

        await test.RunAsync().ConfigureAwait(false);
    }
}