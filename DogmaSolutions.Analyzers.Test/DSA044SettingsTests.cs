using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The editorconfig options of DSA044: defaults, parsing and fallbacks.</summary>
[TestClass]
public class DSA044SettingsTests
{
    private static DSA044Settings Settings(params (string Key, string Value)[] entries) => new(new FakeAnalyzerConfigOptions(entries));

    [TestMethod]
    public void Defaults()
    {
        var settings = Settings();

        CollectionAssert.AreEqual(new[] { "System.Console", "System.Diagnostics.Debug" }, settings.FlaggedTypes.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "Write*" }, settings.FlaggedMembers.ToArrayOrNull());
        Assert.IsTrue(settings.IncludeConsoleStreams);
        Assert.AreEqual(0, settings.LoggerNamespaces.Count);
        CollectionAssert.Contains(settings.LoggerTypes.ToArrayOrNull(), "Microsoft.Extensions.Logging.ILogger");
        CollectionAssert.Contains(settings.LoggerTypes.ToArrayOrNull(), "Serilog.ILogger");
        CollectionAssert.Contains(settings.LoggerTypes.ToArrayOrNull(), "NLog.ILogger");
        CollectionAssert.Contains(settings.LoggerTypes.ToArrayOrNull(), "log4net.ILog");
        Assert.IsTrue(settings.AcceptInheritedLogger);
        Assert.IsTrue(settings.AcceptLoggerParameters);
        Assert.IsTrue(settings.RequireAccessibleLogger);
        Assert.IsTrue(settings.AcceptStaticLoggerCalls);
        CollectionAssert.AreEqual(new[] { "Serilog.Log" }, settings.StaticLoggerTypes.ToArrayOrNull());
        Assert.IsTrue(settings.SkipInteractiveMembers);
        CollectionAssert.AreEqual(new[] { "Program" }, settings.ExcludedTypeNamePatterns.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "Main" }, settings.ExcludedMemberNamePatterns.ToArrayOrNull());
        Assert.AreEqual(0, settings.ExcludedNamespacePatterns.Count);
        CollectionAssert.Contains(settings.ExcludedFilePatterns.ToArrayOrNull(), "*.Designer.cs");
    }

    [TestMethod]
    public void Every_option_can_be_set()
    {
        var settings = Settings(
            (DSA044Settings.FlaggedTypesKey, "System.Console"),
            (DSA044Settings.FlaggedMembersKey, "Write, Print*"),
            (DSA044Settings.IncludeConsoleStreamsKey, "false"),
            (DSA044Settings.LoggerNamespacesKey, "My.Logging"),
            (DSA044Settings.LoggerTypesKey, "My.ILog"),
            (DSA044Settings.AcceptInheritedLoggerKey, "false"),
            (DSA044Settings.AcceptLoggerParametersKey, "false"),
            (DSA044Settings.RequireAccessibleLoggerKey, "false"),
            (DSA044Settings.AcceptStaticLoggerCallsKey, "false"),
            (DSA044Settings.StaticLoggerTypesKey, "My.StaticLog"),
            (DSA044Settings.SkipInteractiveMembersKey, "false"),
            (DSA044Settings.ExcludedTypeNamePatternsKey, "*Cli"),
            (DSA044Settings.ExcludedMemberNamePatternsKey, "Run*"),
            (DSA044Settings.ExcludedNamespacePatternsKey, "*.Tests"),
            (DSA044Settings.ExcludedFilePatternsKey, "*.x.cs"));

        CollectionAssert.AreEqual(new[] { "System.Console" }, settings.FlaggedTypes.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "Write", "Print*" }, settings.FlaggedMembers.ToArrayOrNull());
        Assert.IsFalse(settings.IncludeConsoleStreams);
        CollectionAssert.AreEqual(new[] { "My.Logging" }, settings.LoggerNamespaces.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "My.ILog" }, settings.LoggerTypes.ToArrayOrNull());
        Assert.IsFalse(settings.AcceptInheritedLogger);
        Assert.IsFalse(settings.AcceptLoggerParameters);
        Assert.IsFalse(settings.RequireAccessibleLogger);
        Assert.IsFalse(settings.AcceptStaticLoggerCalls);
        CollectionAssert.AreEqual(new[] { "My.StaticLog" }, settings.StaticLoggerTypes.ToArrayOrNull());
        Assert.IsFalse(settings.SkipInteractiveMembers);
        CollectionAssert.AreEqual(new[] { "*Cli" }, settings.ExcludedTypeNamePatterns.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "Run*" }, settings.ExcludedMemberNamePatterns.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "*.Tests" }, settings.ExcludedNamespacePatterns.ToArrayOrNull());
        CollectionAssert.AreEqual(new[] { "*.x.cs" }, settings.ExcludedFilePatterns.ToArrayOrNull());
    }

    [TestMethod]
    public void Invalid_values_fall_back_to_the_defaults_and_the_settings_are_cached_per_options()
    {
        var options = new FakeAnalyzerConfigOptions((DSA044Settings.IncludeConsoleStreamsKey, "perhaps"), (DSA044Settings.FlaggedMembersKey, "  "));
        var settings = DSA044Settings.Cache.Get(options);

        Assert.IsTrue(settings.IncludeConsoleStreams);
        CollectionAssert.AreEqual(new[] { "Write*" }, settings.FlaggedMembers.ToArrayOrNull());
        Assert.AreSame(settings, DSA044Settings.Cache.Get(options));
    }
}
