using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA031CodeFixTests
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
        public void Update<T>(T entity) { }
        public void Remove<T>(T entity) { }
        public void Attach<T>(T entity) { }
        public EntityEntry<T> Entry<T>(T entity) where T : class => null;
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
        public static IQueryable<T> TagWith<T>(this IQueryable<T> source, string tag) => source;
        public static IQueryable<T> TagWithCallSite<T>(this IQueryable<T> source, [CallerFilePath] string file = null, [CallerLineNumber] int line = 0) => source;
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
        public MyService(MyDbContext context) { _context = context; }
" + serviceBody + @"
    }
}";
    }

    // ── Projection fixes ──────────────────────────────────────────────

    [TestMethod]
    public async Task FixesProjection_AnonymousType()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Select(u => new { u.Name }).ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.AsNoTracking().Select(u => new { u.Name }).ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesProjection_String()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await {|#0:_context.Users.Select(u => u.Name).ToListAsync()|};
            return result;
        }");

        var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.AsNoTracking().Select(u => u.Name).ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Read-only body fixes ──────────────────────────────────────────

    [TestMethod]
    public async Task FixesReadOnly_ToListAsync()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var fixedSource = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_WithFilter()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.Where(u => u.IsActive).ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var fixedSource = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsNoTracking().Where(u => u.IsActive).ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_DirectDbSet()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.ToListAsync()|};
        }");

        var fixedSource = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_SyncToList()
    {
        var source = BuildSource(@"
        public void Test()
        {
            var users = {|#0:_context.Users.ToList()|};
            System.Console.WriteLine(users.Count);
        }");

        var fixedSource = BuildSource(@"
        public void Test()
        {
            var users = _context.Users.AsNoTracking().ToList();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_FirstOrDefaultAsync()
    {
        var source = BuildSource(@"
        public async Task<string> Test()
        {
            var user = await {|#0:_context.Users.FirstOrDefaultAsync()|};
            return user?.Name;
        }");

        var fixedSource = BuildSource(@"
        public async Task<string> Test()
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync();
            return user?.Name;
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_WithTagWith()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await {|#0:_context.Users.TagWith(""q"").ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var fixedSource = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.AsNoTracking().TagWith(""q"").ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_VariableQuery()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var users = await {|#0:query.ToListAsync()|};
            System.Console.WriteLine(users.Count);
        }");

        var fixedSource = BuildSource(@"
        public async Task Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var users = await query.AsNoTracking().ToListAsync();
            System.Console.WriteLine(users.Count);
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesReadOnly_SelectDistinctTagWithCallSite_InsertsAfterSource()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            IQueryable<User> tasksDeleteQuery = _context.Users.Where(u => u.IsActive);
            var ids = await {|#0:tasksDeleteQuery.Select(u => u.Id).Distinct().TagWithCallSite().ToArrayAsync()|};
            System.Console.WriteLine(ids.Length);
        }");

        var fixedSource = BuildSource(@"
        public async Task Test()
        {
            IQueryable<User> tasksDeleteQuery = _context.Users.Where(u => u.IsActive);
            var ids = await tasksDeleteQuery.AsNoTracking().Select(u => u.Id).Distinct().TagWithCallSite().ToArrayAsync();
            System.Console.WriteLine(ids.Length);
        }");

        var test = new CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA031CodeFixProvider.EquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA031Analyzer, DSA031CodeFixProvider>
                .Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToArrayAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
