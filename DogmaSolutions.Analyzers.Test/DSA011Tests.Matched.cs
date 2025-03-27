using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA011Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        [
            "Coalesced assignment expression",
            @"
            public class MyClass
            {
                private static MyClass _instance;

                {|#0:public static MyClass Instance => _instance ??= new MyClass();|}
            }"
        ],
        [
            "If-else assignment in get body (1)",
            @"
            public class MyClass
            {
                private static MyClass _instance;

                {|#0:public static MyClass Instance 
                {
                 get
                 {
                   if(_instance==null)
                    _instance = new MyClass();
                   return _instance;
                 }
                }|}
            }"
        ],
        [
            "If-else assignment in get body (2)",
            @"
            public class MyClass
            {
                private static MyClass _instance;

                {|#0:public static MyClass Instance 
                {
                 get {

if(_instance!=null)

return _instance;


_instance = new MyClass();

return _instance;

}
                }|}
            }"
        ]
        
    ];


    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_Matched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA011Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = test.ReferenceAssemblies.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA011Analyzer>.Diagnostic(DSA011Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}