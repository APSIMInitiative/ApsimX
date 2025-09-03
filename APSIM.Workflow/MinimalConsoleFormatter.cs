using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Globalization;
using System.IO;

namespace APSIM.Workflow
{
    
    /// <summary>
    /// Provides a minimal console formatter for logging output.
    /// </summary>
    public class MinimalConsoleFormatter : ConsoleFormatter
    {
        /// <summary>
        /// Name of the formatter used in the console logger.
        /// </summary>
        public const string FormatterName = "minimal";

        /// <summary>
        /// Initializes a new instance of the <see cref="MinimalConsoleFormatter"/> class.
        /// </summary>
        public MinimalConsoleFormatter() : base(FormatterName) { }

        /// <inheritdoc/>
        public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider scopeProvider, TextWriter textWriter)
        {
            var logLevel = logEntry.LogLevel;
            var exception = logEntry.Exception;
            var state = logEntry.State;

            // ISO 8601 timestamp
            string timestamp = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:ssK", CultureInfo.InvariantCulture);
            textWriter.Write($"{timestamp} [{logLevel}] ");
            if (state != null)
                textWriter.Write(state.ToString());
            if (exception != null)
                textWriter.Write($"\n{exception}");
            textWriter.WriteLine();
        }

    }
}
