using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA021Tests
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
    public class DbContext : IDisposable
    {
        public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class => null;
        public void Dispose() { }
    }

    public class EntityEntry<TEntity> where TEntity : class
    {
        public ReferenceEntry<TEntity, TProperty> Reference<TProperty>(Expression<Func<TEntity, TProperty>> propertyExpression) where TProperty : class => null;
        public CollectionEntry<TEntity, TProperty> Collection<TProperty>(Expression<Func<TEntity, IEnumerable<TProperty>>> propertyExpression) where TProperty : class => null;
    }

    public class ReferenceEntry<TEntity, TProperty> where TEntity : class where TProperty : class
    {
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class CollectionEntry<TEntity, TProperty> where TEntity : class where TProperty : class
    {
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public class DbSet<T> : IQueryable<T>
    {
        public Type ElementType => typeof(T);
        public System.Linq.Expressions.Expression Expression => null;
        public IQueryProvider Provider => null;
        public IEnumerator<T> GetEnumerator() => null;
        IEnumerator IEnumerable.GetEnumerator() => null;
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> FirstAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> SingleAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> LastAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> LastOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<long> LongCountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<bool> AllAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
        public static Task<int> SumAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) => null;
        public static Task<double> AverageAsync<T>(this IQueryable<T> source, Expression<Func<T, int>> selector, CancellationToken cancellationToken = default) => null;
        public static Task<T> MinAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> MaxAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<bool> ContainsAsync<T>(this IQueryable<T> source, T item, CancellationToken cancellationToken = default) => null;
        public static Task<Dictionary<TKey, T>> ToDictionaryAsync<T, TKey>(this IQueryable<T> source, Func<T, TKey> keySelector, CancellationToken cancellationToken = default) => null;
        public static Task<int> ExecuteUpdateAsync<T>(this IQueryable<T> source, Expression<Func<T, T>> setPropertyCalls, CancellationToken cancellationToken = default) => null;
        public static Task<int> ExecuteDeleteAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task LoadAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task ForEachAsync<T>(this IQueryable<T> source, Action<T> action, CancellationToken cancellationToken = default) => null;
        public static IQueryable<T> TagWith<T>(this IQueryable<T> source, string tag) => source;
        public static IQueryable<T> TagWithCallSite<T>(this IQueryable<T> source, [CallerFilePath] string filePath = """", [CallerLineNumber] int lineNumber = 0) => source;
        public static IQueryable<T> AsNoTracking<T>(this IQueryable<T> source) where T : class => source;
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
        public MyService(MyDbContext context) { _context = context; }
" + serviceBody + @"
    }
}";
    }

    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;
        await CSharpAnalyzerVerifier<DSA021Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    // ── Matched (should flag) ──────────────────────────────────────────

    [TestMethod]
    public async Task ToListAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FilteredToListAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ChainedFirstOrDefaultAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).FirstOrDefaultAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AsNoTrackingCountAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.AsNoTracking().Where(u => u.IsActive).CountAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("CountAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SyncToList_OnDbSet_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Where(u => u.IsActive).ToList()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task VariableWithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await {|#0:query.ToListAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterWithoutTag_AtCaller_Flags()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class User
    {
        public bool IsActive { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task<object> Caller()
        {
            var result = await Execute(_context.Users.Where(u => u.IsActive));
            return result;
        }

        public async Task<List<User>> Execute(IQueryable<User> query)
        {
            return await {|#0:query.ToListAsync()|};
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Not matched (should not flag) ──────────────────────────────────

    [TestMethod]
    public async Task TagWithCallSite_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWithCallSite().Where(u => u.IsActive).ToListAsync();
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TagWith_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).TagWith(""GetActiveUsers"").ToListAsync();
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task TagWithCallSite_BeforeFilter_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWithCallSite().Where(u => u.IsActive).OrderBy(u => u.Name).ToListAsync();
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InMemoryCollection_NotFlagged()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class User
    {
        public bool IsActive { get; set; }
    }

    public class MyService
    {
        public object Test()
        {
            var users = new List<User>();
            var result = users.Where(u => u.IsActive).ToList();
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
    public async Task VariableWithTag_NotFlagged()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.TagWithCallSite().Where(u => u.IsActive);
            var result = await query.ToListAsync();
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterWithTag_AtCaller_NotFlagged()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class User
    {
        public bool IsActive { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task<object> Caller()
        {
            var result = await Execute(_context.Users.TagWithCallSite().Where(u => u.IsActive));
            return result;
        }

        public async Task<List<User>> Execute(IQueryable<User> query)
        {
            return await query.ToListAsync();
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AnyAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).AnyAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("AnyAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Additional async terminal methods (should flag) ───────────────

    [TestMethod]
    public async Task ToArrayAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.ToArrayAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToArrayAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FirstAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.FirstAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SingleAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.SingleAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("SingleAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SingleOrDefaultAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.SingleOrDefaultAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("SingleOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LastAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.LastAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("LastAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LastOrDefaultAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.LastOrDefaultAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("LastOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LongCountAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.LongCountAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("LongCountAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AllAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.AllAsync(u => u.IsActive)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("AllAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MinAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.MinAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("MinAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task MaxAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.MaxAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("MaxAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ContainsAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var user = new User();
            var result = await {|#0:_context.Users.ContainsAsync(user)|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ContainsAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExecuteDeleteAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).ExecuteDeleteAsync()|};
            return result;
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ExecuteDeleteAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ForEachAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            await {|#0:_context.Users.ForEachAsync(u => { })|};
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ForEachAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task QueryLoadAsync_WithoutTag_Flags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            await {|#0:_context.Users.LoadAsync()|};
        }");

        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("LoadAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

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
        public SystemVersion SenderGuestSystemVersion { get; set; }
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
            await _context.Entry(entity).Reference(n => n.SenderGuestSystemVersion).LoadAsync().ConfigureAwait(false);
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
