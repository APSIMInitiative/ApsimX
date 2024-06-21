using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Models.Core.Run;
using Models.Storage;
using static Models.Core.Overrides;

namespace APSIM.Server.Commands
{
    /// <summary>
    /// A command to run simulations.
    /// </summary>
    [Serializable]
    public class RunCommand : ICommand
    {
        public bool runPostSimulationTools { get; set; }
        public bool runTests { get; set; }
        public List<string> simulationNamesToRun { get; set; }
        public int numberOfProcessors { get; set; }
        public List<Override> changes { get; set; }

        public RunCommand() { }

        /// <summary>
        /// Creates a <see cref="RunCommand" /> instance with sensible defaults.
        /// </summary>
        /// <param name="changes">Changes to be applied to the simulations before being run.</param>        [Newtonsoft.Json.JsonConstructor]
        [Newtonsoft.Json.JsonConstructor]
        public RunCommand(IEnumerable<Override> changes)
        {
            runPostSimulationTools = true;
            runTests = true;
            numberOfProcessors = -1;
            this.changes = changes.ToList<Override>();
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
        public RunCommand(bool runPostSimTools, bool runTests, int numProcessors, IEnumerable<Override> changes, IEnumerable<string> simulationNames)
        {
            runPostSimulationTools = runPostSimTools;
            this.runTests = runTests;
            numberOfProcessors = numProcessors;
            this.changes = changes.ToList<Override>();
            simulationNamesToRun = simulationNames.ToList<string>();
        }

        /// <summary>
        /// Run the command.
        /// </summary>
        /// <param name="runner">Job runner.</param>
        public void Run(Runner runner, ServerJobRunner jobRunner, IDataStore storage)
        {
            jobRunner.Replacements = changes;
            var timer = Stopwatch.StartNew();
            List<Exception> errors = runner.Run();
            timer.Stop();
            Console.WriteLine($"Raw job took {timer.ElapsedMilliseconds}ms");
            if (errors != null && errors.Count > 0)
                throw new AggregateException("File ran with errors", errors);
        }

        public override bool Equals(object obj)
        {
            if (obj is RunCommand command)
            {
                if (runPostSimulationTools != command.runPostSimulationTools)
                    return false;
                if (runTests != command.runTests)
                    return false;
                if (numberOfProcessors != command.numberOfProcessors)
                    return false;
                if (simulationNamesToRun.Count() != command.simulationNamesToRun.Count())
                    return false;
                if (simulationNamesToRun.Zip(command.simulationNamesToRun, (x, y) => x != y).Any(x => x))
                    return false;
                if (changes.Count() != command.changes.Count())
                    return false;
                if (changes.Zip(command.changes, (x, y) => !x.Equals(y)).Any(x => x))
                    return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (runPostSimulationTools, runTests, numberOfProcessors, simulationNamesToRun, changes).GetHashCode();
        }

        public override string ToString()
        {
            return $"{GetType().Name} with {changes.Count()} changes";
        }
    }
}
