using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The heuristic of DSA051: which members the generated ToString shows.</summary>
[TestClass]
public class DSA051PlannerTests
{
    private static async Task<INamedTypeSymbol> GetTypeAsync(string members)
    {
        var references = await ReferenceAssemblies.Net.Net80.ResolveAsync(LanguageNames.CSharp, default).ConfigureAwait(false);
        var tree = CSharpSyntaxTree.ParseText("public class Subject { " + members + " }");
        var compilation = CSharpCompilation.Create("Planner", new[] { tree }, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        return compilation.GetTypeByMetadataName("Subject");
    }

    private static DSA051Settings Settings(string wellKnown = null) => new(new Options(wellKnown));

    private sealed class Options : Microsoft.CodeAnalysis.Diagnostics.AnalyzerConfigOptions
    {
        private readonly string _wellKnown;

        internal Options(string wellKnown) => _wellKnown = wellKnown;

        public override bool TryGetValue(string key, out string value)
        {
            value = key == DSA051Settings.WellKnownNamesKey ? _wellKnown : null;
            return value != null;
        }
    }

    [TestMethod]
    [DataRow("Id")]
    [DataRow("Key")]
    [DataRow("Pk")]
    [DataRow("Name")]
    [DataRow("Value")]
    [DataRow("FriendlyName")]
    [DataRow("Description")]
    [DataRow("FirstName")]
    [DataRow("LastName")]
    [DataRow("Surname")]
    [DataRow("Email")]
    [DataRow("Address")]
    [DataRow("Url")]
    [DataRow("Range")]
    public async Task Every_default_name_identifies_an_instance(string name)
    {
        var type = await GetTypeAsync("public int Other { get; set; } public string " + name + " { get; set; }").ConfigureAwait(false);

        var plan = DSA051Planner.Create(type, Settings());

        Assert.AreEqual(DSA051Strategy.WellKnown, plan.Strategy);
        Assert.AreEqual(name, plan.Members.Single().Name);
        Assert.AreEqual(2, plan.DataMembers);
    }

    [TestMethod]
    public async Task The_order_of_the_list_decides_between_two_names()
    {
        var type = await GetTypeAsync("public string Url { get; set; } public string Email { get; set; } public string Surname { get; set; }").ConfigureAwait(false);

        Assert.AreEqual("Surname", DSA051Planner.Create(type, Settings()).Members.Single().Name);
        Assert.AreEqual("Url", DSA051Planner.Create(type, Settings("url,email")).Members.Single().Name);
    }

    [TestMethod]
    public async Task The_text_returns_the_value_of_a_well_known_member()
    {
        var type = await GetTypeAsync("public int Id { get; set; }").ConfigureAwait(false);

        Assert.AreEqual("$\"{Id}\"", DSA051Planner.Create(type, Settings()).BuildInterpolatedString(Settings()));
    }

    [TestMethod]
    public async Task There_is_no_text_for_a_type_without_members()
    {
        var type = await GetTypeAsync("public void Run() { }").ConfigureAwait(false);

        var plan = DSA051Planner.Create(type, Settings());

        Assert.AreEqual(DSA051Strategy.None, plan.Strategy);
        Assert.AreEqual(string.Empty, plan.BuildInterpolatedString(Settings()));
    }

    [TestMethod]
    [DataRow("Id_", "id")]
    [DataRow("_first_Name", "FIRSTNAME")]
    public void Names_are_compared_without_underscores_and_casing(string left, string right)
    {
        Assert.AreEqual(DSA051Planner.Normalize(left), DSA051Planner.Normalize(right));
    }
}
