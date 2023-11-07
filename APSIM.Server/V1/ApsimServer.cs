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
    public class ApsimServer : IDisposable
    {
        /// <summary>
        /// Server options.
        /// </summary>
        protected GlobalServerOptions options;

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
            sims = FileFormat.ReadFromFile<Simulations>(options.File, e => throw e, false).NewModel as Simulations;
            sims.FindChild<Models.Storage.DataStore>().UseInMemoryDB = true;
            runner = new Runner(sims);
            jobRunner = new ServerJobRunner();
            runner.Use(jobRunner);
        }

        protected ApsimServer() { }

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
                            while ( (command = GetCommand(conn)) != null)
                            {
                                WriteToLog($"Received {command}");
                                try
                                {
                                    RunCommand(command, conn);
                                }
                                catch (IOException)
                                {
                                    // Broken pipe is handled further down.
                                    throw;
                                }
                                catch (Exception error)
                                {
                                    // Other exceptions will usually be triggered by a
                                    // problem executing the command. This shouldn't cause
                                    // the server to crash.
                                    // todo : custom exception type for comamnd failures?
                                    WriteToLog($"{command} ran with errors:");
                                    WriteToLog(error.ToString());
                                }
                            }

                            WriteToLog($"Connection closed by client.");

                            // If we don't want to keep the server alive we can exit now.
                            // Otherwise we will go back and wait for another connection.
                            if (!options.KeepAlive)
                                return;
                            conn.Disconnect();
                        }
                        catch (IOException err)
                        {
                            WriteToLog(err.ToString());
                            WriteToLog("Pipe is broken. Closing connection...");
                            conn.Disconnect();
                        }
                    }
                }
            }
            finally
            {
                sims?.FindChild<Models.Storage.IDataStore>()?.Close();
            }
        }

        private ICommand GetCommand(IConnectionManager connection)
        {
            WriteToLog("Waiting for commands...");
            return connection.WaitForCommand();
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
                return new NetworkSocketConnection(options.Verbose, options.IPAddress, options.Port, options.Backlog, protocol);
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

        /// <summary>
        /// Cleanup the simulations, disconnect events, etc.
        /// </summary>
        public virtual void Dispose()
        {
            jobRunner.Dispose();
        }
    }
}
