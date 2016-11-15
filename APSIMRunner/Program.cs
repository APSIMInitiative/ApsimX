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

    class Program
    {
        static int Main(string[] args)
        {
            try
            {
                if (args.Length != 1)
                    throw new Exception("Usage: APSIMJobRunner HashOfJob");

                Guid keyJobToRun = Guid.Parse(args[0]);

                AppDomain.CurrentDomain.AssemblyResolve += Manager.ResolveManagerAssembliesEventHandler;

                // Send a command to socket server to get the job to run.
                SocketServer.CommandObject command = new SocketServer.CommandObject() { name = "GetJob", data = keyJobToRun };
                object response = SocketServer.Send("127.0.0.1", 2222, command);

                if (response != null)
                {
                    // Run the simulation.
                    Simulation simulation = response as Simulation;
                    simulation.Run(null, null);

                    // Send all output tables back to socket server.
                    foreach (DataStore.TableToWrite table in DataStore.TablesToWrite)
                    {
                        JobManagerMultiProcess.TransferArguments transferArguments = new JobManagerMultiProcess.TransferArguments();
                        transferArguments.simulationName = table.SimulationName;
                        transferArguments.tableName = table.TableName;
                        transferArguments.data = table.Data;
                        command = new SocketServer.CommandObject() { name = "TransferOutputs", data = transferArguments };
                        SocketServer.Send("127.0.0.1", 2222, command);
                    }
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
    }
}
