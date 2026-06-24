using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA031Tests
{
    private const string EfCoreStubs = @"#nullable disable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Microsoft.EntityFrameworkCore
{
    public class EntityEntry<T> where T : class { }

    public class DbContext : IDisposable
    {
        public void Dispose() { }
        public int SaveChanges() => 0;
        public Task<int> SaveChangesAsync(CancellationToken ct = default) => null;
        public void Add<T>(T entity) { }
        public ValueTask<Microsoft.EntityFrameworkCore.EntityEntry<T>> AddAsync<T>(T entity, CancellationToken ct = default) where T : class => default;
        public void AddRange(params object[] entities) { }
        public void Update<T>(T entity) { }
        public void UpdateRange(params object[] entities) { }
        public void Remove<T>(T entity) { }
        public void RemoveRange(params object[] entities) { }
        public void Attach<T>(T entity) { }
        public void AttachRange(params object[] entities) { }
        public EntityEntry<T> Entry<T>(T entity) where T : class => null;
    }

    public class DbSet<T> : IQueryable<T>
    {
        public Type ElementType => typeof(T);
        public Expression Expression => null;
        public IQueryProvider Provider => null;
        public IEnumerator<T> GetEnumerator() => null;
        IEnumerator IEnumerable.GetEnumerator() => null;
        public void Add(T entity) { }
        public void Remove(T entity) { }
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> FirstAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> SingleAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> LastAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> LastOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<long> LongCountAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<bool> AllAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken ct = default) => null;
        public static Task<int> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector, CancellationToken ct = default) => null;
        public static Task<double> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector, CancellationToken ct = default) => null;
        public static Task<T> MinAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<T> MaxAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task<bool> ContainsAsync<T>(this IQueryable<T> source, T item, CancellationToken ct = default) => null;
        public static Task<Dictionary<TKey, T>> ToDictionaryAsync<T, TKey>(this IQueryable<T> source, Func<T, TKey> keySelector, CancellationToken ct = default) => null;
        public static Task<int> ExecuteUpdateAsync<T>(this IQueryable<T> source, Expression<Func<T, T>> setPropertyCalls, CancellationToken ct = default) => null;
        public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task LoadAsync<T>(this IQueryable<T> source, CancellationToken ct = default) => null;
        public static Task ForEachAsync<T>(this IQueryable<T> source, Action<T> action, CancellationToken ct = default) => null;
        public static IQueryable<T> TagWith<T>(this IQueryable<T> source, string tag) => source;
        public static IQueryable<T> TagWithCallSite<T>(this IQueryable<T> source, [CallerFilePath] string filePath = """", [CallerLineNumber] int lineNumber = 0) => source;
        public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) where T : class => source;
        public static IQueryable<T> AsTracking<T>(this IQueryable<T> source) where T : class => source;
        public static IQueryable<T> AsNoTrackingWithIdentityResolution<T>(this IQueryable<T> source) where T : class => source;
    }
}
";

    private static string BuildSource(string serviceBody)
    {
        return EfCoreStubs + @"
namespace TestApp
{
    public class User
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        private List<User> _cachedUsers;
        public MyService(MyDbContext context) { _context = context; }
" + serviceBody + @"
    }
}";
    }

    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;
        await CSharpAnalyzerVerifier<DSA031Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // MATCHED — Projection to non-entity type
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task Projection_AnonymousType_ToListAsync_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Select(u => new { u.Name }).ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Projection_String_ToListAsync_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Select(u => u.Name).ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Projection_Int_FirstOrDefaultAsync_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Select(u => u.Id).FirstOrDefaultAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Projection_Bool_ToArrayAsync_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Select(u => u.IsActive).ToArrayAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToArrayAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Projection_AnonymousType_SyncToList_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Select(u => new { u.Name, u.Id }).ToList()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Projection_AnonymousType_WithFilter_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).Select(u => new { u.Name }).ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Projection_AnonymousType_WithTagWith_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.TagWith(""q"").Select(u => new { u.Name }).ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // MATCHED — Read-only method body
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ReadOnly_VoidMethod_ResultUsedForCount_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_ReturnsDerivedString_Flags()
    {
        var source = BuildSource(@"
        public async Task<string> Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            return string.Join("", "", users.Select(u => u.Name));
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_ReturnsInt_Flags()
    {
        var source = BuildSource(@"
        public async Task<int> Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            return users.Count(u => u.IsActive);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_UnusedResult_VoidMethod_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_ToArrayAsync_VoidMethod_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToArrayAsync()|};
            System.Console.WriteLine(users.Length);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToArrayAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_SyncToList_VoidMethod_Flags()
    {
        var source = BuildSource(@"
        public void Test()
        {
            var users = {|#0:_context.Users.ToList()|};
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_WithWhereFilter_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.Where(u => u.IsActive).ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_WithTagWith_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.TagWith(""q"").ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_FirstOrDefaultAsync_PropertyRead_Flags()
    {
        var source = BuildSource(@"
        public async Task<string> Test()
        {
            var user = await {|#0:_context.Users.FirstOrDefaultAsync()|};
            return user?.Name;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_LocalVariable_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var users = await {|#0:query.ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_ReturnsBool_Flags()
    {
        var source = BuildSource(@"
        public async Task<bool> Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            return users.Any(u => u.IsActive);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_ForEachReadOnly_VoidMethod_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            foreach (var u in users)
            {
                System.Console.WriteLine(u.Name);
            }
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_StringInterpolation_Flags()
    {
        var source = BuildSource(@"
        public async Task<string> Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            return $""Found {users.Count} users"";
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReadOnly_ReturnsListOfStrings_Flags()
    {
        var source = BuildSource(@"
        public async Task<List<string>> Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            return users.Select(u => u.Name).ToList();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Tracking already specified
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task AsNoTracking_AlreadyPresent_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AsTracking_AlreadyPresent_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsTracking().ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AsNoTrackingWithIdentityResolution_AlreadyPresent_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsNoTrackingWithIdentityResolution().ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AsNoTracking_OnProjection_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.AsNoTracking().Select(u => new { u.Name }).ToListAsync();
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AsNoTracking_InLocalVariable_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var query = _context.Users.AsNoTracking().Where(u => u.IsActive);
            var users = await query.ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Method body contains SaveChanges
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task SaveChanges_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public void Test()
        {
            var users = _context.Users.ToList();
            _context.SaveChanges();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SaveChangesAsync_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            await _context.SaveChangesAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Method body contains DbContext mutation APIs
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ContextAdd_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.Add(new User());
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContextUpdate_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.Update(users[0]);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContextRemove_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.Remove(users[0]);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContextAttach_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.Attach(users[0]);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContextEntry_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.Entry(users[0]);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DbSetAdd_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.Users.Add(new User());
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContextAddRange_InBody_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _context.AddRange(new User());
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }
}
