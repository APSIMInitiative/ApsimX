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
    public class Runner
    {
        /// <summary>How should the simulations be run?</summary>
        private RunTypeEnum runType;

        /// <summary>Wait until all simulations are complete?</summary>
        bool wait = true;

        /// <summary>Number of CPU processes to use. -1 indicates all processes.</summary>
        int numberOfProcessors;

        /// <summary>The descriptions of simulations that we are going to run.</summary>
        private List<JobCollection> jobs = new List<JobCollection>();

        private JobManagerCollection jobCollection; 
        /// <summary>The job runner being used.</summary>
        private IJobRunner jobRunner = null;

        /// <summary>The stop watch we can use to time all runs.</summary>
        private Stopwatch stopwatch = new Stopwatch();

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
            this.runType = runType;
            this.wait = wait;
            this.numberOfProcessors = numberOfProcessors;

            var job = new JobCollection(relativeTo, runSimulations, runPostSimulationTools, runTests, simulationNamesToRun);
            jobs.Add(job);
            jobCollection = new JobManagerCollection(new List<IJobManager>() { job });
        }

        /// <summary>Invoked when all jobs are completed.</summary>
        public event EventHandler<AllJobsCompletedArgs> AllJobsCompleted;

        /// <summary>The number of simulations to run.</summary>
        public int TotalNumberOfSimulations { get { return jobs.Sum(j => j.TotalNumberOfSimulations); } }

        /// <summary>The number of simulations completed running.</summary>
        public int NumberOfSimulationsCompleted { get { return jobs.Sum(j => j.NumberOfSimulationsCompleted); } }

        /// <summary>A list of exceptions thrown during simulation runs. Will be null when no exceptions found.</summary>
        public List<Exception> ExceptionsThrown
        {
            get
            {
                List<Exception> exceptions = null;
                foreach (var job in jobs)
                {
                    if (job.ExceptionsThrown != null)
                    {
                        if (exceptions == null)
                            exceptions = new List<Exception>();
                        exceptions.AddRange(job.ExceptionsThrown);
                    }
                }
                return exceptions;
            }
        }

        /// <summary>Return the next job to run or null if nothing to run.</summary>
        /// <returns>Job to run or null if no more.</returns>
        public IRunnable GetNextJobToRun()
        {
            return jobCollection.GetNextJobToRun();
        }

        /// <summary>
        /// Run all simulations.
        /// </summary>
        /// <returns>A list of exception or null if no exceptions thrown.</returns>
        public List<Exception> Run()
        {
            stopwatch.Start();

            if (jobs.Count > 0)
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

                jobRunner.AllJobsCompleted += OnAllSimulationsCompleted;

                // Run all simulations.
                jobRunner.Run(jobCollection, wait, numberOfProcessors);
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
                jobRunner?.Stop();
                jobRunner = null;
                stopwatch.Stop();
            }
        }

        /// <summary>Calculate a percentage complete.</summary>
        public double PercentComplete()
        {
            if (TotalNumberOfSimulations == 0)
                return 0;
            else
                return 100.0 * NumberOfSimulationsCompleted / TotalNumberOfSimulations;
        }

        /// <summary>Handler for when all simulations have completed.</summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAllSimulationsCompleted(object sender, AllCompletedArgs e)
        {
            // Unsubscribe from our job completion events.
            if (jobRunner != null)
                jobRunner.AllJobsCompleted -= OnAllSimulationsCompleted;

            Stop();

            stopwatch.Stop();
            AllJobsCompleted?.Invoke(this,
                new AllJobsCompletedArgs()
                {
                    AllExceptionsThrown = ExceptionsThrown,
                    ElapsedTime = stopwatch.Elapsed
                });
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
