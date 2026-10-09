using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA031 when the terminal call is made on a local variable: whether the chain involves Entity Framework is decided
/// by looking at the initializer of the local (<c>DSA031Analyzer.LocalInitializerInvolvesEf</c>).
/// </summary>
public partial class DSA031Tests
{
    private static async Task AssertLocalVariableFlagsToList(string serviceBody)
    {
        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = BuildSource(serviceBody);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA031Analyzer>.Diagnostic(DSA031Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToList"));
        await test.RunAsync().ConfigureAwait(false);
    }

    private static async Task AssertLocalVariableNoFlag(string serviceBody)
    {
        var test = new CSharpAnalyzerVerifier<DSA031Analyzer>.Test();
        test.TestCode = BuildSource(serviceBody);
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializedFromDbSet_Flags()
    {
        await AssertLocalVariableFlagsToList(@"
        public void Test()
        {
            var query = _context.Users.Where(u => u.IsActive);
            var result = {|#0:query.ToList()|};
            System.Console.WriteLine(result.Count);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializedWithAnEntityFrameworkMethodOnANonDbSetQueryable_Flags()
    {
        await AssertLocalVariableFlagsToList(@"
        public void Test()
        {
            var query = new List<User>().AsQueryable().TagWith(""x"");
            var result = {|#0:query.ToList()|};
            System.Console.WriteLine(result.Count);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializedFromANonEntityFrameworkQueryable_NoFlag()
    {
        await AssertLocalVariableNoFlag(@"
        public void Test()
        {
            var query = new List<User>().AsQueryable();
            var result = query.ToList();
            System.Console.WriteLine(result.Count);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_InitializerContainingAnInvocationWithoutMethodSymbol_NoFlag()
    {
        // 'nameof(...)' is syntactically an invocation, but it does not resolve to a method.
        await AssertLocalVariableNoFlag(@"
        public void Test()
        {
            var query = nameof(User).Length > 0 ? new List<User>().AsQueryable() : null;
            var result = query.ToList();
            System.Console.WriteLine(result.Count);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_WithoutInitializer_NoFlag()
    {
        await AssertLocalVariableNoFlag(@"
        public void Test()
        {
            IQueryable<User> query;
            query = _context.Users;
            var result = query.ToList();
            System.Console.WriteLine(result.Count);
        }").ConfigureAwait(false);
    }

    [TestMethod]
    public async Task LocalVariable_DeclaredByForEach_NoFlag()
    {
        // The declaration of a foreach variable is not a variable declarator, so there is no initializer to inspect.
        await AssertLocalVariableNoFlag(@"
        public void Test()
        {
            foreach (var query in new List<IQueryable<User>>())
            {
                System.Console.WriteLine(query.ToList().Count);
            }
        }").ConfigureAwait(false);
    }
}
