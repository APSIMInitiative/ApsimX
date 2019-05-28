namespace Models.Core.Run
{
    using APSIM.Shared.JobRunning;
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
    public class JobRunnerMultiProcess : JobRunner
    {
        private SocketServer server;
        private bool allStopped;
        private int numberOfProcessors;

        /// <summary>A mapping of job IDs (Guid) to job  instances.</summary>
        private Dictionary<Guid, MultiProcessJob> runningJobs = new Dictionary<Guid, MultiProcessJob>();

        /// <summary>A list of exceptions thrown during simulation runs. Will be null when no exceptions found.</summary>
        private List<Exception> exceptionsThrown;

        /// <summary>Constructor.</summary>
        /// <param name="numOfProcessors">The maximum number of cores to use.</param>
        public JobRunnerMultiProcess(int numOfProcessors = -1)
        {
            numberOfProcessors = numOfProcessors;
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
        }

        /// <summary>Main worker thread.</summary>
        protected override void WorkerThread()
        {
            // Spin up a job manager server.
            server = new SocketServer();
            server.AddCommand("GetJob", OnGetJob);
            server.AddCommand("TransferData", OnTransferData);
            server.AddCommand("EndJob", OnEndJob);

            // Tell server to start listening.
            Task t = Task.Run(() => server.StartListening(2222));

            DeleteRunners();
            CreateRunners();

            AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

            SpinWait.SpinUntil(() => allStopped);

            server.StopListening();
            server = null;
            DeleteRunners();
            runningJobs.Clear();

            ElapsedTime = DateTime.Now - startTime;
            InvokeAllCompleted();

            completed = true;
        }

        /// <summary>Create one job runner process for each CPU</summary>
        private void CreateRunners()
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
                allStopped = true;
        }

        private int jobManagerIndex;
        private IEnumerator<IRunnable> currentEnumerator;

        /// <summary>
        /// Return the next job to run.
        /// </summary>
        /// <returns>The job to run or null if no more.</returns>
        private MultiProcessJob GetNextJobToRun()
        {
            lock (runningJobs)
            {
                if (currentEnumerator == null)
                    currentEnumerator = jobManagers[jobManagerIndex].GetJobs().GetEnumerator();
                bool ok = currentEnumerator.MoveNext();

                while (!ok && jobManagerIndex < jobManagers.Count)
                {
                    jobManagerIndex++;
                    if (jobManagerIndex < jobManagers.Count)
                    {
                        currentEnumerator = jobManagers[jobManagerIndex].GetJobs().GetEnumerator();
                        ok = currentEnumerator.MoveNext();
                    }
                }
                if (ok)
                    return new MultiProcessJob()
                    {
                        JobManagerIndex = jobManagerIndex,
                        RunnableJob = currentEnumerator.Current,
                        StartTime = DateTime.Now
                    };
                else
                    return null;
            }
        }

        /// <summary>Called by a runner process to get the next job to run.</summary>
        /// <param name="sender">The sender</param>
        /// <param name="args">The command arguments</param>
        private void OnGetJob(object sender, SocketServer.CommandArgs args)
        {
            try
            {
                var jobKey = Guid.NewGuid();
                var job = GetNextJobToRun();
                string fileName = null;

                // At this point DataStore should be a child of the simulation. Store the
                // DataStore in our jobToRun and then remove it from the simulation. We
                // don't want to pass the DataStore to the runner process via a socket.
                if (job == null)
                    server.Send(args.socket, "NULL");
                else
                {
                    if (job.RunnableJob is SimulationDescription)
                    {
                        var simulation = (job.RunnableJob as SimulationDescription).ToSimulation();
                        var dataStore = Apsim.Child(simulation, typeof(DataStore)) as DataStore;
                        fileName = dataStore.FileName;
                        simulation.Children.Remove(dataStore);
                        job.DataStore = dataStore;
                        job.JobSentToClient = simulation;
                    }
                    else
                        job.JobSentToClient = job.RunnableJob;

                    lock (runningJobs)
                        runningJobs.Add(jobKey, job);

                    GetJobReturnData returnData = new GetJobReturnData
                    {
                        id = jobKey,
                        job = job.JobSentToClient,
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

                MultiProcessJob processJob;
                lock (runningJobs)
                    processJob = runningJobs[arguments.id];

                JobCompleteArguments jobCompleteArguments = new JobCompleteArguments
                {
                    Job = processJob.JobSentToClient,
                    ExceptionThrowByJob = arguments.errorMessage
                };


                var finishTime = DateTime.Now;
                var completeArguments = new JobCompleteArguments()
                {
                    Job = processJob.JobSentToClient,
                    ExceptionThrowByJob = arguments.errorMessage,
                    ElapsedTime = finishTime - processJob.StartTime
                };

                InvokeJobCompleted(processJob.RunnableJob, 
                                   jobManagers[processJob.JobManagerIndex], 
                                   processJob.StartTime, 
                                   arguments.errorMessage);

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

        private class MultiProcessJob
        {
            /// <summary>The index of the owining job manager.</summary>
            public int JobManagerIndex { get; set; }

            /// <summary>The job to run.</summary>
            public IRunnable RunnableJob { get; set; }

            /// <summary>The job that was sent to the APSIM Runner client.</summary>
            public IRunnable JobSentToClient { get; set; }

            /// <summary>The data store relating to the job</summary>
            public DataStore DataStore { get; set; }

            /// <summary>The time the job was started.</summary>
            public DateTime StartTime { get; set; }
        }
    }
}
