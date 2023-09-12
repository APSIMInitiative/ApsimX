using APSIM.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Models.Core;
using System.Dynamic;


namespace APSIM.ZMQServer.IO
{
    public interface ICommProtocol : IDisposable
    {
        public void doCommands(ApsimEncapsulator e);
    }

    /// <summary>
    /// This class handles the communications protocol .
    /// </summary>
    public class OneshotComms : ICommProtocol
    {
        private const int protocolVersionMajor = 2; // Increment every time there is a breaking protocol change
        private const int protocolVersionMinor = 0; // Increment every time there is a non-breaking protocol change, set to 0 when the major version changes

        private const string commandState = "STATE";
        private const string commandRun = "RUN";
        private const string commandGetDataStore = "GET";
        private const string commandVersion = "VERSION";
        private ResponseSocket conn;

        private bool verbose;

        /// <summary>
        /// Create a new <see cref="ZMQCommunicationProtocol" /> instance which uses the
        /// specified connection stream.
        /// </summary>
        /// <param name="conn"></param>
        public OneshotComms(GlobalServerOptions options)
        {
            verbose = options.Verbose;

            // Create a TCP socket.
            conn = new ResponseSocket("@tcp://" + options.IPAddress + ":" + options.Port.ToString());
            if (verbose) { Console.WriteLine($"Started external server on {conn.Options.LastEndpoint.ToString()}"); }
        }

        /// <summary>
        /// Wait for a command from the connected clients.
        /// </summary>
        public void doCommands(ApsimEncapsulator apsim)
        {
            while (true)
            {
                var msg = conn.ReceiveMultipartMessage();
                try
                {
                    if (verbose) { Console.WriteLine($"Got multipart message with {msg.FrameCount} frames"); }
                    if (msg.FrameCount <= 0) { continue; }
                    var command = msg[0].ConvertToString();
                    if (verbose) { Console.WriteLine($"Command from client: {command}"); }
                    if (command == commandState)
                    {
                        conn.SendFrame("idle");
                    }
                    else if (command == commandRun)
                    {
                        var args = msg.FrameCount > 1 ? msg[1].ConvertToString().Split("\n") : null;
                        apsim.Run(args);
                        apsim.WaitForStateChange();
                        if (apsim.getErrors()?.Count > 0)
                        {
                            throw new AggregateException("Simulation Error", apsim.getErrors());
                        }
                        conn.SendFrame("ok");
                    }
                    else if (command == commandGetDataStore)
                    {
                        if (msg.FrameCount < 2) { throw new Exception($"Malformed GET: {msg.FrameCount} args"); }
                        if (verbose) { Console.WriteLine("get from DS=" + msg[1].ConvertToString()); }
                        byte[] result = apsim.getVariableFromDS(msg[1].ConvertToString());
                        conn.SendFrame(result);
                    }
                    else if (command == commandVersion)
                    {
                        conn.SendFrame(protocolVersionMajor.ToString() + "." + protocolVersionMinor.ToString());
                    }
                    else
                    {
                        throw new Exception($"Unknown command from client: '{command}'");
                    }
                }
                catch (Exception e)
                {
                    string msgBuf = "ERROR\n" + e.ToString();
                    if (verbose) { Console.WriteLine(msgBuf); }
                    conn.SendFrame(msgBuf);
                }
            }
        }

        public void Dispose()
        {
            conn?.Close();
        }
    }
}
