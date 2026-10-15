using System.Collections.Generic;

namespace DogmaSolutions.Analyzers
{
    /// <summary>What a rule needs to know to recognize a logger (see <see cref="LoggerDetector"/>).</summary>
    internal interface ILoggerRecognitionSettings
    {
        /// <summary>Namespaces (and their child namespaces) whose types are logging types.</summary>
        IReadOnlyList<string> LoggerNamespaces { get; }

        /// <summary>Logging types by full name, without generic arity.</summary>
        IReadOnlyList<string> LoggerTypes { get; }

        /// <summary>A logger declared in a base class counts.</summary>
        bool AcceptInheritedLogger { get; }

        /// <summary>A logger received as a parameter counts.</summary>
        bool AcceptLoggerParameters { get; }
    }
}
