using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Xml.Serialization;
using System.Runtime.Serialization;

namespace Utility
{
    /// <summary>A class for managing asynchronous running of jobs.</summary>
    [Serializable]
    public class JobManager
    {
        /// <summary>The posible status' of a job.</summary>
        public enum StatusEnum
        {
            /// <summary>Job is queued</summary>
            Queued,

            /// <summary>Job is running</summary>
            Running,

            /// <summary>Job has completed</summary>
            Completed,
        }

        /// <summary>A runnable interface.</summary>
        public interface IRunnable
        {
            /// <summary>Called to start the job.</summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
            void Run(object sender, DoWorkEventArgs e);
        }

        /// <summary>A small class to tie a job to a background worker thread</summary>
        private class JobBackgroundWorker : BackgroundWorker
        {
            /// <summary>Gets or sets the job.</summary>
            public Job Job { get; set; }

            /// <summary>Initializes a new instance of the <see cref="JobBackgroundWorker"/> class.</summary>
            /// <param name="job">The job</param>
            public JobBackgroundWorker(Job job)
            {
                this.Job = job;
            }
        }

        /// <summary>A structure for holding information about a completed job</summary>
        [Serializable]
        private class Job
        {
            /// <summary>Initializes a new instance of the <see cref="Job"/> class.</summary>
            public Job()
            {
                Status = StatusEnum.Queued;
            }

            /// <summary>The runnable task</summary>
            public IRunnable RunnableTask { get; set; }

            /// <summary>The error message or null if no error.</summary>
            public string ErrorMessage { get; set; }

            /// <summary>Percentage complete</summary>
            public int PercentComplete { get; set; }

            /// <summary>Gets or sets the status.</summary>
            public StatusEnum Status { get; set; }
        }


        #region Class fields

        /// <summary>The maximum number of processors used by this job manager.</summary>
        private int MaximumNumOfProcessors = 1;

        /// <summary>A job queue containing all jobs.</summary>
        private List<Job> Jobs = new List<Job>();

        /// <summary>Main scheduler thread that goes through all jobs and sets them running.</summary>
        [NonSerialized]
        private BackgroundWorker SchedulerThread = null;

        /// <summary>Keep track of all threads created so that we can stop them if needed.</summary>
        [NonSerialized]
        private List<BackgroundWorker> Threads = new List<BackgroundWorker>();

        public event EventHandler AllJobsCompleted;
        #endregion

        /// <summary>Used by the binary deserialiser when running on a remote machine.</summary>
        /// <param name="context">The streaming context.</param>
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            Threads = new List<BackgroundWorker>();
        }

        /// <summary>Initializes a new instance of the <see cref="JobManager"/> class.</summary>
        public JobManager()
        {
            string NumOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
            if (NumOfProcessorsString != null)
                MaximumNumOfProcessors = Convert.ToInt32(NumOfProcessorsString);
            MaximumNumOfProcessors = System.Math.Max(MaximumNumOfProcessors, 1);
        }

        /// <summary>Return true if all jobs have finished executing.</summary>
        /// <value><c>true</c> if [all jobs finished]; otherwise, <c>false</c>.</value>
        public int CountOfJobsFinished { get { lock (this) { return Jobs.Count(j => j.Status == StatusEnum.Completed); } } }

        /// <summary>Return true if all jobs have finished executing.</summary>
        /// <value><c>true</c> if [all jobs finished]; otherwise, <c>false</c>.</value>
        public int CountOfJobsRunning { get { lock (this) { return Jobs.Count(j => j.Status == StatusEnum.Running); } } }

        /// <summary>Return number of jobs to caller.</summary>
        /// <value>The number of jobs.</value>
        public int CountOfJobs { get { return Jobs.Count; } }

        /// <summary>True when some jobs threw exceptions.</summary>
        /// <value><c>true</c> if [some had errors]; otherwise, <c>false</c>.</value>
        [XmlIgnore]
        public int CountOfJobsWithErrors { get { lock (this) { return Jobs.Count(j => j.ErrorMessage != null); } } }

        /// <summary>Add a job to the list of jobs that need running.</summary>
        /// <param name="job">The job.</param>
        public void AddJob(IRunnable job)
        {
            lock (this) { Jobs.Add(new Job() { RunnableTask = job } ); }
        }

        /// <summary>Remove a job from the list</summary>
        /// <param name="job">The index of the job to remove</param>
        public void RemoveJob(int jobIndex)
        {
            lock (this) { Jobs.Remove(GetJob(jobIndex)); }
        }

        /// <summary>Gets the job error message.</summary>
        /// <param name="jobIndex">Index of the job.</param>
        /// <returns>The error message or null if not error</returns>
        public string GetJobErrorMessage(int jobIndex) { return GetJob(jobIndex).ErrorMessage; }

        /// <summary>Gets the job error message.</summary>
        /// <param name="jobIndex">Index of the job.</param>
        /// <returns>The error message or null if not error</returns>
        public string GetJobName(int jobIndex) { return Utility.Reflection.Name(GetJob(jobIndex)); }

        /// <param name="jobIndex">Index of the job.</param>
        /// <returns>The error message or null if not error</returns>
        public StatusEnum GetJobStatus(int jobIndex) { return GetJob(jobIndex).Status; }

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public void Start(bool waitUntilFinished)
        {
            SchedulerThread = new BackgroundWorker();
            SchedulerThread.WorkerSupportsCancellation = true;
            SchedulerThread.WorkerReportsProgress = true;
            SchedulerThread.DoWork += DoWork;
            SchedulerThread.RunWorkerAsync();
            SchedulerThread.RunWorkerCompleted += OnWorkerCompleted;

            if (waitUntilFinished)
            {
                while (SchedulerThread.IsBusy)
                    Thread.Sleep(200);
            }
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
        /// Start the specified jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="jobs">The jobs.</param>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public void Start(IEnumerable<IRunnable> jobs, bool waitUntilFinished)
        {
            foreach (IRunnable job in jobs)
                Jobs.Add(new Job() { RunnableTask = job });
            Start(waitUntilFinished);
        }

        /// <summary>Stop all jobs currently running in the scheduler.</summary>
        public void Stop()
        {
            lock (this)
            {
                // Change status of jobs.
                foreach (Job job in Jobs)
                    job.Status = StatusEnum.Completed;

                // kill the threads.
                foreach (BackgroundWorker thread in Threads)
                    if (thread.IsBusy)
                        thread.CancelAsync();
            }

            if (SchedulerThread != null)
                SchedulerThread.CancelAsync();            
        }

        /// <summary>
        /// Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="DoWorkEventArgs"/> instance containing the event data.</param>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            
            // Main worker thread for keeping jobs running
            while (!bw.CancellationPending && CountOfJobsFinished < CountOfJobs)
            {
                Job jobToRun = GetNextJobToRun();
                if (jobToRun != null)
                {
                    lock (this) 
                    {
                        jobToRun.Status = StatusEnum.Running;
                        JobBackgroundWorker worker = new JobBackgroundWorker(jobToRun);
                        worker.DoWork += jobToRun.RunnableTask.Run;
                        worker.RunWorkerCompleted += OnJobCompleted;
                        worker.WorkerSupportsCancellation = true;
                        worker.RunWorkerAsync(this);
                        Threads.Add(worker);
                    }
                }
                else
                    Thread.Sleep(100);
            }
        }

        /// <summary>
        /// This event handler will be invoked, on the scheduler thread, everytime a job is completed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void OnJobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            JobBackgroundWorker bw = sender as JobBackgroundWorker;

            lock (this)
            {
                bw.Job.Status = StatusEnum.Completed;
                if (e.Error != null)
                    bw.Job.ErrorMessage = e.Error.Message;
            }
        }
        
        /// <summary>Gets a job</summary>
        /// <param name="jobIndex">Index of the job.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception">Cannot get job number:  + jobIndex + . Job doesn't exist</exception>
        private Job GetJob(int jobIndex)
        {
            if (jobIndex >= Jobs.Count)
                throw new Exception("Cannot get job number: " + jobIndex + ". Job doesn't exist");
            return Jobs[jobIndex];
        }

        /// <summary>Return the next job to run or null if nothing to run.</summary>
        /// <returns></returns>
        private Job GetNextJobToRun()
        {
            lock (this)
            {
                if (CountOfJobsRunning < MaximumNumOfProcessors)
                    return Jobs.FirstOrDefault(j => j.Status == StatusEnum.Queued);
                else
                    return null;
            }
        }
    }
}
