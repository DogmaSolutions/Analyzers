using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The per-type index of identifiers that DSA047, DSA049 and DSA050 share.</summary>
[TestClass]
public class TypeIdentifierIndexTests
{
    private const string Source = @"
namespace TestApp
{
    public partial class Worker
    {
        private readonly object _gate = new object();
        private int _count;
        public void Inc() { lock (_gate) { _count++; } }
        public class Nested { public int N { get { return 1; } } }
    }

    public partial class Worker
    {
        public int Get() => _count;
    }

    public class Plain
    {
        public int X;
    }
}";

    private static (CSharpCompilation Compilation, INamedTypeSymbol Worker, INamedTypeSymbol Plain) Create()
    {
        var compilation = CSharpCompilation.Create("test", new[] { CSharpSyntaxTree.ParseText(Source) }, new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
        return (compilation, compilation.GetTypeByMetadataName("TestApp.Worker"), compilation.GetTypeByMetadataName("TestApp.Plain"));
    }

    [TestMethod]
    public void The_roots_are_the_parts_of_the_type()
    {
        var (compilation, worker, plain) = Create();

        Assert.AreEqual(2, TypeIdentifierIndex.Get(worker, compilation).GetRoots(CancellationToken.None).Length);
        Assert.AreEqual(1, TypeIdentifierIndex.Get(plain, compilation).GetRoots(CancellationToken.None).Length);
    }

    [TestMethod]
    public void A_type_is_indexed_once()
    {
        var (compilation, worker, _) = Create();

        Assert.AreSame(TypeIdentifierIndex.Get(worker, compilation), TypeIdentifierIndex.Get(worker, compilation));
    }

    [TestMethod]
    public void The_locks_are_found_and_remembered()
    {
        var (compilation, worker, plain) = Create();
        var workerIndex = TypeIdentifierIndex.Get(worker, compilation);
        var plainIndex = TypeIdentifierIndex.Get(plain, compilation);

        Assert.IsTrue(workerIndex.HasLocks(CancellationToken.None));
        Assert.IsTrue(workerIndex.HasLocks(CancellationToken.None));
        Assert.IsFalse(plainIndex.HasLocks(CancellationToken.None));
        Assert.IsFalse(plainIndex.HasLocks(CancellationToken.None));
    }

    [TestMethod]
    public void The_identifiers_are_found_by_name_in_every_part_in_source_order()
    {
        var (compilation, worker, _) = Create();
        var index = TypeIdentifierIndex.Get(worker, compilation);

        var found = index.Find("_count", CancellationToken.None);

        Assert.AreEqual(2, found.Count);
        Assert.IsTrue(found[0].SpanStart < found[1].SpanStart);
        Assert.AreEqual(0, index.Find("Missing", CancellationToken.None).Count);
        Assert.AreSame(index.Find("_count", CancellationToken.None), found);
    }

    [TestMethod]
    public void The_owner_of_an_identifier_is_its_innermost_type()
    {
        var (compilation, worker, _) = Create();
        var index = TypeIdentifierIndex.Get(worker, compilation);

        var inNested = index.GetRoots(CancellationToken.None)[0].DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.IdentifierNameSyntax>().First(n => n.Identifier.ValueText == "_gate");
        var owner = TypeIdentifierIndex.GetOwner(inNested);

        Assert.AreEqual("Worker", owner.Identifier.ValueText);
        Assert.IsNotNull(index.GetModel(inNested.SyntaxTree));
    }
}
