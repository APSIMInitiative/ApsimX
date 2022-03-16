using System;
using System.Text;

namespace APSIM.Server.Sensibility
{
    public class SimulationResetException : Exception
    {
        /// <summary>
        /// Simulation serialized to json before bering run.
        /// </summary>
        private readonly string oldJson;

        /// <summary>
        /// Simulation serialized to json after being run once.
        /// </summary>
        private readonly string newJson;

        public SimulationResetException(string simulationName, string oldJson, string newJson) : base($"Simulation {simulationName} failed to reset its state")
        {
            this.oldJson = oldJson;
            this.newJson = newJson;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine(Message);
            message.AppendLine("Simulation before being run:");
            message.AppendLine(oldJson);
            message.AppendLine("Simulation after being run once:");
            message.AppendLine(newJson);
            message.AppendLine(StackTrace);

            return message.ToString();
        }
    }
}
