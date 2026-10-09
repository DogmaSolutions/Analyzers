using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA031 when a loaded entity is stored into a field or a property (directly, through a tuple deconstruction or
/// through an indexer): the entity escapes the method, so it can't be read-only (<c>MethodBodyAssignsEntityToFieldOrProperty</c>).
/// </summary>
public partial class DSA031Tests
{
    private const string EntityStores = @"
        private User _selected;
        private User _other;
        private int _count;
        private string _name;
        private readonly Dictionary<int, User> _byId = new Dictionary<int, User>();
        private User[] _array = new User[1];
        public User Selected { get; set; }
";

    private static async Task AssertEntityAssignmentNoFlag(string body)
    {
        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = BuildSource(EntityStores + body);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    private static async Task AssertEntityAssignmentFlags(string body)
    {
        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = BuildSource(EntityStores + body);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── The entity escapes: no flag ────────────────────────────────────

    [TestMethod]
    public async Task EntityAssignedToProperty_NoFlag()
    {
        await AssertEntityAssignmentNoFlag(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            Selected = user;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityAssignedToFieldThroughTuple_NoFlag()
    {
        await AssertEntityAssignmentNoFlag(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            (_selected, _count) = (user, 1);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityAssignedToPropertyThroughTuple_NoFlag()
    {
        await AssertEntityAssignmentNoFlag(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            (_count, Selected) = (1, user);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityAssignedToFieldIndexer_NoFlag()
    {
        await AssertEntityAssignmentNoFlag(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            _byId[1] = user;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityAssignedToFieldArrayElement_NoFlag()
    {
        await AssertEntityAssignmentNoFlag(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            _array[0] = user;
        }").ConfigureAwait(false);
    }

    // ── Assignments that do not make the entity escape: flagged ────────

    [TestMethod]
    public async Task EntityAssignedToLocalArrayElement_Flags()
    {
        await AssertEntityAssignmentFlags(@"
        public async Task Test()
        {
            var user = await {|#0:_context.Users.FirstOrDefaultAsync()|};
            var local = new User[1];
            local[0] = user;
            System.Console.WriteLine(local.Length);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NonEntityValueAssignedToFields_Flags()
    {
        await AssertEntityAssignmentFlags(@"
        public async Task Test()
        {
            var user = await {|#0:_context.Users.FirstOrDefaultAsync()|};
            _count = 5;
            _name = user.Name;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NonEntityValuesAssignedToFieldsThroughTuple_Flags()
    {
        await AssertEntityAssignmentFlags(@"
        public async Task Test()
        {
            var user = await {|#0:_context.Users.FirstOrDefaultAsync()|};
            (_count, _name) = (1, user.Name);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NonEntityValueAssignedToFieldIndexer_Flags()
    {
        await AssertEntityAssignmentFlags(@"
        public async Task Test()
        {
            var user = await {|#0:_context.Users.FirstOrDefaultAsync()|};
            _byId[1] = null;
            System.Console.WriteLine(user.Name);
        }").ConfigureAwait(false);
    }
}
