using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using APSIM.Server.Cli;
using APSIM.Server.Commands;
using APSIM.Server.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;

namespace APSIM.Server
{
    /// <summary>
    /// An APSIM Server.
    /// </summary>
    public class ApsimServer
    {
        /// <summary>
        /// Server options.
        /// </summary>
        private ServerOptions options;

        /// <summary>
        /// The simulations object.
        /// </summary>
        private Simulations sims;

        private Runner runner;
        private ServerJobRunner jobRunner;

        /// <summary>
        /// Create an <see cref="ApsimServer" /> instance.
        /// </summary>
        /// <param name="file">.apsimx file to be run.</param>
        public ApsimServer(ServerOptions options)
        {
            this.options = options;
            sims = FileFormat.ReadFromFile<Simulations>(options.File, e => throw e, false);
            sims.FindChild<Models.Storage.DataStore>().UseInMemoryDB = true;
            runner = new Runner(sims);
            jobRunner = new ServerJobRunner();
            runner.Use(jobRunner);
        }

        /// <summary>
        /// Run the apsim server. This will block the calling thread.
        /// </summary>
        public void Run()
        {
            try
            {
                if (options.Verbose)
                    Console.WriteLine($"Starting server...");
                using (ISocketConnection conn = CreateConnection())
                {
                    while (true)
                    {
                        try
                        {
                            if (options.Verbose)
                                Console.WriteLine("Waiting for connections...");
                            conn.WaitForConnection();
                            if (options.Verbose)
                                Console.WriteLine("Client connected to server.");
                            ICommand command;
                            while ( (command = conn.WaitForCommand()) != null)
                                RunCommand(command, conn);

                            if (options.Verbose)
                                Console.WriteLine($"Connection closed by client.");

                            // If we don't want to keep the server alive we can exit now.
                            // Otherwise we will go back and wait for another connection.
                            if (!options.KeepAlive)
                                return;
                            conn.Disconnect();
                        }
                        catch (IOException)
                        {
                            if (options.Verbose)
                                Console.WriteLine("Pipe is broken. Closing connection...");
                            conn.Disconnect();
                        }
                    }
                }
            }
            finally
            {
                sims.FindChild<Models.Storage.IDataStore>().Close();
            }
        }

        private ISocketConnection CreateConnection()
        {
            switch (options.Mode)
            {
                case CommunicationMode.Managed:
                    throw new NotImplementedException();
                case CommunicationMode.Native:
                    return new NativeSocketConnection("testpipe", options.Verbose);
                default:
                    throw new NotImplementedException();
            }
        }

        private void RunCommand(ICommand command, ISocketConnection connection)
        {
            if (options.Verbose)
                Console.WriteLine($"Received command {command}. Running command...");
            try
            {
                // Clone the simulations object before running the command.
                var timer = Stopwatch.StartNew();
                command.Run(runner, jobRunner, sims.FindChild<Models.Storage.IDataStore>());
                timer.Stop();
                if (options.Verbose)
                    Console.WriteLine($"Command ran in {timer.ElapsedMilliseconds}ms");
                connection.OnCommandFinished(command);
                
            }
            catch (Exception err)
            {
                if (options.Verbose)
                    Console.Error.WriteLine(err);
                connection.OnCommandFinished(command, err);
            }
        }
    }
}
