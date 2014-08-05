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
    /// <summary>
    /// A class for managing asynchronous running of jobs.
    /// </summary>
    [Serializable]
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

        [Serializable]
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

            /// <summary>
            /// True if this completed arguments instance has been dispatched to 
            /// BackgroundWorker.ReportProgress
            /// </summary>
            public bool Reported;
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
        [NonSerialized]
        private BackgroundWorker SchedulerThread = null;

        /// <summary>
        /// Keep track of all threads created so that we can stop them if needed.
        /// </summary>
        [NonSerialized]
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
        private List<JobCompleteArgs> PendingCompletedJobs = new List<JobCompleteArgs>();

        #endregion


        /// <summary>
        /// Used by the binary deserialiser when running on a remote machine.
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        void OnDeserialized(StreamingContext context)
        {
            Threads = new List<BackgroundWorker>();
        }

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
                        worker.RunWorkerAsync(this);
                        Threads.Add(worker);
                    }
                }
                else
                    Thread.Sleep(100);


                // See if there are any pending completes that we need to report.
                lock (this)
                {
                    for (int i = 0; i < PendingCompletedJobs.Count; i++)
                    {
                        JobCompleteArgs completeArgs = PendingCompletedJobs[i];
                        if (!completeArgs.Reported)
                        {
                            completeArgs.Reported = true;
                            bw.ReportProgress(completeArgs.PercentComplete, completeArgs);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Main scheduler thread is reporting progress. This will be called in the UI thread.
        /// </summary>
        private void OnProgress(object sender, System.ComponentModel.ProgressChangedEventArgs e)
        {
            if (OnComplete != null)
            {
                JobCompleteArgs jobCompleteArgs = e.UserState as JobCompleteArgs;
                jobCompleteArgs.PercentComplete = Convert.ToInt32((NumJobsCompleted * 1.0) / Jobs.Count * 100.0);
                OnComplete(this, jobCompleteArgs);
            }
            lock (this)
                PendingCompletedJobs.Remove(e.UserState as JobCompleteArgs);
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
                    if (e.Error != null)
                    {
                        completeArgs.ErrorMessage = e.Error.Message;
                        SomeHadErrors = true;
                    }
                    else
                        completeArgs.Name = Utility.Reflection.Name(e.Result);

                    PendingCompletedJobs.Add(completeArgs);
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
