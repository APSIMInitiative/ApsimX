// -----------------------------------------------------------------------
// <copyright file="JobManagerMultiProcess.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace Models.Core.Runners
{
    using APSIM.Shared.Utilities;
    using Report;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>A class for managing asynchronous running of jobs.</summary>
    [Serializable]
    public class JobManagerMultiProcess : JobManager
    {
        private SocketServer server;
        private IStorageWriter storageWriter;

        /// <summary>Constructor</summary>
        /// <param name="writer">The writer where all data should be stored</param>
        public JobManagerMultiProcess(IStorageWriter writer)
        {
            storageWriter = writer;
        }

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public override void Start(bool waitUntilFinished)
        {
            DeleteRunners();

            AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

            // Spin up a job manager server.
            server = new SocketServer();
            server.AddCommand("GetJob", OnGetJob);
            server.AddCommand("GetJobFailed", OnGetJobFailed);
            server.AddCommand("TransferData", OnTransferData);
            server.AddCommand("EndJob", OnEndJob);
            server.AddCommand("Error", OnError);

            // Tell server to start listening.
            Task.Run(() => server.StartListening(2222));

            // Call base to begin running jobs.
            base.Start(waitUntilFinished);
        }

        /// <summary>Run the specified job.</summary>
        /// <param name="job">Job to run.</param>
        protected override void RunJob(Job job)
        {
            if (job.RunnableJob is RunSimulation)
            {
                try
                {
                    CreateRunners();
                }
                catch (Exception err)
                {
                    job.Error = err;
                    job.isCompleted = true;
                    job.IsRunning = false;
                }
            }
            else
                base.RunJob(job);
        }

        /// <summary>Create one job runner process for each CPU</summary>
        private void CreateRunners()
        {
            //int numRunners = Process.GetProcessesByName("APSIMRunner").Length;
            //for (int i = numRunners; i < MaximumNumOfProcessors; i++)
            //{
            //    string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            //    string runnerFileName = Path.Combine(workingDirectory, "APSIMRunner.exe");
            //    ProcessUtilities.ProcessWithRedirectedOutput runnerProcess = new ProcessUtilities.ProcessWithRedirectedOutput();
            //    runnerProcess.Exited += OnExited;
            //    runnerProcess.Start(runnerFileName, null, workingDirectory, false);
            //}
        }

        /// <summary>Delete any runners that may exist.</summary>
        private void DeleteRunners()
        {
            foreach (Process runner in Process.GetProcessesByName("APSIMRunner"))
                runner.Kill();
        }

        /// <summary>Stop all jobs currently running in the scheduler.</summary>
        public override void Stop()
        {
            lock (this)
            {
                server.StopListening();
                server = null;
                DeleteRunners();
                base.Stop();
            }
        }

        /// <summary>An error has occurred in the socket server.</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnSocketServerError(object sender, SocketServer.ErrorArgs e)
        {
            internalExceptions.Add(new Exception(e.message + Environment.NewLine));
            Stop();
        }

        /// <summary>A runner process has exited. Check for errors</summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments</param>
        private void OnExited(object sender, EventArgs e)
        {
        }

        /// <summary>Called by the client to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnGetJob(object sender, SocketServer.CommandArgs args)
        {
            Job jobToRun = null;
            lock (this)
            {
                // Free up memory by removing all child models on completed jobs.
                // This helps the garbage collector when there are many jobs.
                foreach (JobManager.Job job in jobs)
                {
                    //if (job.IsCompleted && job.RunnableJob is RunSimulation)
                    //    (job.RunnableJob as RunSimulation).Children.Clear();
                }

                jobToRun = jobs.Find(job => !job.IsRunning && !job.isCompleted && typeof(RunSimulation).IsAssignableFrom(job.RunnableJob.GetType()));
                if (jobToRun != null)
                {
                    jobToRun.IsRunning = true;
                }
            }

            if (jobToRun == null)
                server.Send(args.socket, "NULL");
            else
            {
                RunSimulation runner = jobToRun.RunnableJob as RunSimulation;
                IModel savedParent = runner.SetParentOfSimulation(null);
                GetJobReturnData returnData = new GetJobReturnData();
                returnData.key = jobToRun.Key;
                returnData.job = runner;
                server.Send(args.socket, returnData);
                runner.SetParentOfSimulation(savedParent);
            }
        }

        /// <summary>Called by the client when a socket fails to get a job</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnGetJobFailed(object sender, SocketServer.CommandArgs args)
        {
            Job jobToRun = null;
            lock (this)
            {
                if (jobs.Count > 0)
                {
                    jobToRun = jobs[0];
                    jobs.RemoveAt(0);
                }
            }

            if (jobToRun == null)
                server.Send(args.socket, "NULL");
            else
            {
                GetJobReturnData returnData = new GetJobReturnData();
                returnData.key = jobToRun.Key;
                returnData.job = jobToRun.RunnableJob;
                server.Send(args.socket, returnData);
            }
        }

        /// <summary>Called by the client to send its output data.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnTransferData(object sender, SocketServer.CommandArgs args)
        {
            TransferRowInTable row = args.obj as TransferRowInTable;
            storageWriter.WriteRow(row.simulationName, row.tableName, 
                                   row.columnNames, row.columnUnits, row.values);

            server.Send(args.socket, "OK");
        }

        /// <summary>Called by the client to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnEndJob(object sender, SocketServer.CommandArgs args)
        {
            EndJobArguments arguments = args.obj as EndJobArguments;
            lock (this)
            {
                SetJobCompleted(arguments.key, arguments.errorMessage);
            }
            server.Send(args.socket, "OK");
        }

        /// <summary>Called by the client to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnError(object sender, SocketServer.CommandArgs args)
        {
            string errorMessage = args.obj as string;
            internalExceptions.Add(new Exception(errorMessage));
            server.Send(args.socket, "OK");
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

        /// <summary>An integer / DataTable pair</summary>
        [Serializable]
        public class TransferArguments
        {
            /// <summary>Simulation name</summary>
            public string simulationName;

            /// <summary>Table name</summary>
            public string tableName;

            /// <summary>Data</summary>
            public string fileName;
        }

        /// <summary>An class for encapsulating arguments to an EndJob command</summary>
        [Serializable]
        public class EndJobArguments
        {
            /// <summary>Job Key</summary>
            public Guid key;

            /// <summary>Error message</summary>
            public string errorMessage;
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
