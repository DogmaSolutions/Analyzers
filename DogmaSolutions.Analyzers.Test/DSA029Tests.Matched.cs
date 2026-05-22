using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA029Tests
{
    private static IEnumerable<object[]> GetQueryExpressionSyntaxMatchedCases =>
    [
        // ── bool ──────────────────────────────────────────────
        [
            "[Required] public bool MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public bool MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Boolean MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Boolean MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public bool MyProperty",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public bool MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public System.Boolean MyProperty",
            """
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public System.Boolean MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── int ───────────────────────────────────────────────
        [
            "[Required] public int MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public int MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Int32 MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Int32 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public int MyProperty",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public int MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public System.Int32 MyProperty",
            """
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public System.Int32 MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── long ──────────────────────────────────────────────
        [
            "[Required] public long MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public long MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Int64 MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Int64 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public long MyProperty",
            """
            using System;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public long MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public System.Int64 MyProperty",
            """
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public System.Int64 MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── short ─────────────────────────────────────────────
        [
            "[Required] public short MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public short MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Int16 MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Int16 MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── byte ──────────────────────────────────────────────
        [
            "[Required] public byte MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public byte MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Byte MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Byte MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── sbyte ─────────────────────────────────────────────
        [
            "[Required] public sbyte MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public sbyte MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.SByte MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.SByte MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── ushort ────────────────────────────────────────────
        [
            "[Required] public ushort MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public ushort MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.UInt16 MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.UInt16 MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── uint ──────────────────────────────────────────────
        [
            "[Required] public uint MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public uint MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.UInt32 MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.UInt32 MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── ulong ─────────────────────────────────────────────
        [
            "[Required] public ulong MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public ulong MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.UInt64 MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.UInt64 MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── float ─────────────────────────────────────────────
        [
            "[Required] public float MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public float MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Single MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Single MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── double ────────────────────────────────────────────
        [
            "[Required] public double MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public double MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Double MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Double MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── decimal ───────────────────────────────────────────
        [
            "[Required] public decimal MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public decimal MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Decimal MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Decimal MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── char ──────────────────────────────────────────────
        [
            "[Required] public char MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public char MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Char MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Char MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── Guid ──────────────────────────────────────────────
        [
            "[Required] public Guid MyProperty",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Guid MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.Guid MyProperty",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.Guid MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── enum (custom) ─────────────────────────────────────
        [
            "[Required] public MyEnum MyProperty (custom enum)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public enum MyEnum { None, Value1, Value2 }

                public class MyClass
                {
                  {|#0:[Required] public MyEnum MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public MyEnum MyProperty (custom enum, fully qualified attribute)",
            """
            using System;
            namespace WebApplication1
            {
                public enum MyEnum { None, Value1, Value2 }

                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public MyEnum MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── enum (BCL) ────────────────────────────────────────
        [
            "[Required] public DayOfWeek MyProperty (BCL enum)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public DayOfWeek MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public System.DayOfWeek MyProperty (fully qualified BCL enum)",
            """
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public System.DayOfWeek MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── enum ([Flags]) ────────────────────────────────────
        [
            "[Required] public FlagsEnum MyProperty (flags enum)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                [Flags]
                public enum FlagsEnum { None = 0, A = 1, B = 2, C = 4 }

                public class MyClass
                {
                  {|#0:[Required] public FlagsEnum MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── struct (custom) ───────────────────────────────────
        [
            "[Required] public MyStruct MyProperty (custom struct)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public struct MyStruct { public int X; }

                public class MyClass
                {
                  {|#0:[Required] public MyStruct MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[System.ComponentModel.DataAnnotations.Required] public MyStruct MyProperty (custom struct, fully qualified attribute)",
            """
            using System;
            namespace WebApplication1
            {
                public struct MyStruct { public int X; }

                public class MyClass
                {
                  {|#0:[System.ComponentModel.DataAnnotations.Required] public MyStruct MyProperty { get; set; }|}
                }
            }
            """
        ],
        // ── CLR type names (e.g. Boolean, Int32) ─────────────
        [
            "[Required] public Boolean MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Boolean MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Int32 MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Int32 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Int64 MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Int64 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Int16 MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Int16 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Byte MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Byte MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public SByte MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public SByte MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public UInt16 MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public UInt16 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public UInt32 MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public UInt32 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public UInt64 MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public UInt64 MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Single MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Single MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Double MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Double MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Decimal MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Decimal MyProperty { get; set; }|}
                }
            }
            """
        ],
        [
            "[Required] public Char MyProperty (CLR name)",
            """
            using System;
            using System.ComponentModel.DataAnnotations;
            namespace WebApplication1
            {
                public class MyClass
                {
                  {|#0:[Required] public Char MyProperty { get; set; }|}
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

        test.ExpectedDiagnostics.Add(CSharpAnalyzerVerifier<DSA029Analyzer>.Diagnostic(DSA029Analyzer.DiagnosticId).WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }
}
