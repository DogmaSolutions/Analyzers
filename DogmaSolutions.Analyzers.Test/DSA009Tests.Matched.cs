using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA009Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        [
            "[Required] public DateTimeOffset MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[Required] public DateTimeOffset MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.DateTimeOffset MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[Required] public System.DateTimeOffset MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public DateTimeOffset MyProperty",
            """
            using System;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public DateTimeOffset MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public System.DateTimeOffset MyProperty",
            """
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public System.DateTimeOffset MyProperty { get; set; }|}
                }
            }
            """
        ],
    ];


    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA009Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA009Analyzer>.Diagnostic(DSA009Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}