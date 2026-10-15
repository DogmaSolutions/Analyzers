using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.Diagnostics;

namespace DogmaSolutions.Analyzers
{
    /// <summary>
    /// The editorconfig options of DSA044, read once per options instance. A missing, blank or malformed value falls back to the
    /// documented default.
    /// </summary>
    internal sealed class DSA044Settings : ILoggerRecognitionSettings
    {
        internal const string FlaggedTypesKey = "dotnet_diagnostic.DSA044.flagged_types";
        internal const string FlaggedMembersKey = "dotnet_diagnostic.DSA044.flagged_members";
        internal const string IncludeConsoleStreamsKey = "dotnet_diagnostic.DSA044.include_console_streams";
        internal const string LoggerNamespacesKey = "dotnet_diagnostic.DSA044.logger_namespaces";
        internal const string LoggerTypesKey = "dotnet_diagnostic.DSA044.logger_types";
        internal const string AcceptInheritedLoggerKey = "dotnet_diagnostic.DSA044.accept_inherited_logger";
        internal const string AcceptLoggerParametersKey = "dotnet_diagnostic.DSA044.accept_logger_parameters";
        internal const string RequireAccessibleLoggerKey = "dotnet_diagnostic.DSA044.require_accessible_logger";
        internal const string AcceptStaticLoggerCallsKey = "dotnet_diagnostic.DSA044.accept_static_logger_calls";
        internal const string StaticLoggerTypesKey = "dotnet_diagnostic.DSA044.static_logger_types";
        internal const string SkipInteractiveMembersKey = "dotnet_diagnostic.DSA044.skip_interactive_members";
        internal const string ExcludedTypeNamePatternsKey = "dotnet_diagnostic.DSA044.excluded_type_name_patterns";
        internal const string ExcludedMemberNamePatternsKey = "dotnet_diagnostic.DSA044.excluded_member_name_patterns";
        internal const string ExcludedNamespacePatternsKey = "dotnet_diagnostic.DSA044.excluded_namespace_patterns";
        internal const string ExcludedFilePatternsKey = "dotnet_diagnostic.DSA044.excluded_file_patterns";

        private static readonly string[] DefaultFlaggedTypes = { "System.Console", "System.Diagnostics.Debug" };
        private static readonly string[] DefaultFlaggedMembers = { "Write*" };

        // the loggers of the most common technologies: Microsoft.Extensions.Logging, Serilog, NLog, log4net, Castle, Common.Logging,
        // NServiceBus, Akka.NET and TraceSource. Concrete loggers (ILogger<T>, NLog.Logger, ...) are recognized through their interface.
        private static readonly string[] DefaultLoggerTypes =
        {
            "Microsoft.Extensions.Logging.ILogger", "Microsoft.Extensions.Logging.ILoggerFactory", "Serilog.ILogger", "NLog.ILogger",
            "log4net.ILog", "Castle.Core.Logging.ILogger", "Common.Logging.ILog", "NServiceBus.Logging.ILog", "Akka.Event.ILoggingAdapter",
            "System.Diagnostics.TraceSource",
        };

        private static readonly string[] DefaultStaticLoggerTypes = { "Serilog.Log" };
        private static readonly string[] DefaultExcludedTypeNamePatterns = { "Program" };
        private static readonly string[] DefaultExcludedMemberNamePatterns = { "Main" };

        internal static readonly AnalyzerOptionsCache<DSA044Settings> Cache = new(static options => new DSA044Settings(options));

        internal DSA044Settings(AnalyzerConfigOptions options)
        {
            FlaggedTypes = AnalyzerOptionsReader.ReadList(options, FlaggedTypesKey, DefaultFlaggedTypes);
            FlaggedMembers = AnalyzerOptionsReader.ReadList(options, FlaggedMembersKey, DefaultFlaggedMembers);
            IncludeConsoleStreams = AnalyzerOptionsReader.ReadBool(options, IncludeConsoleStreamsKey, true);

            LoggerNamespaces = AnalyzerOptionsReader.ReadList(options, LoggerNamespacesKey, Array.Empty<string>());
            LoggerTypes = AnalyzerOptionsReader.ReadList(options, LoggerTypesKey, DefaultLoggerTypes);
            AcceptInheritedLogger = AnalyzerOptionsReader.ReadBool(options, AcceptInheritedLoggerKey, true);
            AcceptLoggerParameters = AnalyzerOptionsReader.ReadBool(options, AcceptLoggerParametersKey, true);
            RequireAccessibleLogger = AnalyzerOptionsReader.ReadBool(options, RequireAccessibleLoggerKey, true);
            AcceptStaticLoggerCalls = AnalyzerOptionsReader.ReadBool(options, AcceptStaticLoggerCallsKey, true);
            StaticLoggerTypes = AnalyzerOptionsReader.ReadList(options, StaticLoggerTypesKey, DefaultStaticLoggerTypes);

            SkipInteractiveMembers = AnalyzerOptionsReader.ReadBool(options, SkipInteractiveMembersKey, true);
            ExcludedTypeNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedTypeNamePatternsKey, DefaultExcludedTypeNamePatterns);
            ExcludedMemberNamePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedMemberNamePatternsKey, DefaultExcludedMemberNamePatterns);
            ExcludedNamespacePatterns = AnalyzerOptionsReader.ReadList(options, ExcludedNamespacePatternsKey, Array.Empty<string>());
            ExcludedFilePatterns = AnalyzersUtils.ParseExcludedFilePatterns(options, ExcludedFilePatternsKey);
        }

        /// <summary>Types whose members are flagged, by full name.</summary>
        internal IReadOnlyList<string> FlaggedTypes { get; }

        /// <summary>Glob patterns of the names of the flagged members.</summary>
        internal IReadOnlyList<string> FlaggedMembers { get; }

        /// <summary><c>Console.Out</c> and <c>Console.Error</c> are flagged like <c>Console</c> itself.</summary>
        internal bool IncludeConsoleStreams { get; }

        public IReadOnlyList<string> LoggerNamespaces { get; }

        public IReadOnlyList<string> LoggerTypes { get; }

        public bool AcceptInheritedLogger { get; }

        public bool AcceptLoggerParameters { get; }

        /// <summary>The logger must be reachable from the flagged call: an instance logger does not count in a static member.</summary>
        internal bool RequireAccessibleLogger { get; }

        /// <summary>A class that already calls a static logger (e.g. <c>Serilog.Log</c>) has a logger available everywhere.</summary>
        internal bool AcceptStaticLoggerCalls { get; }

        internal IReadOnlyList<string> StaticLoggerTypes { get; }

        /// <summary>A member that also reads the console (a prompt, a menu) talks to a user: its writes are not logs.</summary>
        internal bool SkipInteractiveMembers { get; }

        internal IReadOnlyList<string> ExcludedTypeNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedMemberNamePatterns { get; }

        internal IReadOnlyList<string> ExcludedNamespacePatterns { get; }

        internal IReadOnlyList<string> ExcludedFilePatterns { get; }
    }
}
