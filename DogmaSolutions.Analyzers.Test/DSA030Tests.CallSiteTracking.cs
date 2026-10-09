using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA030 when the queried <see cref="System.Linq.IQueryable{T}"/> is a method parameter: the tracking choice is looked up
/// in the argument supplied at every call site (<c>DSA030Analyzer.GetArgumentForParameter</c> maps parameter to argument).
/// </summary>
public partial class DSA030Tests
{
    private const string ExtensionsClassHeader = @"
namespace TestApp
{
    public static class QueryExt
    {";

    private static string BuildSourceWithExtensions(string serviceBody, string extensionsBody)
    {
        return BuildSource(serviceBody) + ExtensionsClassHeader + extensionsBody + @"
    }
}";
    }

    private static async Task AssertNoFlag(string source)
    {
        var test = new CSharpAnalyzerVerifier<DSA030Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>Expects a single DSA030 on the <c>{|#0:...|}</c> marked invocation, which must be a ToListAsync.</summary>
    private static async Task AssertFlagsToListAsync(string source)
    {
        var test = new CSharpAnalyzerVerifier<DSA030Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA030Analyzer>.Diagnostic(DSA030Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Positional / named arguments ───────────────────────────────────

    [TestMethod]
    public async Task ParameterQuery_PositionalArgumentWithTracking_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(_context.Users.AsNoTracking()); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_PositionalArgumentWithoutTracking_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(_context.Users); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NamedArgumentWithTracking_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(take: 1, q: _context.Users.AsNoTracking()); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NamedArgumentReorderedWithTracking_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(q: _context.Users.AsNoTracking(), take: 1); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NamedArgumentWithoutTracking_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(take: 1, q: _context.Users); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_PositionalArgumentAfterNamedInPosition_NoFlag()
    {
        // C# 7.2 non-trailing named argument: 'take' is named but in its own position, so 'q' is still positional.
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(take: 1, _context.Users.AsNoTracking()); }")).ConfigureAwait(false);
    }

    // ── Omitted optional argument ──────────────────────────────────────

    [TestMethod]
    public async Task ParameterQuery_OptionalArgumentOmitted_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q = null) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_OptionalArgumentOmittedBeforeNamedArgument_Flags()
    {
        // 'q' is omitted, and the argument sitting at q's position is the named 'skip': it must not be taken as q's value.
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q = null, int skip = 0) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(1, skip: 5); }")).ConfigureAwait(false);
    }

    // ── Extension methods ──────────────────────────────────────────────

    [TestMethod]
    public async Task ExtensionReceiverParameter_ReducedCallWithTracking_NoFlag()
    {
        await AssertNoFlag(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.AsNoTracking().RunList(); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return source.ToListAsync(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionReceiverParameter_ReducedCallWithoutTracking_Flags()
    {
        await AssertFlagsToListAsync(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.RunList(); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return {|#0:source.ToListAsync()|}; }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionReceiverParameter_ConditionalAccessCall_Flags()
    {
        // 'x?.RunList()' is not a plain member access, so the receiver argument cannot be resolved.
        await AssertFlagsToListAsync(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.AsNoTracking()?.RunList(); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return {|#0:source.ToListAsync()|}; }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionReceiverParameter_StaticCallWithTracking_NoFlag()
    {
        await AssertNoFlag(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return QueryExt.RunList(_context.Users.AsNoTracking()); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return source.ToListAsync(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionNonReceiverParameter_ReducedCallWithTracking_NoFlag()
    {
        // 'other' is the second parameter, but the first argument of the reduced call.
        await AssertNoFlag(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.RunList(_context.Users.AsNoTracking()); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source, IQueryable<User> other) { return other.ToListAsync(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionNonReceiverParameter_ReducedCallWithoutTracking_Flags()
    {
        await AssertFlagsToListAsync(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.AsNoTracking().RunList(_context.Users); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source, IQueryable<User> other) { return {|#0:other.ToListAsync()|}; }")).ConfigureAwait(false);
    }
}
