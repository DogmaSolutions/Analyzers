using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA010, and the helpers that classify the types of the fields.</summary>
[TestClass]
public class DSA010SettingsTests
{
    private sealed class Options : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _values;

        internal Options(params (string Key, string Value)[] values) => _values = values.ToDictionary(v => v.Key, v => v.Value);

        public override bool TryGetValue(string key, out string value) => _values.TryGetValue(key, out value);
    }

    [TestMethod]
    public void The_defaults_apply_when_nothing_is_configured()
    {
        var settings = new DSA010Settings(new Options());

        Assert.IsTrue(settings.ReportReads);
        Assert.IsTrue(settings.ReportWrites);
        Assert.IsTrue(settings.ReportSharedLocks);
        Assert.IsFalse(settings.ReportLockOnThis);
        Assert.IsTrue(settings.ReportTearableTypes);
        Assert.IsTrue(settings.ReportVolatileFields);
        Assert.IsTrue(settings.ReportReadonlyFields);
        Assert.IsTrue(settings.SkipSignaledLocks);
        Assert.IsTrue(settings.SoleStatementOnly);
        Assert.AreEqual(DSA010MemberKinds.Accessor | DSA010MemberKinds.Method, settings.MemberKinds);
        Assert.AreEqual(0, settings.ExcludedTypeNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedBaseTypes.Length);
        Assert.AreEqual(0, settings.ExcludedMemberNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedFieldNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedLockNamePatterns.Length);
        Assert.AreEqual(0, settings.ExcludedFieldTypes.Length);
        CollectionAssert.AreEqual(AnalyzersUtils.DefaultExcludedFilePatterns.ToArray(), settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    public void Every_option_can_be_changed()
    {
        var settings = new DSA010Settings(new Options(
            (DSA010Settings.ReportReadsKey, "false"),
            (DSA010Settings.ReportWritesKey, "false"),
            (DSA010Settings.ReportSharedLocksKey, "false"),
            (DSA010Settings.ReportLockOnThisKey, "true"),
            (DSA010Settings.ReportTearableTypesKey, "false"),
            (DSA010Settings.ReportVolatileFieldsKey, "false"),
            (DSA010Settings.ReportReadonlyFieldsKey, "false"),
            (DSA010Settings.SkipSignaledLocksKey, "false"),
            (DSA010Settings.BodyShapeKey, "any_statement"),
            (DSA010Settings.MemberKindsKey, "local_function, Lambda,constructor, other"),
            (DSA010Settings.ExcludedTypeNamePatternsKey, "*Dto, *Options"),
            (DSA010Settings.ExcludedNamespacePatternsKey, "*.Generated"),
            (DSA010Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA010Settings.ExcludedMemberNamePatternsKey, "Get*"),
            (DSA010Settings.ExcludedFieldNamePatternsKey, "_x*"),
            (DSA010Settings.ExcludedLockNamePatternsKey, "_sync*"),
            (DSA010Settings.ExcludedFieldTypesKey, "int,System.DateTime"),
            (DSA010Settings.ExcludedFilePatternsKey, "*.Legacy.cs")));

        Assert.IsFalse(settings.ReportReads);
        Assert.IsFalse(settings.ReportWrites);
        Assert.IsFalse(settings.ReportSharedLocks);
        Assert.IsTrue(settings.ReportLockOnThis);
        Assert.IsFalse(settings.ReportTearableTypes);
        Assert.IsFalse(settings.ReportVolatileFields);
        Assert.IsFalse(settings.ReportReadonlyFields);
        Assert.IsFalse(settings.SkipSignaledLocks);
        Assert.IsFalse(settings.SoleStatementOnly);
        Assert.AreEqual(DSA010MemberKinds.LocalFunction | DSA010MemberKinds.Lambda | DSA010MemberKinds.Constructor | DSA010MemberKinds.Other, settings.MemberKinds);
        CollectionAssert.AreEqual(new[] { "*Dto", "*Options" }, settings.ExcludedTypeNamePatterns);
        CollectionAssert.AreEqual(new[] { "*.Generated" }, settings.ExcludedNamespacePatterns);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes);
        CollectionAssert.AreEqual(new[] { "Get*" }, settings.ExcludedMemberNamePatterns);
        CollectionAssert.AreEqual(new[] { "_x*" }, settings.ExcludedFieldNamePatterns);
        CollectionAssert.AreEqual(new[] { "_sync*" }, settings.ExcludedLockNamePatterns);
        CollectionAssert.AreEqual(new[] { "int", "System.DateTime" }, settings.ExcludedFieldTypes);
        CollectionAssert.AreEqual(new[] { "*.Legacy.cs" }, settings.ExcludedFilePatterns.ToArray());
    }

    [TestMethod]
    [DataRow("sole_statement", true)]
    [DataRow("SOLE_STATEMENT", true)]
    [DataRow("any_statement", false)]
    [DataRow("Any_Statement", false)]
    [DataRow("whatever", true)]
    [DataRow("", true)]
    public void The_shape_of_the_body(string value, bool soleStatement)
    {
        Assert.AreEqual(soleStatement, new DSA010Settings(new Options((DSA010Settings.BodyShapeKey, value))).SoleStatementOnly);
    }

    [TestMethod]
    [DataRow("yes")]
    [DataRow("1")]
    [DataRow("")]
    public void An_invalid_boolean_falls_back_to_the_default(string value)
    {
        var settings = new DSA010Settings(new Options((DSA010Settings.ReportReadsKey, value), (DSA010Settings.ReportLockOnThisKey, value)));

        Assert.IsTrue(settings.ReportReads);
        Assert.IsFalse(settings.ReportLockOnThis);
    }

    [TestMethod]
    [DataRow("accessor", 1)]
    [DataRow("method,accessor", DSA010MemberKinds.Method | DSA010MemberKinds.Accessor)]
    [DataRow("local_function", 4)]
    [DataRow("LocalFunction", 4)]
    [DataRow("nonsense, accessor", 1)]
    [DataRow("nonsense", DSA010MemberKinds.Accessor | DSA010MemberKinds.Method)]
    [DataRow("none", DSA010MemberKinds.Accessor | DSA010MemberKinds.Method)]
    [DataRow("", DSA010MemberKinds.Accessor | DSA010MemberKinds.Method)]
    public void The_kinds_of_member(string value, int expected)
    {
        Assert.AreEqual((DSA010MemberKinds)expected, new DSA010Settings(new Options((DSA010Settings.MemberKindsKey, value))).MemberKinds);
    }

    [TestMethod]
    public void The_settings_are_computed_once_per_options_instance()
    {
        var options = new Options();

        Assert.AreSame(DSA010Settings.Cache.Get(options), DSA010Settings.Cache.Get(options));
        Assert.AreNotSame(DSA010Settings.Cache.Get(options), DSA010Settings.Cache.Get(new Options()));
    }

    // ---- the types of the fields -----------------------------------------------------------------------------------------

    [TestMethod]
    public void Pointers_and_function_pointers_are_not_tearable_and_unconstrained_type_parameters_are()
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(System.IO.Path.PathSeparator).Select(path => MetadataReference.CreateFromFile(path));
        var tree = CSharpSyntaxTree.ParseText("unsafe class C<T, U> where U : class { int* P; delegate*<void> F; T A; U B; }");
        var compilation = CSharpCompilation.Create("t", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true));
        var type = compilation.GetTypeByMetadataName("C`2");
        bool Tearable(string name) => DSA010Types.IsTearable(type.GetMembers(name).OfType<IFieldSymbol>().Single().Type);

        Assert.IsFalse(Tearable("P"), "pointer");
        Assert.IsFalse(Tearable("F"), "function pointer");
        Assert.IsTrue(Tearable("A"), "unconstrained type parameter");
        Assert.IsFalse(Tearable("B"), "class-constrained type parameter");
    }
}
