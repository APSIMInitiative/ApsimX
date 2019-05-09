namespace Models.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    /// <summary>
    /// An class for encapsulating a list of simulations that are ready
    /// to be run. An instance of this class can be used with a job runner.
    /// </summary>
    public class Runner : IJobManager
    {
        /// <summary>The model to use to search for simulations to run.</summary>
        private IModel relativeTo;

        /// <summary>How should the simulations be run?</summary>
        private RunTypeEnum runType;

        /// <summary>Run post simulation tools?</summary>
        bool runPostSimulationTools;

        /// <summary>Run tests?</summary>
        bool runTests;

        /// <summary>Wait until all simulations are complete?</summary>
        bool wait = true;

        /// <summary>Number of CPU processes to use. -1 indicates all processes.</summary>
        int numberOfProcessors;

        /// <summary>Top level model.</summary>
        private IModel rootModel;

        /// <summary>The descriptions of simulations that we are going to run.</summary>
        private List<RunningJob> pendingModels = new List<RunningJob>();

        /// <summary>Simulations that are currently running. Needed for percent complete.</summary>
        private List<RunningJob> runningModels = new List<RunningJob>();
        
        /// <summary>Name of file where the modelsToRun are stored.</summary>
        private string fileNameOfModels;

        /// <summary>The job runner being used.</summary>
        private IJobRunner jobRunner = null;

        /// <summary>The stop watch we can use to time all runs.</summary>
        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>The related storage model.</summary>
        private IDataStore storage;

        /// <summary>An enumerated type for specifying how a series of simulations are run.</summary>
        public enum RunTypeEnum
        {
            /// <summary>Run using a single thread - each job synchronously.</summary>
            SingleThreaded,

            /// <summary>Run using multiple cores - each job asynchronously.</summary>
            MultiThreaded,

            /// <summary>Run using multiple, separate processes - each job asynchronously.</summary>
            MultiProcess
        }

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="runType">How should the simulations be run?</param>
        /// <param name="runSimulations">Run simulations?</param>
        /// <param name="runPostSimulationTools">Run post simulation tools?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        /// <param name="wait">Wait until all simulations are complete?</param>
        /// <param name="numberOfProcessors">Number of CPU processes to use. -1 indicates all processes.</param>
        public Runner(IModel relativeTo,
                      RunTypeEnum runType = RunTypeEnum.MultiThreaded, 
                      bool runSimulations = true,
                      bool runPostSimulationTools = true,
                      bool runTests = true,
                      IEnumerable<string> simulationNamesToRun = null,
                      bool wait = true,
                      int numberOfProcessors = -1)
        {
            this.relativeTo = relativeTo;
            this.runType = runType;
            this.runPostSimulationTools = runPostSimulationTools;
            this.runTests = runTests;
            this.wait = wait;
            this.numberOfProcessors = numberOfProcessors;

            // Find the root model.
            rootModel = relativeTo;
            while (rootModel.Parent != null)
                rootModel = rootModel.Parent;

            if (rootModel is Simulations)
                fileNameOfModels = (rootModel as Simulations).FileName;
            else if (rootModel is Simulation)
                fileNameOfModels = (rootModel as Simulation).FileName;

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

        /// <summary>Invoked when a job is completed.</summary>
        public event EventHandler<JobCompletedArgs> JobCompleted;

        /// <summary>Invoked when all jobs are completed.</summary>
        public event EventHandler<AllJobsCompletedArgs> AllJobsCompleted;

        /// <summary>The number of simulations to run.</summary>
        public int TotalNumberOfSimulations { get; }

        /// <summary>The number of simulations completed running.</summary>
        public int NumberOfSimulationsCompleted { get; private set; }

        /// <summary>Return the time it took to perform the run.</summary>
        public TimeSpan RunTime { get { return stopwatch.Elapsed; } }

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

                return modelToRun.ToRunnable(storage, fileNameOfModels);
            }
            else
                return null;
        }

        /// <summary>
        /// Run all simulations.
        /// </summary>
        /// <returns>A list of exception or null if no exceptions thrown.</returns>
        public List<Exception> Run()
        {
            stopwatch.Start();
            ExceptionsThrown = null;

            if (pendingModels.Count > 0)
            {
                jobRunner = null;

                switch (runType)
                {
                    case RunTypeEnum.SingleThreaded:
                        jobRunner = new JobRunnerSync();
                        break;
                    case RunTypeEnum.MultiThreaded:
                        jobRunner = new JobRunnerAsync();
                        break;
                    case RunTypeEnum.MultiProcess:
                        jobRunner = new JobRunnerMultiProcess();
                        break;
                }

                // Subscribe to a couple of events to keep track of completed
                // jobs.
                jobRunner.JobCompleted += OnJobCompleted;
                jobRunner.AllJobsCompleted += OnAllSimulationsCompleted;

                // Run all simulations.
                jobRunner.Run(this, wait, numberOfProcessors);
            }
            else
                OnAllSimulationsCompleted(this, new AllCompletedArgs());

            return ExceptionsThrown;
        }

        /// <summary>
        /// Stop any running jobs.
        /// </summary>
        public void Stop()
        {
            if (jobRunner != null)
            {
                jobRunner.AllJobsCompleted -= OnAllSimulationsCompleted;
                jobRunner.JobCompleted -= OnJobCompleted;
                jobRunner?.Stop();
                jobRunner = null;
                stopwatch.Stop();

                storage?.Reader.Refresh();
            }
        }

        /// <summary>Calculate a percentage complete.</summary>
        public double PercentComplete()
        {
            if (TotalNumberOfSimulations == 0)
                return 0;

            // Ask each simulation for their percent complete.
            double fractionCompleteofRunningSimulations;
            lock (runningModels)
            {
                fractionCompleteofRunningSimulations = runningModels.Where(m => m.runnableJob is Simulation)
                                                                    .Select(m => m.runnableJob)
                                                                    .Cast<Simulation>()
                                                                    .Sum(sim => sim.FractionComplete);
            }
            return (fractionCompleteofRunningSimulations + NumberOfSimulationsCompleted) / TotalNumberOfSimulations * 100;
        }

        /// <summary>Needed for JobRunner.</summary>
        public void Completed() { }

        /// <summary>Determine the list of IRunnable jobs to run</summary>
        /// <param name="relativeTo">The model to use to search under.</param>
        private void FindIRunnablesToRun(IModel relativeTo)
        {
            foreach (IRunnable runnableJob in Apsim.FindAll(rootModel, typeof(IRunnable)))
            {
                if (runnableJob is Simulation)
                { }
                else
                    pendingModels.Add(new RunningJob() { runnableJob = runnableJob });
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
                    pendingModels.Add(new RunningJob() { descriptionOfSimulation = new SimulationDescription(relativeTo as Simulation) });
            }
            else if (relativeTo is ISimulationDescriptionGenerator)
            {
                foreach (var description in (relativeTo as ISimulationDescriptionGenerator).GenerateSimulationDescriptions())
                    if (simulationNamesToRun == null || simulationNamesToRun.Contains(description.Name))
                        pendingModels.Add(new RunningJob() { descriptionOfSimulation = description });
            }
            else if (relativeTo is Folder || relativeTo is Simulations)
            {
                // Get a list of all models we're going to run.
                foreach (var child in relativeTo.Children)
                    FindListOfSimulationsToRun(child, simulationNamesToRun);
            }
        }

        /// <summary>
        /// Handler for whan a simulation is completed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnJobCompleted(object sender, JobCompleteArgs e)
        {
            AddException(e.exceptionThrowByJob);

            var completedJob = runningModels.Find(m => m.runnableJob == e.job);
            
            lock (runningModels)
            {
                runningModels.Remove(completedJob);
                NumberOfSimulationsCompleted++;
            }

            JobCompleted?.Invoke(sender, 
                new JobCompletedArgs()
                {
                    Job = completedJob.runnableJob as IModel,
                    ExceptionThrowByJob = e.exceptionThrowByJob,
                    ElapsedTime = DateTime.Now - completedJob.startTime
                });
        }

        /// <summary>Handler for when all simulations have completed.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAllSimulationsCompleted(object sender, AllCompletedArgs e)
        {
            // Unsubscribe from our job completion events.
            if (jobRunner != null)
            {
                jobRunner.AllJobsCompleted -= OnAllSimulationsCompleted;
                jobRunner.JobCompleted -= OnJobCompleted;
            }

            if (runPostSimulationTools)
                RunPostSimulationTools();

            if (runTests)
                RunTests();

            Stop();

            stopwatch.Stop();
            AllJobsCompleted?.Invoke(this,
                new AllJobsCompletedArgs()
                {
                    AllExceptionsThrown = ExceptionsThrown,
                    ElapsedTime = stopwatch.Elapsed
                });
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

                JobCompleted?.Invoke(this,
                    new JobCompletedArgs()
                    {
                        Job = tool as IModel,
                        ExceptionThrowByJob = exception,
                        ElapsedTime = DateTime.Now - startTime
                    });

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

                storage?.Writer.Stop();
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
        private class RunningJob
        {
            public IRunnable runnableJob;
            public SimulationDescription descriptionOfSimulation;
            public DateTime startTime;

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
        }

        /// <summary>Arguments for JobCompleted event.</summary>
        public class JobCompletedArgs
        {
            /// <summary>The job that was completed.</summary>
            public IModel Job { get; set; }

            /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
            public Exception ExceptionThrowByJob { get; set; }

            /// <summary>Amount of time the job took to run.</summary>
            public TimeSpan ElapsedTime { get; set; }
        }

        /// <summary>Arguments for all jobs completed event.</summary>
        public class AllJobsCompletedArgs
        {
            /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
            public List<Exception> AllExceptionsThrown { get; set; }

            /// <summary>Amount of time all jobs took to run.</summary>
            public TimeSpan ElapsedTime { get; set; }
        }
    }
}
