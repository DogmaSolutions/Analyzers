using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA021 when the queried <see cref="System.Linq.IQueryable{T}"/> is a method parameter: the tag is looked up
/// in the argument supplied at every call site (<c>DSA021Analyzer.GetArgumentForParameter</c> maps parameter to argument).
/// </summary>
public partial class DSA021Tests
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
        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    /// <summary>Expects a single DSA021 on the <c>{|#0:...|}</c> marked invocation, which must be a ToListAsync.</summary>
    private static async Task AssertFlagsToListAsync(string source)
    {
        var test = new CSharpAnalyzerVerifier<DSA021Analyzer>.Test();
        test.TestCode = source;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA021Analyzer>.Diagnostic(DSA021Analyzer.DiagnosticId).WithLocation(0).WithArguments("ToListAsync"));
        await test.RunAsync().ConfigureAwait(false);
    }

    // ── Positional / named arguments ───────────────────────────────────

    [TestMethod]
    public async Task ParameterQuery_PositionalArgumentWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(_context.Users.TagWith(""x"")); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_PositionalArgumentWithoutTag_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(_context.Users); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NamedArgumentWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(take: 1, q: _context.Users.TagWith(""x"")); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NamedArgumentReorderedWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(int take, IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(q: _context.Users.TagWith(""x""), take: 1); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NamedArgumentWithoutTag_Flags()
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
        public Task<List<User>> Caller() { return Run(take: 1, _context.Users.TagWith(""x"")); }")).ConfigureAwait(false);
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
    public async Task ExtensionReceiverParameter_ReducedCallWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.TagWith(""x"").RunList(); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return source.ToListAsync(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionReceiverParameter_ReducedCallWithoutTag_Flags()
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
            @"public Task<List<User>> Caller() { return _context.Users.TagWith(""x"")?.RunList(); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return {|#0:source.ToListAsync()|}; }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionReceiverParameter_StaticCallWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return QueryExt.RunList(_context.Users.TagWith(""x"")); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source) { return source.ToListAsync(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionNonReceiverParameter_ReducedCallWithTag_NoFlag()
    {
        // 'other' is the second parameter, but the first argument of the reduced call.
        await AssertNoFlag(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.RunList(_context.Users.TagWith(""x"")); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source, IQueryable<User> other) { return other.ToListAsync(); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ExtensionNonReceiverParameter_ReducedCallWithoutTag_Flags()
    {
        await AssertFlagsToListAsync(BuildSourceWithExtensions(
            @"public Task<List<User>> Caller() { return _context.Users.TagWith(""x"").RunList(_context.Users); }",
            @"public static Task<List<User>> RunList(this IQueryable<User> source, IQueryable<User> other) { return {|#0:other.ToListAsync()|}; }")).ConfigureAwait(false);
    }

    // ── Arguments that are not directly a tracked query ────────────────

    [TestMethod]
    public async Task ParameterQuery_ForwardedThroughParametersWithinMaxDepth_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Level1(IQueryable<User> q) { return q.ToListAsync(); }
        private Task<List<User>> Level2(IQueryable<User> q) { return Level1(q); }
        private Task<List<User>> Level3(IQueryable<User> q) { return Level2(q); }
        public Task<List<User>> Caller() { return Level3(_context.Users.TagWith(""x"")); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_ForwardedThroughParametersBeyondMaxDepth_Flags()
    {
        // The lookup gives up after a fixed number of forwarding levels and reports the query.
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Level1(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        private Task<List<User>> Level2(IQueryable<User> q) { return Level1(q); }
        private Task<List<User>> Level3(IQueryable<User> q) { return Level2(q); }
        private Task<List<User>> Level4(IQueryable<User> q) { return Level3(q); }
        public Task<List<User>> Caller() { return Level4(_context.Users.TagWith(""x"")); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_ArgumentIsLocalWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { var query = _context.Users.TagWith(""x""); return Run(query); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_ArgumentIsLocalWithoutTag_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { var query = _context.Users.Where(u => u.IsActive); return Run(query); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_ArgumentIsNull_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(null); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_ArgumentIsMethodResultWithoutTag_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private IQueryable<User> GetUsers() { return _context.Users; }
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller() { return Run(GetUsers()); }")).ConfigureAwait(false);
    }

    // ── Several call sites / unusual call-site shapes ──────────────────

    [TestMethod]
    public async Task ParameterQuery_AllCallSitesWithTag_NoFlag()
    {
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller1() { return Run(_context.Users.TagWith(""x"")); }
        public Task<List<User>> Caller2() { return Run(_context.Users.TagWith(""y"")); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_OneCallSiteWithoutTag_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }
        public Task<List<User>> Caller1() { return Run(_context.Users.TagWith(""x"")); }
        public Task<List<User>> Caller2() { return Run(_context.Users); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_NeverCalled_Flags()
    {
        await AssertFlagsToListAsync(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return {|#0:q.ToListAsync()|}; }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_InvocationsWithoutMethodSymbolAreIgnoredWhenLookingForCallSites_NoFlag()
    {
        // 'nameof(...)' is syntactically an invocation but resolves to no method: it is not a call site.
        await AssertNoFlag(BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return q.ToListAsync(); }
        public string Name() { return nameof(User); }
        public Task<List<User>> Caller() { return Run(_context.Users.TagWith(""x"")); }")).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task ParameterQuery_OfAnIndexer_Flags()
    {
        // The parameter does not belong to a method, so its call sites cannot be looked up.
        await AssertFlagsToListAsync(BuildSource(@"
        public Task<List<User>> this[IQueryable<User> q] { get { return {|#0:q.ToListAsync()|}; } }")).ConfigureAwait(false);
    }

    // ── Direct calls ───────────────────────────────────────────────────

    private static (IParameterSymbol Parameter, Compilation Compilation) GetRunQueryParameter() => GetParameter(compilation =>
        compilation.GetTypeByMetadataName("TestApp.MyService").GetMembers("Run").OfType<IMethodSymbol>().Single().Parameters[0]);

    private static (IParameterSymbol Parameter, Compilation Compilation) GetIndexerParameter() => GetParameter(compilation =>
        compilation.GetTypeByMetadataName("TestApp.MyService").GetMembers().OfType<IPropertySymbol>().Single(p => p.IsIndexer).Parameters[0]);

    private static (IParameterSymbol Parameter, Compilation Compilation) GetParameter(Func<Compilation, IParameterSymbol> selectParameter)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
            .Split(System.IO.Path.PathSeparator)
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path));
        var source = BuildSource(@"
        private Task<List<User>> Run(IQueryable<User> q) { return q.ToListAsync(); }
        public Task<List<User>> Caller() { return Run(_context.Users.TagWith(""x"")); }
        public int this[IQueryable<User> indexerQuery] { get { return 0; } }");
        var compilation = CSharpCompilation.Create(
            "DSA021DirectCalls",
            new[] { CSharpSyntaxTree.ParseText(source) },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        return (selectParameter(compilation), compilation);
    }

    [TestMethod]
    public void ParameterHasTagAtAllCallSites_ReturnsTrueWithEnoughDepth()
    {
        var (parameter, compilation) = GetRunQueryParameter();
        Assert.IsTrue(DSA021Analyzer.ParameterHasTagAtAllCallSites(parameter, compilation));
        Assert.IsTrue(DSA021Analyzer.ParameterHasTagAtAllCallSites(parameter, compilation, 1));
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(-1)]
    public void ParameterHasTagAtAllCallSites_ReturnsFalseWhenNoDepthIsLeft(int maxDepth)
    {
        var (parameter, compilation) = GetRunQueryParameter();
        Assert.IsFalse(DSA021Analyzer.ParameterHasTagAtAllCallSites(parameter, compilation, maxDepth));
    }

    [TestMethod]
    public void ParameterHasTagAtAllCallSites_ReturnsFalseForAParameterNotBelongingToAMethod()
    {
        var (parameter, compilation) = GetIndexerParameter();
        Assert.IsInstanceOfType<IPropertySymbol>(parameter.ContainingSymbol);
        Assert.IsFalse(DSA021Analyzer.ParameterHasTagAtAllCallSites(parameter, compilation));
    }
}
