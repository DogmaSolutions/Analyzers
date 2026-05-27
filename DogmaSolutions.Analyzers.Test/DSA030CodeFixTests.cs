using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA030CodeFixTests
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
        public void Dispose() { }
    }

    public class DbSet<T> : IQueryable<T>
    {
        public Type ElementType => typeof(T);
        public Expression Expression => null;
        public IQueryProvider Provider => null;
        public IEnumerator<T> GetEnumerator() => null;
        IEnumerator IEnumerable.GetEnumerator() => null;
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

    // ── AsNoTracking fixes ────────────────────────────────────────────

    [TestMethod]
    public async Task FixesSimpleQuery_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).AsNoTracking().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesSimpleQuery_WithAsTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).AsTracking().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesChainedQuery_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).FirstOrDefaultAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).AsNoTracking().FirstOrDefaultAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesDirectDbSetAccess_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.AsNoTracking().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesSyncToList_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Where(u => u.IsActive).ToList()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public object Test()
        {
            var result = _context.Users.Where(u => u.IsActive).AsNoTracking().ToList();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesSyncToList_WithAsTracking()
    {
        var source = BuildSource(@"
        public object Test()
        {
            var result = {|#0:_context.Users.Where(u => u.IsActive).ToList()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public object Test()
        {
            var result = _context.Users.Where(u => u.IsActive).AsTracking().ToList();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesCountAsync_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<int> Test()
        {
            return await {|#0:_context.Users.CountAsync()|};
        }");

        var fixedSource = BuildSource(@"
        public async Task<int> Test()
        {
            return await _context.Users.AsNoTracking().CountAsync();
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("CountAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesAnyAsync_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<bool> Test()
        {
            return await {|#0:_context.Users.AnyAsync()|};
        }");

        var fixedSource = BuildSource(@"
        public async Task<bool> Test()
        {
            return await _context.Users.AsNoTracking().AnyAsync();
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("AnyAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesTagWithQuery_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.TagWith(""test"").ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWith(""test"").AsNoTracking().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesVariableQuery_WithAsNoTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await {|#0:query.ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await query.AsNoTracking().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsNoTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesVariableQuery_WithAsTracking()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await {|#0:query.ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await query.AsTracking().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA030CodeFixProvider.AsTrackingEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA030Analyzer, DSA030CodeFixProvider>
                .Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
