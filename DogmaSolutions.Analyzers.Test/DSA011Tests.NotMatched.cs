using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA011Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxNotMatchedCases =>
    [
        [
            "Not coalesce assignment",
            @"
            public class MyClass
            {
                private static MyClass _instance;

                public static MyClass Instance => _instance = new MyClass();
            }"
        ],
        [
            "Not static property",
            @"
            public class MyClass
            {
                private static MyClass _instance;

                public MyClass Instance => _instance ??= new MyClass();
            }"
        ],
        [
            "No lazy initialization",
            @"
            public class MyClass
            {
                private static MyClass _instance = new MyClass();

                public static MyClass Instance => _instance;
            }"
        ],
        [
            "Thread-safe Lazy wrapper",
            @"
            using System;
            public class MyClass
            {
                private static readonly Lazy<MyClass> _lazy = new Lazy<MyClass>(() => new MyClass());

                public static MyClass Instance => _lazy.Value;
            }"
        ],
        [
            "If-null with braces in get body is not detected",
            @"
            public class MyClass
            {
                private static MyClass _instance;

                public static MyClass Instance
                {
                    get
                    {
                        if (_instance == null)
                        {
                            _instance = new MyClass();
                        }
                        return _instance;
                    }
                }
            }"
        ],
        [
            "Static property returning field from a different class",
            @"
            public class Storage
            {
                public static MyClass SharedInstance;
            }
            public class MyClass
            {
                public static MyClass Instance => Storage.SharedInstance ??= new MyClass();
            }"
        ]
    ];

    [TestMethod]
    [DynamicData(nameof(GetQueryExpressionSyntaxNotMatchedCases), DynamicDataDisplayName = nameof(GetQueryExpressionSyntaxCaseDisplayName))]
    public async Task QueryExpressionSyntax_NotMatched(
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

        await test.RunAsync().ConfigureAwait(false);
    }
}