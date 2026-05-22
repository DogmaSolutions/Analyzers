using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA029Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxNotMatchedCases =>
    [
        [
            "[Required] public bool? MyProperty (nullable bool)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public bool? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public int? MyProperty (nullable int)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public int? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public long? MyProperty (nullable long)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public long? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public decimal? MyProperty (nullable decimal)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public decimal? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public float? MyProperty (nullable float)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public float? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public double? MyProperty (nullable double)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public double? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public byte? MyProperty (nullable byte)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public byte? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public char? MyProperty (nullable char)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public char? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public MyEnum? MyProperty (nullable enum)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public enum MyEnum { None, Value1, Value2 }

                public class MyClass
                {
                  {|#0:[Required] public MyEnum? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Nullable<int> MyProperty (Nullable<T> syntax)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Nullable<int> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Guid? MyProperty (nullable Guid)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Guid? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Nullable<MyEnum> MyProperty (nullable enum via Nullable<T>)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public enum MyEnum { None, Value1, Value2 }

                public class MyClass
                {
                  {|#0:[Required] public Nullable<MyEnum> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "int property without [Required] attribute",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                    public int MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "bool property without [Required] attribute",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                    public bool MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "[Required] on string property (reference type, not value type)",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                    [Required] public string MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "[Required] on object property (reference type, not value type)",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                    [Required] public object MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "[Required] on DateTime property (covered by DSA008)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                    [Required] public DateTime MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "[Required] on DateTimeOffset property (covered by DSA009)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                    [Required] public DateTimeOffset MyProperty { get; set; }
                }
            }
            """
        ],
        // ── additional nullable permutations ──────────────────
        [
            "[Required] public short? MyProperty (nullable short)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public short? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public sbyte? MyProperty (nullable sbyte)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public sbyte? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public ushort? MyProperty (nullable ushort)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public ushort? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public uint? MyProperty (nullable uint)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public uint? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public ulong? MyProperty (nullable ulong)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public ulong? MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Nullable<long> MyProperty (nullable long via Nullable<T>)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Nullable<long> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Nullable<Guid> MyProperty (nullable Guid via Nullable<T>)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Nullable<Guid> MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public MyStruct? MyProperty (nullable struct)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public struct MyStruct { public int X; }

                public class MyClass
                {
                  {|#0:[Required] public MyStruct? MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── value type without [Required] ─────────────────────
        [
            "enum property without [Required] attribute",
            """
            using System;
            namespace WebApplication1
            {
                public enum MyEnum { None, Value1, Value2 }

                public class MyClass
                {
                    public MyEnum MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "Guid property without [Required] attribute",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                    public Guid MyProperty { get; set; }
                }
            }
            """
        ],
        [
            "decimal property without [Required] attribute",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                    public decimal MyProperty { get; set; }
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
        var test = new CSharpAnalyzerVerifier<DSA029Analyzer>.Test();
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
