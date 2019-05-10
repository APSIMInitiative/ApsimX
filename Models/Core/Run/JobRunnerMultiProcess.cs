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

        /// <summary>Non simulation errors thrown by runners or this class on socket threads</summary>
        private string errors;

        private Dictionary<Guid, Job> runningJobs = new Dictionary<Guid, Job>();

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
                        jobs.AllCompleted(new AllCompletedArgs());
                        if (AllJobsCompleted != null)
                        {
                            AllCompletedArgs args = new AllCompletedArgs();
                            if (errors != null)
                                args.exceptionThrown = new Exception(errors);
                            AllJobsCompleted.Invoke(this, args);
                        }
                    }
                }
            }
            allStopped = true;
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
                runnerProcess.Start(runnerFileName, null, Directory.GetCurrentDirectory(), false, false);
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
                errors += p.StdOut + Environment.NewLine;

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
                Job jobToRun;
                Guid jobKey = Guid.Empty;
                lock (this)
                {
                    jobKey = Guid.NewGuid();
                    jobToRun = new Job();
                    jobToRun.RunnableJob = jobs.GetNextJobToRun();
                    if (jobToRun.RunnableJob is Simulation)
                    {
                        // At this point DataStore should be a child of the simulation. Store the
                        // DataStore in our jobToRun and then remove it from the simulation. We
                        // don't want to pass the DataStore to the runner process via a socket.
                        jobToRun.DataStore = Apsim.Child(jobToRun.RunnableJob as Simulation, typeof(DataStore)) as DataStore;
                        (jobToRun.RunnableJob as IModel).Children.Remove(jobToRun.DataStore);
                        runningJobs.Add(jobKey, jobToRun);
                    }
                }

                if (jobToRun.RunnableJob == null)
                    server.Send(args.socket, "NULL");
                else
                {
                    GetJobReturnData returnData = new GetJobReturnData();
                    returnData.key = jobKey;
                    returnData.job = jobToRun.RunnableJob;
                    server.Send(args.socket, returnData);
                }
            }
            catch (Exception err)
            {
                errors += err.ToString() + Environment.NewLine;
            }
        }

        /// <summary>Called by a runner process to send its output data.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnTransferData(object sender, SocketServer.CommandArgs args)
        {
            try
            {
                Job jobToRun;
                if (args.obj is TransferReportData)
                {
                    var transferData = args.obj as TransferReportData;
                    jobToRun = runningJobs[transferData.key];
                    jobToRun.DataStore.Writer.WriteTable(transferData.data);
                }
                else if (args.obj is TransferDataTable)
                {
                    var transferData = args.obj as TransferDataTable;
                    jobToRun = runningJobs[transferData.key];
                    jobToRun.DataStore.Writer.WriteTable(transferData.data);
                }
                else
                    throw new Exception("Invalid socket transfer method.");

                server.Send(args.socket, "OK");
            }
            catch (Exception err)
            {
                errors += err.ToString() + Environment.NewLine;
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
                JobCompleteArgs jobCompleteArguments = new JobCompleteArgs();
                jobCompleteArguments.job = runningJobs[arguments.key].RunnableJob;
                if (arguments.errorMessage != null)
                    jobCompleteArguments.exceptionThrowByJob = new Exception(arguments.errorMessage);
                lock (this)
                {
                    if (JobCompleted != null)
                        JobCompleted.Invoke(this, jobCompleteArguments);
                    jobs.JobCompleted(jobCompleteArguments);
                    runningJobs.Remove(arguments.key);
                }
                server.Send(args.socket, "OK");
            }
            catch (Exception err)
            {
                errors += err.ToString() + Environment.NewLine;
            }
        }

        /// <summary>An class for encapsulating a response to a GetJob command</summary>
        [Serializable]
        public class GetJobReturnData
        {
            /// <summary>Simulation name</summary>
            public Guid key;

            /// <summary>Table name</summary>
            public IRunnable job;
        }

        /// <summary>An class for encapsulating arguments to an EndJob command</summary>
        [Serializable]
        public class EndJobArguments
        {
            /// <summary>Job Key</summary>
            public Guid key;

            /// <summary>Error message</summary>
            public string errorMessage;

            /// <summary>Simulation name of job completed</summary>
            public string simulationName;
        }

        /// <summary>An class for encapsulating a row in a table</summary>
        [Serializable]
        public class TransferRowInTable
        {
            /// <summary>Key to the job</summary>
            public Guid key;
            /// <summary>Simulation name</summary>
            public string SimulationName;
            /// <summary>Table name</summary>
            public string TableName;
            /// <summary>Column names</summary>
            public IList<string> ColumnNames;
            /// <summary>Column units</summary>
            public IList<string> columnUnits;
            /// <summary>Row values for each column</summary>
            public IList<object> Values;
        }


        /// <summary>An class for encapsulating a ReportData</summary>
        [Serializable]
        public class TransferReportData
        {
            /// <summary>Key to the job</summary>
            public Guid key;
            /// <summary>Simulation name</summary>
            public ReportData data;
        }


        /// <summary>An class for encapsulating a DataTable</summary>
        [Serializable]
        public class TransferDataTable
        {
            /// <summary>Key to the job</summary>
            public Guid key;

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
