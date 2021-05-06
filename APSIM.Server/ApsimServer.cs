using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using APSIM.Server.Cli;
using APSIM.Server.Commands;
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
            sims = FileFormat.ReadFromFile<Simulations>(options.File, out List<Exception> errors);
            if (errors != null && errors.Count > 0)
                throw new Exception($"Unable to read file {options.File}", errors[0]);
            runner = new Runner(sims);
            jobRunner = new ServerJobRunner();
            runner.UseRunner(jobRunner);
        }

        /// <summary>
        /// Run the apsim server. This will block the calling thread.
        /// </summary>
        public void Run()
        {
            if (options.Verbose)
                Console.WriteLine($"Starting server...");
            using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("testpipe", PipeDirection.InOut, 1))
            {
                while (true)
                {
                    if (options.Verbose)
                        Console.WriteLine("Waiting for connections...");
                    pipeServer.WaitForConnection();
                    if (options.Verbose)
                        Console.WriteLine("Client connected to server.");
                    object response;
                    while ( (response = ReadFromPipe(pipeServer)) != null)
                    {
                        if (response is string message)
                        {
                            Console.WriteLine($"Message from client: '{message}'");
                            SendToPipe(pipeServer, "Hello there");
                        }
                        else if (response is ICommand command)
                        {
                            if (options.Verbose)
                                Console.WriteLine($"Received command {command.GetType().Name}. Running command...");
                            try
                            {
                                // Clone the simulations object before running the command.
                                var timer = Stopwatch.StartNew();
                                command.Run(runner, jobRunner);
                                timer.Stop();
                                if (options.Verbose)
                                    Console.WriteLine($"Command ran in {timer.ElapsedMilliseconds}ms");
                                SendToPipe(pipeServer, "fin");
                            }
                            catch (Exception err)
                            {
                                if (options.Verbose)
                                    Console.Error.WriteLine(err);
                                SendToPipe(pipeServer, err);
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine($"Unknown response {response.GetType()}: {response}");
                            break;
                        }
                    }
                    if (options.Verbose)
                        Console.WriteLine($"Connection closed by client.");

                    // If we don't want to keep the server alive we can exit now.
                    // Otherwise we will go back and wait for another connection.
                    if (!options.KeepAlive)
                        return;
                    pipeServer.Disconnect();
                }
            }
        }

        private void SendToPipe(NamedPipeServerStream pipe, object message)
        {
            byte[] buffer;
            if (options.Mode == CommunicationMode.Managed)
            {
                buffer = ((MemoryStream)ReflectionUtilities.BinarySerialise(message)).ToArray();
            }
            else if (options.Mode == CommunicationMode.Native)
            {
                buffer = Encoding.Default.GetBytes(message.ToString());
            }
            else
                throw new NotImplementedException();
            PipeUtilities.SendObjectToPipe(pipe, buffer);
        }

        private object ReadFromPipe(NamedPipeServerStream pipe)
        {
            byte[] buffer = PipeUtilities.GetObjectFromPipe(pipe);
            // Convert bytes to object.
            if (options.Mode == CommunicationMode.Managed)
            {
                return ReflectionUtilities.BinaryDeserialise(new MemoryStream(buffer));
            }
            else if (options.Mode == CommunicationMode.Native)
            {
                if (buffer == null)
                    return null;
                return Encoding.Default.GetString(buffer);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
