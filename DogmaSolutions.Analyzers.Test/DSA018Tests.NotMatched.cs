using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA018Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Just Add without existence check",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "Negated Any without Add in body",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (!items.Any(x => x == ""test""))
                        {
                            System.Console.WriteLine(""not found"");
                        }
                    }
                }
            }"
        ],
        [
            "Non-existence condition + Add",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(bool someFlag)
                    {
                        var items = new List<string>();
                        if (someFlag)
                        {
                            items.Add(""test"");
                        }
                    }
                }
            }"
        ],
        [
            "Dictionary check-then-act (DSA017 territory, not DSA018)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(Dictionary<string, int> dict, string key, int value)
                    {
                        if (!dict.ContainsKey(key))
                        {
                            dict.Add(key, value);
                        }
                    }
                }
            }"
        ],
        [
            "HashSet check-then-act (DSA017 territory, not DSA018)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(HashSet<string> set, string item)
                    {
                        if (!set.Contains(item))
                        {
                            set.Add(item);
                        }
                    }
                }
            }"
        ],
        [
            "DbSet check-then-act (DSA012 territory, not DSA018)",
            @"
            using System.Linq;
            using Microsoft.EntityFrameworkCore;
            namespace TestApp
            {
                public class Item { public int Id { get; set; } public string Name { get; set; } }
                public class MyDbContext : DbContext { public DbSet<Item> Items { get; set; } }
                public class MyService
                {
                    private readonly MyDbContext _db = null;
                    public void AddItem(string name)
                    {
                        if (!_db.Items.Any(x => x.Name == name))
                        {
                            _db.Items.Add(new Item { Name = name });
                        }
                    }
                }
            }"
        ],
        [
            "Existence check and insert on different collections (validation pattern)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public bool Validate(string scheduleType, List<string> errors)
                    {
                        var validTypes = new List<string> { ""TypeA"", ""TypeB"" };
                        if (!validTypes.Contains(scheduleType))
                        {
                            errors.Add(""Invalid schedule type"");
                            return false;
                        }
                        return true;
                    }
                }
            }"
        ],
        [
            "Check-then-act inside lock statement (already protected)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    private readonly object _lock = new object();
                    public void MyMethod(string item)
                    {
                        lock (_lock)
                        {
                            if (!_items.Contains(item))
                            {
                                _items.Add(item);
                            }
                        }
                    }
                }
            }"
        ],
        [
            "Check-then-act inside lock with negated Any",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _items = new List<string>();
                    private readonly object _sync = new object();
                    public void MyMethod(string item)
                    {
                        lock (_sync)
                        {
                            if (!_items.Any(x => x == item))
                            {
                                _items.Add(item);
                            }
                        }
                    }
                }
            }"
        ],
        [
            "Check-then-act inside nested lock (lock within try-catch)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly List<string> _cache = new List<string>();
                    private readonly object _sync = new object();
                    public void MyMethod(string key)
                    {
                        try
                        {
                            lock (_sync)
                            {
                                if (!_cache.Contains(key))
                                    _cache.Add(key);
                            }
                        }
                        catch (System.Exception) { }
                    }
                }
            }"
        ],
        [
            "Positive Any + throw without Add after",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (items.Any(x => x == ""test""))
                            throw new System.InvalidOperationException();
                    }
                }
            }"
        ],
        [
            "Local variable: negated Contains + Add (no TOCTOU risk)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private List<string> GetTheList() => new List<string>();
                    public int MyMethod()
                    {
                        List<string> myList = GetTheList();
                        if (!myList.Contains(""TEST""))
                            myList.Add(""TEST"");
                        return myList.Count;
                    }
                }
            }"
        ],
        [
            "Local variable: negated Any + Add (no TOCTOU risk)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (!items.Any(x => x == ""test""))
                        {
                            items.Add(""test"");
                        }
                    }
                }
            }"
        ],
        [
            "Local variable: positive Any + else Add (no TOCTOU risk)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (items.Any(x => x == ""test""))
                        {
                            System.Console.WriteLine(""exists"");
                        }
                        else
                        {
                            items.Add(""test"");
                        }
                    }
                }
            }"
        ],
        [
            "Local variable: positive Any + throw + Add after (no TOCTOU risk)",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod()
                    {
                        var items = new List<string>();
                        if (items.Any(x => x == ""test""))
                            throw new System.InvalidOperationException();
                        items.Add(""test"");
                    }
                }
            }"
        ],
        [
            "SortedSet parameter: Contains + Add (DSA017 territory, not DSA018)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    public void MyMethod(SortedSet<string> set, string item)
                    {
                        if (!set.Contains(item))
                        {
                            set.Add(item);
                        }
                    }
                }
            }"
        ],
        [
            "SortedDictionary field: ContainsKey + Add (DSA017 territory, not DSA018)",
            @"
            using System.Collections.Generic;
            namespace TestApp
            {
                public class MyClass
                {
                    private readonly SortedDictionary<string, int> _dict = new SortedDictionary<string, int>();
                    public void MyMethod(string key, int value)
                    {
                        if (!_dict.ContainsKey(key))
                        {
                            _dict.Add(key, value);
                        }
                    }
                }
            }"
        ],
        [
            "Default excluded member: JsonSerializerOptions.Converters",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace System.Text.Json.Serialization { public abstract class JsonConverter { } }
            namespace System.Text.Json
            {
                public class JsonSerializerOptions
                {
                    public IList<System.Text.Json.Serialization.JsonConverter> Converters { get; } = new List<System.Text.Json.Serialization.JsonConverter>();
                }
            }
            namespace TestApp
            {
                using System.Text.Json;
                using System.Text.Json.Serialization;
                public class MyConverter : JsonConverter { }
                public class MyClass
                {
                    private readonly JsonSerializerOptions _options = new JsonSerializerOptions();
                    public void Configure()
                    {
                        if (!_options.Converters.Any(c => c is MyConverter))
                        {
                            _options.Converters.Add(new MyConverter());
                        }
                    }
                }
            }"
        ],
        [
            "Default excluded member: JsonSerializerSettings.Converters",
            @"
            using System.Collections.Generic;
            using System.Linq;
            namespace Newtonsoft.Json
            {
                public abstract class JsonConverter { }
                public class JsonSerializerSettings
                {
                    public IList<JsonConverter> Converters { get; } = new List<JsonConverter>();
                }
            }
            namespace TestApp
            {
                using Newtonsoft.Json;
                public class MyConverter : JsonConverter { }
                public class MyClass
                {
                    private readonly JsonSerializerSettings _settings = new JsonSerializerSettings();
                    public void Configure()
                    {
                        if (!_settings.Converters.Any(c => c is MyConverter))
                        {
                            _settings.Converters.Add(new MyConverter());
                        }
                    }
                }
            }"
        ],
    ];

    [TestMethod]
    [DynamicData(nameof(GetNotMatchedCases), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task NotMatched(
        string title,
        string sourceCode
    )
    {
        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80.AddPackages(
        [
            ..new PackageIdentity[]
            {
                new("Microsoft.EntityFrameworkCore", "3.1.22")
            }
        ]);

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatched_CustomExcludedMember_ViaEditorConfig()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class PipelineOptions
                {
                    public List<string> Filters { get; } = new List<string>();
                }
                public class MyClass
                {
                    private readonly PipelineOptions _options = new PipelineOptions();
                    public void Configure()
                    {
                        if (!_options.Filters.Any(f => f == ""MyFilter""))
                        {
                            _options.Filters.Add(""MyFilter"");
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = PipelineOptions.Filters
"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Matched_WhenDefaultExclusionOverriddenByEditorConfig()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace System.Text.Json.Serialization { public abstract class JsonConverter { } }
            namespace System.Text.Json
            {
                public class JsonSerializerOptions
                {
                    public IList<System.Text.Json.Serialization.JsonConverter> Converters { get; } = new List<System.Text.Json.Serialization.JsonConverter>();
                }
            }
            namespace TestApp
            {
                using System.Text.Json;
                using System.Text.Json.Serialization;
                public class MyConverter : JsonConverter { }
                public class MyClass
                {
                    private readonly JsonSerializerOptions _options = new JsonSerializerOptions();
                    public void Configure()
                    {
                        {|#0:if (!_options.Converters.Any(c => c is MyConverter))
                        {
                            _options.Converters.Add(new MyConverter());
                        }|}
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = SomeOther.Member
"));

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA018Analyzer>.Diagnostic(DSA018Analyzer.DiagnosticId)
                .WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatched_MultipleExcludedMembers_ViaEditorConfig()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class PipelineOptions
                {
                    public List<string> Filters { get; } = new List<string>();
                    public List<string> Handlers { get; } = new List<string>();
                }
                public class MyClass
                {
                    private readonly PipelineOptions _options = new PipelineOptions();
                    public void Configure()
                    {
                        if (!_options.Filters.Any(f => f == ""MyFilter""))
                        {
                            _options.Filters.Add(""MyFilter"");
                        }
                        if (!_options.Handlers.Any(h => h == ""MyHandler""))
                        {
                            _options.Handlers.Add(""MyHandler"");
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = PipelineOptions.Filters, PipelineOptions.Handlers
"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatched_ExcludedMember_CaseInsensitive()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class PipelineOptions
                {
                    public List<string> Filters { get; } = new List<string>();
                }
                public class MyClass
                {
                    private readonly PipelineOptions _options = new PipelineOptions();
                    public void Configure()
                    {
                        if (!_options.Filters.Any(f => f == ""MyFilter""))
                        {
                            _options.Filters.Add(""MyFilter"");
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = pipelineoptions.filters
"));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Matched_ExcludedMember_DoesNotSuppressDifferentPropertyOnSameType()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class PipelineOptions
                {
                    public List<string> Filters { get; } = new List<string>();
                    public List<string> Handlers { get; } = new List<string>();
                }
                public class MyClass
                {
                    private readonly PipelineOptions _options = new PipelineOptions();
                    public void Configure()
                    {
                        {|#0:if (!_options.Handlers.Any(h => h == ""MyHandler""))
                        {
                            _options.Handlers.Add(""MyHandler"");
                        }|}
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = PipelineOptions.Filters
"));

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA018Analyzer>.Diagnostic(DSA018Analyzer.DiagnosticId)
                .WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task Matched_ExcludedMembers_SetToNone_OverridesDefaults()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace System.Text.Json.Serialization { public abstract class JsonConverter { } }
            namespace System.Text.Json
            {
                public class JsonSerializerOptions
                {
                    public IList<System.Text.Json.Serialization.JsonConverter> Converters { get; } = new List<System.Text.Json.Serialization.JsonConverter>();
                }
            }
            namespace TestApp
            {
                using System.Text.Json;
                using System.Text.Json.Serialization;
                public class MyConverter : JsonConverter { }
                public class MyClass
                {
                    private readonly JsonSerializerOptions _options = new JsonSerializerOptions();
                    public void Configure()
                    {
                        {|#0:if (!_options.Converters.Any(c => c is MyConverter))
                        {
                            _options.Converters.Add(new MyConverter());
                        }|}
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = none
"));

        test.ExpectedDiagnostics.Add(
            CSharpAnalyzerVerifier<DSA018Analyzer>.Diagnostic(DSA018Analyzer.DiagnosticId)
                .WithLocation(0));

        await test.RunAsync().ConfigureAwait(false);
    }

    [TestMethod]
    public async Task NotMatched_ExcludedMember_NestedPropertyAccess()
    {
        var sourceCode = @"
            using System.Collections.Generic;
            using System.Linq;
            namespace TestApp
            {
                public class AppConfig
                {
                    public List<string> Tags { get; } = new List<string>();
                }
                public class MyClass
                {
                    private readonly AppConfig _config = new AppConfig();
                    public void Configure()
                    {
                        if (!_config.Tags.Any(t => t == ""important""))
                        {
                            _config.Tags.Add(""important"");
                        }
                    }
                }
            }";

        var test = new CSharpAnalyzerVerifier<DSA018Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", @"
root = true
[*]
dotnet_diagnostic.DSA018.excluded_members = AppConfig.Tags
"));

        await test.RunAsync().ConfigureAwait(false);
    }
}
