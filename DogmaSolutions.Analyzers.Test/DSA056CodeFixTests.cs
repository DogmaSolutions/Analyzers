using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All
#pragma warning disable CA1062 // test parameters come from DynamicData

namespace DogmaSolutions.Analyzers.Test;

/// <summary>The code fix of DSA056: the attribute is added above the class, where the class was.</summary>
[TestClass]
public class DSA056CodeFixTests
{
    private const string Attribute = "[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]";

    // title, the source ({|#n:Name|} on the identifier of each fixture), the fixed source
    private static IEnumerable<object[]> GetFixes =>
    [
        [
            "Plain fixture",
            "namespace App\n{\n    public class {|#0:OrderTests|}\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}",
            "namespace App\n{\n    " + Attribute + "\n    public class OrderTests\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}",
        ],
        [
            "Fixture with an attribute already",
            "namespace App\n{\n    [NUnit.Framework.TestFixture]\n    public class {|#0:OrderTests|}\n    {\n    }\n}",
            "namespace App\n{\n    " + Attribute + "\n    [NUnit.Framework.TestFixture]\n    public class OrderTests\n    {\n    }\n}",
        ],
        [
            "Comments stay above the attribute",
            "namespace App\n{\n    /// <summary>The tests.</summary>\n    public class {|#0:OrderTests|}\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}",
            "namespace App\n{\n    /// <summary>The tests.</summary>\n    " + Attribute + "\n    public class OrderTests\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}",
        ],
        [
            "Fixture without indentation",
            "public class {|#0:OrderTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}",
            Attribute + "\npublic class OrderTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}",
        ],
        [
            "Two fixtures",
            "public class {|#0:ATests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n\npublic class {|#1:BTests|}\n{\n    [Xunit.Fact]\n    public void Works() { }\n}",
            Attribute + "\npublic class ATests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}\n\n" + Attribute + "\npublic class BTests\n{\n    [Xunit.Fact]\n    public void Works() { }\n}",
        ],
        [
            "Nested fixture",
            "public class Outer\n{\n    public class {|#0:InnerTests|}\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}",
            "public class Outer\n{\n    " + Attribute + "\n    public class InnerTests\n    {\n        [Xunit.Fact]\n        public void Works() { }\n    }\n}",
        ],
    ];

    public static string GetCaseDisplayName(System.Reflection.MethodInfo methodInfo, object[] data) => CSharpVerifierHelper.SanitizeTestDisplayName((string)data[0]);

    [TestMethod]
    [DynamicData(nameof(GetFixes), DynamicDataDisplayName = nameof(GetCaseDisplayName))]
    public async Task The_attribute_is_added(string title, string source, string fixedSource)
    {
        var test = new CSharpCodeFixVerifier<DSA056Analyzer, DSA056CodeFixProvider>.Test();
        test.TestCode = source;
        test.FixedCode = fixedSource;
        test.ReferenceAssemblies = DSA056Tests.WithFrameworks;
        test.CodeActionEquivalenceKey = DSA056CodeFixProvider.ExcludeEquivalenceKey;
        test.TestBehaviors = TestBehaviors.SkipSuppressionCheck;
        for (var i = 0; source.Contains("{|#" + i + ":"); i++)
            test.ExpectedDiagnostics.Add(new DiagnosticResult(DSA056Analyzer.DiagnosticId, DiagnosticSeverity.Warning).WithLocation(i));

        await test.RunAsync().ConfigureAwait(false);
    }
}
