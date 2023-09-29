﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace APSIM.Shared.JobRunning
{
    /// <summary>
    /// The class encapsulates the ability to run multiple collections of IRunnable jobs.
    /// Multiple JobManager instances can be added, each managing a collection of jobs.
    /// </summary>
    public class JobRunner
    {
        /// <summary>A list of job managers to iterate through looking for jobs.</summary>
        protected List<IJobManager> jobManagers = new List<IJobManager>();

        /// <summary>The background scheduling task.</summary>
        private Task backgroundTask;

        /// <summary>The number of processors to use to run jobs.</summary>
        private int numberOfProcessors;

        /// <summary>The start time for the beginning of the run.</summary>
        protected DateTime startTime;

        /// <summary>Have all jobs completed running?</summary>
        protected bool completed;

        /// <summary>
        /// A list of jobs current running. 
        /// We keep track of this to allow us to query how much of each job has been completed
        /// </summary>
        /// <remarks>Using ImmutableList here for thread safety.</remarks>
        public ImmutableList<IRunnable> SimsRunning { get; private set; } = ImmutableList<IRunnable>.Empty;

        /// <summary>
        /// Lock object controlling access to SimsRunning list
        /// </summary>
        protected readonly object runningLock = new object();

        /// <summary>The number of jobs that are currently running.</summary>
        protected int numberJobsRunning;

        /// <summary>A token for cancelling running of jobs</summary>
        protected CancellationTokenSource cancelToken;

        /// <summary>The number of jobs which have finished running.</summary>
        public int NumJobsCompleted { get; protected set; }

        /// <summary>Constructor.</summary>
        /// <param name="numProcessors">Number of processors to use.</param>
        public JobRunner(int numProcessors = -1)
        {
            numberOfProcessors = numProcessors;
            if (numberOfProcessors == -1)
            {
                int number;
                string numOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                if (numOfProcessorsString != null && Int32.TryParse(numOfProcessorsString, out number))
                    numberOfProcessors = Math.Max(number, 1);
                else
                    numberOfProcessors = Math.Max(Environment.ProcessorCount - 1, 1);
            }
        }

        /// <summary>Event is invoked when a job is complete.</summary>
        public event EventHandler<JobCompleteArguments> JobCompleted;

        /// <summary>Event is invoked when all jobs are complete.</summary>
        public event EventHandler<AllCompleteArguments> AllCompleted;

        /// <summary>The exception (if any) thrown by the runner.</summary>
        public Exception ExceptionThrownByRunner { get; protected set; }

        /// <summary>The total time taken by the runner to run all jobs.</summary>
        public TimeSpan ElapsedTime { get; protected set; }

        /// <summary>Add a jobmanager to the collection of jobmanagers to run.</summary>
        /// <param name="jobManager">The job manager to add.</param>
        public virtual void Add(IJobManager jobManager)
        {
            jobManagers.Add(jobManager);
        }

        /// <summary>Run the specified jobs</summary>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        public void Run(bool wait = false)
        {
            startTime = DateTime.Now;
                cancelToken = new CancellationTokenSource();
            completed = false;

            if (numberOfProcessors == 1 && wait)
                WorkerThread();
            else
            {
                // Run all jobs on background thread
                backgroundTask = Task.Run(() => WorkerThread());

                if (wait)
                    SpinWait.SpinUntil(() => completed);
            }
        }

        /// <summary>Stop all jobs currently running. Wait until all stopped.</summary>
        public virtual void Stop()
        {
            if (numberJobsRunning > 0)
            {
                cancelToken.Cancel();
                foreach (var sim in SimsRunning)
                    sim.Cleanup(cancelToken);
                SpinWait.SpinUntil(() => numberJobsRunning == 0);
            }
        }

        /// <summary>Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.        /// </summary>
        protected virtual void WorkerThread()
        {
            try
            {
                bool multiThreaded = numberOfProcessors > 1;
                try
                {
                    foreach (var (job, jobManager) in GetJobs())
                    {
                        if (cancelToken.IsCancellationRequested)
                            break;

                        // Wait until we have a spare processor to run a job.
                        if (multiThreaded)
                            SpinWait.SpinUntil(() => numberJobsRunning <= numberOfProcessors);

                        // Run the job.
                        Interlocked.Increment(ref numberJobsRunning);

                        if (multiThreaded)
                            Task.Run(() => { RunActualJob(job, jobManager); });
                        else
                            RunActualJob(job, jobManager);
                    }
                }
                catch (Exception err)
                {
                    ExceptionThrownByRunner = err;
                }

                // Wait for all jobs to complete and then signal completion.
                SpinWait.SpinUntil(() => numberJobsRunning == 0);
                ElapsedTime = DateTime.Now - startTime;
                InvokeAllCompleted();
            }
            finally
            {
                completed = true;
            }
        }

        /// <summary>
        /// Get all jobs to be run.
        /// </summary>
        protected virtual IEnumerable<(IRunnable, IJobManager)> GetJobs()
        {
            foreach (IJobManager jobManager in jobManagers)
                foreach (IRunnable job in jobManager.GetJobs())
                    yield return (job, jobManager);
        }

        /// <summary>Run the specified job.</summary>
        /// <param name="job">The job to run.</param>
        /// <param name="jobManager">The job manager owning the job.</param>
        protected virtual void RunActualJob(IRunnable job, IJobManager jobManager)
        {
            try
            {
                if (!(job is JobRunnerSleepJob))
                    lock (runningLock)
                    {
                        SimsRunning = SimsRunning.Add(job);
                    }
                var startTime = DateTime.Now;

                Exception error = null;
                try
                {
                    // Run job.
                    Prepare(job);
                    Run(job);
                    Cleanup(job);
                }
                catch (Exception err)
                {
                    error = err;
                }

                if (!(job is JobRunnerSleepJob))
                {
                    // Signal to JobManager the job has finished.
                    if (jobManager.NotifyWhenJobComplete)
                        InvokeJobCompleted(job, jobManager, startTime, error);

                    lock (runningLock)
                    {
                        NumJobsCompleted++;
                        SimsRunning = SimsRunning.Remove(job);
                    }
                }
            }
            finally
            {
                Interlocked.Decrement(ref numberJobsRunning);
            }
        }

        /// <summary>
        /// Prepare a job.
        /// </summary>
        /// <param name="job">The job to be prepared.</param>
        protected virtual void Prepare(IRunnable job) => job.Prepare();

        /// <summary>
        /// Run a job.
        /// </summary>
        /// <param name="job">The job to be run.</param>
        protected virtual void Run(IRunnable job) => job.Run(cancelToken);

        /// <summary>
        /// Cleanup a job.
        /// </summary>
        /// <param name="job">The job to be cleaned up.</param>
        protected virtual void Cleanup(IRunnable job) => job.Cleanup(cancelToken);

        /// <summary>
        /// Invoke the job completed event.
        /// </summary>
        /// <param name="job"></param>
        /// <param name="jobManager"></param>
        /// <param name="startTime"></param>
        /// <param name="error"></param>
        protected void InvokeJobCompleted(IRunnable job, IJobManager jobManager, DateTime startTime, Exception error)
        {
            var finishTime = DateTime.Now;
            var arguments = new JobCompleteArguments()
            {
                Job = job,
                ExceptionThrowByJob = error,
                ElapsedTime = finishTime - startTime
            };

            try
            {
                jobManager.JobHasCompleted(arguments);
            }
            finally
            {
                JobCompleted?.Invoke(this, arguments);
            }
        }


        /// <summary>
        /// Invoke the all completed event.
        /// </summary>
        protected void InvokeAllCompleted()
        {
            AllCompleted?.Invoke(this,
                            new AllCompleteArguments()
                            {
                                ElapsedTime = ElapsedTime,
                                ExceptionThrowByRunner = ExceptionThrownByRunner
                            });
        }

    }
}