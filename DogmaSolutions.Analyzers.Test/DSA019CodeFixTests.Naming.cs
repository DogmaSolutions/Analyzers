using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA019CodeFixTests
{

    [TestMethod]
    public async Task ExtractsMemberAccessAsElementAccessPrefixInMixedContexts()
    {
        var source = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public static class Util { public static string Format(string s) => s; }
    public class MyTests
    {
        public void Process(Container container)
        {
            var code = Util.Format({|#3:{|#0:container.Groups[0].Steps|}[0]|}.Code);
            {|#4:{|#1:container.Groups[0].Steps|}[0]|}.Name = ""updated"";
            var items = {|#2:container.Groups[0].Steps|};
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class StepInfo { public string Code; public string Name; }
    public class GroupInfo { public StepInfo[] Steps; }
    public class Container { public GroupInfo[] Groups; }
    public static class Util { public static string Format(string s) => s; }
    public class MyTests
    {
        public void Process(Container container)
        {
            var steps = container.Groups[0].Steps;
            var code = Util.Format(steps[0].Code);
            steps[0].Name = ""updated"";
            var items = steps;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(3).WithArguments("container.Groups[0].Steps[0]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("container.Groups[0].Steps", 3));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(4).WithArguments("container.Groups[0].Steps[0]", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(2).WithArguments("container.Groups[0].Steps", 3));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CodeFixTitle_ContainsTargetExpression()
    {
        var source = @"
namespace TestApp
{
    public class Deep { public int X; public int Y; }
    public class Inner { public Deep Deep; }
    public class Middle { public Inner Inner; }
    public class Outer { public Middle Middle; }
    public class MyService
    {
        public void Process(Outer outer)
        {
            var x = outer.Middle.Inner.Deep.X;
            var y = outer.Middle.Inner.Deep.Y;
        }
    }
}";

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
        };

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer());
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToArray();

        Assert.IsTrue(diagnostics.Length > 0, "Expected DSA019 diagnostics");

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, references);
        var docId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(docId, "Test.cs", source);
        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(docId);
        var docCompilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var docDiags = (await docCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        var provider = new DSA019CodeFixProvider();
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            docDiags[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        Assert.IsTrue(actions.Count > 0, "Expected code fix actions");
        var mainAction = actions.First(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId);
        StringAssert.StartsWith(mainAction.Title, "Extract '");
        StringAssert.Contains(mainAction.Title, "outer.Middle.Inner.Deep");
        StringAssert.EndsWith(mainAction.Title, "' to short variable");
    }

    [TestMethod]
    public async Task CodeFixTitle_TruncatesLongExpression()
    {
        var source = @"
namespace TestApp
{
    public class Z { public int V; }
    public class Y { public Z VeryLongPropertyNameForTestingTruncation; }
    public class X { public Y AnotherVeryLongPropertyNameHere; }
    public class W { public X YetAnotherLongPropertyNameForGoodMeasure; }
    public class MyService
    {
        public void Process(W w)
        {
            var a = w.YetAnotherLongPropertyNameForGoodMeasure.AnotherVeryLongPropertyNameHere.VeryLongPropertyNameForTestingTruncation.V;
            var b = w.YetAnotherLongPropertyNameForGoodMeasure.AnotherVeryLongPropertyNameHere.VeryLongPropertyNameForTestingTruncation.V;
        }
    }
}";

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
        };

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer());
        var docDiags = (await compilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        Assert.IsTrue(docDiags.Length > 0, "Expected DSA019 diagnostics");

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, references);
        var docId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(docId, "Test.cs", source);
        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(docId);
        var docCompilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var docDiags2 = (await docCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        var provider = new DSA019CodeFixProvider();
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            docDiags2[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        Assert.IsTrue(actions.Count > 0, "Expected code fix actions");
        var mainAction = actions.First(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId);
        StringAssert.StartsWith(mainAction.Title, "Extract '");
        StringAssert.EndsWith(mainAction.Title, "' to short variable");
        Assert.IsTrue(mainAction.Title.Contains("..."), "Long expressions should be truncated with '...'");
        Assert.IsFalse(mainAction.Title.Contains("VeryLongPropertyNameForTestingTruncation"),
            "Full long member name should not appear — it should be truncated");
    }


    [TestMethod]
    public async Task LongName_ExtractsFullChainAsCamelCase()
    {
        var source = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var x = {|#0:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
            var y = {|#1:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var scenarioDetailsMyEnvironmentDescriptorDataItem = scenario.Details.MyEnvironmentDescriptor.DataItem;
            var x = scenarioDetailsMyEnvironmentDescriptorDataItem.Id;
            var y = scenarioDetailsMyEnvironmentDescriptorDataItem.Id;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(12, 21, 12, 73).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(13, 21, 13, 73).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongName_SimpleThreeLevelChain()
    {
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = {|#0:config.Settings.Infrastructure.Database|}.ConnectionString;
            var timeout = {|#1:config.Settings.Infrastructure.Database|}.Timeout;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var configSettingsInfrastructureDatabase = config.Settings.Infrastructure.Database;
            var connStr = configSettingsInfrastructureDatabase.ConnectionString;
            var timeout = configSettingsInfrastructureDatabase.Timeout;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Settings.Infrastructure.Database", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Infrastructure.Database", 2));

        await test.RunAsync().ConfigureAwait(false);
    }



    [TestMethod]
    public async Task CompactName_ExtractsFirstWordOfEachMember()
    {
        var source = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var x = {|#0:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
            var y = {|#1:scenario.Details.MyEnvironmentDescriptor.DataItem|}.Id;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class VersionInfo { public int Id; }
    public class DataItemInfo { public VersionInfo DataItem; }
    public class DescriptorInfo { public DataItemInfo MyEnvironmentDescriptor; }
    public class ScenarioData { public DescriptorInfo Details; }
    public class MyService
    {
        public void Process(ScenarioData scenario)
        {
            var scenarioDetailsMyData = scenario.Details.MyEnvironmentDescriptor.DataItem;
            var x = scenarioDetailsMyData.Id;
            var y = scenarioDetailsMyData.Id;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(12, 21, 12, 73).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithSpan(13, 21, 13, 73).WithArguments("scenario.Details.MyEnvironmentDescriptor.DataItem.Id", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_SimpleThreeLevelChain()
    {
        // For single-word members, compact == long (first word of "Database" is "Database")
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = {|#0:config.Settings.Infrastructure.Database|}.ConnectionString;
            var timeout = {|#1:config.Settings.Infrastructure.Database|}.Timeout;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var configSettingsInfrastructureDatabase = config.Settings.Infrastructure.Database;
            var connStr = configSettingsInfrastructureDatabase.ConnectionString;
            var timeout = configSettingsInfrastructureDatabase.Timeout;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("config.Settings.Infrastructure.Database", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("config.Settings.Infrastructure.Database", 2));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task CompactName_WithIndexer()
    {
        var source = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var status = new
            {
                Primary = {|#0:home.Rooms.Bathroom.Lights|}[0].IsOn(),
                Secondary = {|#1:home.Rooms.Bathroom.Lights|}[1].IsOn(),
            };
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Light { public bool IsOn() => true; }
    public class Room { public Light[] Lights; }
    public class Rooms { public Room Bathroom; }
    public class Home { public Rooms Rooms; }
    public class MyService
    {
        public void Process(Home home)
        {
            var homeRoomsBathroomLights = home.Rooms.Bathroom.Lights;
            var status = new
            {
                Primary = homeRoomsBathroomLights[0].IsOn(),
                Secondary = homeRoomsBathroomLights[1].IsOn(),
            };
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Compact";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("home.Rooms.Bathroom.Lights", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("home.Rooms.Bathroom.Lights", 2));

        await test.RunAsync().ConfigureAwait(false);
    }



    [TestMethod]
    public async Task ThreeCodeActions_AreRegisteredWithCorrectTitles()
    {
        var source = @"
namespace TestApp
{
    public class DbSettings { public string ConnectionString; public int Timeout; }
    public class Infra { public DbSettings Database; }
    public class Settings { public Infra Infrastructure; }
    public class AppConfig { public Settings Settings; }
    public class MyService
    {
        public void Process(AppConfig config)
        {
            var connStr = config.Settings.Infrastructure.Database.ConnectionString;
            var timeout = config.Settings.Infrastructure.Database.Timeout;
        }
    }
}";

        var runtimeDir = RuntimeEnvironment.GetRuntimeDirectory();
        var references = new MetadataReference[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Private.CoreLib.dll")),
        };

        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "Test",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new DSA019Analyzer());
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = (await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToArray();

        Assert.IsTrue(diagnostics.Length > 0, "Expected DSA019 diagnostics");

        using var workspace = new AdhocWorkspace();
        var projectId = ProjectId.CreateNewId();
        var solution = workspace.CurrentSolution
            .AddProject(projectId, "Test", "Test", LanguageNames.CSharp)
            .AddMetadataReferences(projectId, references);
        var docId = DocumentId.CreateNewId(projectId);
        solution = solution.AddDocument(docId, "Test.cs", source);
        workspace.TryApplyChanges(solution);

        var document = workspace.CurrentSolution.GetDocument(docId);
        var docCompilation = await document.Project.GetCompilationAsync().ConfigureAwait(false);
        var docDiags = (await docCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false))
            .Where(d => d.Id == DSA019Analyzer.DiagnosticId)
            .ToArray();

        var provider = new DSA019CodeFixProvider();
        var actions = new List<CodeAction>();
        var fixContext = new CodeFixContext(
            document,
            docDiags[0],
            (action, _) => actions.Add(action),
            CancellationToken.None);

        await provider.RegisterCodeFixesAsync(fixContext).ConfigureAwait(false);

        var shortAction = actions.FirstOrDefault(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId);
        var longAction = actions.FirstOrDefault(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId + "_Long");
        var compactAction = actions.FirstOrDefault(a => a.EquivalenceKey == DSA019Analyzer.DiagnosticId + "_Compact");

        Assert.IsNotNull(shortAction, "Short variable action should be registered");
        Assert.IsNotNull(longAction, "Long variable action should be registered");
        Assert.IsNotNull(compactAction, "Compact variable action should be registered");

        StringAssert.EndsWith(shortAction.Title, "' to short variable");
        StringAssert.EndsWith(longAction.Title, "' to long variable");
        StringAssert.EndsWith(compactAction.Title, "' to compact variable");
    }



    [TestMethod]
    public async Task LongName_WithMethodCallInChain()
    {
        var source = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var count = {|#0:provider.Service.GetReport().Summary|}.Count;
            var label = {|#1:provider.Service.GetReport().Summary|}.Label;
        }
    }
}";

        var fixedSource = @"
namespace TestApp
{
    public class Summary { public int Count; public string Label; }
    public class Report { public Summary Summary; }
    public class ServiceProvider { public Report GetReport() => null; }
    public class Root { public ServiceProvider Service; }
    public class MyService
    {
        public void Process(Root provider)
        {
            var providerServiceGetReportSummary = provider.Service.GetReport().Summary;
            var count = providerServiceGetReportSummary.Count;
            var label = providerServiceGetReportSummary.Label;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA019Analyzer.DiagnosticId + "_Long";
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(0).WithArguments("provider.Service.GetReport().Summary", 2));
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA019Analyzer, DSA019CodeFixProvider>.Diagnostic(DSA019Analyzer.DiagnosticId)
                .WithLocation(1).WithArguments("provider.Service.GetReport().Summary", 2));

        await test.RunAsync().ConfigureAwait(false);
    }


}
