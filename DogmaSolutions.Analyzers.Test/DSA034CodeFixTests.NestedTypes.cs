using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// DSA034 "split by topic" when the type holds a nested type: the nested type gets a partial file of its own, declared
/// with the same kind of type (<c>CreatePartialTypeWithNestedOnly</c>).
/// </summary>
public partial class DSA034CodeFixTests
{
   private const string NestedTypesEditorConfig = @"
root = true
[*]
dotnet_diagnostic.DSA034.max_lines = 14
dotnet_diagnostic.DSA034.count_blank_lines = true
";

   [TestMethod]
   [DataRow("struct")]
   [DataRow("record")]
   [DataRow("record struct")]
   public async Task Topic_NestedTypeExtraction_InAnotherKindOfType(string kind)
   {
#pragma warning disable CA1062
      string Host(string name = "MyLedger") => kind + " " + name;
#pragma warning restore CA1062

      var source = @"namespace TestApp
{
    public " + Host("{|#0:MyLedger|}") + @"
    {
        public MyLedger() { }

        public void ImportOrder() { }
        public void ExportOrder() { }
        public void ClearCache() { }
        public void WarmCache() { }
        public void Alpha() { }
        public void Beta() { }

        public enum Priority { Low, High }
    }
}";

      string Partial(string members) => @"namespace TestApp
{
    public partial " + Host() + @"
    {
" + members + @"
    }
}";

      var fixedCtors = Partial(@"        public MyLedger()
        {
        }");

      var fixedCache = Partial(@"        public void ClearCache()
        {
        }

        public void WarmCache()
        {
        }");

      var fixedOrder = Partial(@"        public void ImportOrder()
        {
        }

        public void ExportOrder()
        {
        }");

      var fixedMisc = Partial(@"        public void Alpha()
        {
        }

        public void Beta()
        {
        }");

      var fixedNested = Partial(@"        public enum Priority
        {
            Low,
            High
        }");

      var test = new CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Test();
      test.TestState.Sources.Add(source);
      test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
      test.CodeFixTestBehaviors = CodeFixTestBehaviors.SkipFixAllCheck;
      test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", NestedTypesEditorConfig));
      test.ExpectedDiagnostics.Add(
         CSharpCodeFixVerifier<DSA034Analyzer, DSA034CodeFixProvider>.Diagnostic(DSA034Analyzer.DiagnosticId)
            .WithLocation(0)
            .WithArguments("Test0.cs", 16, 14));
      test.CodeActionEquivalenceKey = DSA034CodeFixProvider.TopicEquivalenceKey;
      test.FixedState.Sources.Add(("MyLedger.Ctors.cs", fixedCtors));
      test.FixedState.Sources.Add(("/0/MyLedger.Cache.cs", fixedCache));
      test.FixedState.Sources.Add(("/0/MyLedger.Order.cs", fixedOrder));
      test.FixedState.Sources.Add(("/0/MyLedger.Misc.cs", fixedMisc));
      test.FixedState.Sources.Add(("/0/MyLedger.Priority.cs", fixedNested));
      test.FixedState.InheritanceMode = StateInheritanceMode.Explicit;
      test.FixedState.AnalyzerConfigFiles.Add(("/.editorconfig", NestedTypesEditorConfig));

      await test.RunAsync().ConfigureAwait(false);
   }
}
