using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using Models.Factorial;

namespace APSIM.Server.Commands
{
    /// <summary>
    /// A command to run simulations.
    /// </summary>
    [Serializable]
    public class RunCommand : ICommand
    {
        private bool runPostSimulationTools;
        private bool runTests;
        private IEnumerable<string> simulationNamesToRun;
        private int numberOfProcessors;
        private IEnumerable<CompositeFactor> changes;

        /// <summary>
        /// Creates a <see cref="RunCommand" /> instance with sensible defaults.
        /// </summary>
        /// <param name="changes">Changes to be applied to the simulations before being run.</param>
        public RunCommand(IEnumerable<CompositeFactor> changes)
        {
            runPostSimulationTools = true;
            runTests = true;
            numberOfProcessors = -1;
            this.changes = changes;
            simulationNamesToRun = null;
        }

        /// <summary>
        /// Create a <see cref="RunCommand" /> instance.
        /// </summary>
        /// <param name="runPostSimTools">Should post-simulation tools be run?</param>
        /// <param name="runTests">Should tests be run?</param>
        /// <param name="numProcessors">Max number of processors to use.</param>
        /// <param name="simulationNames">Simulation names to run.</param>
        /// <param name="changes">Changes to be applied to the simulations before being run.</param>
        public RunCommand(bool runPostSimTools, bool runTests, int numProcessors, IEnumerable<CompositeFactor> changes, IEnumerable<string> simulationNames)
        {
            runPostSimulationTools = runPostSimTools;
            this.runTests = runTests;
            numberOfProcessors = numProcessors;
            this.changes = changes;
            simulationNamesToRun = simulationNames;
        }

        /// <summary>
        /// Run the command.
        /// </summary>
        public void Run(Simulations sims)
        {
            if (changes != null && changes.Any())
                sims = EditFile.ApplyChanges(sims, changes);

            Runner runner = new Runner(sims,
                                       runPostSimulationTools: runPostSimulationTools,
                                       runSimulations: true,
                                       runTests: runTests,
                                       simulationNamesToRun: simulationNamesToRun,
                                       numberOfProcessors: numberOfProcessors);
            var timer = Stopwatch.StartNew();
            List<Exception> errors = runner.Run();
            timer.Stop();
            Console.WriteLine($"Raw job took {timer.ElapsedMilliseconds}ms");
            if (errors != null && errors.Count > 0)
                throw new AggregateException($"{sims.FileName} ran with errors", errors);
        }
    }
}
