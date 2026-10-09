using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class CheckThenActUtilsTests
{
    private const string TypesSource = @"
namespace Microsoft.EntityFrameworkCore { public class DbSet<T> { } }
namespace Other { public class DbSet<T> { } public class Dictionary<K, V> { } public interface IQueryable { } }
namespace Fakes
{
    public class MyQueryable : System.Linq.IQueryable<int>
    {
        public System.Type ElementType => null;
        public System.Linq.Expressions.Expression Expression => null;
        public System.Linq.IQueryProvider Provider => null;
        public System.Collections.Generic.IEnumerator<int> GetEnumerator() => null;
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => null;
    }
    public class ImplementsOtherQueryable : Other.IQueryable { }
}
namespace System.Collections.Generic { public class NotACollection { } }
";

    // ---- HasAtomicAlternative ---------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("System.Collections.Generic.Dictionary`2", "TryAdd or indexer assignment [key] = value")]
    [DataRow("System.Collections.Generic.SortedDictionary`2", "TryAdd or indexer assignment [key] = value")]
    [DataRow("System.Collections.Generic.SortedList`2", "indexer assignment [key] = value")]
    [DataRow("System.Collections.Generic.HashSet`1", "Add (already returns a bool indicating whether the element was added)")]
    [DataRow("System.Collections.Generic.SortedSet`1", "Add (already returns a bool indicating whether the element was added)")]
    [DataRow("System.Collections.Concurrent.ConcurrentDictionary`2", "GetOrAdd, AddOrUpdate, or TryAdd")]
    [DataRow("System.Collections.Immutable.ImmutableHashSet`1", "Add (already handles duplicates)")]
    [DataRow("System.Collections.Immutable.ImmutableSortedSet`1", "Add (already handles duplicates)")]
    [DataRow("System.Collections.Immutable.ImmutableDictionary`2", "SetItem (upsert semantics)")]
    [DataRow("System.Collections.Immutable.ImmutableSortedDictionary`2", "SetItem (upsert semantics)")]
    public void HasAtomicAlternative_returns_the_suggestion(string metadataName, string expectedSuggestion)
    {
        var type = GetType(CreateCompilation(), metadataName);
        Assert.IsTrue(CheckThenActUtils.HasAtomicAlternative(type, out var suggestion));
        Assert.AreEqual(expectedSuggestion, suggestion);
    }

    [TestMethod]
    [DataRow("System.Collections.Generic.List`1")]
    [DataRow("System.Collections.Generic.Queue`1")]
    [DataRow("System.Collections.Concurrent.ConcurrentBag`1")]
    [DataRow("System.Collections.Immutable.ImmutableList`1")]
    [DataRow("System.Collections.Generic.NotACollection")]
    [DataRow("Other.Dictionary`2")]
    [DataRow("Other.DbSet`1")]
    public void HasAtomicAlternative_returns_false_for_other_types(string metadataName)
    {
        var type = GetType(CreateCompilation(TypesSource), metadataName);
        Assert.IsFalse(CheckThenActUtils.HasAtomicAlternative(type, out var suggestion));
        Assert.IsNull(suggestion);
    }

    [TestMethod]
    public void HasAtomicAlternative_returns_false_for_null_type()
    {
        Assert.IsFalse(CheckThenActUtils.HasAtomicAlternative(null, out var suggestion));
        Assert.IsNull(suggestion);
    }

    [TestMethod]
    public void HasAtomicAlternative_returns_false_for_a_type_without_namespace()
    {
        var arrayType = CreateCompilation().CreateArrayTypeSymbol(CreateCompilation().GetSpecialType(SpecialType.System_Int32));
        Assert.IsNull(arrayType.ContainingNamespace);
        Assert.IsFalse(CheckThenActUtils.HasAtomicAlternative(arrayType, out var suggestion));
        Assert.IsNull(suggestion);
    }

    // ---- CategorizeReceiverType -------------------------------------------------------------------------------

    [TestMethod]
    [DataRow("Microsoft.EntityFrameworkCore.DbSet`1", nameof(CheckThenActUtils.ReceiverCategory.Database))]
    [DataRow("System.Linq.IQueryable`1", nameof(CheckThenActUtils.ReceiverCategory.Database))]
    [DataRow("Fakes.MyQueryable", nameof(CheckThenActUtils.ReceiverCategory.Database))]
    [DataRow("System.Collections.Generic.Dictionary`2", nameof(CheckThenActUtils.ReceiverCategory.AtomicAlternative))]
    [DataRow("System.Collections.Immutable.ImmutableHashSet`1", nameof(CheckThenActUtils.ReceiverCategory.AtomicAlternative))]
    [DataRow("System.Collections.Generic.List`1", nameof(CheckThenActUtils.ReceiverCategory.NoAtomicAlternative))]
    [DataRow("Other.DbSet`1", nameof(CheckThenActUtils.ReceiverCategory.NoAtomicAlternative))]
    [DataRow("Fakes.ImplementsOtherQueryable", nameof(CheckThenActUtils.ReceiverCategory.NoAtomicAlternative))]
    public void CategorizeReceiverType_categorizes(string metadataName, string expectedCategory)
    {
        var type = GetType(CreateCompilation(TypesSource), metadataName);
        Assert.AreEqual(expectedCategory, CheckThenActUtils.CategorizeReceiverType(type).ToString());
    }

    [TestMethod]
    public void CategorizeReceiverType_null_type_has_no_atomic_alternative()
    {
        Assert.AreEqual(CheckThenActUtils.ReceiverCategory.NoAtomicAlternative, CheckThenActUtils.CategorizeReceiverType(null));
    }

    [TestMethod]
    public void CategorizeReceiverType_type_without_namespace_has_no_atomic_alternative()
    {
        var compilation = CreateCompilation();
        var arrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));
        Assert.AreEqual(CheckThenActUtils.ReceiverCategory.NoAtomicAlternative, CheckThenActUtils.CategorizeReceiverType(arrayType));
    }

    // ---- ResolveReceiverType ----------------------------------------------------------------------------------

    [TestMethod]
    public void ResolveReceiverType_returns_the_type_of_the_receiver_expression()
    {
        var compilation = CreateCompilation("class C { void M(System.Collections.Generic.HashSet<int> items) { items.Add(1); } }");
        var tree = compilation.SyntaxTrees.Single();
        var receiver = tree.GetRoot().DescendantNodes().OfType<MemberAccessExpressionSyntax>().Single().Expression;

        var type = CheckThenActUtils.ResolveReceiverType(receiver, compilation.GetSemanticModel(tree));

        Assert.IsNotNull(type);
        Assert.AreEqual("HashSet", type.Name);
    }
}
