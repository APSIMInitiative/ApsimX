// -----------------------------------------------------------------------
// <copyright file="Program.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIMJobRunner
{
    using APSIM.Shared.Utilities;
    using Models;
    using Models.Core;
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
                    JobManagerMultiProcess.GetJobReturnData job = response as JobManagerMultiProcess.GetJobReturnData;
                    // Run the simulation.
                    string errorMessage = null;
                    try
                    {
                        Simulation simulation = job.job as Simulation;
                        simulation.Run(null, null);
                    }
                    catch (Exception err)
                    {
                        errorMessage = err.ToString();
                    }

                    // Send all output tables back to socket server.
                    foreach (DataStore.TableToWrite table in DataStore.TablesToWrite)
                    {
                        JobManagerMultiProcess.TransferArguments transferArguments = new JobManagerMultiProcess.TransferArguments();
                        transferArguments.simulationName = table.SimulationName;
                        transferArguments.tableName = table.TableName;
                        transferArguments.data = table.Data;
                        SocketServer.CommandObject transferCommand = new SocketServer.CommandObject() { name = "TransferOutputs", data = transferArguments };
                        SocketServer.Send("127.0.0.1", 2222, transferCommand);
                    }

                    // Signal end of job.
                    JobManagerMultiProcess.EndJobArguments endJobArguments = new JobManagerMultiProcess.EndJobArguments();
                    endJobArguments.key = job.key;
                    endJobArguments.errorMessage = errorMessage;
                    SocketServer.CommandObject endJobCommand = new SocketServer.CommandObject() { name = "EndJob", data = endJobArguments };
                    SocketServer.Send("127.0.0.1", 2222, endJobCommand);

                    // Get next job.
                    response = GetNextJob();
                }

            }
            catch (Exception err)
            {
                Console.Error.WriteLine(err);
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
            try
            {
                SocketServer.CommandObject command = new SocketServer.CommandObject() { name = "GetJob" };
                object response = SocketServer.Send("127.0.0.1", 2222, command);
                while (response is string)
                {
                    Thread.Sleep(300);
                    response = SocketServer.Send("127.0.0.1", 2222, command);
                }
                return response;
            }
            catch (Exception err)
            {
                SocketServer.CommandObject command = new SocketServer.CommandObject() { name = "Error", data = err.ToString() };
                SocketServer.Send("127.0.0.1", 2222, command);
                return null;
            }
        }
    }
}
