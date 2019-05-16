namespace Models.Core.Run
{
    using APSIM.Shared.Utilities;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>A class for managing asynchronous running of jobs transferred via a socket connection</summary>
    [Serializable]
    public class JobRunnerMultiProcess : IJobRunner
    {
        private SocketServer server;
        private IJobManager jobs;
        private bool allStopped;

        /// <summary>A token for cancelling running of jobs</summary>
        private CancellationTokenSource cancelToken;

        /// <summary>A mapping of job IDs (Guid) to job  instances.</summary>
        private Dictionary<Guid, Job> runningJobs = new Dictionary<Guid, Job>();

        /// <summary>A list of exceptions thrown during simulation runs. Will be null when no exceptions found.</summary>
        private List<Exception> exceptionsThrown;

        /// <summary>Occurs when all jobs completed.</summary>
        public event EventHandler<AllCompletedArgs> AllJobsCompleted;

        /// <summary>Invoked when a job is completed.</summary>
        public event EventHandler<JobCompleteArgs> JobCompleted;

        /// <summary>Run the specified jobs</summary>
        /// <param name="jobManager">An instance of a class that manages all jobs.</param>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        /// <param name="numberOfProcessors">The maximum number of cores to use.</param>
        public void Run(IJobManager jobManager, bool wait = false, int numberOfProcessors = -1)
        {
            jobs = jobManager;
            allStopped = false;
            // If the job manager is a RunExternal, then we don't need to worry about storage - 
            // each ApsimRunner will launch its own Models.exe which will manage storage itself.

            // Determine number of threads to use
            if (numberOfProcessors == -1)
            {
                int number;
                string numOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                if (numOfProcessorsString != null && Int32.TryParse(numOfProcessorsString, out number))
                    numberOfProcessors = System.Math.Max(number, 1);
                else
                    numberOfProcessors = System.Math.Max(Environment.ProcessorCount - 1, 1);
            }

            cancelToken = new CancellationTokenSource();

            // Spin up a job manager server.
            server = new SocketServer();
            server.AddCommand("GetJob", OnGetJob);
            server.AddCommand("TransferData", OnTransferData);
            server.AddCommand("EndJob", OnEndJob);

            // Tell server to start listening.
            Task t = Task.Run(() => server.StartListening(2222));

            DeleteRunners();
            CreateRunners(numberOfProcessors);

            AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

            if (wait)
                while (!allStopped)
                    Thread.Sleep(200);
        }

        /// <summary>Stop all jobs currently running</summary>
        public void Stop()
        {
            if (server != null)
            {
                lock (this)
                {
                    if (server != null)
                    {
                        cancelToken.Cancel();
                        server.StopListening();
                        server = null;
                        DeleteRunners();
                        runningJobs.Clear();
                        AllCompletedArgs args = new AllCompletedArgs();
                        if (exceptionsThrown != null)
                            args.exceptionsThrown = exceptionsThrown;
                        jobs.AllCompleted(args);
                        if (AllJobsCompleted != null)
                            AllJobsCompleted.Invoke(this, args);
                        allStopped = true;
                    }
                }
            }
        }

        /// <summary>Create one job runner process for each CPU</summary>
        /// <param name="numberOfProcessors">The maximum number of cores to use</param>
        private void CreateRunners(int numberOfProcessors)
        {
            int numRunners = Process.GetProcessesByName("APSIMRunner").Length;
            for (int i = numRunners; i < numberOfProcessors; i++)
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string runnerFileName = Path.Combine(workingDirectory, "APSIMRunner.exe");
                ProcessUtilities.ProcessWithRedirectedOutput runnerProcess = new ProcessUtilities.ProcessWithRedirectedOutput();
                runnerProcess.Exited += OnExited;
                runnerProcess.Start(runnerFileName, null, Directory.GetCurrentDirectory(), true, false);
            }
        }

        /// <summary>Delete any runners that may exist.</summary>
        private void DeleteRunners()
        {
            foreach (Process runner in Process.GetProcessesByName("APSIMRunner"))
                runner.Kill();
        }

        /// <summary>A runner process has exited. Check for errors</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnExited(object sender, EventArgs e)
        {
            ProcessUtilities.ProcessWithRedirectedOutput p = sender as ProcessUtilities.ProcessWithRedirectedOutput;
            if (p.ExitCode != 0)
            {
                var exception = new Exception(p.StdOut + Environment.NewLine + p.StdErr);
                AddException(exception);
            }

            if (Process.GetProcessesByName("APSIMRunner").Length == 0)
                Stop();
        }

        /// <summary>Called by a runner process to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnGetJob(object sender, SocketServer.CommandArgs args)
        {
            try
            {
                var jobKey = Guid.NewGuid();
                var runnableJob = jobs.GetNextJobToRun();
                string fileName = null;

                // At this point DataStore should be a child of the simulation. Store the
                // DataStore in our jobToRun and then remove it from the simulation. We
                // don't want to pass the DataStore to the runner process via a socket.
                if (runnableJob != null)
                {
                    var dataStore = Apsim.Child(runnableJob as IModel, typeof(DataStore)) as DataStore;
                    fileName = dataStore.FileName;
                    (runnableJob as IModel).Children.Remove(dataStore);

                    var job = new Job()
                    {
                        RunnableJob = runnableJob,
                        DataStore = dataStore
                    };

                    lock (runningJobs)
                        runningJobs.Add(jobKey, job);
                }

                if (runnableJob == null)
                    server.Send(args.socket, "NULL");
                else
                {
                    GetJobReturnData returnData = new GetJobReturnData
                    {
                        id = jobKey,
                        job = runnableJob,
                        fileName = fileName
                    };
                    server.Send(args.socket, returnData);
                }
            }
            catch (Exception err)
            {
                AddException(err);
            }
        }

        /// <summary>Called by a runner process to send its output data.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnTransferData(object sender, SocketServer.CommandArgs args)
        {
            try
            {
                IDataStore dataStore;
                if (args.obj is TransferReportData)
                {
                    var transferData = args.obj as TransferReportData;
                    lock (runningJobs)
                        dataStore = runningJobs[transferData.id].DataStore;
                    dataStore.Writer.WriteTable(transferData.data);
                }
                else if (args.obj is TransferDataTable)
                {
                    var transferData = args.obj as TransferDataTable;
                    lock (runningJobs)
                        dataStore = runningJobs[transferData.id].DataStore;
                    dataStore.Writer.WriteTable(transferData.data);
                }
                else
                    throw new Exception("Invalid socket transfer method.");

                server.Send(args.socket, "OK");
            }
            catch (Exception err)
            {
                AddException(err);
            }
        }

        /// <summary>Called by a runner process to signal end of job</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnEndJob(object sender, SocketServer.CommandArgs args)
        {
            try
            {
                EndJobArguments arguments = args.obj as EndJobArguments;

                Job job;
                lock (runningJobs)
                    job = runningJobs[arguments.id];

                JobCompleteArgs jobCompleteArguments = new JobCompleteArgs
                {
                    job = job.RunnableJob,
                    exceptionThrowByJob = arguments.errorMessage
                };

                if (JobCompleted != null)
                    JobCompleted.Invoke(this, jobCompleteArguments);
                jobs.JobCompleted(jobCompleteArguments);

                server.Send(args.socket, "OK");
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
                if (exceptionsThrown == null)
                    exceptionsThrown = new List<Exception>();
                exceptionsThrown.Add(err);
            }
        }

        /// <summary>An class for encapsulating a response to a GetJob command</summary>
        [Serializable]
        public class GetJobReturnData
        {
            /// <summary>Job ID</summary>
            public Guid id;

            /// <summary>Table name</summary>
            public IRunnable job;

            /// <summary>File name</summary>
            public string fileName;

            /// <summary>Name of job completed</summary>
            public string jobName;
        }

        /// <summary>An class for encapsulating arguments to an EndJob command</summary>
        [Serializable]
        public class EndJobArguments
        {
            /// <summary>Job ID</summary>
            public Guid id;
            
            /// <summary>Error message</summary>
            public Exception errorMessage;
        }

        /// <summary>An class for encapsulating a ReportData</summary>
        [Serializable]
        public class TransferReportData
        {
            /// <summary>Job ID</summary>
            public Guid id;
            
            /// <summary>Simulation name</summary>
            public ReportData data;
        }


        /// <summary>An class for encapsulating a DataTable</summary>
        [Serializable]
        public class TransferDataTable
        {
            /// <summary>Job ID</summary>
            public Guid id;
            
            /// <summary>Simulation name</summary>
            public DataTable data;
        }

        private class Job
        {
            /// <summary>The job being run.</summary>
            public IRunnable RunnableJob { get; set; }

            /// <summary>The data store relating to the job</summary>
            public DataStore DataStore { get; set; }
        }
    }
}
