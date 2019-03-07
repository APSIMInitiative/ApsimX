// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIMRunner
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core.Runners;
    using System;
    using System.Threading;

    class Program
    {
        /// <summary>Main program</summary>
        static int Main(string[] args)
        {
            try
            { 
                AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

                // Send a command to socket server to get the job to run.
                object response = GetNextJob();
                while (response != null)
                {
                    JobRunnerMultiProcess.GetJobReturnData job = response as JobRunnerMultiProcess.GetJobReturnData;

                    // Run the simulation.
                    Exception error = null;
                    string simulationName = null;
                    RunSimulation simulationRunner = null;
                    StorageViaSockets storage = new StorageViaSockets(job.key);
                    try
                    {
                        IRunnable jobToRun = job.job;
                        if (jobToRun is RunExternal)
                            jobToRun.Run(new CancellationTokenSource());
                        else
                        {
                            simulationRunner = job.job as RunSimulation;

                            // Replace datastore with a socket writer
                            simulationRunner.Services = new object[] { storage };
                            // Run simulation
                            simulationName = simulationRunner.simulationToRun.Name;
                            simulationRunner.cloneSimulationBeforeRun = false;
                            simulationRunner.Run(new CancellationTokenSource());
                        }
                    }
                    catch (Exception err)
                    {
                        error = err;
                    }

                    // Signal we have completed writing data for this sim.
                    storage.WriteAllData();

                    // Signal end of job.
                    JobRunnerMultiProcess.EndJobArguments endJobArguments = new JobRunnerMultiProcess.EndJobArguments();
                    endJobArguments.key = job.key;
                    if (error != null)
                        endJobArguments.errorMessage = error.ToString();
                    endJobArguments.simulationName = simulationName;
                    SocketServer.CommandObject endJobCommand = new SocketServer.CommandObject() { name = "EndJob", data = endJobArguments };
                    SocketServer.Send("127.0.0.1", 2222, endJobCommand);

                    // Get next job.
                    response = GetNextJob();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
                return 1;
            }
            finally
            {
                AppDomain.CurrentDomain.AssemblyResolve -= Manager.ResolveManagerAssembliesEventHandler;
            }
            return 0;
        }

        /// <summary>Get the next job to run. Returns the job to run or null if no more jobs.</summary>
        private static object GetNextJob()
        {
            SocketServer.CommandObject command = new SocketServer.CommandObject() { name = "GetJob" };
            object response = SocketServer.Send("127.0.0.1", 2222, command);

            if (response is string && response.ToString() == "NULL")
                return null;

            while (response is string)
            {
                Thread.Sleep(300);
                response = SocketServer.Send("127.0.0.1", 2222, command);
            }
            return response;
        }
    }
}
