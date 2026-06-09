using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public class DSR001RefactoringTests
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
        public static Task<bool> AllAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
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

   // ──────────────────────────────────────────────────────────────────────────
   //  Positive: TagWithCallSite
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task BasicUntagged_InsertsTagWithCallSite()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).[||]ToListAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).TagWithCallSite().ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task BasicUntagged_InsertsTagWith()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).[||]ToListAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).TagWith(""TODO: describe this query"").ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task FirstOrDefaultAsync_InsertsTagWithCallSite()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).[||]FirstOrDefaultAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).TagWithCallSite().FirstOrDefaultAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task CountAsync_InsertsTagWithCallSite()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.[||]CountAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWithCallSite().CountAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SyncToList_InsertsTagWithCallSite()
   {
      var source = BuildSource(@"
        public object Test()
        {
            var result = _context.Users.Where(u => u.IsActive).[||]ToList();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public object Test()
        {
            var result = _context.Users.Where(u => u.IsActive).TagWithCallSite().ToList();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ChainedWhereOrderBy_InsertsBeforeTerminal()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).[||]FirstOrDefaultAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).OrderBy(u => u.Name).TagWithCallSite().FirstOrDefaultAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task VariableQuery_InsertsTagWithCallSite()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await query.[||]ToListAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = await query.TagWithCallSite().ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task AsNoTrackingChain_StillOffered()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.AsNoTracking().Where(u => u.IsActive).[||]ToListAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.AsNoTracking().Where(u => u.IsActive).TagWithCallSite().ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task DirectDbSetAccess_InsertsTagWithCallSite()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.[||]ToListAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWithCallSite().ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task ToArrayAsync_InsertsTagWith()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).[||]ToArrayAsync();
            return result;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).TagWith(""TODO: describe this query"").ToArrayAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task MultipleQueries_RefactorsTargetOnly()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var a = await _context.Users.[||]ToListAsync();
            var b = await _context.Users.FirstOrDefaultAsync();
            return a;
        }");

      var fixedSource = BuildSource(@"
        public async Task<object> Test()
        {
            var a = await _context.Users.TagWithCallSite().ToListAsync();
            var b = await _context.Users.FirstOrDefaultAsync();
            return a;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = fixedSource;
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      test.CodeActionEquivalenceKey = DSR001RefactoringProvider.TagWithCallSiteEquivalenceKey;
      await test.RunAsync().ConfigureAwait(false);
   }

   // ──────────────────────────────────────────────────────────────────────────
   //  Negative: no refactoring expected
   // ──────────────────────────────────────────────────────────────────────────

   [TestMethod]
   public async Task AlreadyTaggedCallSite_NoRefactoring()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.TagWithCallSite().Where(u => u.IsActive).[||]ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task AlreadyTaggedWith_NoRefactoring()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.Where(u => u.IsActive).TagWith(""my query"").[||]ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task NonEfCollection_NoRefactoring()
   {
      var source = BuildSource(@"
        public object Test()
        {
            var users = new System.Collections.Generic.List<User>();
            return users.Where(u => u.IsActive).[||]ToList();
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task CursorOnNonTerminal_NoRefactoring()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users.[||]Where(u => u.IsActive).ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }

   [TestMethod]
   public async Task SubqueryLambda_NoRefactoring()
   {
      var source = BuildSource(@"
        public async Task<object> Test()
        {
            var result = await _context.Users
                .Where(u => _context.Users.[||]Any(u2 => u2.Name == u.Name))
                .TagWithCallSite()
                .ToListAsync();
            return result;
        }");

      var test = new CSharpCodeRefactoringVerifier<DSR001RefactoringProvider>.Test();
      test.TestCode = source;
      test.FixedCode = source.Replace("[||]", string.Empty);
      test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
      await test.RunAsync().ConfigureAwait(false);
   }
}
