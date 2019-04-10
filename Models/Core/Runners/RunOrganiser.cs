namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using Models.Core.Run;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.IO;
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

        /// <summary>A delegate that gets called to indicate progress during an operation.</summary>
        /// <param name="percent">Percentage compete.</param>
        public delegate void OnProgress(int percent);

        /// <summary>Simulation names being run</summary>
        public int NumSimulationNamesBeingRun { get; private set; }

        /// <summary>A list of simulation names to run.</summary>
        public List<string> SimulationNamesToRun { get; set; }

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

                Runner.SimulationEnumerator enumerator= new Runner.SimulationEnumerator(modelSelectedByUser, SimulationNamesToRun);
                simulationEnumerator = enumerator;
                NumSimulationNamesBeingRun = enumerator.NumSimulationsBeingRun;
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
                return new RunSimulation(simulations, simulationEnumerator.Current);
            else
                return null;
        }

        /// <summary>Called by the job runner when all jobs completed</summary>
        public void Completed()
        {
            var storage = Apsim.Find(simulations, typeof(IDataStore)) as IDataStore;

            storage.Writer.WaitForIdle();

            RunPostSimulationTools(simulations, storage);

            storage.Writer.WaitForIdle();

            // Optionally run the tests
            if (runTests)
            {
                foreach (ITest test in Apsim.ChildrenRecursively(simulations, typeof(ITest)))
                {
                    simulations.Links.Resolve(test);

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
            simulationEnumerator = null;
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


        /// <summary>
        /// Generates .apsimx files for each child model under a given model.
        /// Returns false if errors were encountered, or true otherwise.
        /// </summary>
        /// <param name="path">
        /// Path which the files will be saved to. 
        /// If null, the user will be prompted to choose a directory.
        /// </param>
        /// <param name="progressCallBack">Invoked when the method needs to indicate progress.</param>
        /// <returns>null for success or a list of exceptions.</returns>
        public List<Exception> GenerateApsimXFiles(string path, OnProgress progressCallBack)
        {
            IEnumerable<ISimulationDescriptionGenerator> children;
            if (modelSelectedByUser is ISimulationDescriptionGenerator)
                children = new List<IModel> { modelSelectedByUser }.Cast<ISimulationDescriptionGenerator>();
            else
                children = Apsim.ChildrenRecursively(modelSelectedByUser, typeof(ISimulationDescriptionGenerator)).Cast<ISimulationDescriptionGenerator>();

            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            List<Exception> errors = null;
            int i = 0;
            foreach (var sim in children)
            {
                progressCallBack?.Invoke(100 * i / children.Count());
                try
                {
                    foreach (var simDescription in sim.GenerateSimulationDescriptions())
                    {
                        string st = FileFormat.WriteToString(simDescription.ToSimulation(simulations));
                        File.WriteAllText(Path.Combine(path, simDescription.Name + ".apsimx"), st);
                    }
                }
                catch (Exception err)
                {
                    if (errors == null)
                        errors = new List<Exception>();
                    errors.Add(err);
                }

                i++;
            }
            return errors;
        }
    }
}
