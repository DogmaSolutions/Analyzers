using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

[TestClass]
public partial class DSA020Tests
{
    [TestMethod]
    public async Task Empty()
    {
        var test = string.Empty;
        await CSharpAnalyzerVerifier<DSA020Analyzer>.VerifyAnalyzerAsync(test).ConfigureAwait(false);
    }

    public static string GetCaseDisplayName(MethodInfo methodInfo, object[] data)
    {
        #pragma warning disable CA1062
        return (string)data[0];
        #pragma warning restore CA1062
    }
}
