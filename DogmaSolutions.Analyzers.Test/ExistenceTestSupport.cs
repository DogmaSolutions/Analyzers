using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>What the tests of DSA052 and DSA053 have in common: the source around the code, the messages, the editorconfig.</summary>
internal static class ExistenceTestSupport
{
    /// <summary>
    /// A query provider written for the tests: it has the same shape as the one of Entity Framework (IQueryable extensions that return tasks), so that the
    /// async scenarios do not depend on a package. The real package is used by a test of its own.
    /// </summary>
    internal const string Types = @"
    public class Item { public int Id; public string Name; public Item Parent; }
    public struct Pair { public int X; }
    public static class Provider
    {
        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> FirstOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
        public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<T> SingleOrDefaultAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
        public static Task<bool> AnyAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<bool> AnyAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
        public static Task<int> CountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<int> CountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
        public static Task<long> LongCountAsync<T>(this IQueryable<T> source, CancellationToken cancellationToken = default) => null;
        public static Task<long> LongCountAsync<T>(this IQueryable<T> source, Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) => null;
    }";

    internal const string Usings = "using System;\nusing System.Collections.Generic;\nusing System.Linq;\nusing System.Linq.Expressions;\nusing System.Threading;\nusing System.Threading.Tasks;\n";

    /// <summary>A source file with the members of a class. <c>[[...]]</c> marks the code a diagnostic is expected on, in order.</summary>
    internal static string BuildSource(string members, bool expectDiagnostics, string nullable = "")
    {
        var index = 0;
        var body = Regex.Replace(members, @"\[\[(.*?)\]\]", match => expectDiagnostics ? "{|#" + index++ + ":" + match.Groups[1].Value + "|}" : match.Groups[1].Value, RegexOptions.Singleline);
        return nullable + Usings + "namespace TestApp\n{" + Types + "\n    public class Repository\n    {\n" + body + "\n    }\n}";
    }

    internal static string ToEditorConfig(string rule, string settings)
    {
        var lines = (settings ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(entry =>
        {
            var parts = entry.Split(new[] { '=' }, 2);
            return "dotnet_diagnostic." + rule + "." + parts[0].Trim() + " = " + parts[1].Trim();
        });

        return "root = true\n[*.cs]\n" + string.Join("\n", lines) + "\n";
    }

    /// <summary>
    /// The expected message: "Queryable|Direct|FirstOrDefault|Any|variable" (the source of the call, how the result reaches the test, the method, the method
    /// that replaces it and the variable); the note about SingleOrDefault is added as the analyzer does.
    /// </summary>
    internal static string Message(string rule, string expected, string singleNoteResource)
    {
        var parts = expected.Split('|');
        var message = string.Format(Resources.Culture, Resources.ResourceManager.GetString("DSA" + rule + "Message" + parts[0] + parts[1], Resources.Culture), parts[2], parts[3], parts[4]);
        return singleNoteResource != null && parts[2].StartsWith("Single", StringComparison.Ordinal) ? message + string.Format(Resources.Culture, Resources.ResourceManager.GetString(singleNoteResource, Resources.Culture), parts[2], parts[3]) : message;
    }
}
