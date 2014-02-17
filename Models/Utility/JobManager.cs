using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Xml.Serialization;
namespace Utility
{
    /// <summary>
    /// A class for managing asynchronous running of jobs.
    /// </summary>
    public class JobManager
    {
        /// <summary>
        /// A runnable interface.
        /// </summary>
        public interface IRunnable
        {
            /// <summary>
            /// Called to start the job.
            /// </summary>
            void Run(object sender, DoWorkEventArgs e);
        }

        public class JobCompleteArgs : EventArgs
        {
            /// <summary>
            /// Name of the job that has finished.
            /// </summary>
            public string Name;

            /// <summary>
            /// The error message or null if no error.
            /// </summary>
            public string ErrorMessage;

            /// <summary>
            /// Percentage complete
            /// </summary>
            public int PercentComplete;
        }


        #region Class fields

        /// <summary>
        /// The maximum number of processors used by this job manager.
        /// </summary>
        private int MaximumNumOfProcessors = 1;

        /// <summary>
        /// A job queue containing all jobs.
        /// </summary>
        private List<IRunnable> Jobs = new List<IRunnable>();

        /// <summary>
        /// Main scheduler thread that goes through all jobs and sets them running.
        /// </summary>
        private BackgroundWorker SchedulerThread = null;

        /// <summary>
        /// Keep track of all threads created so that we can stop them if needed.
        /// </summary>
        private List<BackgroundWorker> Threads = new List<BackgroundWorker>();

        /// <summary>
        /// The number of jobs that completed.
        /// </summary>
        private int NumJobsCompleted = 0;

        /// <summary>
        /// The number of jobs running
        /// </summary>
        public int NumJobsRunning = 0;

        /// <summary>
        /// Index of next job to run.
        /// </summary>
        private int IndexOfNextJob = -1;

        /// <summary>
        /// A list of pending completions that we need to advertise through invokes
        /// to OnComplete event.
        /// </summary>
        private Queue<JobCompleteArgs> PendingCompletedJobs = new Queue<JobCompleteArgs>();


        #endregion






        /// <summary>
        /// Construtor
        /// </summary>
        public JobManager()
        {
            string NumOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
            if (NumOfProcessorsString != null)
                MaximumNumOfProcessors = Convert.ToInt32(NumOfProcessorsString);
            MaximumNumOfProcessors = System.Math.Max(MaximumNumOfProcessors, 1);
            SomeHadErrors = false;
        }

        /// <summary>
        /// This event is invoked when a job completes
        /// </summary>
        public event EventHandler<JobCompleteArgs> OnComplete;

        /// <summary>
        /// Return true if all jobs have finished executing.
        /// </summary>
        public bool AllJobsFinished
        {
            get
            {
                lock (this)
                {
                    return NumJobsCompleted == Jobs.Count &&
                           PendingCompletedJobs.Count == 0;
                }
            }
        }

        /// <summary>
        /// Return number of jobs to caller.
        /// </summary>
        public int NumberOfJobs { get { return Jobs.Count; } }

        /// <summary>
        /// True when some jobs threw exceptions.
        /// </summary>
        [XmlIgnore]
        public bool SomeHadErrors { get; private set; }

        /// <summary>
        /// Add a job to the list of jobs that need running.
        /// </summary>
        public void AddJob(IRunnable job)
        {
            lock (this) { Jobs.Add(job); }
        }

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        public void Start(bool waitUntilFinished)
        {
            SchedulerThread = new BackgroundWorker();
            SchedulerThread.WorkerSupportsCancellation = true;
            SchedulerThread.WorkerReportsProgress = true;
            SchedulerThread.DoWork += DoWork;
            SchedulerThread.ProgressChanged += OnProgress;
            SchedulerThread.RunWorkerAsync();

            if (waitUntilFinished)
            {
                while (SchedulerThread.IsBusy)
                    Thread.Sleep(200);
            }
        }

        /// <summary>
        /// Start the specified jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        public void Start(IEnumerable<IRunnable> jobs, bool waitUntilFinished)
        {
            Jobs.AddRange(jobs);
            Start(waitUntilFinished);
        }


        /// <summary>
        /// Stop all jobs currently running in the scheduler.
        /// </summary>
        public void Stop()
        {
            lock (this)
            {
                foreach (BackgroundWorker thread in Threads)
                    if (thread.IsBusy)
                        thread.CancelAsync();
            }

            SchedulerThread.CancelAsync();            
        }

        /// <summary>
        /// Main DoWork method for the scheduler thread. NB this does NOT run on the UI thread.
        /// </summary>
        private void DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            
            // Main worker thread for keeping jobs running
            while (!bw.CancellationPending && !AllJobsFinished)
            {
                IRunnable jobToRun = GetNextJob();
                if (jobToRun != null)
                {
                    lock (this) 
                    { 
                        NumJobsRunning++;
                        BackgroundWorker worker = new BackgroundWorker();
                    
                        worker.DoWork += jobToRun.Run;
                        worker.RunWorkerCompleted += OnJobCompleted;
                        worker.WorkerSupportsCancellation = true;
                        worker.WorkerReportsProgress = true;
                        worker.RunWorkerAsync(this);
                        Threads.Add(worker);
                    }
                }

                // See if there are any pending completes that we need to report.
                lock (this)
                {
                    while (PendingCompletedJobs.Count > 0)
                    {
                        JobCompleteArgs completeArgs = PendingCompletedJobs.Dequeue();
                        bw.ReportProgress(completeArgs.PercentComplete, completeArgs);
                    }
                }

                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Main scheduler thread is reporting progress. This will be called in the UI thread.
        /// </summary>
        private void OnProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (OnComplete != null)
                OnComplete(this, e.UserState as JobCompleteArgs);
        }


        /// <summary>
        /// This event handler will be invoked, on the scheduler thread, everytime a job is completed.
        /// </summary>
        private void OnJobCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;

            lock (this)
            {
                NumJobsRunning--;
                NumJobsCompleted++;
                if (OnComplete != null)
                {
                    JobCompleteArgs completeArgs = new JobCompleteArgs();
                    completeArgs.Name = Utility.Reflection.Name(e.Result);
                    if (e.Error != null)
                    {
                        completeArgs.ErrorMessage = e.Error.Message;
                        SomeHadErrors = true;
                    }
                    completeArgs.PercentComplete = Convert.ToInt32((NumJobsCompleted * 1.0) / Jobs.Count * 100.0);
                    PendingCompletedJobs.Enqueue(completeArgs);
                }
            }
        }

        /// <summary>
        /// Return the next job to run or null if nothing to run.
        /// </summary>
        private IRunnable GetNextJob()
        {
            lock (this)
            {
                if (IndexOfNextJob < Jobs.Count - 1 && NumJobsRunning < MaximumNumOfProcessors)
                {
                    IndexOfNextJob++;
                    return Jobs[IndexOfNextJob];
                }
                else
                    return null;
            }
        }


    }
}
