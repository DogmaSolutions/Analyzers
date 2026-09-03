using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

public partial class DSA040Tests
{
    private static IEnumerable<object[]> GetNotMatchedCases =>
    [
        [
            "Non-security sink (dice roll)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        int diceRoll = rnd.Next(1, 7);
                        System.Console.WriteLine(diceRoll);
                    }
                }
            }"
        ],
        [
            "Security term only as a substring (keyboard), not a word boundary",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        var keyboardShortcut = rnd.Next();
                        System.Console.WriteLine(keyboardShortcut);
                    }
                }
            }"
        ],
        [
            "Security sink already fed by RandomNumberGenerator (not Random)",
            @"
            using System.Security.Cryptography;
            namespace TestApp
            {
                public class MyType
                {
                    public void M()
                    {
                        var apiKey = RandomNumberGenerator.GetBytes(32);
                        System.Console.WriteLine(apiKey.Length);
                    }
                }
            }"
        ],
        [
            "Security-named sink with no Random involved",
            @"
            namespace TestApp
            {
                public class MyType
                {
                    public void M()
                    {
                        var token = GetToken();
                        System.Console.WriteLine(token);
                    }

                    private string GetToken() => ""x"";
                }
            }"
        ],
        [
            "NextBytes into a non-security buffer",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        var noise = new byte[16];
                        rnd.NextBytes(noise);
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
        var test = new CSharpAnalyzerVerifier<DSA040Analyzer>.Test();
        test.TestCode = sourceCode;
        test.ReferenceAssemblies = ReferenceAssemblies.Net.Net80;

        await test.RunAsync().ConfigureAwait(false);
    }
}
