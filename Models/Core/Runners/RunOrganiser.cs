namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Models.Core.Run;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>A job manager that looks after running all simulations</summary>
    public class RunOrganiser : IJobManager
    {
        private Simulations simulations;
        private IModel modelSelectedByUser;
        private bool runTests;
        private IEnumerator<Simulation> simulationEnumerator;
        private List<IRunnable> toolsToRun = new List<IRunnable>();

        /// <summary>Simulation names being run</summary>
        public List<string> SimulationNamesBeingRun { get; private set; }

        /// <summary>
        /// Clocks of simulations that have begun running
        /// </summary>
        public List<IClock> SimClocks
        {
            get
            {
                if (simulationEnumerator as Runner.SimulationEnumerator != null)
                    return (simulationEnumerator as Runner.SimulationEnumerator).simClocks;
                else
                    return null;
            }
        }

        /// <summary>All known simulation names</summary>
        public List<string> AllSimulationNames
        {
            get
            {
                List<string> AllSimulationNames = new List<string>();
                List<ISimulationGenerator> allModels = Apsim.ChildrenRecursively(simulations, typeof(ISimulationGenerator)).Cast<ISimulationGenerator>().ToList();
                allModels.ForEach(model => AllSimulationNames.AddRange(model.GetSimulationNames(false)));
                return AllSimulationNames;
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="model">The model to run.</param>
        /// <param name="simulations">simulations object.</param>
        /// <param name="runTests">Run the test nodes?</param>
        public RunOrganiser(Simulations simulations, IModel model, bool runTests)
        {
            this.simulations = simulations;
            this.modelSelectedByUser = model;
            this.runTests = runTests;
        }

        /// <summary>Return the index of next job to run or -1 if nothing to run.</summary>
        /// <returns>Job to run or null if no more jobs to run</returns>
        public IRunnable GetNextJobToRun()
        {
            // First time through there. Get a list of things to run.
            if (simulationEnumerator == null)
            {
                // Find runnable objects that aren't simulations e.g. ExcelInput
                toolsToRun = Apsim.FindAll(simulations, typeof(IRunnable)).Cast<IRunnable>().ToList();

                // Send event telling all models that we're about to begin running.
                Events events = new Events(simulations);
                events.Publish("BeginRun", null);

                Runner.SimulationEnumerator enumerator= new Runner.SimulationEnumerator(modelSelectedByUser);
                simulationEnumerator = enumerator;
                SimulationNamesBeingRun = enumerator.SimulationNamesBeingRun;

                // Send event telling all models that we're about to begin running.
                Dictionary<string, string> simAndFolderNames = new Dictionary<string, string>();
                foreach (ISimulationGenerator simulation in Apsim.ChildrenRecursively(simulations, typeof(ISimulationGenerator)).Where(m => m.Enabled).Cast<ISimulationGenerator>())
                {
                    string folderName = Apsim.Parent(simulation as IModel, typeof(Folder)).Name;
                    foreach (string simulationName in simulation.GetSimulationNames())
                    {
                        if (simAndFolderNames.ContainsKey(simulationName))
                            throw new Exception(string.Format("Duplicate simulation names found: {0} in simulation {1}", simulationName, (simulation as IModel).Name));
                        simAndFolderNames.Add(simulationName, folderName);
                    }
                }
            }

            // Are there any runnable things?
            if (toolsToRun.Count > 0)
            {
                var toolToRun = toolsToRun[0];
                toolsToRun.RemoveAt(0);
                return toolToRun;
            }

            // If we didn't find anything to run then return null to tell job runner to exit.
            if (simulationEnumerator.MoveNext())
                return new RunSimulation(simulations, simulationEnumerator.Current, false);
            else
                return null;
        }

        /// <summary>Called by the job runner when all jobs completed</summary>
        public void Completed()
        {
            var storage = Apsim.Find(simulations, typeof(IDataStore)) as IDataStore;

            storage.Writer.WaitForIdle();

            RunPostSimulationTools(simulations, storage);

            // Optionally run the tests
            if (runTests)
            {
                foreach (ITest test in Apsim.ChildrenRecursively(simulations, typeof(ITest)))
                {
                    // If we run into problems, we will want to include the name of the test in the 
                    // exception's message. However, tests may be manager scripts, which always have
                    // a name of 'Script'. Therefore, if the test's parent is a Manager, we use the
                    // manager's name instead.
                    string testName = test.Parent is Manager ? test.Parent.Name : test.Name;
                    try
                    {
                        test.Run();
                    }
                    catch (Exception err)
                    {
                        throw new Exception("Encountered an error while running test " + testName, err);
                    }
                }
            }

            storage.Writer.Stop();
        }

        /// <summary>
        /// Run all post simulation tools.
        /// </summary>
        /// <param name="rootModel">The root model to look under for tools to run.</param>
        /// <param name="storage">The data store.</param>
        public static void RunPostSimulationTools(IModel rootModel, IDataStore storage)
        {
            storage.Writer.Stop();

            // Call all post simulation tools.
            foreach (IPostSimulationTool tool in Apsim.FindAll(rootModel, typeof(IPostSimulationTool)))
            {
                if ((tool as IModel).Enabled)
                {
                    tool.Run(storage);
                    storage.Writer.WaitForIdle();
                }
            }

            storage.Writer.Stop();
        }

    }
}
