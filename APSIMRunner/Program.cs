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
    using Models.Report;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Sockets;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.Threading;

    class Program
    {
        Byte[] bytes = new Byte[65536];

        /// <summary>Main program</summary>
        static int Main(string[] args)
        {
            try
            { 
                AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

                // Setup a binary formatter and a stream for writing to.
                IFormatter formatter = new BinaryFormatter();

                // Send a command to socket server to get the job to run.
                object response = GetNextJob();
                while (response != null)
                {
                    JobManagerMultiProcess.GetJobReturnData job = response as JobManagerMultiProcess.GetJobReturnData;

                    // Run the simulation.
                    string errorMessage = null;
                    Simulation simulation = null;
                    try
                    {
                        simulation = job.job as Simulation;
                        simulation.Run(null, null);
                    }
                    catch (Exception err)
                    {
                        errorMessage = err.ToString();
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

                SocketServer.CommandObject transferDataCommand = new SocketServer.CommandObject() { name = "TransferData", data = DataStore.TablesToWrite };
                SocketServer.Send("127.0.0.1", 2222, transferDataCommand);
            }
            catch (SocketException)
            {
                // Couldn't connect to socket. Server not running?
                return 1;
            }
            catch (Exception err)
            {
                SocketServer.CommandObject command = new SocketServer.CommandObject() { name = "Error" };
                command.data = err.ToString();
                SocketServer.Send("127.0.0.1", 2222, command);
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
