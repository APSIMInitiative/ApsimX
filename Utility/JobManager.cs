using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Diagnostics;

namespace Utility
{
    /// <summary>A class for managing asynchronous running of jobs.</summary>
    [Serializable]
    public class JobManager
    {
        /// <summary>A runnable interface.</summary>
        public interface IRunnable
        {
            /// <summary>Gets a value indicating whether this instance is computationally time consuming.</summary>
            bool IsComputationallyTimeConsuming { get; }

            /// <summary>Gets a value indicating whether this job is completed. Set by JobManager.</summary>
            bool IsCompleted { get; set; }

            /// <summary>Gets the error message. Can be null if no error. Set by JobManager.</summary>
            string ErrorMessage { get; set; }

            /// <summary>Called to start the job. Can throw on error.</summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
            void Run(object sender, DoWorkEventArgs e);
        }

        /// <summary>The maximum number of processors used by this job manager.</summary>
        private int MaximumNumOfProcessors = 1;

        /// <summary>A job queue containing all jobs.</summary>
        private List<KeyValuePair<BackgroundWorker, IRunnable>> jobs = new List<KeyValuePair<BackgroundWorker, IRunnable>>();

        /// <summary>Main scheduler thread that goes through all jobs and sets them running.</summary>
        [NonSerialized]
        private BackgroundWorker schedulerThread = null;

        /// <summary>The cpu usage counter</summary>
        private PerformanceCounter cpuUsage;

        /// <summary>The previous CPU sample</summary>
        private CounterSample previousSample;

        /// <summary>
        /// Gets a value indicating whether there are more jobs to run.
        /// </summary>
        /// <value><c>true</c> if [more jobs to run]; otherwise, <c>false</c>.</value>
        private bool MoreJobsToRun
        {
            get
            {
                lock (this)
                {
                    return jobs.Count > 0;
                }
            }
        }

        /// <summary>Occurs when all jobs completed.</summary>
        public event EventHandler AllJobsCompleted;

        /// <summary>Initializes a new instance of the <see cref="JobManager"/> class.</summary>
        public JobManager()
        {
            string NumOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
            if (NumOfProcessorsString != null)
                MaximumNumOfProcessors = Convert.ToInt32(NumOfProcessorsString);
            MaximumNumOfProcessors = System.Math.Max(MaximumNumOfProcessors, 1);
            cpuUsage = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        }

        /// <summary>Add a job to the list of jobs that need running.</summary>
        /// <param name="job">The job to add to the queue</param>
        public void AddJob(IRunnable job)
        {
            lock (this) { jobs.Add(new KeyValuePair<BackgroundWorker, IRunnable>(null, job)); }
        }

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public void Start(bool waitUntilFinished)
        {
            schedulerThread = new BackgroundWorker();
            schedulerThread.WorkerSupportsCancellation = true;
            schedulerThread.WorkerReportsProgress = true;
            schedulerThread.DoWork += DoWork;
            schedulerThread.RunWorkerAsync();
            schedulerThread.RunWorkerCompleted += OnWorkerCompleted;

            if (waitUntilFinished)
            {
                while (schedulerThread.IsBusy)
                    Thread.Sleep(200);
            }
        }

        /// <summary>Stop all jobs currently running in the scheduler.</summary>
        public void Stop()
        {
            lock (this)
            {
                // Change status of jobs.
                foreach (KeyValuePair<BackgroundWorker, IRunnable> job in jobs)
                {
                    job.Value.IsCompleted = true;
                    if (job.Key.IsBusy)
                        job.Key.CancelAsync();
                }
            }

            if (schedulerThread != null)
                schedulerThread.CancelAsync();            
        }

        /// <summary>Called when [worker completed].</summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void OnWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (AllJobsCompleted != null)
                AllJobsCompleted.Invoke(this, new EventArgs());
        }

        /// <summary>
        /// Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            
            CounterSample previousSample = cpuUsage.NextSample();

            // Main worker thread for keeping jobs running
            while (!bw.CancellationPending && MoreJobsToRun)
            {
                int i = GetNextJobToRun();
                if (i != -1)
                {
                    lock (this) 
                    {
                        BackgroundWorker worker = new BackgroundWorker();
                        jobs[i] = new KeyValuePair<BackgroundWorker,IRunnable>(worker, jobs[i].Value);
                        worker.DoWork += jobs[i].Value.Run;
                        worker.RunWorkerCompleted += OnJobCompleted;
                        worker.WorkerSupportsCancellation = true;
                        worker.RunWorkerAsync(this);
                    }
                }
                Thread.Sleep(300);
            }
        }

        /// <summary>
        /// This event handler will be invoked, on the scheduler thread, everytime a job is completed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void OnJobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            
            lock (this)
            {
                int i = GetJob(bw);
                jobs[i].Value.IsCompleted = true;
                if (e.Error != null)
                    jobs[i].Value.ErrorMessage = e.Error.Message;
                jobs.RemoveAt(i);
            }
        }
        
        /// <summary>Gets a job</summary>
        /// <param name="bw">Background worker of job to find</param>
        /// <returns>The IRunnable job.</returns>
        private int GetJob(BackgroundWorker bw)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                if (jobs[i].Key == bw)
                    return i;
            }

            throw new Exception("Cannot find job.");
        }

        /// <summary>Return the index of next job to run or -1 if nothing to run.</summary>
        /// <returns>Index of job or -1.</returns>
        private int GetNextJobToRun()
        {
            lock (this)
            {
                int index = 0;
                int countRunning = 0;
                foreach (KeyValuePair<BackgroundWorker, IRunnable> job in jobs)
                {
                    if (countRunning == MaximumNumOfProcessors)
                    {
                        return -1;
                    }

                    // Is this job running?
                    if (job.Key == null)
                        return index;     // not running so return it to be run next.
                    else if (job.Value.IsComputationallyTimeConsuming)
                        countRunning++;   // is running.

                    index++;
                }
            }
            
            return -1;
        }
    }
}
