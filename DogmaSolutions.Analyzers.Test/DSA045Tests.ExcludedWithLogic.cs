using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The opposite scenario of DSA045: a type that has logic and is excluded from the code coverage.</summary>
public partial class DSA045Tests
{
    private const string Excluded = "[ExcludeFromCodeCoverage]\n";

    // title, types declared before, the declaration ([[Name]] marks the expected type), settings (key=value;...), expected
    private static IEnumerable<object[]> GetExcludedScenarios =>
    [
        // ---- what the logic is ---------------------------------------------------------------------------------------------------
        ["A method", "", Excluded + "public class [[Report]] { public int Id { get; set; } public string Render() => string.Empty; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Render'")],
        ["An explicit constructor", "", Excluded + "public class [[Report]] { public Report(int id) { Id = id; } public int Id { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "an explicit constructor")],
        ["A parameterless constructor with a body", "", Excluded + "public class [[Report]] { public Report() { Id = 1; } public int Id { get; set; } }", "allow_parameterless_constructor=true", X("class", "Report", "ExcludeFromCodeCoverage", "an explicit constructor")],
        ["A static constructor", "", Excluded + "public class [[Report]] { static Report() { } public int Id { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "a static constructor")],
        ["A finalizer", "", Excluded + "public class [[Report]] { ~Report() { } public int Id { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "a finalizer")],
        ["An operator", "", Excluded + "public class [[Report]] { public static Report operator +(Report a, Report b) => a; public int Id { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "an operator")],
        ["A conversion operator", "", Excluded + "public class [[Report]] { public static implicit operator int(Report r) => 1; public int Id { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "an operator")],
        ["An indexer", "", Excluded + "public class [[Report]] { public int this[int i] => i; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "an indexer")],
        ["A static property", "", Excluded + "public class [[Report]] { public static int Count { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the static property 'Count'")],
        ["A computed property", "", Excluded + "public class [[Report]] { public int A { get; set; } public int Double => A * 2; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the property 'Double', which does more than store a value")],
        ["A property with an accessor body", "", Excluded + "public class [[Report]] { private int _a; public int A { get { return _a + 1; } set { _a = value; } } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the property 'A', which does more than store a value")],
        ["A static field", "", Excluded + "public class [[Report]] { public static int Count; public int Id { get; set; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the static field 'Count'")],
        ["A field initialized by a call", "", Excluded + "public class [[Report]] { public Guid Id = Guid.NewGuid(); }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the field 'Id'")],
        ["A field when fields are not data", "", Excluded + "public class [[Report]] { public int Id; }", "allow_fields=false", X("class", "Report", "ExcludeFromCodeCoverage", "the field 'Id'")],
        ["A nested type", "", Excluded + "public class [[Report]] { public int Id { get; set; } public class Line { } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the nested type 'Line'")],
        ["An event", "", Excluded + "public class [[Report]] { public event EventHandler Changed; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the member 'Changed'")],
        ["The first logic is the one named", "", Excluded + "public class [[Report]] { public int Id { get; set; } public int First() => 1; public int Second() => 2; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'First'")],
        ["A struct", "", Excluded + "public struct [[Point]] { public int X; public int Y; public int Sum() => X + Y; }", "", X("struct", "Point", "ExcludeFromCodeCoverage", "the method 'Sum'")],
        ["A record", "", Excluded + "public record [[Point]](int X, int Y) { public int Sum() => X + Y; }", "", X("record", "Point", "ExcludeFromCodeCoverage", "the method 'Sum'")],
        ["A record struct", "", Excluded + "public record struct [[Point]](int X, int Y) { public int Sum() => X + Y; }", "", X("record struct", "Point", "ExcludeFromCodeCoverage", "the method 'Sum'")],
        ["An abstract class", "", Excluded + "public abstract class [[Report]] { public abstract int Compute(); }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],
        ["A nested type that is excluded itself", "", "public class Outer { [ExcludeFromCodeCoverage] public class [[Report]] { public int Compute() => 1; } }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],

        // ---- how the attribute is written -----------------------------------------------------------------------------------------
        ["Qualified attribute name", "", "[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic class [[Report]] { public int Compute() => 1; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],
        ["Attribute name with the suffix", "", "[ExcludeFromCodeCoverageAttribute]\npublic class [[Report]] { public int Compute() => 1; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],
        ["Attribute with a justification", "", "[ExcludeFromCodeCoverage(Justification = \"UI\")]\npublic class [[Report]] { public int Compute() => 1; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],
        ["Attribute in a list with another one", "", "[Serializable, ExcludeFromCodeCoverage]\npublic class [[Report]] { public int Compute() => 1; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],
        ["Attribute on a part of a partial type", "", Excluded + "public partial class [[Report]] { public int Id { get; set; } }\npublic partial class Report { public int Compute() => 1; }", "", X("class", "Report", "ExcludeFromCodeCoverage", "the method 'Compute'")],
        ["Another exclusion attribute", "public class NoCoverageAttribute : Attribute { }", "[NoCoverage]\npublic class [[Report]] { public int Compute() => 1; }", "exclusion_attributes=TestApp.NoCoverageAttribute", X("class", "Report", "NoCoverage", "the method 'Compute'")],

        // ---- what is not reported --------------------------------------------------------------------------------------------------
        ["A pure data carrier that is excluded", "", Excluded + "public class Report { public int Id { get; set; } }", "", ""],
        ["An empty type that is excluded", "", Excluded + "public class Report { }", "", ""],
        ["An excluded record without logic", "", Excluded + "public record Point(int X, int Y);", "", ""],
        ["The option is off", "", Excluded + "public class Report { public int Compute() => 1; }", "report_excluded_types_with_logic=false", ""],
        ["A nested type with logic under an excluded outer type is not looked at", "", Excluded + "public class Outer { public int Id { get; set; } public class Inner { public int Compute() => 1; } }", "allow_nested_types=true", ""],
        ["The base type has logic, the type does not", "public class Base { public int Compute() => 1; }", Excluded + "public class Report : Base { public int Id { get; set; } }", "", ""],
        ["Another attribute", "", "[Serializable]\npublic class Report { public int Compute() => 1; }", "", ""],
        ["Static class", "", Excluded + "public static class Report { public static int Compute() => 1; }", "", ""],
        ["Generated code", "", "[System.CodeDom.Compiler.GeneratedCode(\"tool\", \"1\")]\n" + Excluded + "public class Report { public int Compute() => 1; }", "", ""],
        ["Excluded type name", "", Excluded + "public class ReportDto { public int Compute() => 1; }", "excluded_type_name_patterns=*Dto", ""],
        ["Excluded namespace", "", Excluded + "public class Report { public int Compute() => 1; }", "excluded_namespace_patterns=Test*", ""],
        ["Excluded file", "", Excluded + "public class Report { public int Compute() => 1; }", "excluded_file_patterns=Test0.cs", ""],
        ["Excluded base type", "public class Entity { }", Excluded + "public class Report : Entity { public int Compute() => 1; }", "excluded_base_types=TestApp.Entity", ""],
        ["Test project", "", Excluded + "public class Report { public int Compute() => 1; }", "test_assembly_names=System.Runtime", ""],
        ["Kind not included", "", Excluded + "public struct Point { public int Sum() => 1; }", "included_type_kinds=class", ""],
        ["Abstract classes not included", "", Excluded + "public abstract class Report { public abstract int Compute(); }", "include_abstract_classes=false", ""],
        ["Another exclusion attribute configured replaces the default", "", Excluded + "public class Report { public int Compute() => 1; }", "exclusion_attributes=TestApp.NoCoverageAttribute", ""],
    ];

    [TestMethod]
    [DynamicData(nameof(GetExcludedScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Excluded_scenario(string title, string pre, string declaration, string settings, string expected)
    {
        await RunAsync(pre, declaration, settings, expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Assembly_wide_exclusion_hides_the_type_with_logic()
    {
        var source = "using System.Diagnostics.CodeAnalysis;\n[assembly: ExcludeFromCodeCoverage]\nnamespace TestApp\n{\n[ExcludeFromCodeCoverage]\npublic class Report { public int Compute() => 1; }\n}";

        var test = await CreateTestAsync(source, string.Empty, string.Empty).ConfigureAwait(false);
        await test.RunAsync().ConfigureAwait(false);
    }
}
