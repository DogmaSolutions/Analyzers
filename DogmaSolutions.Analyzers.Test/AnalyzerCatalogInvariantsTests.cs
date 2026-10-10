using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// ReSharper disable All

namespace DogmaSolutions.Analyzers.Test;

/// <summary>
/// Poka-yoke for the catalog of rules: whatever is added to (or changed in) an analyzer must stay aligned with its
/// documentation, the README table, the release tracking files and the code fixes. A drift fails here, with the rule id.
/// </summary>
[TestClass]
public class AnalyzerCatalogInvariantsTests
{
    private static readonly Regex AnalyzerNamePattern = new(@"^DSA\d{3}Analyzer$", RegexOptions.Compiled);

    private static string RepositoryRoot { get; } = FindRepositoryRoot();

    private static string FindRepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "DogmaSolutions.Analyzers.slnx")))
                return dir.FullName;
        }

        throw new InvalidOperationException("Repository root not found from " + AppContext.BaseDirectory);
    }

    private static IReadOnlyList<DiagnosticAnalyzer> Analyzers { get; } = typeof(DSA001Analyzer).Assembly.GetTypes()
        .Where(type => !type.IsAbstract && AnalyzerNamePattern.IsMatch(type.Name) && typeof(DiagnosticAnalyzer).IsAssignableFrom(type))
        .OrderBy(type => type.Name, StringComparer.Ordinal)
        .Select(type => (DiagnosticAnalyzer)Activator.CreateInstance(type))
        .ToList();

    private static IEnumerable<(string Id, DiagnosticDescriptor Descriptor)> Rules =>
        Analyzers.SelectMany(analyzer => analyzer.SupportedDiagnostics.Select(descriptor => (descriptor.Id, descriptor)));

    private static string Read(params string[] relativePath) => File.ReadAllText(Path.Combine(new[] { RepositoryRoot }.Concat(relativePath).ToArray()));

    // ---- identity ---------------------------------------------------------------------------------------------

    [TestMethod]
    public void Analyzers_are_found()
    {
        Assert.IsTrue(Analyzers.Count >= 41, "Analyzers found: " + Analyzers.Count);
    }

    [TestMethod]
    public void Every_analyzer_reports_exactly_the_rule_named_after_its_class()
    {
        foreach (var analyzer in Analyzers)
        {
            var expectedId = analyzer.GetType().Name.Substring(0, "DSA000".Length);
            CollectionAssert.AreEqual(new[] { expectedId }, analyzer.SupportedDiagnostics.Select(d => d.Id).ToArray(), analyzer.GetType().Name);
        }
    }

    [TestMethod]
    public void Rule_ids_are_unique()
    {
        var duplicates = Rules.GroupBy(rule => rule.Id).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
        Assert.AreEqual(0, duplicates.Count, "Duplicated ids: " + string.Join(", ", duplicates));
    }

    [TestMethod]
    public void Every_analyzer_exposes_its_id_as_a_public_constant()
    {
        foreach (var analyzer in Analyzers)
        {
            var field = analyzer.GetType().GetField("DiagnosticId");
            Assert.IsNotNull(field, analyzer.GetType().Name + " has no DiagnosticId constant");
            Assert.AreEqual(analyzer.SupportedDiagnostics.Single().Id, field.GetRawConstantValue(), analyzer.GetType().Name);
        }
    }

    // ---- descriptors ------------------------------------------------------------------------------------------

    [TestMethod]
    public void Every_descriptor_has_title_message_description_and_category()
    {
        var problems = new List<string>();
        foreach (var (id, descriptor) in Rules)
        {
            if (string.IsNullOrWhiteSpace(descriptor.Title.ToString(System.Globalization.CultureInfo.InvariantCulture))) problems.Add(id + ": title");
            if (string.IsNullOrWhiteSpace(descriptor.MessageFormat.ToString(System.Globalization.CultureInfo.InvariantCulture))) problems.Add(id + ": message");
            if (string.IsNullOrWhiteSpace(descriptor.Description.ToString(System.Globalization.CultureInfo.InvariantCulture))) problems.Add(id + ": description");
            if (string.IsNullOrWhiteSpace(descriptor.Category)) problems.Add(id + ": category");
        }

        Assert.AreEqual(0, problems.Count, string.Join("; ", problems));
    }

    [TestMethod]
    public void Every_help_link_points_to_the_document_of_the_rule_and_the_document_exists()
    {
        var problems = new List<string>();
        foreach (var (id, descriptor) in Rules)
        {
            var expected = "https://github.com/DogmaSolutions/Analyzers/blob/main/docs/rules/" + id + ".md";
            if (descriptor.HelpLinkUri != expected)
                problems.Add(id + ": help link is '" + descriptor.HelpLinkUri + "'");
            if (!File.Exists(Path.Combine(RepositoryRoot, "docs", "rules", id + ".md")))
                problems.Add(id + ": docs/rules/" + id + ".md is missing");
        }

        Assert.AreEqual(0, problems.Count, string.Join("; ", problems));
    }

    // ---- documentation and release tracking -------------------------------------------------------------------

    [TestMethod]
    public void Every_rule_is_in_the_release_tracking_files()
    {
        var tracked = Read("DogmaSolutions.Analyzers", "AnalyzerReleases.Shipped.md") + Read("DogmaSolutions.Analyzers", "AnalyzerReleases.Unshipped.md");
        var missing = Rules.Select(rule => rule.Id).Where(id => !Regex.IsMatch(tracked, @"^" + id + @"\b", RegexOptions.Multiline)).ToList();
        Assert.AreEqual(0, missing.Count, "Not tracked: " + string.Join(", ", missing));
    }

    [TestMethod]
    public void Every_rule_is_in_the_readme_table_with_the_right_severity_and_enabled_state()
    {
        var readme = Read("README.md");
        var problems = new List<string>();

        foreach (var (id, descriptor) in Rules)
        {
            var row = readme.Split('\n').FirstOrDefault(line => line.StartsWith("| [" + id + "](", StringComparison.Ordinal));
            if (row == null)
            {
                problems.Add(id + ": not in the README table");
                continue;
            }

            var columns = row.Split('|').Select(column => column.Trim()).ToArray();
            // | Id | Category | Description | Severity | Enabled | Refactoring | Code review |
            var severity = columns[columns.Length - 5];
            var enabled = columns[columns.Length - 4];
            // The IDE calls "Suggestion" the Info severity of the compiler API.
            var expectedSeverity = descriptor.DefaultSeverity == DiagnosticSeverity.Info ? new[] { "Info", "Suggestion" } : new[] { descriptor.DefaultSeverity.ToString() };
            if (!expectedSeverity.Any(name => severity.Contains(name, StringComparison.Ordinal)))
                problems.Add(id + ": README severity '" + severity + "' but the rule is " + descriptor.DefaultSeverity);
            if ((enabled == "✅") != descriptor.IsEnabledByDefault)
                problems.Add(id + ": README enabled '" + enabled + "' but IsEnabledByDefault is " + descriptor.IsEnabledByDefault);
        }

        Assert.AreEqual(0, problems.Count, string.Join(Environment.NewLine, problems));
    }

    // ---- code fixes -------------------------------------------------------------------------------------------

    [TestMethod]
    public void Every_rule_has_a_code_fix_provider_registered_for_it()
    {
        var fixable = typeof(DSA001Analyzer).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(CodeFixProvider).IsAssignableFrom(type))
            .SelectMany(type => ((CodeFixProvider)Activator.CreateInstance(type)).FixableDiagnosticIds)
            .ToHashSet(StringComparer.Ordinal);

        var without = Rules.Select(rule => rule.Id).Where(id => !fixable.Contains(id)).ToList();
        Assert.AreEqual(0, without.Count, "Rules without any code fix: " + string.Join(", ", without));
    }

    [TestMethod]
    public void Every_rule_registers_its_code_review_comment_fix()
    {
        // Every rule must ship the "add a comment for the code review" fix: its resource has to be used by some code fix.
        var codeFixSources = Directory.GetFiles(Path.Combine(RepositoryRoot, "DogmaSolutions.Analyzers"), "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileName(path).Contains("CodeFix", StringComparison.Ordinal))
            .Select(File.ReadAllText)
            .ToList();

        var withoutReviewComment = Rules.Select(rule => rule.Id)
            .Where(id => !codeFixSources.Any(source => source.Contains("nameof(Resources." + id + "ReviewComment)", StringComparison.Ordinal)))
            .ToList();

        Assert.AreEqual(0, withoutReviewComment.Count, "Rules whose review comment fix is not wired: " + string.Join(", ", withoutReviewComment));
    }

    [TestMethod]
    public void Every_analyzer_skips_generated_code_runs_concurrently_and_registers_something()
    {
        foreach (var analyzer in Analyzers)
        {
            var context = new RecordingAnalysisContext();
            analyzer.Initialize(context);

            var name = analyzer.GetType().Name;
            Assert.AreEqual(GeneratedCodeAnalysisFlags.None, context.GeneratedCodeFlags, name + " must opt out of generated code analysis");
            Assert.IsTrue(context.ConcurrentExecutionEnabled, name + " must enable concurrent execution");
            Assert.IsTrue(context.RegistrationCount > 0, name + " registers no action");
        }
    }

    [TestMethod]
    public void Documented_severity_examples_use_the_editorconfig_vocabulary()
    {
        var valid = new HashSet<string>(new[] { "error", "warning", "suggestion", "silent", "none", "default" }, StringComparer.Ordinal);
        var files = Directory.GetFiles(Path.Combine(RepositoryRoot, "docs", "rules"), "*.md").Concat(new[] { Path.Combine(RepositoryRoot, "README.md") });

        foreach (var file in files)
        {
            foreach (Match match in Regex.Matches(File.ReadAllText(file), @"\.severity\s*=\s*(?<value>\S+)"))
                Assert.IsTrue(valid.Contains(match.Groups["value"].Value), Path.GetFileName(file) + ": '" + match.Groups["value"].Value + "' is not an editorconfig severity");
        }
    }
}
