using System;
using APSIM.Shared.Utilities;
using Models;
using Models.Core;

namespace APSIM.Server.Sensibility
{
    /// <summary>
    /// This model will serialize the entire simulation to JSON after the first
    /// day of the simulation.
    /// </summary>
    internal class Logger : Model
    {
        /// <summary>
        /// Link to clock, used to find first day of simulation.
        /// </summary>
        [Link]
        private Clock clock = null;

        /// <summary>
        /// Link to the simulation. This is what will be serialized.
        /// </summary>
        [Link(Type = LinkType.Ancestor)]
        private Simulation sim = null;

        /// <summary>
        /// This is the simulation serialized to json after the first day.
        /// </summary>
        public string Json { get; set; }

        /// <summary>
        /// Called at end of day. If today is simulation start date, will
        /// serialize the simulation to json.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        [EventSubscribe("EndOfDay")]
        private void Log(object sender, EventArgs args)
        {
            if (clock.Today == clock.StartDate)
            {
                // Set json to null before logging, to prevent it from being serialized.
                string oldJson = Json;
                Json = null;
                Json = ReflectionUtilities.JsonSerialise(sim, true);
                if (!string.IsNullOrEmpty(oldJson))
                {
                    // Second run.
                    if (!string.Equals(oldJson, Json, StringComparison.Ordinal))
                        throw new SimulationResetException(sim.Name, oldJson, Json);
                    clock.EndDate = clock.Today;
                }
            }
        }
    }
}
