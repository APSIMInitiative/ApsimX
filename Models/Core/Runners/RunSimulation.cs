namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Interfaces;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;

    /// <summary>
    /// This runnable class clones a simulation and then runs it.
    /// </summary>
    [Serializable]
    public class RunSimulation : JobManager.IRunnable, JobManager.IComputationalyTimeConsuming
    {
        /// <summary>The simulation to run.</summary>
        private Simulation simulationToRun;

        /// <summary>The .apsimx filename where this simulation resides</summary>
        private string fileName;

        /// <summary>The engine</summary>
        [NonSerialized]
        private ISimulationEngine simulationEngine;

        /// <summary>The simulation to run.</summary>
        public bool cloneSimulationBeforeRun;

        /// <summary>An array of services that can be used to resolve links in the simulation</summary>
        public object[] Services { get; set; }

        /// <summary>A timer to record how long it takes to run</summary>
        [NonSerialized]
        private Stopwatch timer;

        /// <summary>Initializes a new instance of the <see cref="RunExternal"/> class.</summary>
        /// <param name="simulation">The simulation to clone and run.</param>
        /// <param name="engine">The engine</param>
        /// <param name="doClone">Clone the simulation before running?</param>
        public RunSimulation(Simulation simulation, ISimulationEngine engine, bool doClone)
        {
            simulationToRun = simulation;
            cloneSimulationBeforeRun = doClone;
            simulationEngine = engine;
            fileName = simulationEngine.FileName;
        }

        /// <summary>
        /// Set the parent of the simulation. Sometimes the parent of the sim can be an
        /// instance of Simulations. When this is deserialised to pass through socket,
        /// this is not what we want.
        /// </summary>
        /// <returns>The former parent of the simulation before its reset</returns>
        public IModel SetParentOfSimulation(IModel parent)
        {
            IModel formerParent = simulationToRun.Parent;
            simulationToRun.Parent = parent;
            return formerParent;
        }

        /// <summary>Called to start the job.</summary>
        /// <param name="jobManager">The job manager running this job.</param>
        /// <param name="workerThread">The thread this job is running on.</param>
        public void Run(JobManager jobManager, BackgroundWorker workerThread)
        {
            Console.WriteLine("File: " + Path.GetFileNameWithoutExtension(fileName) + 
                              ", Simulation " + simulationToRun.Name + " has commenced.");

            // Start timer to record how long it takes to run
            timer = new Stopwatch();
            timer.Start();

            Events events = null;
            Links links = null;
            try
            {
                // Clone simulation
                if (cloneSimulationBeforeRun)
                    simulationToRun = Apsim.Clone(simulationToRun) as Simulation;

                // Get an event and links service
                events = new Events(simulationToRun);

                // Setup the simulation
                if (simulationEngine != null)
                {
                    simulationEngine.MakeSubstitutions(simulationToRun);
                    links = simulationEngine.Links;
                }
                else
                    links = new Core.Links(Services);

                links.Resolve(simulationToRun);
                events.ConnectEvents();
                events.Publish("Loaded", null);

                // Send a commence event so the simulation runs
                object[] args = new object[] { null, new EventArgs() };
                events.Publish("Commencing", args);
                events.Publish("DoCommence", args);
            }
            catch (Exception err)
            {
                string errorMessage = "ERROR in file: " + fileName + "\r\n" +
                                      "Simulation name: " + simulationToRun.Name + "\r\n" +
                                      err.ToString();

                ISummary summary = Apsim.Find(simulationToRun, typeof(Summary)) as ISummary;
                if (summary != null)
                    summary.WriteMessage(simulationToRun, errorMessage);

                throw new Exception(errorMessage);
            }
            finally
            {
                // Cleanup the simulation
                if (events != null)
                    events.DisconnectEvents();
                links.Unresolve(simulationToRun);

                timer.Stop();
                Console.WriteLine("File: " + Path.GetFileNameWithoutExtension(fileName) +
                                  ", Simulation " + simulationToRun.Name + " complete. Time: " + timer.Elapsed.TotalSeconds.ToString("0.00 sec"));
                simulationEngine = null;
                simulationToRun = null;
            }
        }
    }
}
