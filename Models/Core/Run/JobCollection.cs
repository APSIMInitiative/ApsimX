namespace Models.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models.Core.ApsimFile;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Encapsulates a collection of jobs. A job can be a simulation run or 
    /// a EXCEL input run.
    /// </summary>
    public class JobCollection : IJobManager
    {
        /// <summary>The model to use to search for simulations to run.</summary>
        private IModel relativeTo;

        /// <summary>Top level model.</summary>
        private IModel rootModel;

        /// <summary>Run simulations?</summary>
        private bool runSimulations;

        /// <summary>Run post simulation tools?</summary>
        private bool runPostSimulationTools;

        /// <summary>Run tests?</summary>
        private bool runTests;

        /// <summary>Has this instance been initialised?</summary>
        private bool initialised = false;

        /// <summary>Specific simulation names to run.</summary>
        private IEnumerable<string> simulationNamesToRun;

        /// <summary>The collection of jobs still to run.</summary>
        private List<Job> pendingModels = new List<Job>();

        /// <summary>Index into the job list.</summary>
        private int jobIndex;

        /// <summary>The related storage model.</summary>
        private IDataStore storage;

        /// <summary>Time when job collection first started.</summary>
        private DateTime startTime;

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
            this.runSimulations = runSimulations;
            this.runPostSimulationTools = runPostSimulationTools;
            this.runTests = runTests;
            this.simulationNamesToRun = simulationNamesToRun;

            Initialise();
        }

        /// <summary>Contstructor</summary>
        /// <param name="fileName">The name of the file to run.</param>
        /// <param name="runTests">Run tests?</param>
        public JobCollection(string fileName,
                             bool runTests = true)
        {
            this.FileName = fileName;
            this.runSimulations = true;
            this.runPostSimulationTools = true;
            this.runTests = runTests;
        }


        /// <summary>Name of file where the jobs came from.</summary>
        public string FileName { get; set; }

        /// <summary>Invoked every time a job has completed.</summary>
        public event EventHandler<JobHasCompletedArgs> JobHasCompleted;

        /// <summary>Invoked when the job collection has completed.</summary>
        public event EventHandler<JobCollectionHasCompletedArgs> JobCollectionHasCompleted;

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
            if (!initialised)
                lock (this)
                    if (!initialised)
                        Initialise();

            Job jobToReturn = null;
            lock (this)
            {
                if (jobIndex < pendingModels.Count)
                {
                    jobToReturn = pendingModels[jobIndex];
                    jobIndex++;
                }
            }

            if (jobToReturn == null)
                return null;
            else
                return jobToReturn.ToRunnable(storage, FileName);
        }

        /// <summary>Job has completed.</summary>
        void IJobManager.JobCompleted(JobCompleteArgs e)
        {
            var completedJob = pendingModels.Find(m => m.Equals(e.job));
            if (completedJob != null)
            {
                if (e.exceptionThrowByJob != null)
                    AddException(e.exceptionThrowByJob);
                lock (this)
                {
                        NumberOfSimulationsCompleted++;
                }

                JobHasCompleted?.Invoke(this, new JobHasCompletedArgs()
                {
                    Job = e.job as IModel,
                    ElapsedTime = completedJob.ElapsedTime,
                    ExceptionThrown = e.exceptionThrowByJob
                });
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

            JobCollectionHasCompleted?.Invoke(this,
                new JobCollectionHasCompletedArgs()
                {
                    JobCollection = this,
                    ElapsedTime = startTime - DateTime.Now,
                    ExceptionsThrown = ExceptionsThrown
                });
        }

        /// <summary>Initialise the instance.</summary>
        private void Initialise()
        {
            initialised = true;
            startTime = DateTime.Now;

            if (relativeTo == null)
            {
                if (!File.Exists(FileName))
                    throw new Exception("Cannot find file: " + FileName);
                List<Exception> exceptions;
                try
                {
                    relativeTo = FileFormat.ReadFromFile<Simulations>(FileName, out exceptions);
                }
                catch (Exception readException)
                {
                    exceptions = new List<Exception>() { readException };
                }
                Exception err = null;
                if (exceptions != null && exceptions.Count > 0)
                {
                    ExceptionsThrown = new List<Exception>();
                    ExceptionsThrown.AddRange(exceptions);
                    err = exceptions[0];

                    //JobHasCompleted?.Invoke(this, new JobHasCompletedArgs()
                    //{
                    //    Job = null,
                    //    ElapsedTime = new TimeSpan(),
                    //    ExceptionThrown = err
                    //});
                }
            }
            if (relativeTo != null)
            {
                // Find the root model.
                rootModel = relativeTo;
                while (rootModel.Parent != null)
                    rootModel = rootModel.Parent;

                if (rootModel is Simulations)
                    FileName = (rootModel as Simulations).FileName;
                else if (rootModel is Simulation)
                    FileName = (rootModel as Simulation).FileName;

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
            storage?.Reader.Refresh();

            // Call all post simulation tools.
            object[] args = new object[] { this, new EventArgs() };
            foreach (IPostSimulationTool tool in Apsim.ChildrenRecursively(rootModel, typeof(IPostSimulationTool)))
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

                JobHasCompleted?.Invoke(this,
                    new JobHasCompletedArgs()
                    {
                        Job = tool as IModel,
                        ExceptionThrown = exception,
                        ElapsedTime = DateTime.Now - startTime
                    });

            }
        }

        /// <summary>Run all tests.</summary>
        private void RunTests()
        {
            storage?.Writer.WaitForIdle();
            storage?.Reader.Refresh();

            foreach (ITest test in Apsim.ChildrenRecursively(rootModel, typeof(ITest)))
            {
                DateTime startTime = DateTime.Now;

                Links.Resolve(test as IModel, rootModel);

                // If we run into problems, we will want to include the name of the test in the 
                // exception's message. However, tests may be manager scripts, which always have
                // a name of 'Script'. Therefore, if the test's parent is a Manager, we use the
                // manager's name instead.
                string testName = test.Parent is Manager ? test.Parent.Name : test.Name;
                Exception exception = null;
                try
                {
                    test.Run();
                }
                catch (Exception err)
                {
                    exception = err;
                    AddException(new Exception("Encountered an error while running test " + testName, err));
                }

                JobHasCompleted?.Invoke(this,
                    new JobHasCompletedArgs()
                    {
                        Job = test as IModel,
                        ExceptionThrown = exception,
                        ElapsedTime = DateTime.Now - startTime
                    });
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
        /// Event arguments for a JobHasCompleted event.
        /// </summary>
        public class JobHasCompletedArgs : EventArgs
        {
            /// <summary>The job that was run.</summary>
            public IModel Job { get; set; }

            /// <summary>Exception thrown (if any). Can be null.</summary>
            public Exception ExceptionThrown { get; set; }

            /// <summary>The elapsed time.</summary>
            public TimeSpan ElapsedTime { get; set; }
        }

        /// <summary>
        /// Event arguments for a JobCollectionHasCompleted event.
        /// </summary>
        public class JobCollectionHasCompletedArgs : EventArgs
        {
            /// <summary>The job collection that was run.</summary>
            public JobCollection JobCollection { get; set; }

            /// <summary>Exception thrown (if any). Can be null.</summary>
            public List<Exception> ExceptionsThrown { get; set; }

            /// <summary>The elapsed time.</summary>
            public TimeSpan ElapsedTime { get; set; }
        }

        /// <summary>
        /// This class encapsulates a running job. It can either be an IRunnable
        /// e.g. like ExcelInput or a SimulationDescription.
        /// </summary>
        private class Job
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

                    // Give the file name to the simulation.
                    simulationToRun.FileName = fileName;

                    runnableJob = simulationToRun;
                }
                else
                {
                    (runnableJob as IModel).Parent = null;
                }

                // Give the datastore to the simulation so that links can be resolved.
                if (storage != null)
                {
                    var storageInSimulation = Apsim.Find(runnableJob as IModel, typeof(IDataStore)) as Model;
                    if (storageInSimulation != null)
                        Apsim.Delete(storageInSimulation);
                    (runnableJob as IModel).Children.Add(storage as Model);
                }

                startTime = DateTime.Now;
                return runnableJob as IRunnable;
            }

            /// <summary>Return true if this job is the same job as the one specified.</summary>
            /// <param name="compareTo">Job to compare this job to.</param>
            public bool Equals(IRunnable compareTo)
            {
                return runnableJob == compareTo;
            }

            /// <summary>Gets the time the job took to run.</summary>
            public TimeSpan ElapsedTime { get { return DateTime.Now - startTime; } }
        }

    }
}