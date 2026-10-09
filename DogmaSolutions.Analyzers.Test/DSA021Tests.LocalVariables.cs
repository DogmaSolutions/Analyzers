using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA021 when the terminal call is made on a local variable: whether the chain involves Entity Framework is decided
/// by looking at the initializer of the local (<c>DSA021Analyzer.LocalInitializerInvolvesEf</c>).
/// </summary>
public partial class DSA021Tests
{
    private static async Task AssertLocalVariableFlagsToList(string serviceBody)
    {
        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = BuildSource(serviceBody);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    private static async Task AssertLocalVariableNoFlag(string serviceBody)
    {
        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = BuildSource(serviceBody);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializedFromDbSet_Flags()
    {
        await AssertLocalVariableFlagsToList(@"
        public object Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = {|#0:query.ToList()|};
            return result;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializedWithAnEntityFrameworkMethodOnANonDbSetQueryable_Flags()
    {
        await AssertLocalVariableFlagsToList(@"
        public object Test()
        {
            var query = new List<User>().AsQueryable().AsNoTracking();
            var result = {|#0:query.ToList()|};
            return result;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializedFromANonEntityFrameworkQueryable_NoFlag()
    {
        await AssertLocalVariableNoFlag(@"
        public object Test()
        {
            var query = new List<User>().AsQueryable();
            var result = query.ToList();
            return result;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializerContainingAnInvocationWithoutMethodSymbol_NoFlag()
    {
        // 'nameof(...)' is syntactically an invocation, but it does not resolve to a method.
        await AssertLocalVariableNoFlag(@"
        public object Test()
        {
            var query = nameof(User).Length > 0 ? new List<User>().AsQueryable() : null;
            var result = query.ToList();
            return result;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_WithoutInitializer_NoFlag()
    {
        await AssertLocalVariableNoFlag(@"
        public object Test()
        {
            IQueryable<User> query;
            query = _context.Users;
            var result = query.ToList();
            return result;
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_DeclaredByForEach_NoFlag()
    {
        // The declaration of a foreach variable is not a variable declarator, so there is no initializer to inspect.
        await AssertLocalVariableNoFlag(@"
        public object Test()
        {
            var results = new List<object>();
            foreach (var query in new List<IQueryable<User>>())
            {
                results.Add(query.ToList());
            }
            return results;
        }").ConfigureAwait(false);
    }
}
