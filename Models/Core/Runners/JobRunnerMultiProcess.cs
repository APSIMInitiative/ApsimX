// -----------------------------------------------------------------------
// <copyright file="JobManagerMultiProcess.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using System;
    using System.Collections.Generic;
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
        private IStorageWriter storageWriter = null;
        private IJobManager jobs;

        /// <summary>A token for cancelling running of jobs</summary>
        private CancellationTokenSource cancelToken;

        /// <summary>Write child process' output to standard output?</summary>
        private bool verbose;

        /// <summary>Non simulation errors thrown by runners or this class on socket threads</summary>
        private string errors;

        private Dictionary<Guid, IRunnable> runningJobs = new Dictionary<Guid, IRunnable>();

        /// <summary>Occurs when all jobs completed.</summary>
        public event EventHandler<AllCompletedArgs> AllJobsCompleted;

        /// <summary>Invoked when a job is completed.</summary>
        public event EventHandler<JobCompleteArgs> JobCompleted;

        /// <summary>Constructor</summary>
        /// <param name="verbose">Write child process' output to standard output?</param>
        public JobRunnerMultiProcess(bool verbose)
        {
            this.verbose = verbose;
        }

        /// <summary>Run the specified jobs</summary>
        /// <param name="jobManager">An instance of a class that manages all jobs.</param>
        /// <param name="wait">Wait until all jobs finished before returning?</param>
        /// <param name="numberOfProcessors">The maximum number of cores to use.</param>
        public void Run(IJobManager jobManager, bool wait = false, int numberOfProcessors = -1)
        {
            jobs = jobManager;

            // If the job manager is a RunExternal, then we don't need to worry about storage - 
            // each ApsimRunner will launch its own Models.exe which will manage storage itself.
            if (jobs is RunOrganiser)
                storageWriter = (jobs as RunOrganiser).Storage;

            // Determine number of threads to use
            if (numberOfProcessors == -1)
            {
                string numOfProcessorsString = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
                if (numOfProcessorsString != null)
                    numberOfProcessors = Convert.ToInt32(numOfProcessorsString);
                numberOfProcessors = System.Math.Max(numberOfProcessors, 1);
            }

            cancelToken = new CancellationTokenSource();

            // Spin up a job manager server.
            server = new SocketServer();
            server.AddCommand("GetJob", OnGetJob);
            server.AddCommand("TransferData", OnTransferData);
            server.AddCommand("EndJob", OnEndJob);

            // Tell server to start listening.
            Task t = Task.Run(() =>
            {
                server.StartListening(2222);
                try
                {
                    jobs.Completed();
                }
                catch (Exception err)
                {
                    errors += Environment.NewLine + err.ToString();
                }

                if (AllJobsCompleted != null)
                {
                    AllCompletedArgs args = new AllCompletedArgs();
                    if (errors != null)
                        args.exceptionThrown = new Exception(errors);
                    AllJobsCompleted.Invoke(this, args);
                }
            });

            DeleteRunners();
            CreateRunners(numberOfProcessors);

            AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

            if (wait)
                while (!t.IsCompleted)
                    Thread.Sleep(200);
        }

        /// <summary>Stop all jobs currently running</summary>
        public void Stop()
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
                runnerProcess.Start(runnerFileName, null, Directory.GetCurrentDirectory(), verbose, verbose);
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
                IRunnable jobToRun;
                Guid jobKey;
                lock (this)
                {
                    jobToRun = jobs.GetNextJobToRun();
                    jobKey = Guid.NewGuid();
                    runningJobs.Add(jobKey, jobToRun);
                }

                if (jobToRun == null)
                    server.Send(args.socket, "NULL");
                else
                {
                    if (jobToRun is RunSimulation)
                    {
                        RunSimulation runner = jobToRun as RunSimulation;
                        IModel savedParent = runner.SetParentOfSimulation(null);
                        GetJobReturnData returnData = new GetJobReturnData();
                        returnData.key = jobKey;
                        returnData.job = runner;
                        server.Send(args.socket, returnData);
                        runner.SetParentOfSimulation(savedParent);
                    }
                    else
                    {
                        GetJobReturnData returnData = new GetJobReturnData()
                        {
                            key = jobKey,
                            job = jobToRun
                        };
                        server.Send(args.socket, returnData);
                    }
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
                List<TransferRowInTable> rows = args.obj as List<TransferRowInTable>;
                foreach (TransferRowInTable row in rows)
                {
                    storageWriter.WriteRow(row.simulationName, row.tableName,
                                           row.columnNames, row.columnUnits, row.values);
                }
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
                jobCompleteArguments.job = runningJobs[arguments.key];
                if (arguments.Error != null)
                    jobCompleteArguments.exceptionThrowByJob = arguments.Error;
                lock (this)
                {
                    if (JobCompleted != null)
                        JobCompleted.Invoke(this, jobCompleteArguments);
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
            public Exception Error;

            /// <summary>Simulation name of job completed</summary>
            public string simulationName;
        }

        /// <summary>An class for encapsulating a row in a table</summary>
        [Serializable]
        public class TransferRowInTable
        {
            /// <summary>Simulation name</summary>
            public string simulationName;
            /// <summary>Table name</summary>
            public string tableName;
            /// <summary>Column names</summary>
            public IEnumerable<string> columnNames;
            /// <summary>Column units</summary>
            public IEnumerable<string> columnUnits;
            /// <summary>Row values for each column</summary>
            public IEnumerable<object> values;
        }
    }
}
