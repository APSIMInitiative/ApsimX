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
                MemoryStream stream = new MemoryStream(524288);

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
                        string tempFileName = Path.GetTempFileName();

                        stream.Seek(0, SeekOrigin.Begin);
                        formatter.Serialize(stream, table.Data);

                        MemoryStream s = ReflectionUtilities.BinarySerialise(table.Data) as MemoryStream;
                        File.WriteAllBytes(tempFileName, s.ToArray());

                        JobManagerMultiProcess.TransferArguments transferArguments = new JobManagerMultiProcess.TransferArguments();
                        transferArguments.simulationName = table.SimulationName;
                        transferArguments.tableName = table.TableName;
                        transferArguments.fileName = tempFileName;
                        SocketServer.CommandObject transferCommand = new SocketServer.CommandObject() { name = "TransferOutputs", data = transferArguments };
                        SocketServer.Send("127.0.0.1", 2222, transferCommand);
                    }

                    DataStore.ClearTablesToWritten();

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

        /// <summary>
        /// Send an object to the socket server, wait for a response and return the
        /// response as an object.
        /// </summary>
        /// <param name="serverName">The server name.</param>
        /// <param name="port">The port number.</param>
        /// <param name="obj">The object to send.</param>
        public object Send(string serverName, int port, object obj)
        {
            TcpClient Server = new TcpClient(serverName, Convert.ToInt32(port));
            MemoryStream s = new MemoryStream();
            try
            {
                Byte[] bData = SocketServer.EncodeData(obj);
                Server.GetStream().Write(bData, 0, bData.Length);
                

                // Loop to receive all the data sent by the client.
                int numBytesExpected = 0;
                int totalNumBytes = 0;
                int i = 0;
                int NumBytesRead;
                bool allDone = false;
                do
                {
                    NumBytesRead = Server.GetStream().Read(bytes, 0, bytes.Length);
                    s.Write(bytes, 0, NumBytesRead);
                    totalNumBytes += NumBytesRead;

                    if (numBytesExpected == 0 && totalNumBytes > 4)
                        numBytesExpected = BitConverter.ToInt32(bytes, 0);
                    if (numBytesExpected + 4 == totalNumBytes)
                        allDone = true;

                    i++;
                }
                while (!allDone);

                // Decode the bytes and return.
                return SocketServer.DecodeData(s.ToArray());
            }
            finally
            {
                if (Server != null) Server.Close();
            }
        }
    }
}
