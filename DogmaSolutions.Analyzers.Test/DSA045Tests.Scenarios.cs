using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData
#pragma warning disable SA1122 // empty strings are the "nothing" of the scenarios

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA045Tests
{
    private const string Two = "public int A { get; set; } public string B { get; set; }";

    // title, types declared before, the declaration ([[Name]] marks the expected type), settings (key=value;...), expected ("kind|name|members|note")
    private static IEnumerable<object[]> GetScenarios =>
    [
        // ---- the properties ---------------------------------------------------------------------------------------------
        ["Auto properties", "", "public class [[Dto]] { " + Two + " }", "", E("class", "Dto", "2 data members")],
        ["A single property", "", "public class [[Dto]] { public int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Init, get-only and private setters", "", "public class [[Dto]] { public int A { get; init; } public int B { get; } public int C { get; private set; } }", "", E("class", "Dto", "3 data members")],
        ["Required property", "", "public class [[Dto]] { public required string A { get; init; } }", "", E("class", "Dto", "1 data member")],
        ["Attributes and documentation", "", "/// <summary>A data carrier.</summary>\n[Serializable]\npublic class [[Dto]]\n{\n    /// <summary>The A.</summary>\n    [Obsolete] public int A { get; set; }\n}", "", E("class", "Dto", "1 data member")],
        ["Virtual property", "", "public class [[Dto]] { public virtual int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Generic type", "", "public class [[Dto]]<T> { public T Value { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Interface implemented by the properties", "public interface IDto { int A { get; set; } }", "public class [[Dto]] : IDto { public int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Properties of any accessibility", "", "public class [[Dto]] { internal int A { get; set; } protected int B { get; set; } private int C { get; set; } }", "", E("class", "Dto", "3 data members")],
        ["Internal and private-nested types", "", "internal class [[Dto]] { public int A { get; set; } }", "", E("class", "Dto", "1 data member")],

        // ---- the initializers ----------------------------------------------------------------------------------------------
        ["Literal initializer", "", "public class [[Dto]] { public int A { get; set; } = 5; }", "", E("class", "Dto", "1 data member")],
        ["Constant and static readonly initializers", "", "public class [[Dto]] { public string A { get; set; } = string.Empty; public System.DayOfWeek D { get; set; } = System.DayOfWeek.Monday; public int B { get; set; } = int.MaxValue; }", "", E("class", "Dto", "3 data members")],
        ["Object creation initializers", "", "public class [[Dto]] { public List<string> A { get; set; } = new List<string>(); public List<int> B { get; set; } = new(); public List<int> C { get; set; } = new List<int>(16); }", "", E("class", "Dto", "3 data members")],
        ["Object initializer", "[ExcludeFromCodeCoverage] public class Item { public int X { get; set; } }", "public class [[Dto]] { public Item A { get; set; } = new Item { X = 1 }; }", "", E("class", "Dto", "1 data member")],
        ["Array and collection initializers", "", "public class [[Dto]] { public int[] A { get; set; } = new[] { 1, 2 }; public int[] B { get; set; } = new int[3]; public List<int> C { get; set; } = new List<int> { 1, 2 }; public int[] D { get; set; } = [1, 2]; }", "", E("class", "Dto", "4 data members")],
        ["Negative, parenthesized, typeof and default initializers", "", "public class [[Dto]] { public int A { get; set; } = -1; public int B { get; set; } = (2); public Type C { get; set; } = typeof(int); public int D { get; set; } = default; public int E { get; set; } = default(int); public bool F { get; set; } = !true; public int G { get; set; } = +1; public int H { get; set; } = ~1; }", "", E("class", "Dto", "8 data members")],
        ["Method call initializer", "", "public class Dto { public Guid A { get; set; } = Guid.NewGuid(); }", "", ""],
        ["Generic method call initializer", "", "public class Dto { public int[] A { get; set; } = Array.Empty<int>(); }", "", ""],
        ["Object creation with a call as argument", "", "public class Dto { public List<int> A { get; set; } = new List<int>(Compute()); private static int Compute() => 1; }", "", ""],
        ["Object initializer with a call", "[ExcludeFromCodeCoverage] public class Item { public int X { get; set; } }", "public class Dto { public Item A { get; set; } = new Item { X = Compute() }; private static int Compute() => 1; }", "", ""],
        ["Collection with a call", "", "public class Dto { public int[] A { get; set; } = [Compute()]; private static int Compute() => 1; }", "", ""],
        ["Collection with a spread", "", "public class Dto { public int[] A { get; set; } = [..new[] { 1 }]; }", "", ""],
        ["Conditional initializer", "", "public class Dto { public int A { get; set; } = DateTime.Now.Year > 1 ? 1 : 2; }", "", ""],
        ["Initializers refused by the option", "", "public class Dto { public int A { get; set; } = 5; }", "allow_property_initializers=false", ""],
        ["Properties without initializers with the option off", "", "public class [[Dto]] { public int A { get; set; } }", "allow_property_initializers=false", E("class", "Dto", "1 data member")],

        // ---- the fields -----------------------------------------------------------------------------------------------------
        ["Public fields", "", "public class [[Dto]] { public int A; public string B; }", "", E("class", "Dto", "2 data members")],
        ["Field with an initializer", "", "public class [[Dto]] { public int A = 1; public readonly string B = string.Empty; }", "", E("class", "Dto", "2 data members")],
        ["Field with a call as initializer", "", "public class Dto { public Guid A = Guid.NewGuid(); }", "", ""],
        ["Field with an initializer refused by the option", "", "public class Dto { public int A = 1; }", "allow_property_initializers=false", ""],
        ["Fields refused by the option", "", "public class Dto { public int A; }", "allow_fields=false", ""],
        ["Constants are not data", "", "public class [[Dto]] { public const int Max = 3; public int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Static field", "", "public class Dto { public static int Counter; public int A { get; set; } }", "", ""],
        ["Several fields in a declaration", "", "public class [[Dto]] { public int A, B, C; }", "", E("class", "Dto", "3 data members")],

        // ---- the logic -----------------------------------------------------------------------------------------------------
        ["Method", "", "public class Dto { public int A { get; set; } public int Twice() => A * 2; }", "", ""],
        ["Override of ToString", "", "public class Dto { public int A { get; set; } public override string ToString() => \"x\"; }", "", ""],
        ["Operator", "", "public class Dto { public int A { get; set; } public static bool operator ==(Dto a, Dto b) => true; public static bool operator !=(Dto a, Dto b) => false; public override bool Equals(object o) => true; public override int GetHashCode() => 1; }", "", ""],
        ["Conversion operator", "", "public class Dto { public int A { get; set; } public static implicit operator int(Dto d) => d.A; }", "", ""],
        ["Event", "", "public class Dto { public int A { get; set; } public event EventHandler Changed; }", "", ""],
        ["Indexer", "", "public class Dto { public int this[int i] { get => i; } public int A { get; set; } }", "", ""],
        ["Expression-bodied property", "", "public class Dto { public int A { get; set; } public int Twice => A * 2; }", "", ""],
        ["Property with a getter body", "", "public class Dto { public int A { get { return 1; } set { } } }", "", ""],
        ["Property with a setter body", "", "public class Dto { public int A { get { return 1; } set { } } }", "", ""],
        ["Computed properties allowed", "", "public class [[Dto]] { public int A { get; set; } public int Twice => A * 2; }", "allow_computed_properties=true", E("class", "Dto", "2 data members")],
        ["Property with bodies allowed", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } set { _a = value; } } }", "allow_computed_properties=true;allow_backing_field_properties=false", E("class", "Dto", "2 data members")],
        ["Static property", "", "public class Dto { public static int Counter { get; set; } public int A { get; set; } }", "", ""],
        ["Static constructor", "", "public class Dto { static Dto() { } public int A { get; set; } }", "", ""],
        ["Finalizer", "", "public class Dto { ~Dto() { } public int A { get; set; } }", "", ""],
        ["Nested class", "", "public class Dto { public int A { get; set; } public class Inner { } }", "", ""],
        ["Nested enum", "", "public class Dto { public int A { get; set; } public enum Kind { X } }", "", ""],
        ["Nested types allowed", "", "public class [[Dto]] { public int A { get; set; } public enum Kind { X } }", "allow_nested_types=true", E("class", "Dto", "1 data member")],
        ["Nothing but a constant", "", "public class Dto { public const int Max = 3; }", "", ""],

        // ---- the constructors ----------------------------------------------------------------------------------------------
        ["Explicit parameterless constructor", "", "public class Dto { public Dto() { } public int A { get; set; } }", "", ""],
        ["Explicit parameterless constructor allowed", "", "public class [[Dto]] { public Dto() { } public int A { get; set; } }", "allow_parameterless_constructor=true", E("class", "Dto", "1 data member")],
        ["Constructor with a statement", "", "public class Dto { public Dto() { A = 1; } public int A { get; set; } }", "allow_parameterless_constructor=true", ""],
        ["Constructor with an initializer", "", "public class Dto { public Dto() : this(1) { } public Dto(int a) { } public int A { get; set; } }", "allow_parameterless_constructor=true", ""],
        ["Constructor with an expression body", "", "public class Dto { public Dto() => A = 1; public int A { get; set; } }", "allow_parameterless_constructor=true", ""],
        ["Constructor with parameters", "", "public class Dto { public Dto(int a) { A = a; } public int A { get; set; } }", "allow_parameterless_constructor=true", ""],
        ["Primary constructor of a class", "", "public class Dto(int a) { public int A { get; set; } = a; }", "", ""],
        ["Primary constructor of a struct", "", "public struct Dto(int a) { public int A { get; set; } = a; }", "included_type_kinds=struct", ""],

        // ---- the kinds of type -----------------------------------------------------------------------------------------------
        ["Struct by default", "", "public struct [[Dto]] { public int A { get; set; } }", "", E("struct", "Dto", "1 data member")],
        ["Struct opted out", "", "public struct Dto { public int A { get; set; } }", "included_type_kinds=class, record, record_struct", ""],
        ["Class opted out", "", "public class Dto { public int A { get; set; } }", "included_type_kinds=struct, record, record_struct", ""],
        ["Record opted out", "", "public record Dto(int A);", "included_type_kinds=class, struct, record_struct", ""],
        ["Record struct opted out", "", "public record struct Dto(int A);", "included_type_kinds=class, struct, record", ""],
        ["Struct", "", "public struct [[Dto]] { public int A { get; set; } }", "included_type_kinds=struct", E("struct", "Dto", "1 data member")],
        ["Positional record by default", "", "public record [[Dto]](int A);", "", E("record", "Dto", "1 data member")],
        ["Record struct by default", "", "public record struct [[Dto]](int A);", "", E("record struct", "Dto", "1 data member")],
        ["Positional record", "", "public record [[Dto]](int A, string B);", "included_type_kinds=record", E("record", "Dto", "2 data members")],
        ["Record with properties", "", "public record [[Dto]] { public int A { get; init; } }", "included_type_kinds=record", E("record", "Dto", "1 data member")],
        ["Record with positional and declared properties", "", "public record [[Dto]](int A) { public int B { get; init; } }", "included_type_kinds=record", E("record", "Dto", "2 data members")],
        ["Record with a method", "", "public record Dto(int A) { public int Twice() => A * 2; }", "included_type_kinds=record", ""],
        ["Record struct", "", "public record struct [[Dto]](int A);", "included_type_kinds=record_struct", E("record struct", "Dto", "1 data member")],
        ["Record struct spelled with a space", "", "public record struct [[Dto]](int A);", "included_type_kinds=record struct", E("record struct", "Dto", "1 data member")],
        ["Several kinds", "", "public struct [[Dto]] { public int A { get; set; } }", "included_type_kinds=class, struct, record", E("struct", "Dto", "1 data member")],
        ["Unknown kinds fall back to the default", "", "public class [[Dto]] { public int A { get; set; } }", "included_type_kinds=nonsense", E("class", "Dto", "1 data member")],
        ["Class not looked at when only records are", "", "public class Dto { public int A { get; set; } }", "included_type_kinds=record", ""],
        ["Interface", "", "public interface IDto { int A { get; set; } }", "", ""],
        ["Enum", "", "public enum Kind { A }", "", ""],
        ["Static class", "", "public static class Dto { public static int A { get; set; } }", "", ""],
        ["Empty class", "", "public class Dto { }", "", ""],
        ["Empty class allowed", "", "public class [[Dto]] { }", "min_data_members=0", E("class", "Dto", "0 data members")],
        ["More data members required", "", "public class Dto { public int A { get; set; } }", "min_data_members=2", ""],
        ["Enough data members", "", "public class [[Dto]] { public int A { get; set; } public int B { get; set; } }", "min_data_members=2", E("class", "Dto", "2 data members")],
        ["Abstract class", "", "public abstract class [[Dto]] { public abstract int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Abstract class refused", "", "public abstract class Dto { public abstract int A { get; set; } }", "include_abstract_classes=false", ""],
        ["Nested type", "", "public class Outer { public void Run() { } public class [[Dto]] { public int A { get; set; } } }", "", E("class", "Dto", "1 data member")],
        ["Nested type refused", "", "public class Outer { public void Run() { } public class Dto { public int A { get; set; } } }", "include_nested_types=false", ""],
        ["Partial type with a pure part", "", "public partial class [[Dto]] { public int A { get; set; } }\npublic partial class Dto { public int B { get; set; } }", "", E("class", "Dto", "2 data members")],
        ["Partial type with a logic part", "", "public partial class Dto { public int A { get; set; } }\npublic partial class Dto { public void Run() { } }", "", ""],


        // ---- old-style properties that an auto-property could replace -----------------------------------------------------------
        ["Property over a private field", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } set { _a = value; } } }", "", E("class", "Dto", "1 data member")],
        ["Property over a private field, expression bodies", "", "public class [[Dto]] { private int _a; public int A { get => _a; set => _a = value; } }", "", E("class", "Dto", "1 data member")],
        ["Property over a private field, qualified with this", "", "public class [[Dto]] { private int _a; public int A { get { return this._a; } set { this._a = value; } } }", "", E("class", "Dto", "1 data member")],
        ["Property over a private field, parenthesized", "", "public class [[Dto]] { private int _a; public int A { get { return (_a); } set { _a = value; } } }", "", E("class", "Dto", "1 data member")],
        ["Property over a private field, init accessor", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } init { _a = value; } } }", "", E("class", "Dto", "1 data member")],
        ["Property over a private field, private setter", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } private set { _a = value; } } }", "", E("class", "Dto", "1 data member")],
        ["Get-only property over a private field", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } } }", "", E("class", "Dto", "1 data member")],
        ["Get-only expression-bodied property over a private field", "", "public class [[Dto]] { private readonly int _a = 3; public int A => _a; }", "", E("class", "Dto", "1 data member")],
        ["Property over a private field, setter first", "", "public class [[Dto]] { private int _a; public int A { set { _a = value; } get { return _a; } } }", "", E("class", "Dto", "1 data member")],
        ["Property over a field declared with others", "", "public class [[Dto]] { private int _a, _b; public int A { get { return _a; } set { _a = value; } } public int B { get { return _b; } set { _b = value; } } }", "", E("class", "Dto", "2 data members")],
        ["Property over a field with a simple initializer", "", "public class [[Dto]] { private List<int> _a = new(); public List<int> A { get { return _a; } set { _a = value; } } }", "", E("class", "Dto", "1 data member")],
        ["Property over a field with an initializer that runs code", "", "public class Dto { private Guid _a = Guid.NewGuid(); public Guid A { get { return _a; } set { _a = value; } } }", "", ""],
        ["Property over a field with an initializer refused", "", "public class Dto { private int _a = 5; public int A { get { return _a; } set { _a = value; } } }", "allow_property_initializers=false", ""],
        ["Old-style and auto-properties together", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } set { _a = value; } } public string B { get; set; } }", "", E("class", "Dto", "2 data members")],
        ["Backing field exempt from the fields option", "", "public class [[Dto]] { private int _a; public int A { get { return _a; } set { _a = value; } } }", "allow_fields=false", E("class", "Dto", "1 data member")],
        ["Old-style properties refused by the option", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a = value; } } }", "allow_backing_field_properties=false", ""],
        ["Old-style properties refused, the fields option does not matter", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a = value; } } }", "allow_backing_field_properties=false;allow_fields=true", ""],
        ["Setter with logic", "", "public class Dto { private int _a; public int A { get { return _a; } set { if (value > 0) _a = value; } } }", "", ""],
        ["Setter with a notification", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a = value; Changed?.Invoke(this, EventArgs.Empty); } } public event EventHandler Changed; }", "", ""],
        ["Getter with logic", "", "public class Dto { private int _a; public int A { get { return _a + 1; } set { _a = value; } } }", "", ""],
        ["Getter with a statement before", "", "public class Dto { private int _a; public int A { get { var x = _a; return x; } set { _a = value; } } }", "", ""],
        ["Getter and setter on different fields", "", "public class Dto { private int _a, _b; public int A { get { return _a; } set { _b = value; } } }", "", ""],
        ["Setter that does not assign value", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a = 5; } } }", "", ""],
        ["Setter with a compound assignment", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a += value; } } }", "", ""],
        ["Set-only property", "", "public class Dto { private int _a; public int A { set { _a = value; } } }", "", ""],
        ["Field of another type", "", "public class Dto { private long _a; public int A { get { return (int)_a; } set { _a = value; } } }", "", ""],
        ["Field of a type that converts", "", "public class Dto { private int _a; public long A { get { return _a; } set { _a = (int)value; } } }", "", ""],
        ["Public field behind the property", "", "public class Dto { public int _a; public int A { get { return _a; } set { _a = value; } } }", "", ""],
        ["Protected field behind the property", "", "public class Dto { protected int _a; public int A { get { return _a; } set { _a = value; } } }", "", ""],
        ["Volatile field behind the property", "", "public class Dto { private volatile int _a; public int A { get { return _a; } set { _a = value; } } }", "", ""],
        ["Static field behind the property", "", "public class Dto { private static int _a; public int A { get { return _a; } set { _a = value; } } }", "", ""],
        ["Field shared by two properties", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a = value; } } public int B { get { return _a; } set { _a = value; } } }", "", ""],
        ["Field that is not in the type", "", "public class Dto { public int A { get { return Other; } set { Other = value; } } private static int Other; }", "", ""],
        ["Property returning a property", "", "public class Dto { public int X { get; set; } public int A { get { return X; } set { X = value; } } }", "", ""],
        ["Read-only field with a setter", "", "public class Dto { private readonly int _a; public int A { get { return _a; } set { } } }", "", ""],
        ["Indexer over a field", "", "public class Dto { private int _a; public int this[int i] { get { return _a; } set { _a = value; } } }", "", ""],
        ["Static property over a static field", "", "public class Dto { private static int _a; public static int A { get { return _a; } set { _a = value; } } public int B { get; set; } }", "", ""],
        ["Field used by a method too", "", "public class Dto { private int _a; public int A { get { return _a; } set { _a = value; } } public void Reset() { _a = 0; } }", "", ""],
        ["Old-style properties of the base type", "[ExcludeFromCodeCoverage] public class Base { private int _id; public int Id { get { return _id; } set { _id = value; } } }", "public class [[Dto]] : Base { public string Name { get; set; } }", "", E("class", "Dto", "2 data members", true)],
        ["Old-style properties of the library base type", "", "public class [[Dto]] : TestLib.BackedBase { public string Name { get; set; } }", "", E("class", "Dto", "2 data members", true)],
        ["Old-style properties of the library base type, the fields option does not matter", "", "public class [[Dto]] : TestLib.BackedBase { public string Name { get; set; } }", "allow_fields=false", E("class", "Dto", "2 data members", true)],

        // ---- the base types -------------------------------------------------------------------------------------------------
        ["Base type that is a data carrier", "[ExcludeFromCodeCoverage] public class Base { public int Id { get; set; } }", "public class [[Dto]] : Base { public string Name { get; set; } }", "", E("class", "Dto", "2 data members", true)],
        ["Chain of data carriers", "[ExcludeFromCodeCoverage] public class A { public int X { get; set; } } [ExcludeFromCodeCoverage] public class B : A { public int Y { get; set; } }", "public class [[Dto]] : B { public int Z { get; set; } }", "", E("class", "Dto", "3 data members", true)],
        ["The base type is reported too", "", "public class [[Base]] { public int Id { get; set; } }\n[ExcludeFromCodeCoverage] public class Derived : Base { public string Name { get; set; } }", "", E("class", "Base", "1 data member")],
        ["Base type with a method", "[ExcludeFromCodeCoverage] public class Base { public int Id { get; set; } public int Compute() => 1; }", "public class Dto : Base { public string Name { get; set; } }", "", ""],
        ["Distant base type with a method", "public class A { public int Compute() => 1; } public class B : A { public int Y { get; set; } }", "public class Dto : B { public int Z { get; set; } }", "", ""],
        ["Base type with a constructor", "public class Base { public Base(int id) { } public int Id { get; set; } }", "public class Dto : Base { public Dto() : base(1) { } public string Name { get; set; } }", "allow_parameterless_constructor=true", ""],
        ["Base type with a computed property", "public class Base { public int Id { get; set; } public int Twice => Id * 2; }", "public class Dto : Base { public string Name { get; set; } }", "", ""],
        ["Base type inspection off", "[ExcludeFromCodeCoverage] public class Base { public int Id { get; set; } public int Compute() => 1; }", "public class [[Dto]] : Base { public string Name { get; set; } }", "inspect_base_types=false", E("class", "Dto", "1 data member")],
        ["Excluded base type", "[ExcludeFromCodeCoverage] public class Base { public int Id { get; set; } }", "public class Dto : Base { public string Name { get; set; } }", "excluded_base_types=TestApp.Base", ""],
        ["Base type of the library that is a data carrier", "", "public class [[Dto]] : TestLib.PureBase { public string Extra { get; set; } }", "", E("class", "Dto", "3 data members", true)],
        ["Base type of the library with a method", "", "public class Dto : TestLib.ImpureBase { public string Extra { get; set; } }", "", ""],
        ["Base type of the library with a constructor", "", "public class Dto : TestLib.ParameterBase { public Dto() : base(1) { } public string Extra { get; set; } }", "allow_parameterless_constructor=true", ""],
        ["Base type of the library with a static field", "", "public class Dto : TestLib.StaticBase { public string Extra { get; set; } }", "", ""],
        ["Base type of the library with a field", "", "public class [[Dto]] : TestLib.FieldBase { public string Extra { get; set; } }", "", E("class", "Dto", "2 data members", true)],
        ["Base type of the library with a field, fields refused", "", "public class Dto : TestLib.FieldBase { public string Extra { get; set; } }", "allow_fields=false", ""],
        ["Base type of the library with an indexer", "", "public class Dto : TestLib.IndexerBase { public string Extra { get; set; } }", "", ""],
        ["Empty base type of the library", "", "public class [[Dto]] : TestLib.EmptyBase { public string Extra { get; set; } }", "", E("class", "Dto", "1 data member", true)],
        ["Record base type of the library", "", "public record Dto(int Id) : TestLib.RecordBase(Id) { public string Extra { get; init; } }", "included_type_kinds=record", ""],
        ["Framework base type", "", "public class Dto : Exception { public string Extra { get; set; } }", "", ""],
        ["Framework collection base type", "", "public class Dto : List<int> { public string Extra { get; set; } }", "", ""],

        // ---- already excluded from the coverage ------------------------------------------------------------------------------
        ["Already excluded", "", "[ExcludeFromCodeCoverage]\npublic class Dto { public int A { get; set; } }", "", ""],
        ["Already excluded, long attribute name", "", "[ExcludeFromCodeCoverageAttribute]\npublic class Dto { public int A { get; set; } }", "", ""],
        ["Already excluded, qualified attribute name", "", "[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]\npublic class Dto { public int A { get; set; } }", "", ""],
        ["Already excluded with a justification", "", "[ExcludeFromCodeCoverage(Justification = \"data\")]\npublic class Dto { public int A { get; set; } }", "", ""],
        ["Already excluded on another part", "", "[ExcludeFromCodeCoverage]\npublic partial class Dto { public int A { get; set; } }\npublic partial class Dto { public int B { get; set; } }", "", ""],
        ["Already excluded on the outer type", "", "[ExcludeFromCodeCoverage]\npublic class Outer { public void Run() { } public class Dto { public int A { get; set; } } }", "", ""],
        ["Excluded outer type does not hide an unrelated type", "", "[ExcludeFromCodeCoverage]\npublic class Other { public void Run() { } }\npublic class [[Dto]] { public int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Another attribute is not an exclusion", "", "[Obsolete]\npublic class [[Dto]] { public int A { get; set; } }", "", E("class", "Dto", "1 data member")],
        ["Exclusion attribute configured", "public class NoCoverageAttribute : Attribute { }", "[NoCoverage]\npublic class Dto { public int A { get; set; } }", "exclusion_attributes=TestApp.NoCoverage", ""],
        ["Exclusion attribute configured replaces the default", "public class NoCoverageAttribute : Attribute { }", "[ExcludeFromCodeCoverage]\npublic class [[Dto]] { public int A { get; set; } }", "exclusion_attributes=TestApp.NoCoverage", E("class", "Dto", "1 data member")],
        ["Generated code attribute", "", "[System.CodeDom.Compiler.GeneratedCode(\"tool\", \"1.0\")]\npublic class Dto { public int A { get; set; } }", "", ""],
        ["Compiler generated attribute", "", "[System.Runtime.CompilerServices.CompilerGenerated]\npublic class Dto { public int A { get; set; } }", "", ""],
        ["Generated code attribute on the outer type", "", "[System.CodeDom.Compiler.GeneratedCode(\"tool\", \"1.0\")]\npublic class Outer { public class Dto { public int A { get; set; } } }", "", ""],
        ["Ignored attributes configured", "public class SkipAttribute : Attribute { }", "[Skip]\npublic class Dto { public int A { get; set; } }", "ignored_attributes=TestApp.Skip", ""],
        ["Ignored attributes configured replace the default", "public class SkipAttribute : Attribute { } public class OtherAttribute : Attribute { }", "[Skip]\npublic class [[Dto]] { public int A { get; set; } }", "ignored_attributes=TestApp.Other", E("class", "Dto", "1 data member")],

        // ---- the exclusions of the options -------------------------------------------------------------------------------------
        ["Excluded type name", "", "public class OrderDto { public int A { get; set; } }", "excluded_type_name_patterns=*Dto", ""],
        ["Type name exclusion not matching", "", "public class [[Order]] { public int A { get; set; } }", "excluded_type_name_patterns=*Dto", E("class", "Order", "1 data member")],
        ["Excluded namespace", "", "public class Dto { public int A { get; set; } }", "excluded_namespace_patterns=Test*", ""],
        ["Namespace exclusion not matching", "", "public class [[Dto]] { public int A { get; set; } }", "excluded_namespace_patterns=Other*", E("class", "Dto", "1 data member")],
        ["Excluded file", "", "public class Dto { public int A { get; set; } }", "excluded_file_patterns=Test0.cs", ""],
        ["File exclusion not matching", "", "public class [[Dto]] { public int A { get; set; } }", "excluded_file_patterns=Other.cs", E("class", "Dto", "1 data member")],
        ["Test project", "", "public class Dto { public int A { get; set; } }", "test_assembly_names=System.Runtime", ""],
        ["Test project not skipped", "", "public class [[Dto]] { public int A { get; set; } }", "test_assembly_names=System.Runtime;skip_test_assemblies=false", E("class", "Dto", "1 data member")],
        ["Test assembly names not matching", "", "public class [[Dto]] { public int A { get; set; } }", "test_assembly_names=Nothing.Like.This", E("class", "Dto", "1 data member")],
    ];

    [TestMethod]
    [DynamicData(nameof(GetScenarios), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task Scenario(string title, string pre, string declaration, string settings, string expected)
    {
        await RunAsync(pre, declaration, settings, expected).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Assembly_wide_exclusion()
    {
        var source = "using System.Diagnostics.CodeAnalysis;\n[assembly: ExcludeFromCodeCoverage]\nnamespace TestApp\n{\npublic class Dto { public int A { get; set; } }\n}";

        var test = await CreateTestAsync(source, string.Empty, string.Empty).ConfigureAwait(false);
        await test.RunAsync().ConfigureAwait(false);
    }
}
