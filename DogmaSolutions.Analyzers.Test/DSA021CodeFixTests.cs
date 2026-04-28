using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSA021CodeFixTests
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
        public System.Linq.Expressions.Expression Expression => null;
        public IQueryProvider Provider => null;
        public IEnumerator<T> GetEnumerator() => null;
        IEnumerator IEnumerable.GetEnumerator() => null;
    }

    public static class EntityFrameworkQueryableExtensions
    {
        public static Task<List<T>> ToListAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T[]> ToArrayAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
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
    public async Task FixesSimpleQuery_WithTagWithCallSite()
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
            var result = await _context.Users.Where(u => u.IsActive).TagWithCallSite().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA021CodeFixProvider.TagWithCallSiteEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>
                .Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesSimpleQuery_WithTagWith()
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
            var result = await _context.Users.Where(u => u.IsActive).TagWith(""TODO: describe this query"").ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA021CodeFixProvider.TagWithEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>
                .Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesChainedQuery_WithTagWithCallSite()
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
            var result = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).TagWithCallSite().FirstOrDefaultAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA021CodeFixProvider.TagWithCallSiteEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>
                .Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesChainedQuery_WithTagWith()
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
            var result = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).TagWith(""TODO: describe this query"").FirstOrDefaultAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA021CodeFixProvider.TagWithEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>
                .Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("FirstOrDefaultAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixesDirectDbSetAccess_WithTagWithCallSite()
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
            var result = await _context.Users.TagWithCallSite().ToListAsync();
            return result;
        }");

        var test = new CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.CodeActionEquivalenceKey = DSA021CodeFixProvider.TagWithCallSiteEquivalenceKey;
        test.ExpectedDiagnostics.Add(
            CSharpCodeFixVerifier<DSA021Analyzer, DSA021CodeFixProvider>
                .Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }
}
