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
            "Non-Next method on Random is ignored",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public string M(Random rnd) => rnd.ToString();
                }
            }"
        ],
        [
            "Random value in a bare non-sink position (default: not a sink)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public void M(Random rnd)
                    {
                        rnd.Next();
                    }
                }
            }"
        ],
        [
            "Random value assigned to a non-security member access target",
            @"
            using System;
            namespace TestApp
            {
                public class Holder { public int Count; }
                public class MyType
                {
                    private Holder _h = new Holder();
                    public void M(Random rnd) { _h.Count = rnd.Next(); }
                }
            }"
        ],
        [
            "Random value positional to a non-security parameter",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    private static void Log(int value) { }
                    public void M(Random rnd) => Log(rnd.Next());
                }
            }"
        ],
        [
            "Random value as an element-access index (argument owner is not a method)",
            @"
            using System;
            namespace TestApp
            {
                public class MyType
                {
                    public int Pick(int[] items, Random rnd) => items[rnd.Next(items.Length)];
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
