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
        private List<ProcessUtilities.ProcessWithRedirectedOutput> runners = null;
        private string dbFileName = null;

        /// <summary>
        /// Start the jobs asynchronously. If 'waitUntilFinished'
        /// is true then control won't return until all jobs have finished.
        /// </summary>
        /// <param name="waitUntilFinished">if set to <c>true</c> [wait until finished].</param>
        public override void Start(bool waitUntilFinished)
        {
            CleanupOldRunners();

            AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

            // Spin up a job manager server.
            server = new SocketServer();
            server.AddCommand("GetJob", OnGetJob);
            server.AddCommand("GetJobFailed", OnGetJobFailed);
            server.AddCommand("TransferOutputs", OnTransferOutputs);
            server.AddCommand("TransferData", OnTransferData);
            server.AddCommand("EndJob", OnEndJob);
            server.AddCommand("Error", OnError);

            // Tell server to start listening.
            Task.Run(() => server.StartListening(2222));

            // Call base to begin running jobs.
            base.Start(waitUntilFinished);
        }

        /// <summary>Cleanup old runners.</summary>
        private void CleanupOldRunners()
        {
            foreach (Process runner in Process.GetProcessesByName("APSIMRunner"))
                runner.Kill();
        }

        /// <summary>Run the specified job.</summary>
        /// <param name="job">Job to run.</param>
        protected override void RunJob(Job job)
        {
            if (job.RunnableJob is Simulation)
            {
                try
                {
                    if (runners == null || runners.Count != MaximumNumOfProcessors)
                        CreateRunners();
                    if (dbFileName == null)
                        dbFileName = (job.RunnableJob as Simulation).FileName;
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
            if (runners == null)
                runners = new List<ProcessUtilities.ProcessWithRedirectedOutput>();
            for (int i = runners.Count; i < MaximumNumOfProcessors; i++)
            {
                string workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string runnerFileName = Path.Combine(workingDirectory, "APSIMRunner.exe");
                ProcessUtilities.ProcessWithRedirectedOutput runnerProcess = new ProcessUtilities.ProcessWithRedirectedOutput();
                runnerProcess.Exited += OnExited;
                runnerProcess.Start(runnerFileName, null, workingDirectory, false);
                runners.Add(runnerProcess);
            }
        }

        /// <summary>Kill all child runners</summary>
        private void KillRunners()
        {
            if (runners != null)
            {
                runners.ForEach(runner => runner.Kill());
                runners.Clear();
            }
        }

        /// <summary>Stop all jobs currently running in the scheduler.</summary>
        public override void Stop()
        {
            lock (this)
            {
                server.StopListening();
                server = null;
                KillRunners();
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
            ProcessUtilities.ProcessWithRedirectedOutput runner = sender as ProcessUtilities.ProcessWithRedirectedOutput;
            runners.Remove(runner);
        }

        /// <summary>Called by the client to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnGetJob(object sender, SocketServer.CommandArgs args)
        {
            Job jobToRun = null;
            lock (jobs)
            {
                jobToRun = jobs.Find(job => !job.IsRunning && !job.isCompleted && typeof(Simulation).IsAssignableFrom(job.RunnableJob.GetType()));
                if (jobToRun != null)
                {
                    jobToRun.IsRunning = true;
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

        /// <summary>Called by the client when a socket fails to get a job</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnGetJobFailed(object sender, SocketServer.CommandArgs args)
        {
            Job jobToRun = null;
            lock (jobs)
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
        private void OnTransferOutputs(object sender, SocketServer.CommandArgs args)
        {
            TransferArguments arguments = args.obj as TransferArguments;

            // Read from temp file.
            byte[] bytes = File.ReadAllBytes(arguments.fileName);
            MemoryStream s = new MemoryStream(bytes);
            DataTable table = ReflectionUtilities.BinaryDeserialise(s) as DataTable;
            File.Delete(arguments.fileName);

            DataStore store = new DataStore();
            store.Filename = Path.ChangeExtension(dbFileName, ".db");
            store.WriteTable(arguments.simulationName, arguments.tableName, table);
            server.Send(args.socket, "OK");
        }

        /// <summary>Called by the client to send its output data.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnTransferData(object sender, SocketServer.CommandArgs args)
        {
            TransferData arguments = args.obj as TransferData;
            System.Runtime.Serialization.IFormatter formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
            MemoryStream stream = new MemoryStream(arguments.data, 0, (int) arguments.dataLength);
            DataTable data = formatter.Deserialize(stream) as DataTable;
            DataStore store = new DataStore();
            store.Filename = Path.ChangeExtension(dbFileName, ".db");
            store.WriteTable(arguments.simulationName, arguments.tableName, data);
            server.Send(args.socket, "OK");
        }
        /// <summary>Called by the client to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnEndJob(object sender, SocketServer.CommandArgs args)
        {
            EndJobArguments arguments = args.obj as EndJobArguments;
            lock (jobs)
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


        /// <summary>A transfer data command.</summary>
        [Serializable]
        public class TransferData
        {
            /// <summary>Simulation name</summary>
            public string simulationName;

            /// <summary>Table name</summary>
            public string tableName;

            /// <summary>Data</summary>
            public byte[] data;

            /// <summary>length of data to use</summary>
            public long dataLength;
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


    }
}
