namespace Models.Core.Run
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// An class for encapsulating a list of simulations that are ready
    /// to be run. An instance of this class can be used with a job runner.
    /// </summary>
    public class Runner
    {
        /// <summary>The descriptions of simulations that we are going to run.</summary>
        private List<IJobManager> jobs = new List<IJobManager>();
        
        /// <summary>The job runner being used.</summary>
        private JobRunner jobRunner = null;

        /// <summary>The stop watch we can use to time all runs.</summary>
        private DateTime startTime;

        /// <summary>How should the simulations be run?</summary>
        private RunTypeEnum runType;

        /// <summary>Wait until all simulations are complete?</summary>
        private bool wait = true;

        /// <summary>Number of CPU processes to use. -1 indicates all processes.</summary>
        private int numberOfProcessors = -1;

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

        /// <summary>
        /// Gets the aggregate progress of all jobs as a real number in range [0, 1].
        /// </summary>
        public double Progress
        {
            get
            {
                if (jobRunner == null || jobs == null)
                    return 0;

                int numJobs = jobs.Select(j => j.NumJobs).Sum();
                if (numJobs == 0)
                    return 0;

                return (jobRunner.NumJobsCompleted + jobRunner.SimsRunning.Sum(j => j.Progress)) / numJobs;
            }
        }

        /// <summary>
        /// Current status of the running jobs.
        /// </summary>
        public string Status
        {
            get
            {
                if (jobRunner == null || jobs == null)
                    return null;

                int numComplete = jobRunner.NumJobsCompleted;
                int numJobs = jobs.Select(j => j.NumJobs).Sum();

                // If progress is at 100% (ie all jobs have finished running), and the job manager supports
                // status reporting, allow the job manager to provide a status message. This lets the user
                // know why the job is still running even though progress is at 100%.
                if (MathUtilities.FloatsAreEqual(Progress, 1) && jobs.Count == 1 && jobs[0] is IReportsStatus jobManager && !string.IsNullOrEmpty(jobManager.Status))
                    return jobManager.Status;

                // If there's only one job to be run, and that job is specifically designed
                // to provide status reports, return that job's status message.
                if (numJobs == 1 && jobRunner.SimsRunning.Count == 1 && jobRunner.SimsRunning[0] is IReportsStatus statusReporter && !string.IsNullOrEmpty(statusReporter.Status))
                    return statusReporter.Status;

                // Otherwise, return the generic "x of y completed" message.
                return $"{numComplete} of {numJobs} completed";
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="runSimulations">Run simulations?</param>
        /// <param name="runPostSimulationTools">Run post simulation tools?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        /// <param name="runType">How should the simulations be run?</param>
        /// <param name="wait">Wait until all simulations are complete?</param>
        /// <param name="numberOfProcessors">Number of CPU processes to use. -1 indicates all processes.</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public Runner(IEnumerable<IModel> relativeTo,
                      bool runSimulations = true,
                      bool runPostSimulationTools = true,
                      bool runTests = true,
                      IEnumerable<string> simulationNamesToRun = null,
                      RunTypeEnum runType = RunTypeEnum.MultiThreaded,
                      bool wait = true,
                      int numberOfProcessors = -1,
                      string simulationNamePatternMatch = null)
        {
            this.runType = runType;
            this.wait = wait;
            this.numberOfProcessors = numberOfProcessors;

            foreach (IModel model in relativeTo)
            {
                var simulationGroup = new SimulationGroup(model, runSimulations, runPostSimulationTools, runTests, simulationNamesToRun, simulationNamePatternMatch);
                simulationGroup.Completed += OnSimulationGroupCompleted;
                jobs.Add(simulationGroup);
            }
        }

        /// <summary>Constructor</summary>
        /// <param name="relativeTo">The model to use to search for simulations to run.</param>
        /// <param name="runSimulations">Run simulations?</param>
        /// <param name="runPostSimulationTools">Run post simulation tools?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="simulationNamesToRun">Only run these simulations.</param>
        /// <param name="runType">How should the simulations be run?</param>
        /// <param name="wait">Wait until all simulations are complete?</param>
        /// <param name="numberOfProcessors">Number of CPU processes to use. -1 indicates all processes.</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public Runner(IModel relativeTo,
                      bool runSimulations = true,
                      bool runPostSimulationTools = true,
                      bool runTests = true,
                      IEnumerable<string> simulationNamesToRun = null,
                      RunTypeEnum runType = RunTypeEnum.MultiThreaded,
                      bool wait = true,
                      int numberOfProcessors = -1,
                      string simulationNamePatternMatch = null)
        {
            this.runType = runType;
            this.wait = wait;
            this.numberOfProcessors = numberOfProcessors;

            var simulationGroup = new SimulationGroup(relativeTo, runSimulations, runPostSimulationTools, runTests, simulationNamesToRun, simulationNamePatternMatch);
            simulationGroup.Completed += OnSimulationGroupCompleted;
            jobs.Add(simulationGroup);
        }

        /// <summary>Constructor</summary>
        /// <param name="pathAndFileSpec">Path and file specification for finding files.</param>
        /// <param name="recurse">Recurse into child folder?</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="runType">How should the simulations be run?</param>
        /// <param name="wait">Wait until all simulations are complete?</param>
        /// <param name="numberOfProcessors">Number of CPU processes to use. -1 indicates all processes.</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public Runner(string pathAndFileSpec,
                      bool recurse = true,
                      bool runTests = true,
                      RunTypeEnum runType = RunTypeEnum.MultiThreaded,
                      bool wait = true,
                      int numberOfProcessors = -1,
                      string simulationNamePatternMatch = null) : this(DirectoryUtilities.FindFiles(pathAndFileSpec, recurse),
                                                                       runTests,
                                                                       runType,
                                                                       wait,
                                                                       numberOfProcessors,
                                                                       simulationNamePatternMatch)
        {
        }

        /// <summary>Constructor</summary>
        /// <param name="files">Files to be run.</param>
        /// <param name="runTests">Run tests?</param>
        /// <param name="runType">How should the simulations be run?</param>
        /// <param name="wait">Wait until all simulations are complete?</param>
        /// <param name="numberOfProcessors">Number of CPU processes to use. -1 indicates all processes.</param>
        /// <param name="simulationNamePatternMatch">A regular expression used to match simulation names to run.</param>
        public Runner(string[] files,
                      bool runTests = true,
                      RunTypeEnum runType = RunTypeEnum.MultiThreaded,
                      bool wait = true,
                      int numberOfProcessors = -1,
                      string simulationNamePatternMatch = null)
        {
            this.runType = runType;
            this.wait = wait;
            this.numberOfProcessors = numberOfProcessors;

            foreach (string fileName in files)
            {
                var simulationGroup = new SimulationGroup(fileName, runTests, simulationNamePatternMatch);
                simulationGroup.Completed += OnSimulationGroupCompleted;
                jobs.Add(simulationGroup);
            }
        }

        /// <summary>Invoked every time a job has completed.</summary>
        public event EventHandler<JobCompleteArguments> SimulationCompleted;

        /// <summary>Invoked every time a job has completed.</summary>
        public event EventHandler<EventArgs> SimulationGroupCompleted;

        /// <summary>Invoked when all jobs are completed.</summary>
        public event EventHandler<AllJobsCompletedArgs> AllSimulationsCompleted;

        /// <summary>A list of exceptions thrown during simulation runs. Will be null when no exceptions found.</summary>
        public List<Exception> ExceptionsThrown { get; private set; }

        /// <summary>The time the run took.</summary>
        public TimeSpan ElapsedTime { get; private set; }

        /// <summary>An enumerator for simulations in the collection.</summary>
        public IEnumerable<Simulation> Simulations()
        {
            foreach (var manager in jobs)
            {
                foreach (var job in manager.GetJobs())
                {
                    if (job is SimulationDescription)
                        yield return (job as SimulationDescription).ToSimulation();
                }
            }
        }

        /// <summary>
        /// Run all simulations.
        /// </summary>
        /// <returns>A list of exception or null if no exceptions thrown.</returns>
        public List<Exception> Run()
        {
            startTime = DateTime.Now;

            if (jobs.Count > 0)
            {
                jobRunner = null;

                switch (runType)
                {
                    case RunTypeEnum.SingleThreaded:
                        jobRunner = new JobRunner(numProcessors:1);
                        break;
                    case RunTypeEnum.MultiThreaded:
                        jobRunner = new JobRunner(numberOfProcessors);
                        break;
                    case RunTypeEnum.MultiProcess:
                        jobRunner = new JobRunnerMultiProcess(numberOfProcessors);
                        break;
                }

                jobRunner.JobCompleted += OnJobCompleted;
                jobRunner.AllCompleted += OnAllCompleted;

                // Run all simulations.
                jobs.ForEach(j => jobRunner.Add(j));
                jobRunner.Run(wait);
            }
            else
                OnAllCompleted(this, new AllCompleteArguments());

            return ExceptionsThrown;
        }

        /// <summary>
        /// Stop any running jobs.
        /// </summary>
        public void Stop()
        {
            if (jobRunner != null)
            {
                jobRunner.AllCompleted -= OnAllCompleted;
                jobRunner?.Stop();
                jobRunner = null;
                ElapsedTime = DateTime.Now - startTime;
            }
        }

        /// <summary>
        /// Invoked when a job is completed.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OnJobCompleted(object sender, JobCompleteArguments e)
        {
            AddException(e.ExceptionThrowByJob);
            SimulationCompleted?.Invoke(this, e);
        }

        /// <summary>
        /// Invoked when an entire simulation group has completed running.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSimulationGroupCompleted(object sender, EventArgs e)
        {
            SimulationGroupCompleted?.Invoke(sender, e);
        }

        /// <summary>Handler for when all simulations have completed.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAllCompleted(object sender, AllCompleteArguments e)
        {
            Stop();

            AddException(e.ExceptionThrowByRunner);

            foreach (var job in jobs.OfType<SimulationGroup>())
                if (job.PrePostExceptionsThrown != null)
                    job.PrePostExceptionsThrown.ForEach(ex => AddException(ex));

            AllSimulationsCompleted?.Invoke(this,
                new AllJobsCompletedArgs()
                {
                    AllExceptionsThrown = ExceptionsThrown,
                    ElapsedTime = ElapsedTime
                });
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

        /// <summary>Arguments for all jobs completed event.</summary>
        public class AllJobsCompletedArgs
        {
            /// <summary>The exception thrown by the job. Can be null for no exception.</summary>
            public List<Exception> AllExceptionsThrown { get; set; }

            /// <summary>Amount of time all jobs took to run.</summary>
            public TimeSpan ElapsedTime { get; set; }
        }

        /// <summary>
        /// Dispose (close) the Datastore. Use with caution!
        /// This is intended to be used when running from the Models.exe command line
        /// When we're running in the GUI, we normally want to keep the Datastore open when the run completes.
        /// </summary>
        public void DisposeStorage()
        {
            foreach (var job in jobs)
                if (job is SimulationGroup)
                    (job as SimulationGroup).DisposeStorage();
        }
    }
}
