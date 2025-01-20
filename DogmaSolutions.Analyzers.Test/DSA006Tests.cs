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
public class DSA006Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;

        await CSharpAnalyzerVerifier<DSA006Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task QueryExpressionSyntax_NotMatched()
    {
        var sourceCode = @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {     
      public void IsOk(int id)
      {     
        if(id < 0)
          throw new ArgumentException(""Invalid id"");
      }
    }
}
";
        var test = new CSharpAnalyzerVerifier<DSA006Analyzer>.Test();
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
            "Exception", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {     
      public void IsOk(int id)
      {     
        if(id < 0)
          throw new Exception(""Invalid id"");
      }
    }
}
",
            10, 11, 10, 45
        ],
        [
            "System.Exception", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {     
      public void IsOk(int id)
      {     
        if(id < 0)
          throw new System.Exception(""Invalid id"");
      }
    }
}
",
            10, 11, 10, 52
        ],
        [
            "System.SystemException", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {     
      public void IsOk(int id)
      {     
        if(id < 0)
          throw new System.SystemException(""Invalid id"");
      }
    }
}
",
            10, 11, 10, 58
        ],
        [
            "SystemException", @"
using System;
namespace WebApplication1
{      
    public class MyClass 
    {     
      public void IsNotOk(int id)
      {     
        if(id < 0)
          throw new SystemException(""Invalid id"");
      }
    }
}
",
            10, 11, 10, 51
        ],
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
        var test = new CSharpAnalyzerVerifier<DSA006Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA006Analyzer>.Diagnostic(DSA006Analyzer.DiagnosticId).WithSpan(startLine, startColumn, endLine, endColumn));

        await test.RunAsync().ConfigureAwait(false);
    }
}