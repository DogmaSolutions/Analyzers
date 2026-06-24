using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA031Tests
{

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Entity property assignment
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task EntityPropertyAssignment_Direct_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            user.Name = ""updated"";
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityPropertyAssignment_InForEach_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            foreach (var u in users)
            {
                u.IsActive = false;
            }
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityPropertyAssignment_ViaIndexer_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            users[0].Name = ""updated"";
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Result escapes via return type
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ReturnListOfEntities_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<List<User>> Test()
        {
            return await _context.Users.ToListAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReturnArrayOfEntities_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<User[]> Test()
        {
            return await _context.Users.ToArrayAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReturnSingleEntity_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<User> Test()
        {
            return await _context.Users.FirstOrDefaultAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Result escapes via field/property assignment
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ResultAssignedToField_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            _cachedUsers = users;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Result escapes via method argument
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ResultPassedToMethod_NoFlag()
    {
        var source = EfCoreStubs + @"
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

        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            Process(users);
        }

        private void Process(List<User> items) { }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SingleEntityPassedToMethod_NoFlag()
    {
        var source = EfCoreStubs + @"
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

        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            Process(users[0]);
        }

        private void Process(User item) { }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Projection to same entity type
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task Projection_SameEntityType_ReturnsEntities_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<List<User>> Test()
        {
            return await _context.Users.Select(u => u).ToListAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Bulk operations, in-memory, subqueries
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ExecuteDeleteAsync_BulkOp_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            await _context.Users.Where(u => !u.IsActive).ExecuteDeleteAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExecuteUpdateAsync_BulkOp_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            await _context.Users.ExecuteUpdateAsync(u => u);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task InMemoryList_NoFlag()
    {
        var source = BuildSource(@"
        public void Test()
        {
            var users = new List<User>();
            var result = users.Where(u => u.IsActive).ToList();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task SubqueryInsideLambda_OnlyOuterFlags()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var result = await {|#0:_context.Users.Where(u => _context.Users.Any()).ToListAsync()|};
            System.Console.WriteLine(result.Count);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Scalar terminal methods (not entity-materializing)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task CountAsync_ScalarTerminal_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<int> Test()
        {
            return await _context.Users.CountAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task AnyAsync_ScalarTerminal_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<bool> Test()
        {
            return await _context.Users.AnyAsync();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Increment/Decrement on entity property
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task EntityPropertyPostfixIncrement_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            user.Id++;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task EntityPropertyPrefixDecrement_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            --user.Id;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Return expression involves entity type (object/dynamic return)
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ReturnObjectHidingEntity_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            return user;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ReturnObjectHidingEntityList_NoFlag()
    {
        var source = BuildSource(@"
        public async Task<object> Test()
        {
            var users = await _context.Users.ToListAsync();
            return users;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Entity escapes via out/ref parameter
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task OutParameterEntityType_NoFlag()
    {
        var source = BuildSource(@"
        public bool TryGetUser(out User result)
        {
            result = _context.Users.FirstOrDefault();
            return result != null;
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task RefParameterEntityType_NoFlag()
    {
        var source = BuildSource(@"
        public void FillUser(ref User target)
        {
            target = _context.Users.FirstOrDefault();
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Indexer assignment on entity
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task EntityIndexerAssignment_NoFlag()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class Entity
    {
        public int Id { get; set; }
        public string this[string key] { get => null; set { } }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<Entity> Entities { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task Test()
        {
            var entity = await _context.Entities.FirstOrDefaultAsync();
            entity[""ShadowProp""] = ""value"";
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Navigation property mutation
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task NavigationPropertyMutation_NoFlag()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public bool IsActive { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            user.Address.Street = ""updated"";
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task DeepNavigationPropertyMutation_NoFlag()
    {
        var source = EfCoreStubs + @"
namespace TestApp
{
    public class City
    {
        public string Name { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public City City { get; set; }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public Address Address { get; set; }
    }

    public class MyDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
    }

    public class MyService
    {
        private readonly MyDbContext _context;
        public MyService(MyDbContext context) { _context = context; }

        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            user.Address.City.Name = ""updated"";
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Tuple deconstruction assigns entity to field
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task TupleDeconstructionAssignsEntityToField_NoFlag()
    {
        var source = EfCoreStubs + @"
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
        private User _lastUser;
        private int _count;
        public MyService(MyDbContext context) { _context = context; }

        public void Test()
        {
            var user = _context.Users.FirstOrDefault();
            (_lastUser, _count) = (user, 1);
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Yield return entity as widened type
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task YieldReturnEntityAsObject_NoFlag()
    {
        var source = EfCoreStubs + @"
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

        public IEnumerable<object> Test()
        {
            var users = _context.Users.ToList();
            foreach (var u in users)
                yield return u;
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Expression-bodied method returning entity as widened type
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task ExpressionBodiedReturnEntityAsObject_NoFlag()
    {
        var source = BuildSource(@"
        public object Test() => _context.Users.FirstOrDefault();");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Entity stored in field array element
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task EntityStoredInFieldArrayElement_NoFlag()
    {
        var source = EfCoreStubs + @"
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
        private readonly User[] _cache = new User[10];
        public MyService(MyDbContext context) { _context = context; }

        public void Test()
        {
            var user = _context.Users.FirstOrDefault();
            _cache[0] = user;
        }
    }
}";

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Compound assignment on entity property
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task EntityPropertyCompoundAssignment_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var user = await _context.Users.FirstOrDefaultAsync();
            user.Name += "" suffix"";
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ══════════════════════════════════════════════════════════════════
    // NOT MATCHED — Entity property assignment via lambda mutation
    // ══════════════════════════════════════════════════════════════════

    [TestMethod]
    public async Task EntityPropertyAssignment_InsideLambda_NoFlag()
    {
        var source = BuildSource(@"
        public async Task Test()
        {
            var users = await _context.Users.ToListAsync();
            users.ForEach(u => u.IsActive = false);
        }");

        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }
}
