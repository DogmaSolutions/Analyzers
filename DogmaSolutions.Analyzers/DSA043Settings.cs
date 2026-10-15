using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>The operations that make a class worth logging about (see <see cref="DSA043Settings.ActivityKinds"/>).</summary>
    [Flags]
    internal enum ActivityKinds
    {
        None = 0,
        Catch = 1,
        Await = 2,
        Throw = 4,
        Call = 8,
    }

    /// <summary>
    /// The editorconfig options of DSA043, read once per options instance. Every threshold can be disabled by setting it to 0, and an
    /// invalid value falls back to the documented default.
    /// </summary>
    internal sealed class DSA043Settings : ILoggerRecognitionSettings
    {
        internal const string MinCyclomaticComplexityKey = "dotnet_diagnostic.DSA043.min_cyclomatic_complexity";
        internal const string MinMethodComplexityKey = "dotnet_diagnostic.DSA043.min_method_complexity";
        internal const string MinLinesKey = "dotnet_diagnostic.DSA043.min_lines";
        internal const string MinMethodsKey = "dotnet_diagnostic.DSA043.min_methods";
        internal const string MinConstructorParametersKey = "dotnet_diagnostic.DSA043.min_constructor_parameters";
        internal const string RequireAllThresholdsKey = "dotnet_diagnostic.DSA043.require_all_thresholds";
        internal const string CountBlankLinesKey = "dotnet_diagnostic.DSA043.count_blank_lines";
        internal const string RequireActivityKey = "dotnet_diagnostic.DSA043.require_activity";
        internal const string ActivityKindsKey = "dotnet_diagnostic.DSA043.activity_kinds";
        internal const string ActivityNamespacesKey = "dotnet_diagnostic.DSA043.activity_namespaces";
        internal const string LoggerNamespacesKey = "dotnet_diagnostic.DSA043.logger_namespaces";
        internal const string LoggerTypesKey = "dotnet_diagnostic.DSA043.logger_types";
        internal const string AcceptInheritedLoggerKey = "dotnet_diagnostic.DSA043.accept_inherited_logger";
        internal const string AcceptLoggerParametersKey = "dotnet_diagnostic.DSA043.accept_logger_parameters";
        internal const string IncludedTypeKindsKey = "dotnet_diagnostic.DSA043.included_type_kinds";
        internal const string IncludeStaticClassesKey = "dotnet_diagnostic.DSA043.include_static_classes";
        internal const string IncludeAbstractClassesKey = "dotnet_diagnostic.DSA043.include_abstract_classes";
        internal const string IncludeNestedTypesKey = "dotnet_diagnostic.DSA043.include_nested_types";
        internal const string ExcludedBaseTypesKey = "dotnet_diagnostic.DSA043.excluded_base_types";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA043.excluded_type_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA043.excluded_namespace_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA043.excluded_file_patterns";

        private static readonly string[] DefaultActivityKinds = { "catch", "await", "call" };

        private static readonly string[] DefaultActivityNamespaces =
        {
            "System.Net.Http", "System.Net.Sockets", "System.Net.Mail", "System.IO", "System.Data", "Microsoft.Data",
            "Microsoft.EntityFrameworkCore", "Dapper", "Npgsql", "MySql", "Oracle", "MongoDB", "StackExchange.Redis",
            "Azure", "Amazon", "RabbitMQ", "Confluent.Kafka", "MassTransit",
        };

        private static readonly string[] DefaultLoggerNamespaces = { "Microsoft.Extensions.Logging" };
        private static readonly string[] DefaultIncludedTypeKinds = { "class" };
        private static readonly string[] DefaultExcludedBaseTypes = { "System.Exception", "System.Attribute" };

        private static readonly string[] DefaultExcludedTypeNamePatterns =
        {
            "*Dto", "*Request", "*Response", "*Options", "*Settings", "*Config", "*Configuration", "*Constants", "*Extensions",
            "*Mapper", "*Mapping", "*Profile", "*Validator", "*Specification", "*Comparer", "*Converter",
        };

        private static readonly string[] DefaultExcludedNamespacePatterns = { "*.Migrations" };

        internal static readonly AnalyzerOptionsCache<DSA043Settings> Cache = new(static options => new DSA043Settings(options));

        internal DSA043Settings(AnalyzerConfigOptions options)
        {
            MinCyclomaticComplexity = AnalyzerOptionsReader.ReadInt(options, MinCyclomaticComplexityKey, 30, minInclusive: 0);
            MinMethodComplexity = AnalyzerOptionsReader.ReadInt(options, MinMethodComplexityKey, 15, minInclusive: 0);
            MinLines = AnalyzerOptionsReader.ReadInt(options, MinLinesKey, 200, minInclusive: 0);
            MinMethods = AnalyzerOptionsReader.ReadInt(options, MinMethodsKey, 0, minInclusive: 0);
            MinConstructorParameters = AnalyzerOptionsReader.ReadInt(options, MinConstructorParametersKey, 0, minInclusive: 0);
            RequireAllThresholds = AnalyzerOptionsReader.ReadBool(options, RequireAllThresholdsKey, false);
            CountBlankLines = AnalyzerOptionsReader.ReadBool(options, CountBlankLinesKey, false);

            RequireActivity = AnalyzerOptionsReader.ReadBool(options, RequireActivityKey, true);
            ActivityKinds = ParseActivityKinds(AnalyzerOptionsReader.ReadList(options, ActivityKindsKey, DefaultActivityKinds));
            ActivityNamespaces = AnalyzerOptionsReader.ReadList(options, ActivityNamespacesKey, DefaultActivityNamespaces);

            LoggerNamespaces = AnalyzerOptionsReader.ReadList(options, LoggerNamespacesKey, DefaultLoggerNamespaces);
            LoggerTypes = AnalyzerOptionsReader.ReadList(options, LoggerTypesKey, Array.Empty<string>());
            AcceptInheritedLogger = AnalyzerOptionsReader.ReadBool(options, AcceptInheritedLoggerKey, true);
            AcceptLoggerParameters = AnalyzerOptionsReader.ReadBool(options, AcceptLoggerParametersKey, true);

            IncludedTypeKinds = AnalyzerOptionsReader.ReadList(options, IncludedTypeKindsKey, DefaultIncludedTypeKinds);
            IncludeStaticClasses = AnalyzerOptionsReader.ReadBool(options, IncludeStaticClassesKey, false);
            IncludeAbstractClasses = AnalyzerOptionsReader.ReadBool(options, IncludeAbstractClassesKey, false);
            IncludeNestedTypes = AnalyzerOptionsReader.ReadBool(options, IncludeNestedTypesKey, false);
            ExcludedBaseTypes = AnalyzerOptionsReader.ReadList(options, ExcludedBaseTypesKey, DefaultExcludedBaseTypes);
            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, DefaultExcludedTypeNamePatterns);
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, DefaultExcludedNamespacePatterns);
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Total cyclomatic complexity of the class (0 = criterion disabled).</summary>
        internal int MinCyclomaticComplexity { get; }

        /// <summary>Cyclomatic complexity of the most complex method (0 = criterion disabled).</summary>
        internal int MinMethodComplexity { get; }

        /// <summary>Lines of code of the class, all its parts included (0 = criterion disabled).</summary>
        internal int MinLines { get; }

        /// <summary>Number of methods and operators (0 = criterion disabled).</summary>
        internal int MinMethods { get; }

        /// <summary>Parameters of the constructor with the most parameters, a primary constructor included (0 = criterion disabled).</summary>
        internal int MinConstructorParameters { get; }

        /// <summary>All the enabled criteria must be met, instead of any of them.</summary>
        internal bool RequireAllThresholds { get; }

        internal bool CountBlankLines { get; }

        /// <summary>The class must also do something worth logging about.</summary>
        internal bool RequireActivity { get; }

        internal ActivityKinds ActivityKinds { get; }

        internal IReadOnlyList<string> ActivityNamespaces { get; }

        public IReadOnlyList<string> LoggerNamespaces { get; }

        public IReadOnlyList<string> LoggerTypes { get; }

        public bool AcceptInheritedLogger { get; }

        public bool AcceptLoggerParameters { get; }

        internal IReadOnlyList<string> IncludedTypeKinds { get; }

        internal bool IncludeStaticClasses { get; }

        internal bool IncludeAbstractClasses { get; }

        internal bool IncludeNestedTypes { get; }

        internal IReadOnlyList<string> ExcludedBaseTypes { get; }

        internal IReadOnlyList<string> ExcludedTypeNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedNamespacePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }

        /// <summary>
        /// Whether the metrics make the class a target: any (or, with <see cref="RequireAllThresholds"/>, all) of the enabled
        /// criteria is met. With every criterion disabled no class is a target.
        /// </summary>
        internal bool IsComplexEnough(TypeMetrics metrics)
        {
            var enabled = new List<bool>(5);
            AddCriterion(enabled, MinCyclomaticComplexity, metrics.CyclomaticComplexity);
            AddCriterion(enabled, MinMethodComplexity, metrics.MaxMethodComplexity);
            AddCriterion(enabled, MinLines, metrics.Lines);
            AddCriterion(enabled, MinMethods, metrics.Methods);
            AddCriterion(enabled, MinConstructorParameters, metrics.MaxConstructorParameters);

            if (enabled.Count == 0)
                return false;

            return RequireAllThresholds ? enabled.All(met => met) : enabled.Any(met => met);
        }

        private static void AddCriterion(List<bool> enabledCriteria, int threshold, int value)
        {
            if (threshold > 0)
                enabledCriteria.Add(value >= threshold);
        }

        /// <summary>The activity kinds named in the list (case-insensitive); unknown names are ignored.</summary>
        internal static ActivityKinds ParseActivityKinds(IEnumerable<string> names)
        {
            return names.Aggregate(ActivityKinds.None, (kinds, name) => kinds | ParseActivityKind(name));
        }

        private static ActivityKinds ParseActivityKind(string name)
        {
            switch (name.ToUpperInvariant())
            {
                case "CATCH": return ActivityKinds.Catch;
                case "AWAIT": return ActivityKinds.Await;
                case "THROW": return ActivityKinds.Throw;
                case "CALL": return ActivityKinds.Call;
                default: return ActivityKinds.None;
            }
        }
    }
}
