namespace APSIMRunner
{
    using APSIM.Shared.JobRunning;
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
    using Models.Storage;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Pipes;
    using System.Linq;
    using System.Net.Sockets;
    using System.Threading;
    using static Models.Core.Run.JobRunnerMultiProcess;

    class Program
    {
        /// <summary>Main program</summary>
        static int Main(string[] args)
        {
            try
            {
                if (args == null || args.Length < 2)
                    throw new Exception("Usage: APSIMRunner.exe pipeWriteHandle pipeReadHandle");

                // Get read and write pipe handles
                // Note: Roles are now reversed from how the other process is passing the handles in
                string pipeWriteHandle = args[0];
                string pipeReadHandle = args[1];

                // Let in for debugging purposes.
                //while (pipeReadHandle != null) 
                //    Thread.Sleep(500);

                // Add hook for manager assembly resolve method.
                AppDomain.CurrentDomain.AssemblyResolve += ScriptCompiler.ResolveManagerAssemblies;

                // Create 2 anonymous pipes (read and write) for duplex communications
                // (each pipe is one-way)
                using (var pipeRead = new AnonymousPipeClientStream(PipeDirection.In, pipeReadHandle))
                using (var pipeWrite = new AnonymousPipeClientStream(PipeDirection.Out, pipeWriteHandle))
                {

                    while (PipeUtilities.GetObjectFromPipe(pipeRead) is IRunnable runnable)
                    {
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
                                Apsim.ParentAllChildren(sim);
                            }
                            else if (runnable is IModel model)
                            {
                                IDataStore oldStorage = Apsim.Find(model, typeof(IDataStore)) as IDataStore;
                                if (oldStorage != null)
                                    storage = new StorageViaSockets(oldStorage.FileName);

                                storage.Parent = model;
                                storage.Children.AddRange(model.Children.OfType<DataStore>().SelectMany(d => d.Children).Select(m => Apsim.Clone(m)));
                                model.Children.RemoveAll(m => m is DataStore);
                                model.Children.Add(storage);

                                Apsim.ParentAllChildren(model);
                            }

                            // Run the job.
                            runnable.Run(new CancellationTokenSource());
                        }
                        catch (Exception err)
                        {
                            error = err;
                        }

                        // Signal end of job.
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
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= ScriptCompiler.ResolveManagerAssemblies;
            }
            return 0;
        }
    }
}