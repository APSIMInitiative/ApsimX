namespace APSIM.Shared.JobRunning
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

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
        public List<IRunnable> SimsRunning { get; private set; } = new List<IRunnable>();

        /// <summary>
        /// Lock object controlling access to SimsRunning list
        /// </summary>
        
        public readonly object runningLock = new object();

        /// <summary>The number of jobs that are currently running.</summary>
        protected int numberJobsRunning;

        /// <summary>A token for cancelling running of jobs</summary>
        protected CancellationTokenSource cancelToken;

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
        public void Add(IJobManager jobManager)
        {
            jobManagers.Add(jobManager);
        }

        /// <summary>Run the specified jobs</summary>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        public void Run(bool wait = false)
        {
            startTime = DateTime.Now;
            cancelToken = new CancellationTokenSource();

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
                SpinWait.SpinUntil(() => numberJobsRunning == 0);
            }
        }

        /// <summary>Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.        /// </summary>
        protected virtual void WorkerThread()
        {
            bool multiThreaded = numberOfProcessors > 1;
            try
            {
                foreach (var jobManager in jobManagers)
                {
                    var jobs = jobManager.GetJobs();
                    foreach (var job in jobs)
                    {
                        if (cancelToken.IsCancellationRequested)
                            return;

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
            }
            catch (Exception err)
            {
                ExceptionThrownByRunner = err;
            }

            // Wait for all jobs to complete and then signal completion.
            SpinWait.SpinUntil(() => numberJobsRunning == 0);
            ElapsedTime = DateTime.Now - startTime;
            InvokeAllCompleted();
            completed = true;
        }

        /// <summary>Run the specified job.</summary>
        /// <param name="job">The job to run.</param>
        /// <param name="jobManager">The job manager owning the job.</param>
        private void RunActualJob(IRunnable job, IJobManager jobManager)
        {
            try
            {
                if (!(job is JobRunnerSleepJob))
                    lock (runningLock)
                    {
                        SimsRunning.Add(job);
                    }

                var startTime = DateTime.Now;

                Exception error = null;
                try
                {
                    // Run job.
                    job.Run(cancelToken);
                }
                catch (Exception err)
                {
                    error = err;
                }

                // Signal to JobManager the job has finished.
                InvokeJobCompleted(job, jobManager, startTime, error);

                if (!(job is JobRunnerSleepJob))
                    lock (runningLock)
                    {
                        SimsRunning.Remove(job);
                    }
            }
            finally
            {
                Interlocked.Decrement(ref numberJobsRunning);
            }
        }

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