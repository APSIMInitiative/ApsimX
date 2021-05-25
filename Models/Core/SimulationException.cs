using System;
using System.Text;

namespace Models.Core
{
    /// <summary>
    /// An exception thrown during a simulation run.
    /// </summary>
    public class SimulationException : Exception
    {
        /// <summary>
        /// Name of the file containing the simulation.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Create a <see cref="SimulationException" /> instance.
        /// </summary>
        /// <param name="simulationName">Name of the simulation in which the error was thrown.</param>
        /// <param name="fileName">Name of the file containing the simulation.</param>
        public SimulationException(string simulationName, string fileName) : base(GetContext(simulationName))
        {
            FileName = fileName;
        }

        /// <summary>
        /// Create a <see cref="SimulationException" /> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="simulationName">Name of the simulation in which the error was thrown.</param>
        /// <param name="fileName">Name of the file containing the simulation.</param>
        public SimulationException(string message, string simulationName, string fileName) : base($"{GetContext(simulationName)}: {message}")
        {
            FileName = fileName;
        }

        /// <summary>
        /// Create a <see cref="SimulationException" /> instance.
        /// </summary>
        /// <param name="innerException">Inner exception data.</param>
        /// <param name="simulationName">Name of the simulation in which the error was thrown.</param>
        /// <param name="fileName">Name of the file containing the simulation.</param>
        public SimulationException(Exception innerException, string simulationName, string fileName) : base(GetContext(simulationName), innerException)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Create a <see cref="SimulationException" /> instance.
        /// </summary>
        /// <param name="message">Error message.</param>
        /// <param name="innerException">Inner exception data.</param>
        /// <param name="simulationName">Name of the simulation in which the error was thrown.</param>
        /// <param name="fileName">Name of the file containing the simulation.</param>
        public SimulationException(string message, Exception innerException, string simulationName, string fileName) : base($"{GetContext(simulationName)}: {message}", innerException)
        {
            FileName = fileName;
        }

        /// <summary>
        /// Convert to string.
        /// </summary>
        public override string ToString()
        {
            StringBuilder result = new StringBuilder($"ERROR in file: {FileName}");
            result.Append(base.ToString());
            return result.ToString();
        }

        /// <summary>
        /// Get context message about an error in a simulation.
        /// </summary>
        /// <param name="simulation">Simulation name.</param>
        private static string GetContext(string simulation)
        {
            return $"Error in simulation {simulation}";
        }
    }
}