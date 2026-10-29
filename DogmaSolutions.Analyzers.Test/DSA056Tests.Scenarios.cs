using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA056Tests
{
    // title, the source ({|#n:Name|} on the identifier of each fixture expected), settings (key=value;...), the names of the fixtures expected
    private static IEnumerable<object[]> GetScenarios =>
    [
        ["NUnit fixture", "namespace TestApp\n{\n[NUnit.Framework.TestFixture]\npublic class {|#0:OrderTests|}\n{\n    [NUnit.Framework.Test]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["NUnit tests without a fixture attribute", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [NUnit.Framework.Test]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["NUnit test case", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [NUnit.Framework.TestCase(1)]\n    public void Works(int a) { }\n}\n}", "", "OrderTests"],
        ["NUnit set up only", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [NUnit.Framework.SetUp]\n    public void Init() { }\n}\n}", "", "OrderTests"],
        ["xUnit fact", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["xUnit theory", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Theory]\n    [Xunit.InlineData(1)]\n    public void Works(int a) { }\n}\n}", "", "OrderTests"],
        ["xUnit derived fact", "namespace TestApp\n{\npublic class MyFactAttribute : Xunit.FactAttribute { }\npublic class {|#0:OrderTests|}\n{\n    [MyFact]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["MSTest class", "namespace TestApp\n{\n[Microsoft.VisualStudio.TestTools.UnitTesting.TestClass]\npublic class {|#0:OrderTests|}\n{\n}\n}", "", "OrderTests"],
        ["MSTest method without a class attribute", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Microsoft.VisualStudio.TestTools.UnitTesting.TestMethod]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["MSTest data test method", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Microsoft.VisualStudio.TestTools.UnitTesting.DataTestMethod]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["MSTest initialize", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Microsoft.VisualStudio.TestTools.UnitTesting.TestInitialize]\n    public void Init() { }\n}\n}", "", "OrderTests"],
        ["Attribute written with the suffix", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Xunit.FactAttribute]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["Attribute through a using alias", "using F = Xunit.FactAttribute;\nnamespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [F]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["Abstract fixture", "namespace TestApp\n{\npublic abstract class {|#0:BaseTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", "BaseTests"],
        ["Derived class without tests of its own is not a fixture of the rule", "namespace TestApp\n{\n[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic abstract class BaseTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\npublic class Derived : BaseTests { }\n\n}", "", ""],
        ["Fixture with other attributes", "namespace TestApp\n{\n[System.Obsolete]\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", "OrderTests"],
        ["Partial fixture is reported once", "namespace TestApp\n{\npublic partial class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\npublic partial class OrderTests\n{\n    [Xunit.Fact]\n    public void Also() { }\n}\n}", "", "OrderTests"],
        ["Nested fixture of a class that is not one", "namespace TestApp\n{\npublic class Outer\n{\n    public class {|#0:InnerTests|}\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}\n}", "", "InnerTests"],
        ["Nested fixture of an excluded class is reported: the attribute is not applied to the nested types", "namespace TestApp\n{\n[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic class Outer\n{\n    public class {|#0:InnerTests|}\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}\n}", "", "InnerTests"],
        ["Two fixtures", "namespace TestApp\n{\npublic class {|#0:ATests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\npublic class {|#1:BTests|}\n{\n    [NUnit.Framework.Test]\n    public void Works() { }\n}\n}", "", "ATests;BTests"],
        ["Fixture of a generic class", "namespace TestApp\n{\npublic class {|#0:GenericTests|}<T>\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", "GenericTests"],
        ["Excluded fixture", "namespace TestApp\n{\n[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", ""],
        ["Excluded fixture, attribute with the suffix and the global alias", "namespace TestApp\n{\n[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", ""],
        ["Excluded on one part of a partial fixture", "namespace TestApp\n{\n[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic partial class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\npublic partial class OrderTests\n{\n    [Xunit.Fact]\n    public void Also() { }\n}\n}", "", ""],
        ["Excluded assembly", "[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\nnamespace TestApp\n{\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", ""],
        ["A class of the project that is not a fixture", "namespace TestApp\n{\npublic class OrderBuilder\n{\n    public void Test() { }\n    public void Fact() { }\n}\n}", "", ""],
        ["A class with an attribute that is not a test attribute", "namespace TestApp\n{\npublic class OrderBuilder\n{\n    [System.Obsolete]\n    public void Build() { }\n}\n}", "", ""],
        ["A look-alike attribute of the project", "namespace TestApp\n{\npublic class FactAttribute : System.Attribute { }\npublic class OrderBuilder\n{\n    [Fact]\n    public void Build() { }\n}\n}", "", ""],
        ["A static class", "namespace TestApp\n{\npublic static class Helpers\n{\n    [Xunit.Fact]\n    public static void Works() { }\n}\n}", "", ""],
        ["A struct", "namespace TestApp\n{\npublic struct Holder\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "", ""],
        ["An interface", "namespace TestApp\n{\npublic interface IHolder\n{\n    [Xunit.Fact]\n    void Works();\n}\n}", "", ""],
        ["An enum", "namespace TestApp\n{\npublic enum Kind { A, B }\n}", "", ""],
        ["Unused attribute names", "namespace TestApp\n{\npublic class OrderTests\n{\n    public void Test() { }\n}\n}", "", ""],
        ["Test attributes can be changed: another framework is not looked at", "namespace TestApp\n{\npublic class OrderTests\n{\n    [NUnit.Framework.Test]\n    public void Works() { }\n}\n}", "test_attributes=Xunit.FactAttribute", ""],
        ["Test attributes can be changed: the configured one is", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "test_attributes=Xunit.FactAttribute", "OrderTests"],
        ["Test attributes without the suffix", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "test_attributes=Xunit.Fact", "OrderTests"],
        ["Exclusion attributes can be changed", "namespace TestApp\n{\npublic class SkipCoverageAttribute : System.Attribute { }\n[SkipCoverage]\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "exclusion_attributes=TestApp.SkipCoverageAttribute", ""],
        ["Exclusion attributes changed: the standard one no longer counts", "namespace TestApp\n{\n[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "exclusion_attributes=TestApp.SkipCoverageAttribute", "OrderTests"],
        ["Excluded type", "namespace TestApp\n{\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "excluded_type_name_patterns=Order*", ""],
        ["Another excluded type", "namespace TestApp\n{\npublic class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "excluded_type_name_patterns=Other*", "OrderTests"],
        ["Excluded namespace", "namespace TestApp\n{\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "excluded_namespace_patterns=TestApp", ""],
        ["Excluded base type", "namespace TestApp\n{\npublic class Base { }\npublic class OrderTests : Base\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "excluded_base_types=TestApp.Base", ""],
        ["Another excluded base type", "namespace TestApp\n{\npublic class Base { }\npublic class {|#0:OrderTests|} : Base\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "excluded_base_types=TestApp.Other", "OrderTests"],
        ["Excluded file", "namespace TestApp\n{\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n}", "excluded_file_patterns=Test0.cs", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenarios(string title, string source, string settings, string expected)
    {
        await CreateTest(source, settings, expected).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task A_project_that_references_no_test_framework_has_no_fixture()
    {
        // the attribute is of the project, and the rule does not even start
        var source = "namespace TestApp { public class FactAttribute : System.Attribute { } public class OrderTests { [Fact] public void Works() { } } }";
        await CreateTest(source, string.Empty, string.Empty, ReferenceAssemblies.Net.Net80).RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task A_project_whose_assembly_is_excluded_has_no_fixture_to_report()
    {
        var source = "[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\nnamespace TestApp { public class OrderTests { [Xunit.Fact] public void Works() { } } }";
        await CreateTest(source, string.Empty, string.Empty).RunAsync().ConfigureAwait(false);
    }
}
