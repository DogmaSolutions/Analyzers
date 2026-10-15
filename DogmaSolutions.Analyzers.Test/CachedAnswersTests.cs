using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA010, DSA044 and DSA045 remember what they find out about a type, a base type or a compilation, because the same question comes for each
/// call, each derived type, each lock. The answers depend on the settings of the file that asks: they must never leak from one to another.
/// </summary>
[TestClass]
public class CachedAnswersTests
{
    private static FakeAnalyzerConfigOptions Options(params (string Key, string Value)[] entries) => new(entries);

    private static CSharpCompilation Compile(string source)
    {
        var compilation = DSA043TestSupport.CreateCompilation(CSharpSyntaxTree.ParseText(source, path: "/Source.cs"));
        Assert.AreEqual(0, compilation.GetDiagnostics().Count(d => d.Severity == DiagnosticSeverity.Error), string.Join("\n", compilation.GetDiagnostics()));
        return compilation;
    }

    // ---- DSA044 ---------------------------------------------------------------------------------------------------------------

    [TestMethod]
    public void The_logger_of_a_type_follows_the_settings_of_the_call()
    {
        var compilation = Compile("namespace N { public interface ILogger { } public class Base { protected ILogger L; } public class Service { public void Run() { } } public class Derived : Base { public void Run() { } } }");
        var run = compilation.GetTypeByMetadataName("N.Derived").GetMembers("Run").Single();
        var inherited = new DSA044Settings(Options(("dotnet_diagnostic.DSA044.logger_types", "N.ILogger")));
        var notInherited = new DSA044Settings(Options(("dotnet_diagnostic.DSA044.logger_types", "N.ILogger"), ("dotnet_diagnostic.DSA044.accept_inherited_logger", "false")));

        Assert.AreEqual("L", DSA044LoggerAvailability.Find(run, compilation, inherited, CancellationToken.None)?.Name);
        Assert.IsNull(DSA044LoggerAvailability.Find(run, compilation, notInherited, CancellationToken.None), "other settings, same type");
        Assert.AreEqual("L", DSA044LoggerAvailability.Find(run, compilation, inherited, CancellationToken.None)?.Name, "and back");
    }

    [TestMethod]
    public void The_static_logger_of_a_type_follows_the_settings_of_the_call()
    {
        var compilation = Compile("namespace Serilog { public static class Log { public static void Information(string m) { } } public static class Other { public static void Information(string m) { } } } namespace N { public class Service { public void Run() { Serilog.Log.Information(\"x\"); } } }");
        var run = compilation.GetTypeByMetadataName("N.Service").GetMembers("Run").Single();
        var log = new DSA044Settings(Options());
        var other = new DSA044Settings(Options(("dotnet_diagnostic.DSA044.static_logger_types", "Serilog.Other")));
        var missing = new DSA044Settings(Options(("dotnet_diagnostic.DSA044.static_logger_types", "Not.There")));

        Assert.AreEqual("Log", DSA044LoggerAvailability.Find(run, compilation, log, CancellationToken.None)?.Name);
        Assert.IsNull(DSA044LoggerAvailability.Find(run, compilation, other, CancellationToken.None), "a static logger that exists but is not called");
        Assert.IsNull(DSA044LoggerAvailability.Find(run, compilation, missing, CancellationToken.None), "a static logger that does not exist");
        Assert.AreEqual("Log", DSA044LoggerAvailability.Find(run, compilation, log, CancellationToken.None)?.Name, "and back");
    }

    [TestMethod]
    public void The_static_logger_is_not_looked_for_when_it_does_not_exist()
    {
        var compilation = Compile("namespace N { public class Service { public void Run() { } } }");
        var run = compilation.GetTypeByMetadataName("N.Service").GetMembers("Run").Single();

        Assert.IsNull(DSA044LoggerAvailability.Find(run, compilation, new DSA044Settings(Options()), CancellationToken.None));
    }

    // ---- DSA045 ---------------------------------------------------------------------------------------------------------------

    [TestMethod]
    public void A_base_type_is_counted_with_the_settings_of_the_type_that_asks()
    {
        var compilation = Compile("namespace N { public class Base { public int A; } public class Derived : Base { public int B { get; set; } } }");
        var derived = compilation.GetTypeByMetadataName("N.Derived");
        var withFields = new DSA045Settings(Options());
        var withoutFields = new DSA045Settings(Options(("dotnet_diagnostic.DSA045.allow_fields", "false")));

        Assert.IsTrue(DSA045DtoDetector.Evaluate(derived, withFields, CancellationToken.None).IsPure);
        Assert.IsFalse(DSA045DtoDetector.Evaluate(derived, withoutFields, CancellationToken.None).IsPure, "other settings, same base type");
        Assert.IsTrue(DSA045DtoDetector.Evaluate(derived, withFields, CancellationToken.None).IsPure, "and back");
    }

    [TestMethod]
    public void An_exclusion_of_the_assembly_follows_the_settings_of_the_type_that_asks()
    {
        var compilation = Compile("[assembly: System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage] namespace N { public class Dto { public int A { get; set; } } }");
        var dto = compilation.GetTypeByMetadataName("N.Dto");
        var tree = compilation.SyntaxTrees.Single();
        var excluding = new DSA045Settings(Options());
        var notExcluding = new DSA045Settings(Options(("dotnet_diagnostic.DSA045.exclusion_attributes", "Not.There")));
        var none = new HashSet<string>();

        Assert.IsTrue(DSA045Exclusions.IsExcluded(dto, tree, excluding, compilation, none));
        Assert.IsFalse(DSA045Exclusions.IsExcluded(dto, tree, notExcluding, compilation, none), "other settings, same compilation");
        Assert.IsTrue(DSA045Exclusions.IsExcluded(dto, tree, excluding, compilation, none), "and back");
    }
}
