using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA008Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxNotMatchedCases =>
    [
        [
            "[Required] public DateTime? MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[Required] public DateTime? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.DateTime? MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[Required] public System.DateTime? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public DateTime? MyProperty",
            """
            using System;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public DateTime? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public System.DateTime? MyProperty",
            """
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public System.DateTime? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Nullable<DateTime> MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[Required] public Nullable<DateTime> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Nullable<System.DateTime> MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[Required] public System.Nullable<System.DateTime> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public Nullable<DateTime> MyProperty",
            """
            using System;
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public Nullable<DateTime> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public System.Nullable<System.DateTime> MyProperty",
            """
            namespace WebApplication1
            {      
                public class MyClass 
                {    
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public System.Nullable<System.DateTime> MyProperty { get; set; }|}
                }
            }
            """
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxNotMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA008Analyzer>.Test();
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