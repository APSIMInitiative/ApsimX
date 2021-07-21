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
        private GlobalServerOptions options;

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
        public ApsimServer(GlobalServerOptions options)
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
        public virtual void Run()
        {
            try
            {
                WriteToLog($"Starting server...");
                using (IConnectionManager conn = CreateConnection())
                {
                    while (true)
                    {
                        try
                        {
                            WriteToLog("Waiting for connections...");
                            conn.WaitForConnection();
                            WriteToLog("Client connected to server.");
                            ICommand command;
                            while ( (command = conn.WaitForCommand()) != null)
                                RunCommand(command, conn);

                            WriteToLog($"Connection closed by client.");

                            // If we don't want to keep the server alive we can exit now.
                            // Otherwise we will go back and wait for another connection.
                            if (!options.KeepAlive)
                                return;
                            conn.Disconnect();
                        }
                        catch (IOException)
                        {
                            WriteToLog("Pipe is broken. Closing connection...");
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

        /// <summary>
        /// Create a connection manager based on the user options.
        /// </summary>
        private IConnectionManager CreateConnection()
        {
            Protocol protocol = GetProtocol();
            if (options.LocalMode)
                return new LocalSocketConnection(options.SocketName, options.Verbose, protocol);
            if (options.RemoteMode)
                return new NetworkSocketConnection(options.Verbose, options.IPAddress, options.Port, protocol);
            // We shouldn't be able to reach here from the CLI, but if the class is
            // instantiated in code, this could certainly occur.
            throw new NotImplementedException("Unknown connection type. Use either local or remote mode.");
        }

        /// <summary>
        /// Get the protocol type from the user options.
        /// </summary>
        private Protocol GetProtocol()
        {
            if (options.NativeMode)
                return Protocol.Native;
            if (options.ManagedMode)
                return Protocol.Managed;
            // The command line arguments parser should prevent us from reaching this point,
            // but theoretically the options object can be contructed and passed around
            // in code without invoking the parser.
            throw new NotImplementedException($"Unknown protocol type. Use either native or managed mode");
        }

        /// <summary>
        /// Run a command received from a given connection manager.
        /// </summary>
        /// <param name="command">Command to be run.</param>
        /// <param name="connection">Connection on which we received the command.</param>
        protected virtual void RunCommand(ICommand command, IConnectionManager connection)
        {
            WriteToLog($"Received command {command}. Running command...");
            try
            {
                // Clone the simulations object before running the command.
                var timer = Stopwatch.StartNew();
                command.Run(runner, jobRunner, sims.FindChild<Models.Storage.IDataStore>());
                timer.Stop();
                WriteToLog($"Command ran in {timer.ElapsedMilliseconds}ms");
                connection.OnCommandFinished(command);
                
            }
            catch (Exception err)
            {
                if (options.Verbose)
                    Console.Error.WriteLine(err);
                connection.OnCommandFinished(command, err);
            }
        }

        /// <summary>
        /// Write a message to the log.
        /// </summary>
        /// <param name="message">The message to be written.</param>
        protected void WriteToLog(string message)
        {
            if (options.Verbose)
                Console.WriteLine(message);
            else
                Console.WriteLine($"Verbose mode is disabled but here ya go anyway: {message}");
        }
    }
}
