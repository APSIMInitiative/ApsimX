using APSIM.Shared.JobRunning;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Storage;
using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static Models.Core.Run.JobRunnerMultiProcess;

namespace APSIMRunner
{
    internal class Client
    {
        private AnonymousPipeClientStream pipeRead;
        private AnonymousPipeClientStream pipeWrite;

        /// <summary>
        /// A timer which allows us to provide scheduled progress updates to the server
        /// (aka JobRunnerMultiProcess instance).
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary>
        /// The currently-running job.
        /// </summary>
        private IRunnable job;

        private object timerLock = new object();

        public Client(AnonymousPipeClientStream reader, AnonymousPipeClientStream writer)
        {
            pipeRead = reader;
            pipeWrite = writer;

            timer = new System.Timers.Timer(1000);
            timer.AutoReset = false;
            timer.Elapsed += UpdateProgress;
        }

        private void UpdateProgress(object sender, ElapsedEventArgs e)
        {
            lock (timerLock)
            {
                if (timer.Enabled)
                {
                    ProgressReport progress = new ProgressReport(job.Progress);
                    PipeUtilities.SendObjectToPipe(pipeWrite, progress);
                }
            }
            timer.Start();
        }

        public void Run()
        {
            while (PipeUtilities.GetObjectFromPipe(pipeRead) is IRunnable runnable)
            {
                job = runnable;
                Exception error = null;
                StorageViaSockets storage = new StorageViaSockets();
                try
                {
                    if (runnable is Simulation sim)
                    {
                        storage = new StorageViaSockets(sim.FileName);

                        // Remove existing DataStore
                        sim.Children.RemoveAll(model => model is Models.Storage.DataStore);

                        // Add in a socket datastore to satisfy links.
                        sim.Children.Add(storage);

                        if (sim.Services != null)
                        {
                            sim.Services.RemoveAll(s => s is Models.Storage.IDataStore);
                            sim.Services.Add(storage);
                        }

                        // Initialise the model so that Simulation.Run doesn't call OnCreated.
                        // We don't need to recompile any manager scripts and a simulation
                        // should be ready to run at this point following a binary 
                        // deserialisation.
                        sim.ParentAllDescendants();
                    }
                    else if (runnable is IModel model)
                    {
                        IDataStore oldStorage = model.FindInScope<IDataStore>();
                        if (oldStorage != null)
                            storage = new StorageViaSockets(oldStorage.FileName);

                        storage.Parent = model;
                        storage.Children.AddRange(model.Children.OfType<DataStore>().SelectMany(d => d.Children).Select(m => Apsim.Clone(m)));
                        model.Children.RemoveAll(m => m is DataStore);
                        model.Children.Add(storage);

                        model.ParentAllDescendants();
                    }

                    // Initiate progress updates.
                    lock (timerLock)
                        timer.Start();

                    // Run the job.
                    runnable.Run(new CancellationTokenSource());

                    // Stop progress updates.
                    lock (timerLock)
                        timer.Stop();
                }
                catch (Exception err)
                {
                    error = err;
                }

                // Signal end of job.
                lock (timerLock)
                {
                    PipeUtilities.SendObjectToPipe(pipeWrite, new JobOutput
                    {
                        ErrorMessage = error,
                        ReportData = storage.reportDataThatNeedsToBeWritten,
                        DataTables = storage.dataTablesThatNeedToBeWritten
                    });
                    pipeWrite.WaitForPipeDrain();
                }
            }
        }
    }
}
