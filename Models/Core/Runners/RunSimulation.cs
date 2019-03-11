namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Interfaces;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;

    /// <summary>
    /// This runnable class clones a simulation and then runs it.
    /// </summary>
    [Serializable]
    public class RunSimulation : IRunnable, IComputationalyTimeConsuming
    {
        private IDataStore storage = null;

        /// <summary>The arguments for a commence event.</summary>
        public class CommenceArgs
        {
            /// <summary>The token to check for a job cancellation</summary>
            public CancellationTokenSource CancelToken;
        }

        /// <summary>The simulation to run.</summary>
        public Simulation simulationToRun { get; private set; }

        /// <summary>The .apsimx filename where this simulation resides</summary>
        private string fileName;

        /// <summary>The engine</summary>
        [NonSerialized]
        [Link]
        private ISimulationEngine simulationEngine;

        /// <summary>The simulation to run.</summary>
        public bool cloneSimulationBeforeRun;

        /// <summary>An array of services that can be used to resolve links in the simulation</summary>
        public object[] Services { get; set; }

        /// <summary>Gets the data store for this simulation.</summary>
        public IDataStore DataStore
        {
            get
            {
                if (storage == null)
                    storage = Apsim.Find(simulationEngine as IModel, typeof(DataStore)) as IDataStore;
                return storage;
            }
        }

        /// <summary>A timer to record how long it takes to run</summary>
        [NonSerialized]
        private Stopwatch timer;

        /// <summary>Constructor</summary>
        /// <param name="simEngine">Simulation engine</param>
        /// <param name="simulation">The simulation to clone and run.</param>
        /// <param name="doClone">Clone the simulation before running?</param>
        public RunSimulation(ISimulationEngine simEngine, Simulation simulation, bool doClone)
        {
            simulationToRun = simulation;
            cloneSimulationBeforeRun = doClone;
            simulationEngine = simEngine;
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
        /// <param name="cancelToken">The token to check if job has been cancelled</param>
        public void Run(CancellationTokenSource cancelToken)
        {
            if (simulationEngine != null)
            {
                fileName = simulationEngine.FileName;
                Console.Write("File: " + Path.GetFileNameWithoutExtension(fileName) + ", ");
            }
            Console.WriteLine("Simulation " + simulationToRun.Name + " has commenced.");

            // Start timer to record how long it takes to run
            timer = new Stopwatch();
            timer.Start();

            Events events = null;
            Links links = null;
            try
            {
                // Clone simulation
                if (cloneSimulationBeforeRun)
                {
                    simulationToRun = Apsim.Clone(simulationToRun) as Simulation;
                    simulationEngine.MakeSubsAndLoad(simulationToRun);
                }
                else
                    events = new Events(simulationToRun);

                // Remove disabled models from simulation
                foreach (IModel model in Apsim.ChildrenRecursively(simulationToRun))
                {
                    if (!model.Enabled)
                        model.Parent.Children.Remove(model as Model);
                }

                // Get an event and links service
                if (simulationEngine != null)
                    links = simulationEngine.Links;
                else
                    links = new Core.Links(Services);

                // Resolve links and events.
                links.Resolve(simulationToRun, allLinks:true);
                events.ConnectEvents();

                simulationToRun.ClearCaches();

                // Send a commence event so the simulation runs
                object[] args = new object[] { null, new EventArgs() };
                object[] commenceArgs = new object[] { null, new CommenceArgs() { CancelToken = cancelToken } };
                events.Publish("Commencing", args);
                events.Publish("DoCommence", commenceArgs);
            }
            catch (Exception err)
            {
                string errorMessage = "ERROR in file: " + fileName + "\r\n" +
                                      "Simulation name: " + simulationToRun.Name + "\r\n";
                if (err.InnerException == null)
                    errorMessage += err.Message;
                else
                    errorMessage += err.InnerException.Message;
                
                ISummary summary = Apsim.Find(simulationToRun, typeof(Summary)) as ISummary;
                if (summary != null)
                    summary.WriteError(simulationToRun, errorMessage);
                
                throw new Exception(errorMessage, err);
            }
            finally
            {
                events.Publish("Completed", new object[] { null, new EventArgs() });

                // Cleanup the simulation
                if (events != null)
                    events.DisconnectEvents();
                links.Unresolve(simulationToRun, allLinks:true);

                timer.Stop();
                Console.WriteLine("File: " + Path.GetFileNameWithoutExtension(fileName) +
                                  ", Simulation " + simulationToRun.Name + " complete. Time: " + timer.Elapsed.TotalSeconds.ToString("0.00 sec"));
                simulationEngine = null;
                simulationToRun = null;
            }
        }
    }
}
