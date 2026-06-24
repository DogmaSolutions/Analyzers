using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA028CodeFixTests
{

    [TestMethod]
    public async Task FixAsyncTaskIReadOnlyListToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public async Task<IReadOnlyList<string>> GetNamesAsync(IEnumerable<string> source)
        {
            await Task.Delay(1);
            return {|#0:source.OrderBy(x => x).ToList()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public async Task<IReadOnlyList<string>> GetNamesAsync(IEnumerable<string> source)
        {
            await Task.Delay(1);
            return source.OrderBy(x => x).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixTernaryReturnToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> a, IEnumerable<int> b, bool flag)
        {
            return flag ? {|#0:a.ToList()|} : {|#1:b.ToList()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> a, IEnumerable<int> b, bool flag)
        {
            return flag ? a.ToArray() : b.ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixParenthesizedReturnToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            return ({|#0:source.ToList()|});
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            return (source.ToArray());
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixSwitchExpressionToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, int mode)
        {
            return mode switch
            {
                1 => {|#0:source.Where(x => x > 0).ToList()|},
                _ => {|#1:source.ToList()|}
            };
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, int mode)
        {
            return mode switch
            {
                1 => source.Where(x => x > 0).ToArray(),
                _ => source.ToArray()
            };
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 2).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixIndexerGetterToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[][] _data = new int[][] { new[] { 1, 2 }, new[] { 3, 4 } };
        public IReadOnlyList<int> this[int index]
        {
            get { return {|#0:_data[index].Where(x => x > 0).ToList()|}; }
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[][] _data = new int[][] { new[] { 1, 2 }, new[] { 3, 4 } };
        public IReadOnlyList<int> this[int index]
        {
            get { return _data[index].Where(x => x > 0).ToArray(); }
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixPropertyGetterExpressionBodyToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IReadOnlyCollection<int> Items
        {
            get => {|#0:_data.Where(x => x > 0).ToList()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IReadOnlyCollection<int> Items
        {
            get => _data.Where(x => x > 0).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixTernaryOneBranchToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
        {
            return flag ? {|#0:source.ToList()|} : System.Array.Empty<int>();
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
        {
            return flag ? source.ToArray() : System.Array.Empty<int>();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixNullCoalescingToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> cached, IEnumerable<int> source)
        {
            return cached ?? {|#0:source.Where(x => x > 0).ToList()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> cached, IEnumerable<int> source)
        {
            return cached ?? source.Where(x => x > 0).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixExpressionBodyTernaryToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IEnumerable<int> GetItems(bool flag) => flag ? {|#0:_data.ToList()|} : System.Array.Empty<int>();
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        private readonly int[] _data = new int[] { 1, 2, 3 };
        public IEnumerable<int> GetItems(bool flag) => flag ? _data.ToArray() : System.Array.Empty<int>();
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixVariableWithExplicitInterfaceType()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            IEnumerable<int> result = {|#0:source.Where(x => x > 0).ToList()|};
            return result;
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            IEnumerable<int> result = source.Where(x => x > 0).ToArray();
            return result;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixVariableAssignedSeparately()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            IEnumerable<int> result;
            result = {|#0:source.Where(x => x > 0).ToList()|};
            return result;
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            IEnumerable<int> result;
            result = source.Where(x => x > 0).ToArray();
            return result;
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixAsyncValueTaskToList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public async ValueTask<IEnumerable<int>> GetItemsAsync(IEnumerable<int> source)
        {
            await Task.Delay(1);
            return {|#0:source.Where(x => x > 0).ToList()|};
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
namespace TestApp
{
    public class MyService
    {
        public async ValueTask<IEnumerable<int>> GetItemsAsync(IEnumerable<int> source)
        {
            await Task.Delay(1);
            return source.Where(x => x > 0).ToArray();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task FixTernaryWithVariableBranch()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
        {
            var result = {|#0:source.ToList()|};
            return flag ? result : System.Array.Empty<int>();
        }
    }
}";

        var fixedSource = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source, bool flag)
        {
            var result = source.ToArray();
            return flag ? result : System.Array.Empty<int>();
        }
    }
}";

        await VerifyFixAsync(source, fixedSource, 1).ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixWhenReturnTypeIsList()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public List<int> GetItems(IEnumerable<int> source)
        {
            return source.Where(x => x > 0).ToList();
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA028Analyzer, DSA028CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NoFixWhenVariableMutated()
    {
        var source = @"
using System.Collections.Generic;
using System.Linq;
namespace TestApp
{
    public class MyService
    {
        public IEnumerable<int> GetItems(IEnumerable<int> source)
        {
            var result = source.Where(x => x > 0).ToList();
            result.Add(999);
            return result;
        }
    }
}";

        var test = new CSharpCodeFixVerifier<DSA028Analyzer, DSA028CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedState.InheritanceMode = StateInheritanceMode.AutoInheritAll;
        test.FixedState.MarkupHandling = MarkupMode.Allow;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        await test.RunAsync().ConfigureAwait(false);
    }

    // ---------------------------------------------------------------
    // Helper
    // ---------------------------------------------------------------

    private static async Task VerifyFixAsync(
        string source,
        string fixedSource,
        int expectedDiagnosticCount)
    {
        var test = new CSharpCodeFixVerifier<DSA028Analyzer, DSA028CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        for (var i = 0; i < expectedDiagnosticCount; i++)
        {
            test.ExpectedDiagnostics.Add(
                CSharpCodeFixVerifier<DSA028Analyzer, DSA028CodeFixProvider>
                    .Diagnostic(DSA028Analyzer.DiagnosticId).WithLocation(i));
        }

        await test.RunAsync().ConfigureAwait(false);
    }
}
