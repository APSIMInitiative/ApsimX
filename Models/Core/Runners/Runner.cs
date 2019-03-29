namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Runners;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Collections;
    using System.Linq;
    using Models.Core.ApsimFile;
    using Models.Core.Run;

    /// <summary>
    /// Gets a run job for running one or more simulations.
    /// </summary>
    public class Runner
    {
        /// <summary>Runs the specified simulations.</summary>
        /// <param name="model">Simulations to run.</param>
        /// <param name="simulations">Simulations model.</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <returns>A runnable job or null if nothing to run.</returns>
        public static RunOrganiser ForSimulations(Simulations simulations, IModel model, bool runTests)
        {
            return new RunOrganiser(simulations, model, runTests);
        }

        /// <summary>Run simulations in files specified by a file specification.</summary>
        /// <param name="fileSpec">The file specification</param>
        /// <param name="recurse">Recurse throug sub directories?</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <param name="verbose">Should the child process' output be redirected?</param>
        /// <param name="multiProcess">Should the child processes be run in multi-process mode?</param>
        /// <returns>The file of jobs that were run.</returns>
        public static IJobManager ForFolder(string fileSpec, bool recurse, bool runTests, bool verbose, bool multiProcess)
        {
            return new RunDirectoryOfApsimFiles(fileSpec, recurse, runTests, verbose, multiProcess);
        }

        /// <summary>Run simulations in files specified by a file specification.</summary>
        /// <param name="fileName">The file specification</param>
        /// <param name="runTests">Run the test nodes?</param>
        /// <returns>The file of jobs that were run.</returns>
        public static RunOrganiser ForFile(string fileName, bool runTests)
        {
            if (!File.Exists(fileName))
                throw new Exception("Cannot find file: " + fileName);
            List<Exception> creationExceptions;
            Simulations simulations = FileFormat.ReadFromFile<Simulations>(fileName, out creationExceptions);            
            return ForSimulations(simulations, simulations, runTests);
        }

        /// <summary>An enumable class for creating simulations ready for running.</summary>
        public class SimulationCreator : IEnumerable<Simulation>
        {
            private SimulationEnumerator simulations;

            /// <summary>Simulation names being run</summary>
            public int SimulationNamesBeingRun { get { return simulations.NumSimulationsBeingRun; } }

            /// <summary>Constructor</summary>
            /// <param name="underModel">Look at this model and all child models for simulations to create</param>
            public SimulationCreator(IModel underModel)
            {
                simulations = new SimulationEnumerator(underModel);
            }

            /// <summary>Return simulation enumerator</summary>
            IEnumerator<Simulation> IEnumerable<Simulation>.GetEnumerator()
            {
                return simulations;
            }

            /// <summary>Return simulation enumerator</summary>
            IEnumerator IEnumerable.GetEnumerator()
            {
                return simulations;
            }
        }

        /// <summary>An enumerator for creating simulations ready for running.</summary>
        public class SimulationEnumerator : IEnumerator<Simulation>
        {
            private IModel relativeTo;
            private List<SimulationDescription> simulationDescriptionsToRun = new List<SimulationDescription>();
            private Simulation currentSimulation;
            private string fileName;
            private Simulations simulations;

            /// <summary>
            /// List of simulation clocks - allows us to monitor progress of the runs.
            /// </summary>
            public List<IClock> simClocks { get; private set; } = new List<IClock>();

            /// <summary>Simulation names being run</summary>
            public int NumSimulationsBeingRun { get { return simulationDescriptionsToRun.Count; } }

            /// <summary>Constructor</summary>
            /// <param name="underModel">Look at this model and all child models for simulations to create</param>
            public SimulationEnumerator(IModel underModel)
            {
                relativeTo = underModel;
                FindListOfModelsToRun();
                simulations = Apsim.Parent(relativeTo, typeof(Simulations)) as Simulations;
                if (simulations != null)
                    fileName = simulations.FileName;
            }

            /// <summary>Return the current simulation</summary>
            Simulation IEnumerator<Simulation>.Current { get { return currentSimulation; } }

            /// <summary>Return the current simulation</summary>
            object IEnumerator.Current { get { return currentSimulation; } }

            /// <summary>Dispose of object</summary>
            void IDisposable.Dispose() { }

            /// <summary>Move to next simulation</summary>
            bool IEnumerator.MoveNext()
            {
                currentSimulation = null;

                if (simulationDescriptionsToRun.Count > 0)
                {
                    // Determine if there are any simulation descriptions that need 
                    // converting to a simulation and then run.
                    currentSimulation = simulationDescriptionsToRun[0].ToSimulation(simulations);
                    currentSimulation.FileName = fileName;
                    IClock simClock = (IClock)Apsim.ChildrenRecursively(currentSimulation).Find(m => typeof(IClock).IsAssignableFrom(m.GetType()));
                    simClocks.Add(simClock);

                    simulationDescriptionsToRun.RemoveAt(0);
                }
                return currentSimulation != null;
            }

            /// <summary>Reset the enumerator</summary>
            void IEnumerator.Reset()
            {
                simClocks.Clear();
                FindListOfModelsToRun();
            }

            /// <summary>Determine the list of jobs to run</summary>
            private void FindListOfModelsToRun()
            {
                simulationDescriptionsToRun.Clear();

                // Get a list of all models we're going to run.
                foreach (var modelsToRun in Apsim.ChildrenRecursively(relativeTo, typeof(ISimulationDescriptionGenerator)).Cast<ISimulationDescriptionGenerator>())
                    simulationDescriptionsToRun.AddRange(modelsToRun.GenerateSimulationDescriptions());
            }

        }

    }
}
