using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA051: the ToString override is generated from the members the heuristic picks.</summary>
[TestClass]
public class DSA051CodeFixTests
{
    private static CSharpCodeFixVerifier<DSA051Analyzer, DSA051CodeFixProvider>.Test CreateTest(string source, string fixedSource, string settings, int diagnostics)
    {
        var test = new CSharpCodeFixVerifier<DSA051Analyzer, DSA051CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA051CodeFixProvider.GenerateEquivalenceKey;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", DSA051Tests.ToEditorConfig(settings)));
        for (var i = 0; i < diagnostics; i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA051Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        return test;
    }

    private static string Wrap(string body) => "namespace TestApp\n{\n" + body + "\n}";

    // title, class declaration, fixed class declaration, settings
    private static IEnumerable<object[]> GetFixes =>
    [
        [
            "A well known property is returned",
            "    public class {|#0:Invoice|}\n    {\n        public int Id { get; set; }\n        public decimal Total { get; set; }\n    }",
            "    public class Invoice\n    {\n        public int Id { get; set; }\n        public decimal Total { get; set; }\n\n        public override string ToString() => $\"{Id}\";\n    }",
            "",
        ],
        [
            "The key is written when the option asks for it",
            "    public class {|#0:Invoice|}\n    {\n        public int Id { get; set; }\n    }",
            "    public class Invoice\n    {\n        public int Id { get; set; }\n\n        public override string ToString() => $\"Id={Id}\";\n    }",
            "well_known_include_key=true",
        ],
        [
            "A well known field is returned",
            "    public class {|#0:Contact|}\n    {\n        private string _email;\n        public int Total;\n    }",
            "    public class Contact\n    {\n        private string _email;\n        public int Total;\n\n        public override string ToString() => $\"{_email}\";\n    }",
            "",
        ],
        [
            "The public properties are listed with their values quoted",
            "    public class {|#0:Invoice|}\n    {\n        public decimal Total { get; set; }\n        public string Currency { get; set; }\n        protected int Hidden { get; set; }\n    }",
            "    public class Invoice\n    {\n        public decimal Total { get; set; }\n        public string Currency { get; set; }\n        protected int Hidden { get; set; }\n\n        public override string ToString() => $\"Total=`{Total}`, Currency=`{Currency}`\";\n    }",
            "",
        ],
        [
            "The protected properties are listed",
            "    public class {|#0:Invoice|}\n    {\n        protected decimal Total { get; set; }\n        protected string Currency { get; set; }\n    }",
            "    public class Invoice\n    {\n        protected decimal Total { get; set; }\n        protected string Currency { get; set; }\n\n        public override string ToString() => $\"Total={Total}, Currency={Currency}\";\n    }",
            "",
        ],
        [
            "The fields are listed",
            "    public class {|#0:Invoice|}\n    {\n        private decimal total;\n        private string currency;\n    }",
            "    public class Invoice\n    {\n        private decimal total;\n        private string currency;\n\n        public override string ToString() => $\"total={total}, currency={currency}\";\n    }",
            "",
        ],
        [
            "The internal and private properties are listed",
            "    public class {|#0:Invoice|}\n    {\n        internal decimal Total { get; set; }\n        private string Currency { get; set; }\n    }",
            "    public class Invoice\n    {\n        internal decimal Total { get; set; }\n        private string Currency { get; set; }\n\n        public override string ToString() => $\"Total={Total}, Currency={Currency}\";\n    }",
            "",
        ],
        [
            "The secrets are not written",
            "    public class {|#0:Login|}\n    {\n        public string User { get; set; }\n        public string Password { get; set; }\n    }",
            "    public class Login\n    {\n        public string User { get; set; }\n        public string Password { get; set; }\n\n        public override string ToString() => $\"User=`{User}`\";\n    }",
            "",
        ],
        [
            "The most members the text lists",
            "    public class {|#0:Invoice|}\n    {\n        public int A { get; set; }\n        public int B { get; set; }\n        public int C { get; set; }\n    }",
            "    public class Invoice\n    {\n        public int A { get; set; }\n        public int B { get; set; }\n        public int C { get; set; }\n\n        public override string ToString() => $\"A=`{A}`, B=`{B}`\";\n    }",
            "max_members=2",
        ],
        [
            "A keyword used as a name is escaped",
            "    public class {|#0:Shape|}\n    {\n        public string @class { get; set; }\n        public int Size { get; set; }\n    }",
            "    public class Shape\n    {\n        public string @class { get; set; }\n        public int Size { get; set; }\n\n        public override string ToString() => $\"class=`{@class}`, Size=`{Size}`\";\n    }",
            "",
        ],
        [
            "A block body",
            "    public class {|#0:Invoice|}\n    {\n        public decimal Total { get; set; }\n        public string Currency { get; set; }\n    }",
            "    public class Invoice\n    {\n        public decimal Total { get; set; }\n        public string Currency { get; set; }\n\n        public override string ToString()\n        {\n            return $\"Total=`{Total}`, Currency=`{Currency}`\";\n        }\n    }",
            "use_expression_body=false",
        ],
        [
            "Tabs are kept",
            "\tpublic class {|#0:Invoice|}\n\t{\n\t\tpublic decimal Total { get; set; }\n\t}",
            "\tpublic class Invoice\n\t{\n\t\tpublic decimal Total { get; set; }\n\n\t\tpublic override string ToString()\n\t\t{\n\t\t\treturn $\"Total=`{Total}`\";\n\t\t}\n\t}",
            "use_expression_body=false",
        ],
        [
            "A method is kept where it is",
            "    public class {|#0:Invoice|}\n    {\n        public decimal Total { get; set; }\n\n        public void Pay()\n        {\n        }\n    }",
            "    public class Invoice\n    {\n        public decimal Total { get; set; }\n\n        public void Pay()\n        {\n        }\n\n        public override string ToString() => $\"Total=`{Total}`\";\n    }",
            "",
        ],
        [
            "A nested class",
            "    public class Outer : System.Exception\n    {\n        public class {|#0:Inner|}\n        {\n            public int Id { get; set; }\n        }\n    }",
            "    public class Outer : System.Exception\n    {\n        public class Inner\n        {\n            public int Id { get; set; }\n\n            public override string ToString() => $\"{Id}\";\n        }\n    }",
            "",
        ],
        [
            "A struct",
            "    public struct {|#0:Point|}\n    {\n        public int X;\n        public int Y;\n    }",
            "    public struct Point\n    {\n        public int X;\n        public int Y;\n\n        public override string ToString() => $\"X={X}, Y={Y}\";\n    }",
            "included_type_kinds=struct",
        ],
        [
            "The part with no members receives the method",
            "    public partial class {|#0:Invoice|}\n    {\n    }\n\n    public partial class Invoice\n    {\n        public int Id { get; set; }\n    }",
            "    public partial class Invoice\n    {\n\n        public override string ToString() => $\"{Id}\";\n    }\n\n    public partial class Invoice\n    {\n        public int Id { get; set; }\n    }",
            "",
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(DSA051Tests.GetCaseDisplayName), DynamicDataDisplayNameDeclaringType = typeof(DSA051Tests))]
    public async Task Fix(string title, string source, string fixedSource, string settings)
    {
        await CreateTest(Wrap(source), Wrap(fixedSource), settings, 1).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Nothing_is_generated_for_a_type_with_nothing_to_show()
    {
        var source = Wrap("    public class {|#0:Marker|}\n    {\n    }");

        // only the review comment is offered, and the generating action is not
        var test = CreateTest(source, source, string.Empty, 1);
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Every_class_of_the_file_is_fixed_at_once()
    {
        var source = Wrap("    public class {|#0:First|}\n    {\n        public int Id { get; set; }\n    }\n\n    public class {|#1:Second|}\n    {\n        public string Name { get; set; }\n    }");
        var fixedSource = Wrap("    public class First\n    {\n        public int Id { get; set; }\n\n        public override string ToString() => $\"{Id}\";\n    }\n\n    public class Second\n    {\n        public string Name { get; set; }\n\n        public override string ToString() => $\"{Name}\";\n    }");

        await CreateTest(source, fixedSource, string.Empty, 2).RunAsync().ConfigureAwait(false);
    }
}
