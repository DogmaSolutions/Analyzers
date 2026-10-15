using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DataRow

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The syntactic measure of a class: cyclomatic complexity, lines, methods, constructor parameters and activity.</summary>
[TestClass]
public class DSA043MetricsTests
{
    private static TypeMetrics Measure(string members, bool countBlankLines = false, string header = "class C")
    {
        var type = DSA043TestSupport.GetType("using System; using System.Collections.Generic;\n" + header + "\n{\n" + members + "\n}");
        return DSA043TypeMetrics.Compute(type, countBlankLines, CancellationToken.None);
    }

    [TestMethod]
    [DataRow("return 1;", 1)]
    [DataRow("if (x > 0) return 1; return 0;", 2)]
    [DataRow("for (int i = 0; i < x; i++) { } return 0;", 2)]
    [DataRow("foreach (var a in l) { } return 0;", 2)]
    [DataRow("foreach (var (a, b) in new (int, int)[0]) { } return 0;", 2)]
    [DataRow("while (x > 0) x--; return x;", 2)]
    [DataRow("do { x--; } while (x > 0); return x;", 2)]
    [DataRow("switch (x) { case 1: return 1; case 2: return 2; default: return 0; }", 3)]
    [DataRow("switch (o) { case int i: return i; case string s: return 1; default: return 0; }", 3)]
    [DataRow("return x switch { 1 => 1, 2 => 2, _ => 0 };", 4)]
    [DataRow("try { return 1; } catch (Exception) { return 0; }", 2)]
    [DataRow("try { return 1; } catch (Exception) when (x > 0) { return 0; }", 3)]
    [DataRow("return x > 0 ? 1 : 0;", 2)]
    [DataRow("return x > 0 && x < 5 ? 1 : 0;", 3)]
    [DataRow("return x > 0 || x < 5 ? 1 : 0;", 3)]
    [DataRow("return n ?? 0;", 2)]
    [DataRow("n ??= 3; return 0;", 2)]
    [DataRow("return o is 1 or 2 ? 1 : 0;", 3)]
    [DataRow("return o is > 0 and < 5 ? 1 : 0;", 3)]
    [DataRow("Func<int, int> f = y => y > 0 ? 1 : 0; return f(x);", 2)]
    [DataRow("int Local(int y) { if (y > 0) return 1; return 0; } return Local(x);", 2)]
    public void Every_decision_point_adds_one_path(string body, int expectedComplexity)
    {
        var metrics = Measure("public int M(int x, int? n, object o, List<int> l) { " + body + " }");

        Assert.AreEqual(expectedComplexity, metrics.CyclomaticComplexity);
        Assert.AreEqual(expectedComplexity, metrics.MaxMethodComplexity);
    }

    [TestMethod]
    public void Total_is_the_sum_and_max_is_the_most_complex_unit()
    {
        var metrics = Measure(@"
            public int A(int x) { if (x > 0) return 1; return 0; }
            public int B(int x) { if (x > 0 && x < 9) { while (x > 0) x--; } return x; }
            public C(int x) { }
            ~C() { }
            public static C operator +(C a, C b) => a;
            public static implicit operator int(C c) => 0;
            public int P { get { return 1; } set { if (value > 0) { } } }
            public int Q => 1;
            public int this[int i] => i > 0 ? 1 : 0;
            public int this[string s] { get { return 1; } }
            public event Action E { add { } remove { } }
            public int Field = 3;
            public event Action Plain;
            public delegate void D();
            public enum Nested { A }");

        // A=2 B=4 ctor=1 dtor=1 operator=1 conversion=1 P.get=1 P.set=2 Q=1 indexer=2 indexer2.get=1 E.add=1 E.remove=1
        Assert.AreEqual(2 + 4 + 1 + 1 + 1 + 1 + 1 + 2 + 1 + 2 + 1 + 1 + 1, metrics.CyclomaticComplexity);
        Assert.AreEqual(4, metrics.MaxMethodComplexity);
        Assert.AreEqual(4, metrics.Methods); // A, B and the two operators
    }

    [TestMethod]
    public void A_property_without_accessor_list_has_no_unit()
    {
        var metrics = Measure("public int P { }");

        Assert.AreEqual(0, metrics.CyclomaticComplexity);
        Assert.AreEqual(0, metrics.MaxMethodComplexity);
    }

    [TestMethod]
    public void A_class_with_no_executable_code_has_no_complexity_and_a_nested_type_is_not_measured()
    {
        var metrics = Measure("public int Field; public class Inner { public int M(int x) { if (x > 0) return 1; return 0; } }");

        Assert.AreEqual(0, metrics.CyclomaticComplexity);
        Assert.AreEqual(0, metrics.MaxMethodComplexity);
        Assert.AreEqual(0, metrics.Methods);
    }

    [TestMethod]
    public void The_parts_of_a_partial_class_are_measured_together()
    {
        const string preamble = "using System;\n";
        var first = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(preamble + "partial class C { public int A(int x) { if (x > 0) return 1; return 0; } }");
        var second = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(preamble + "partial class C\n{\n    public int B(int x) { if (x > 0) return 1; return 0; }\n    public C(int a, int b, int c) { }\n}");
        var type = DSA043TestSupport.CreateCompilation(first, second).GetTypeByMetadataName("C");

        var metrics = DSA043TypeMetrics.Compute(type, countBlankLines: false, CancellationToken.None);

        Assert.AreEqual(2 + 2 + 1, metrics.CyclomaticComplexity);
        Assert.AreEqual(2, metrics.Methods);
        Assert.AreEqual(3, metrics.MaxConstructorParameters);
        Assert.AreEqual(1 + 5, metrics.Lines); // the first part is on one line, the second on 5
    }

    [TestMethod]
    public void Lines_are_counted_from_the_first_to_the_last_token_and_blank_lines_only_when_asked()
    {
        const string members = "    public int A() { return 1; }\n\n    // comment\n    public int B() { return 2; }\n    \n";

        // class C, {, A, comment, B and } are code; between A and the comment, and after B, there are 3 blank lines
        Assert.AreEqual(6, Measure(members, countBlankLines: false).Lines);
        Assert.AreEqual(9, Measure(members, countBlankLines: true).Lines);
    }

    [TestMethod]
    public void A_one_line_class_is_one_line()
    {
        var type = DSA043TestSupport.GetType("class C { }");

        Assert.AreEqual(1, DSA043TypeMetrics.Compute(type, countBlankLines: false, CancellationToken.None).Lines);
    }

    [TestMethod]
    [DataRow("class C { }", 0)]
    [DataRow("class C { public C() { } }", 0)]
    [DataRow("class C { public C(int a) { } public C(int a, string b, long c) : this(a) { } }", 3)]
    [DataRow("class C(int a, int b) { }", 2)]
    [DataRow("class C(int a, int b) { public C() : this(1, 2) { } }", 2)]
    [DataRow("class C(int a) { public C(int a, int b, int c) : this(a) { } }", 3)]
    [DataRow("record C(int A, int B, int C2);", 3)]
    public void The_constructor_with_most_parameters_counts_primary_constructors_too(string declaration, int expected)
    {
        var type = DSA043TestSupport.GetType(declaration);

        Assert.AreEqual(expected, DSA043TypeMetrics.Compute(type, false, CancellationToken.None).MaxConstructorParameters);
    }

    [TestMethod]
    [DataRow("try { } catch (Exception) { }", 1)]
    [DataRow("await System.Threading.Tasks.Task.Delay(1);", 2)]
    [DataRow("throw new Exception();", 4)]
    [DataRow("var s = (string)null ?? throw new Exception();", 4)]
    [DataRow("try { await System.Threading.Tasks.Task.Delay(1); } catch (Exception) { throw; }", 7)]
    [DataRow("var x = 1;", 0)]
    public void The_syntactic_activity_is_the_union_of_catch_await_and_throw(string body, int expected)
    {
        var metrics = Measure("public async System.Threading.Tasks.Task M() { " + body + " }");

        Assert.AreEqual((ActivityKinds)expected, metrics.SyntacticActivity);
    }

    [TestMethod]
    public void Declarations_are_the_type_declarations_of_the_symbol()
    {
        var type = DSA043TestSupport.GetType("class C { }");

        Assert.AreEqual(1, DSA043TypeMetrics.GetDeclarations(type, CancellationToken.None).Count);
        Assert.AreEqual(0, DSA043TypeMetrics.GetDeclarations(type.BaseType, CancellationToken.None).Count, "System.Object has no declaration in the source");
    }
}
