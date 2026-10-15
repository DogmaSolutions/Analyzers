namespace DogmaSolutions.Analyzers.Test;

/// <summary>Sources shared by the tests of DSA044.</summary>
internal static class DSA044TestSupport
{
    /// <summary>Stand-ins for the loggers of the most common technologies (TraceSource is part of the framework).</summary>
    internal const string FakeLoggersSource = @"
namespace Microsoft.Extensions.Logging
{
    public interface ILogger { }
    public interface ILogger<T> : ILogger { }
    public interface ILoggerFactory { }
    public static class LoggerExtensions
    {
        public static void LogInformation(this ILogger logger, string message, params object[] args) { }
        public static void LogDebug(this ILogger logger, string message, params object[] args) { }
        public static void LogError(this ILogger logger, string message, params object[] args) { }
    }
}
namespace Serilog { public interface ILogger { } public static class Log { public static void Information(string message) { } } }
namespace NLog { public interface ILogger { } public class Logger : ILogger { } }
namespace log4net { public interface ILog { } }
namespace Castle.Core.Logging { public interface ILogger { } }
namespace Common.Logging { public interface ILog { } }
namespace NServiceBus.Logging { public interface ILog { } }
namespace Akka.Event { public interface ILoggingAdapter { } }
namespace Mine { public interface IMyLog { } }
namespace Other { public class NotALogger { } public static class Log { public static void Information(string message) { } } }
";

    internal const string Usings = "using System;\nusing System.Diagnostics;\nusing Microsoft.Extensions.Logging;\n";
}
