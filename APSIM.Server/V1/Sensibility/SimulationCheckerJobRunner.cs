using APSIM.Shared.JobRunning;
using Models.Core;
using Models.Core.Run;

namespace APSIM.Server.Sensibility
{
    /// <summary>
    /// A job runner used by the simulation state checker. This job runner
    /// will insert a Logger model into each simulation before it is run, iff
    /// the simulation doesn't already contain a logger object.
    /// </summary>
    internal class SimulationCheckerJobRunner : ServerJobRunner
    {
        /// <inheritdoc />
        protected override void Run(IRunnable job)
        {
            if (job is SimulationDescription desc)
                AddLoggerToSim(desc.SimulationToRun);
            base.Run(job);
        }

        /// <summary>
        /// Add a logger instance to the given simulation if the simulation
        /// doesn't already contain a logger. This function will also connect
        /// links and events to the logger object.
        /// </summary>
        /// <param name="sim">The simulation.</param>
        private void AddLoggerToSim(Simulation sim)
        {
            if (sim.Node.FindChild<Logger>() == null)
            {
                Logger logger = new Logger();
                sim.Children.Add(logger);
                logger.Parent = sim;
                var links = new Links(sim.ModelServices);
                links.Resolve(logger, true, throwOnFail: true);
                var events = new Events(logger);
                events.ConnectEvents();
            }
        }
    }
}
