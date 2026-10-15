using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DataRow

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA043: defaults, parsing and the "is the class complex enough" decision.</summary>
[TestClass]
public class DSA043SettingsTests
{
    private static DSA043Settings Settings(params (string Key, string Value)[] entries) => new(new FakeAnalyzerConfigOptions(entries));

    private static TypeMetrics Metrics(int complexity = 0, int maxMethod = 0, int lines = 0, int methods = 0, int ctorParameters = 0) =>
        new(complexity, maxMethod, lines, methods, ctorParameters, ActivityKinds.None);

    [TestMethod]
    public void Defaults()
    {
        var settings = Settings();

        Assert.AreEqual(30, settings.MinCyclomaticComplexity);
        Assert.AreEqual(15, settings.MinMethodComplexity);
        Assert.AreEqual(200, settings.MinLines);
        Assert.AreEqual(0, settings.MinMethods);
        Assert.AreEqual(0, settings.MinConstructorParameters);
        Assert.IsFalse(settings.RequireAllThresholds);
        Assert.IsFalse(settings.CountBlankLines);
        Assert.IsTrue(settings.RequireActivity);
        Assert.AreEqual(ActivityKinds.Catch | ActivityKinds.Await | ActivityKinds.Call, settings.ActivityKinds);
        CollectionAssert.Contains(settings.ActivityNamespaces.ToArrayOrNull(), "System.Net.Http");
        CollectionAssert.AreEqual(new[] { "Microsoft.Extensions.Logging" }, settings.LoggerNamespaces.ToArrayOrNull());
        Assert.AreEqual(0, settings.LoggerTypes.Count);
        Assert.IsTrue(settings.AcceptInheritedLogger);
        Assert.IsTrue(settings.AcceptLoggerParameters);
        CollectionAssert.AreEqual(new[] { "class" }, settings.IncludedTypeKinds.ToArrayOrNull());
        Assert.IsFalse(settings.IncludeStaticClasses);
        Assert.IsFalse(settings.IncludeAbstractClasses);
        Assert.IsFalse(settings.IncludeNestedTypes);
        CollectionAssert.AreEqual(new[] { "System.Exception", "System.Attribute" }, settings.ExcludedBaseTypes.ToArrayOrNull());
        CollectionAssert.Contains(settings.ExcludedTypeNamePatterns.ToArrayOrNull(), "*Dto");
        CollectionAssert.AreEqual(new[] { "*.Migrations" }, settings.ExcludedNamespacePatterns.ToArrayOrNull());
        CollectionAssert.Contains(settings.ExcludedFilePatterns.ToArrayOrNull(), "*.Designer.cs");
    }

    [TestMethod]
    public void Every_option_can_be_set()
    {
        var settings = Settings(
            (DSA043Settings.MinCyclomaticComplexityKey, "10"),
            (DSA043Settings.MinMethodComplexityKey, "0"),
            (DSA043Settings.MinLinesKey, "50"),
            (DSA043Settings.MinMethodsKey, "7"),
            (DSA043Settings.MinConstructorParametersKey, "6"),
            (DSA043Settings.RequireAllThresholdsKey, "true"),
            (DSA043Settings.CountBlankLinesKey, "true"),
            (DSA043Settings.RequireActivityKey, "false"),
            (DSA043Settings.ActivityKindsKey, "throw, Await, nonsense"),
            (DSA043Settings.ActivityNamespacesKey, "My.Io"),
            (DSA043Settings.LoggerNamespacesKey, "Serilog, NLog"),
            (DSA043Settings.LoggerTypesKey, "My.ILog"),
            (DSA043Settings.AcceptInheritedLoggerKey, "false"),
            (DSA043Settings.AcceptLoggerParametersKey, "false"),
            (DSA043Settings.IncludedTypeKindsKey, "record, struct"),
            (DSA043Settings.IncludeStaticClassesKey, "true"),
            (DSA043Settings.IncludeAbstractClassesKey, "true"),
            (DSA043Settings.IncludeNestedTypesKey, "true"),
            (DSA043Settings.ExcludedBaseTypesKey, "My.Base"),
            (DSA043Settings.ExcludedTypeNamePatternsKey, "*Foo"),
            (DSA043Settings.ExcludedNamespacePatternsKey, "*.Tests"),
            (DSA043Settings.ExcludedFilePatternsKey, "*.x.cs"));

        Assert.AreEqual(10, settings.MinCyclomaticComplexity);
        Assert.AreEqual(0, settings.MinMethodComplexity);
        Assert.AreEqual(50, settings.MinLines);
        Assert.AreEqual(7, settings.MinMethods);
        Assert.AreEqual(6, settings.MinConstructorParameters);
        Assert.IsTrue(settings.RequireAllThresholds);
        Assert.IsTrue(settings.CountBlankLines);
        Assert.IsFalse(settings.RequireActivity);
        Assert.AreEqual(ActivityKinds.Throw | ActivityKinds.Await, settings.ActivityKinds);
        CollectionAssert.AreEqual(new[] { "My.Io" }, settings.ActivityNamespaces.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "Serilog", "NLog" }, settings.LoggerNamespaces.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "My.ILog" }, settings.LoggerTypes.ToArrayOrNull());
        Assert.IsFalse(settings.AcceptInheritedLogger);
        Assert.IsFalse(settings.AcceptLoggerParameters);
        CollectionAssert.AreEqual(new[] { "record", "struct" }, settings.IncludedTypeKinds.ToArrayOrNull());
        Assert.IsTrue(settings.IncludeStaticClasses);
        Assert.IsTrue(settings.IncludeAbstractClasses);
        Assert.IsTrue(settings.IncludeNestedTypes);
        CollectionAssert.AreEqual(new[] { "My.Base" }, settings.ExcludedBaseTypes.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "*Foo" }, settings.ExcludedTypeNamePatterns.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "*.Tests" }, settings.ExcludedNamespacePatterns.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "*.x.cs" }, settings.ExcludedFilePatterns.ToArrayOrNull());
    }

    [TestMethod]
    public void Invalid_values_fall_back_to_the_defaults()
    {
        var settings = Settings(
            (DSA043Settings.MinCyclomaticComplexityKey, "many"),
            (DSA043Settings.MinLinesKey, "-5"),
            (DSA043Settings.RequireAllThresholdsKey, "maybe"),
            (DSA043Settings.ActivityKindsKey, "   "));

        Assert.AreEqual(30, settings.MinCyclomaticComplexity);
        Assert.AreEqual(200, settings.MinLines);
        Assert.IsFalse(settings.RequireAllThresholds);
        Assert.AreEqual(ActivityKinds.Catch | ActivityKinds.Await | ActivityKinds.Call, settings.ActivityKinds);
    }

    [TestMethod]
    public void The_settings_are_computed_once_per_options_instance()
    {
        var options = new FakeAnalyzerConfigOptions((DSA043Settings.MinLinesKey, "77"));

        Assert.AreSame(DSA043Settings.Cache.Get(options), DSA043Settings.Cache.Get(options));
        Assert.AreEqual(77, DSA043Settings.Cache.Get(options).MinLines);
    }

    [TestMethod]
    [DataRow("catch", 1)]
    [DataRow("AWAIT", 2)]
    [DataRow("Throw", 4)]
    [DataRow("call", 8)]
    [DataRow("other", 0)]
    public void ParseActivityKinds_reads_one_kind_case_insensitively(string name, int expected)
    {
        Assert.AreEqual((ActivityKinds)expected, DSA043Settings.ParseActivityKinds(new[] { name }));
    }

    [TestMethod]
    public void ParseActivityKinds_of_nothing_is_none()
    {
        Assert.AreEqual(ActivityKinds.None, DSA043Settings.ParseActivityKinds(System.Array.Empty<string>()));
    }

    // ---- is the class complex enough -----------------------------------------------------------------------------

    [TestMethod]
    public void With_the_default_thresholds_any_one_of_complexity_method_complexity_or_lines_is_enough()
    {
        var settings = Settings();

        Assert.IsFalse(settings.IsComplexEnough(Metrics(complexity: 29, maxMethod: 14, lines: 199)));
        Assert.IsTrue(settings.IsComplexEnough(Metrics(complexity: 30)));
        Assert.IsTrue(settings.IsComplexEnough(Metrics(maxMethod: 15)));
        Assert.IsTrue(settings.IsComplexEnough(Metrics(lines: 200)));
        Assert.IsFalse(settings.IsComplexEnough(Metrics(methods: 500, ctorParameters: 50)), "methods and constructor parameters are disabled by default");
    }

    [TestMethod]
    public void Methods_and_constructor_parameters_are_criteria_when_enabled()
    {
        var settings = Settings(
            (DSA043Settings.MinCyclomaticComplexityKey, "0"),
            (DSA043Settings.MinMethodComplexityKey, "0"),
            (DSA043Settings.MinLinesKey, "0"),
            (DSA043Settings.MinMethodsKey, "10"),
            (DSA043Settings.MinConstructorParametersKey, "5"));

        Assert.IsFalse(settings.IsComplexEnough(Metrics(complexity: 999, maxMethod: 999, lines: 9999, methods: 9, ctorParameters: 4)));
        Assert.IsTrue(settings.IsComplexEnough(Metrics(methods: 10)));
        Assert.IsTrue(settings.IsComplexEnough(Metrics(ctorParameters: 5)));
    }

    [TestMethod]
    public void With_require_all_thresholds_every_enabled_criterion_must_be_met()
    {
        var settings = Settings(
            (DSA043Settings.RequireAllThresholdsKey, "true"),
            (DSA043Settings.MinCyclomaticComplexityKey, "10"),
            (DSA043Settings.MinMethodComplexityKey, "5"),
            (DSA043Settings.MinLinesKey, "0"));

        Assert.IsFalse(settings.IsComplexEnough(Metrics(complexity: 10, maxMethod: 4)));
        Assert.IsFalse(settings.IsComplexEnough(Metrics(complexity: 9, maxMethod: 5)));
        Assert.IsTrue(settings.IsComplexEnough(Metrics(complexity: 10, maxMethod: 5)), "lines are disabled, so they do not matter");
    }

    [TestMethod]
    public void With_every_threshold_disabled_no_class_is_complex()
    {
        var settings = Settings(
            (DSA043Settings.MinCyclomaticComplexityKey, "0"),
            (DSA043Settings.MinMethodComplexityKey, "0"),
            (DSA043Settings.MinLinesKey, "0"));

        Assert.IsFalse(settings.IsComplexEnough(Metrics(complexity: 9999, maxMethod: 9999, lines: 99999, methods: 999, ctorParameters: 99)));
    }
}

internal static class ReadOnlyListExtensions
{
    internal static string[] ToArrayOrNull(this System.Collections.Generic.IReadOnlyList<string> list) => System.Linq.Enumerable.ToArray(list);
}
