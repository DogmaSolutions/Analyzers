using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA051Tests
{
    // title, types declared before, the declaration ([[Name]] marks the expected type), settings (key=value;...), expected ("scenario|type|members")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the member that identifies an instance -----------------------------------------------------------------------------
        ["A well known property", "", "public class [[Invoice]] { public int Id { get; set; } public decimal Total { get; set; } }", "", E("WellKnown", "Invoice", "Id")],
        ["The list order decides, not the declaration order", "", "public class [[Invoice]] { public string Name { get; set; } public int Id { get; set; } }", "", E("WellKnown", "Invoice", "Id")],
        ["Casing and underscores are ignored", "", "public class [[Invoice]] { private string _first_name; public int Total { get; set; } }", "", E("WellKnown", "Invoice", "_first_name")],
        ["A well known field", "", "public class [[Invoice]] { public string Email; public int Total; }", "", E("WellKnown", "Invoice", "Email")],
        ["The property comes before the field that backs it", "", "public class [[Invoice]] { private int _id; public int Id { get { return _id; } set { _id = value; } } }", "", E("WellKnown", "Invoice", "Id")],
        ["A private well known member", "", "public class [[Invoice]] { private string Description { get; set; } public int Total { get; set; } }", "", E("WellKnown", "Invoice", "Description")],
        ["The configured list replaces the default", "", "public class [[Invoice]] { public int Id { get; set; } public string Code { get; set; } }", "well_known_names=code", E("WellKnown", "Invoice", "Code")],
        ["A configured list with nothing the type has", "", "public class [[Invoice]] { public int Id { get; set; } public string Code { get; set; } }", "well_known_names=nothing", E("PublicProperties", "Invoice", "Id, Code")],

        // ---- the properties and the fields ----------------------------------------------------------------------------------------
        ["Public properties", "", "public class [[Invoice]] { public decimal Total { get; set; } public string Currency { get; set; } }", "", E("PublicProperties", "Invoice", "Total, Currency")],
        ["Only the public properties are listed", "", "public class [[Invoice]] { public decimal Total { get; set; } protected int Hidden { get; set; } private int Secretive { get; set; } }", "", E("PublicProperties", "Invoice", "Total")],
        ["Many public properties", "", "public class [[Invoice]] { public int A { get; set; } public int B { get; set; } public int C { get; set; } public int D { get; set; } public int E { get; set; } public int F { get; set; } }", "", E("PublicProperties", "Invoice", "A, B, C, D and 2 more")],
        ["The most members the text lists", "", "public class [[Invoice]] { public int A { get; set; } public int B { get; set; } public int C { get; set; } }", "max_members=2", E("PublicProperties", "Invoice", "A, B")],
        ["A write-only property is not listed", "", "public class [[Invoice]] { public int A { get; set; } public int B { set { } } }", "", E("PublicProperties", "Invoice", "A")],
        ["Protected properties", "", "public class [[Invoice]] { protected decimal Total { get; set; } protected internal string Currency { get; set; } private protected int Rate { get; set; } internal int Skipped { get; set; } }", "", E("ProtectedProperties", "Invoice", "Total, Currency, Rate")],
        ["Fields", "", "public class [[Invoice]] { private decimal total; internal string currency; protected int rate; }", "", E("Fields", "Invoice", "total, currency, rate")],
        ["The fields when the properties are not public nor protected", "", "public class [[Invoice]] { private decimal total; private int Skipped { get; set; } }", "", E("Fields", "Invoice", "total")],
        ["Internal and private properties", "", "public class [[Invoice]] { internal decimal Total { get; set; } private string Currency { get; set; } }", "", E("OtherProperties", "Invoice", "Total, Currency")],
        ["No properties and no fields", "", "public class [[Invoice]] { public void Pay() { } }", "", E("NoMembers", "Invoice")],
        ["Nothing to show but a secret", "", "public class [[Login]] { public string Password { get; set; } }", "", E("NoMembers", "Login")],
        ["Members that suggest a secret are not listed", "", "public class [[Login]] { public string User { get; set; } public string Password { get; set; } public string ApiToken { get; set; } public string _secret_key { get; set; } }", "", E("PublicProperties", "Login", "User")],
        ["The configured sensitive names replace the default", "", "public class [[Login]] { public string User { get; set; } public string Password { get; set; } public string Nickname { get; set; } }", "sensitive_name_patterns=nick", E("PublicProperties", "Login", "User, Password")],
        ["Static, constant, indexer and explicit members are not listed", "public interface IHasCode { string Code { get; } }", "public class [[Invoice]] : IHasCode { public static int Count { get; set; } public const int Max = 3; public static int Last; public int this[int i] => i; string IHasCode.Code => string.Empty; public decimal Total { get; set; } }", "", E("PublicProperties", "Invoice", "Total")],
        ["The fields of an automatic property are not listed", "", "public class [[Invoice]] { private int Skipped { get; set; } internal decimal Total { get; set; } }", "", E("OtherProperties", "Invoice", "Skipped, Total")],
        ["A partial class", "", "public partial class [[Invoice]] { public int Total { get; set; } }\npublic partial class Invoice { public string Currency { get; set; } }", "", E("PublicProperties", "Invoice", "Total, Currency")],
        ["A struct", "", "public struct [[Point]] { public int X; public int Y; }", "included_type_kinds=class,struct", E("Fields", "Point", "X, Y")],
        ["A ref struct hides what cannot be written in a string", "", "public ref struct [[Window]] { public Span<int> Items; public int Length; }", "included_type_kinds=struct", E("Fields", "Window", "Length")],
        ["A generic class", "", "public class [[Box]]<T> { public T Content { get; set; } public int Size { get; set; } }", "", E("PublicProperties", "Box", "Content, Size")],
        ["A nested class", "", "public class Outer : Exception { public class [[Inner]] { public int Total { get; set; } } }", "", E("PublicProperties", "Inner", "Total")],

        // ---- a ToString that is there --------------------------------------------------------------------------------------------
        ["The type overrides ToString", "", "public class Invoice { public int Id { get; set; } public override string ToString() => string.Empty; }", "", ""],
        ["The type hides ToString", "", "public class Invoice { public int Id { get; set; } public new string ToString() => string.Empty; }", "", ""],
        ["ToString declared in another part", "", "public partial class Invoice { public int Id { get; set; } }\npublic partial class Invoice { public override string ToString() => string.Empty; }", "", ""],
        ["A base type overrides ToString", "public class Document { public override string ToString() => string.Empty; }", "public class Invoice : Document { public int Id { get; set; } }", "", ""],
        ["A base type further up overrides ToString", "public class Document { public override string ToString() => string.Empty; } public class Letter : Document { }", "public class Invoice : Letter { public int Id { get; set; } }", "", ""],
        ["A base type overrides ToString and the option is off", "public class Document { public override string ToString() => string.Empty; }", "public class [[Invoice]] : Document { public int Id { get; set; } }", "consider_inherited_overrides=false", E("WellKnown", "Invoice", "Id")],
        ["A sealed ToString cannot be overridden", "public class Document { public sealed override string ToString() => string.Empty; }", "public class Invoice : Document { public int Id { get; set; } }", "consider_inherited_overrides=false", ""],
        ["A base type without ToString", "[DebuggerDisplay(\"x\")] public class Document { public int Number { get; set; } }", "public class [[Invoice]] : Document { public int Id { get; set; } }", "", E("WellKnown", "Invoice", "Id")],
        ["An exception has its ToString", "", "public class InvoiceException : Exception { public int Id { get; set; } }", "", ""],
        ["A ToString with a parameter is another method", "", "public class [[Invoice]] { public int Id { get; set; } public string ToString(string format) => format; }", "", E("WellKnown", "Invoice", "Id")],
        ["A static ToString is another method", "", "public class [[Invoice]] { public int Id { get; set; } public static string ToString(int id) => string.Empty; }", "", E("WellKnown", "Invoice", "Id")],

        // ---- the types that are not looked at ---------------------------------------------------------------------------------------
        ["Static class", "", "public static class Invoices { public static int Count { get; set; } }", "", ""],
        ["Record", "", "public record Invoice(int Id);", "", ""],
        ["Record with members", "", "public record Invoice { public int Id { get; set; } }", "", ""],
        ["Struct by default", "", "public struct Point { public int X; }", "", ""],
        ["Class when only structs are included", "", "public class Invoice { public int Id { get; set; } }", "included_type_kinds=struct", ""],
        ["Unknown kinds fall back to the default", "", "public class [[Invoice]] { public int Id { get; set; } }", "included_type_kinds=nothing", E("WellKnown", "Invoice", "Id")],
        ["Interface", "", "public interface IInvoice { int Id { get; } }", "", ""],
        ["Enum", "", "public enum Kind { A }", "", ""],
        ["Delegate", "", "public delegate void Handler();", "", ""],
        ["Attribute class", "", "public class InvoiceAttribute : Attribute { public int Id { get; set; } }", "", ""],
        ["Attribute class with the exclusion removed", "", "public class [[InvoiceAttribute]] : Attribute { public int Id { get; set; } }", "excluded_base_types=Nothing.Like.This", E("WellKnown", "InvoiceAttribute", "Id")],
        ["Abstract class", "", "public abstract class Invoice { public int Id { get; set; } }", "include_abstract_classes=false", ""],
        ["Abstract class included", "", "public abstract class [[Invoice]] { public int Id { get; set; } }", "", E("WellKnown", "Invoice", "Id")],
        ["Nested class excluded", "", "public class Outer : Exception { public class Inner { public int Id { get; set; } } }", "include_nested_types=false", ""],
        ["Debugger display", "", "[DebuggerDisplay(\"{Id}\")]\npublic class Invoice { public int Id { get; set; } }", "", ""],
        ["Debugger display on the containing type", "", "[DebuggerDisplay(\"x\")]\npublic class Outer { public class Inner { public int Id { get; set; } } }", "", ""],
        ["Generated code", "", "[System.CodeDom.Compiler.GeneratedCode(\"tool\", \"1\")]\npublic class Invoice { public int Id { get; set; } }", "", ""],
        ["Ignored attributes configured", "public class SkipAttribute : Attribute { }", "[Skip]\npublic class Invoice { public int Id { get; set; } }", "ignored_attributes=TestApp.Skip", ""],
        ["Ignored attributes configured replace the default", "", "[DebuggerDisplay(\"x\")]\npublic class [[Invoice]] { public int Id { get; set; } }", "ignored_attributes=TestApp.Skip", E("WellKnown", "Invoice", "Id")],
        ["An attribute that is not ignored", "public class MarkAttribute : Attribute { }", "[Mark]\npublic class [[Invoice]] { public int Id { get; set; } }", "", E("WellKnown", "Invoice", "Id")],
        ["The minimum number of members", "", "public class Invoice { public int Id { get; set; } }", "min_data_members=2", ""],
        ["The minimum number of members reached", "", "public class [[Invoice]] { public int Id { get; set; } public int Total { get; set; } }", "min_data_members=2", E("WellKnown", "Invoice", "Id")],
        ["The minimum number of members counts also the secrets", "", "public class [[Login]] { public string Password { get; set; } }", "min_data_members=1", E("NoMembers", "Login")],
        ["A type with no members and a minimum", "", "public class Marker { }", "min_data_members=1", ""],
        ["A type with no members", "", "public class [[Marker]] { }", "", E("NoMembers", "Marker")],

        // ---- the exclusions of the options -----------------------------------------------------------------------------------------
        ["Excluded type name", "", "public class InvoiceDto { public int Id { get; set; } }", "excluded_type_name_patterns=*Dto", ""],
        ["Type name exclusion not matching", "", "public class [[Invoice]] { public int Id { get; set; } }", "excluded_type_name_patterns=*Dto", E("WellKnown", "Invoice", "Id")],
        ["Excluded namespace", "", "public class Invoice { public int Id { get; set; } }", "excluded_namespace_patterns=Test*", ""],
        ["Namespace exclusion not matching", "", "public class [[Invoice]] { public int Id { get; set; } }", "excluded_namespace_patterns=Other*", E("WellKnown", "Invoice", "Id")],
        ["Excluded file", "", "public class Invoice { public int Id { get; set; } }", "excluded_file_patterns=Test0.cs", ""],
        ["File exclusion not matching", "", "public class [[Invoice]] { public int Id { get; set; } }", "excluded_file_patterns=Other.cs", E("WellKnown", "Invoice", "Id")],
        ["Excluded base type", "[DebuggerDisplay(\"x\")] public class Entity { }", "public class Invoice : Entity { public int Id { get; set; } }", "excluded_base_types=TestApp.Entity", ""],
        ["Test project", "", "public class Invoice { public int Id { get; set; } }", "test_assembly_names=System.Runtime", ""],
        ["Test project not skipped", "", "public class [[Invoice]] { public int Id { get; set; } }", "test_assembly_names=System.Runtime;skip_test_assemblies=false", E("WellKnown", "Invoice", "Id")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string pre, string declaration, string settings, string expected)
    {
        await RunAsync(pre, declaration, settings, expected).ConfigureAwait(false);
    }
}
