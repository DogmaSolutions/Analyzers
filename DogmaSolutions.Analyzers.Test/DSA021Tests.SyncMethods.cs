using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA021Tests
{

    // ── Sync terminal methods (should flag) ───────────────────────────

    [TestMethod]
    public async Task SyncToArray_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Where(u => u.IsActive).ToArray()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToArray"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncFirst_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.First()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("First"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncFirstOrDefault_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.FirstOrDefault()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefault"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncCount_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Count()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Count"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncAny_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Any()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Any"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Additional not-matched scenarios ──────────────────────────────

    [TestMethod]
    public async Task MultipleTagWithCalls_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWith(""first"").Where(u => u.IsActive).TagWithCallSite().ToListAsync();
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Additional async terminal methods that have stubs but need tests ─

    [TestMethod]
    public async Task SumAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.SumAsync(u => u.IsActive ? 1 : 0)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("SumAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AverageAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.AverageAsync(u => u.IsActive ? 1 : 0)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("AverageAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ToDictionaryAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.ToDictionaryAsync(u => u.Name)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToDictionaryAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExecuteUpdateAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.ExecuteUpdateAsync(u => u)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ExecuteUpdateAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Additional sync terminal methods (should flag) ────────────────

    [TestMethod]
    public async Task SyncSingle_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Single()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Single"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncSingleOrDefault_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.SingleOrDefault()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("SingleOrDefault"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncLast_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Last()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Last"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncLastOrDefault_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.LastOrDefault()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("LastOrDefault"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncLongCount_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.LongCount()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("LongCount"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncAll_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.All(u => u.IsActive)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("All"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncMin_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Min()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Min"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncMax_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Max()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Max"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncSum_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Sum(u => u.IsActive ? 1 : 0)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Sum"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncAverage_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Average(u => u.IsActive ? 1 : 0)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("Average"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Entry/Reference/Collection navigation not-matched ─────────────

    [TestMethod]
    public async Task MaterializedArray_LinqToObjects_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToArrayAsync()|};
            var first = users.FirstOrDefault();
            var any = users.Any();
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToArrayAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MaterializedList_LinqToObjects_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            var count = users.Count();
            var first = users.First();
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SubqueryInsideWhereLambda_NotFlagged()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class Report
    {
        public int Id { get; set; }
        public string CommandKey { get; set; }
        public string CorrelationId { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class Trace
    {
        public string CommandKey { get; set; }
        public string CorrelationId { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<Report> Reports { get; set; }
        public DbSet<Trace> Traces { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task<long?> Test(DateTime olderThan, CancellationToken ct)
        {
            var reportId = await _context.Reports
                .Where(r => r.CreatedOn < olderThan &&
                            !_context.Traces.Any(t => t.CommandKey == r.CommandKey &&
                                                      t.CorrelationId == r.CorrelationId))
                .Select(r => (long?)r.Id)
                .TagWithCallSite()
                .FirstOrDefaultAsync(ct);
            return reportId;
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SubqueryInsideSelectLambda_NotFlagged()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class Order
    {
        public int Id { get; set; }
    }

    public class OrderLine
    {
        public int OrderId { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderLine> OrderLines { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task<object> Test(CancellationToken ct)
        {
            var result = await _context.Orders
                .Select(o => new { o.Id, LineCount = _context.OrderLines.Count(l => l.OrderId == o.Id) })
                .TagWithCallSite()
                .ToListAsync(ct);
            return result;
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntryReferenceLoadAsync_NotFlagged()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class SystemVersion
    {
        public int Id { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public SystemVersion SenderMachineVersion { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<Notification> Notifications { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task Test(Notification entity)
        {
            await _context.Entry(entity).Reference(n => n.SenderMachineVersion).LoadAsync().ConfigureAwait(false);
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntryCollectionLoadAsync_NotFlagged()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class Comment
    {
        public int Id { get; set; }
    }

    public class Post
    {
        public int Id { get; set; }
        public IEnumerable<Comment> Comments { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<Post> Posts { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task Test(Post entity)
        {
            await _context.Entry(entity).Collection(p => p.Comments).LoadAsync().ConfigureAwait(false);
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }
}
