using System;
using System.Text;
using Models.Core;

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

        public SimulationResetException(string simulationName, IModel model, string oldJson, string newJson) : base($"Model {model.Name} in simulation {simulationName} failed to reset its state")
        {
            this.oldJson = oldJson;
            this.newJson = newJson;
        }

        public override string ToString()
        {
            StringBuilder message = new StringBuilder();
            message.AppendLine(Message);
            message.AppendLine("Before being run:");
            message.AppendLine(oldJson);
            message.AppendLine("After being run once:");
            message.AppendLine(newJson);
            message.AppendLine(StackTrace);

            return message.ToString();
        }
    }
}
