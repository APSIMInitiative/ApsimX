namespace Models.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a collection of jobs.
    /// </summary>
    public class JobCollection : IJobManager
    {       
        /// <summary>The model to use to search for simulations to run.</summary>
        private IModel relativeTo;
        
        /// <summary>Top level model.</summary>
        private IModel rootModel;

        /// <summary>Run post simulation tools?</summary>
        bool runPostSimulationTools;

        /// <summary>Run tests?</summary>
        bool runTests;

        /// <summary>The collection of jobs still to run.</summary>
        private List<Job> pendingModels = new List<Job>();

        /// <summary>The collection of jobs that are currently running. Needed for percent complete.</summary>
        private List<Job> runningModels = new List<Job>();

        /// <summary>Name of file where the jobs came from.</summary>
        private string fileName;

        /// <summary>The related storage model.</summary>
        private IDataStore storage;

        /// <summary>Contstructor</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="runSimulations">Run simulations?</param>
        /// <param name="runPostSimulationTools">Run post simulation tools?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        public JobCollection(IModel relativeTo,
                             bool runSimulations = true,
                             bool runPostSimulationTools = true,
                             bool runTests = true,
                             IEnumerable<string> simulationNamesToRun = null)
        {
            this.relativeTo = relativeTo;
            this.runPostSimulationTools = runPostSimulationTools;
            this.runTests = runTests;

            // Find the root model.
            rootModel = relativeTo;
            while (rootModel.Parent != null)
                rootModel = rootModel.Parent;

            if (rootModel is Simulations)
                fileName = (rootModel as Simulations).FileName;
            else if (rootModel is Simulation)
                fileName = (rootModel as Simulation).FileName;

            if (runPostSimulationTools)
                FindIRunnablesToRun(relativeTo);
            if (runSimulations)
                FindListOfSimulationsToRun(relativeTo, simulationNamesToRun);
            TotalNumberOfSimulations = pendingModels.Count;

            // Find a storage model.
            storage = Apsim.Child(rootModel, typeof(IDataStore)) as IDataStore;

            // If this simulation was not created from deserialisation then we need
            // to parent all child models correctly and call OnCreated for each model.
            bool hasBeenDeserialised = relativeTo.Children.Count > 0 &&
                                       relativeTo.Children[0].Parent == relativeTo;
            if (!hasBeenDeserialised)
            {
                // Parent all models.
                Apsim.ParentAllChildren(relativeTo);

                // Call OnCreated in all models.
                Apsim.ChildrenRecursively(relativeTo).ForEach(m => m.OnCreated());
            }
        }

        /// <summary>The number of simulations to run.</summary>
        public int TotalNumberOfSimulations { get; private set; }

        /// <summary>The number of simulations completed running.</summary>
        public int NumberOfSimulationsCompleted { get; private set; }

        /// <summary>A list of exceptions thrown during simulation runs. Will be null when no exceptions found.</summary>
        public List<Exception> ExceptionsThrown { get; private set; }

        /// <summary>Return the next job to run or null if nothing to run.</summary>
        /// <returns>Job to run or null if no more.</returns>
        public IRunnable GetNextJobToRun()
        {
            if (pendingModels.Count > 0)
            {
                // Remove next model to run from our pending list.
                var modelToRun = pendingModels.First();
                pendingModels.Remove(modelToRun);

                // Add model to our running list.
                lock (runningModels)
                {
                    runningModels.Add(modelToRun);
                }

                return modelToRun.ToRunnable(storage, fileName);
            }
            else
                return null;
        }

        /// <summary>Job has completed.</summary>
        void IJobManager.JobCompleted(JobCompleteArgs e)
        {
            var completedJob = runningModels.Find(m => m.Equals(e.job));
            if (completedJob != null)
            {
                if (e.exceptionThrowByJob != null)
                    AddException(e.exceptionThrowByJob);
                lock (runningModels)
                {
                    runningModels.Remove(completedJob);
                    if (e.job is Simulation)
                        NumberOfSimulationsCompleted++;
                }
            }
        }

        /// <summary>Job has completed.</summary>
        void IJobManager.AllCompleted(AllCompletedArgs e)
        {
            if (runPostSimulationTools)
                RunPostSimulationTools();

            if (runTests)
                RunTests();

            storage?.Writer.Stop();
            storage?.Reader.Refresh();
        }

        /// <summary>Determine the list of IRunnable jobs to run</summary>
        /// <param name="relativeTo">The model to use to search under.</param>
        private void FindIRunnablesToRun(IModel relativeTo)
        {
            foreach (IRunnable runnableJob in Apsim.FindAll(rootModel, typeof(IRunnable)))
            {
                if (runnableJob is Simulation)
                { }
                else
                    pendingModels.Add(new Job(runnableJob));
            }
        }

        /// <summary>Determine the list of jobs to run</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        private void FindListOfSimulationsToRun(IModel relativeTo, IEnumerable<string> simulationNamesToRun)
        {
            if (relativeTo is Simulation)
            {
                if (simulationNamesToRun == null || simulationNamesToRun.Contains(relativeTo.Name))
                    pendingModels.Add(new Job(new SimulationDescription(relativeTo as Simulation)));
            }
            else if (relativeTo is ISimulationDescriptionGenerator)
            {
                foreach (var description in (relativeTo as ISimulationDescriptionGenerator).GenerateSimulationDescriptions())
                    if (simulationNamesToRun == null || simulationNamesToRun.Contains(description.Name))
                        pendingModels.Add(new Job(description));
            }
            else if (relativeTo is Folder || relativeTo is Simulations)
            {
                // Get a list of all models we're going to run.
                foreach (var child in relativeTo.Children)
                    FindListOfSimulationsToRun(child, simulationNamesToRun);
            }
        }

        /// <summary>Run all post simulation tools.</summary>
        private void RunPostSimulationTools()
        {
            storage?.Writer.WaitForIdle();

            // Call all post simulation tools.
            object[] args = new object[] { this, new EventArgs() };
            foreach (IPostSimulationTool tool in Apsim.FindAll(rootModel, typeof(IPostSimulationTool)))
            {
                DateTime startTime = DateTime.Now;
                Exception exception = null;
                try
                {
                    Links.Resolve(tool, rootModel);
                    if ((tool as IModel).Enabled)
                        tool.Run(storage);
                }
                catch (Exception err)
                {
                    exception = err;
                    AddException(err);
                }

                //JobCompleted?.Invoke(this,
                //    new JobCompletedArgs()
                //    {
                //        Job = tool as IModel,
                //        ExceptionThrowByJob = exception,
                //        ElapsedTime = DateTime.Now - startTime
                //    });

            }
        }

        /// <summary>Run all tests.</summary>
        private void RunTests()
        {
            try
            {
                storage?.Writer.WaitForIdle();

                foreach (ITest test in Apsim.ChildrenRecursively(rootModel, typeof(ITest)))
                {
                    Links.Resolve(test as IModel, rootModel);

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
                        AddException(new Exception("Encountered an error while running test " + testName, err));
                    }
                }
            }
            catch (Exception err)
            {
                AddException(err);
            }
        }

        /// <summary>
        /// Add an exception to our list of exceptions.
        /// </summary>
        /// <param name="err">The exception to add.</param>
        private void AddException(Exception err)
        {
            if (err != null)
            {
                if (ExceptionsThrown == null)
                    ExceptionsThrown = new List<Exception>();
                ExceptionsThrown.Add(err);
            }
        }

        /// <summary>
        /// This class encapsulates a running job. It can either be an IRunnable
        /// e.g. like ExcelInput or a SimulationDescription.
        /// </summary>
        public class Job
        {
            private IRunnable runnableJob;
            private SimulationDescription descriptionOfSimulation;
            private DateTime startTime;

            /// <summary>Constructor</summary>
            /// <param name="job">The job that will be run.</param>
            public Job(IRunnable job)
            {
                runnableJob = job;
            }

            /// <summary>Constructor</summary>
            /// <param name="description">A description of a simulation.</param>
            public Job(SimulationDescription description)
            {
                descriptionOfSimulation = description;
            }

            /// <summary>Convert the job to something that is runnable by a JobRunner.</summary>
            /// <param name="storage">The datastore. Can be null.</param>
            /// <param name="fileName">The filename where the job came from.</param>
            /// <returns></returns>
            public IRunnable ToRunnable(IDataStore storage, string fileName)
            {
                if (descriptionOfSimulation != null)
                {
                    Simulation simulationToRun = descriptionOfSimulation.ToSimulation();

                    // Give the datastore to the simulation so that links can be resolved.
                    if (storage != null)
                    {
                        var storageInSimulation = Apsim.Find(simulationToRun, typeof(IDataStore)) as Model;
                        if (storageInSimulation != null)
                            Apsim.Delete(storageInSimulation);
                        simulationToRun.Children.Add(storage as Model);
                    }

                    // Give the file name to the simulation.
                    simulationToRun.FileName = fileName;

                    runnableJob = simulationToRun;
                }

                startTime = DateTime.Now;
                return runnableJob;
            }

            /// <summary>Return true if this job is the same job as the one specified.</summary>
            /// <param name="compareTo">Job to compare this job to.</param>
            public bool Equals(IRunnable compareTo)
            {
                return runnableJob == compareTo;
            }
        }
    }
}