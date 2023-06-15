using APSIM.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Models.Core;

namespace APSIM.ZMQServer.IO
{
    /// <summary>
    /// This class handles the communications protocol .
    /// </summary>
    public class ZMQComms : IDisposable
    {
        private const int protocolVersionMajor = 2; // Increment every time there is a breaking protocol change
        private const int protocolVersionMinor = 0; // Increment every time there is a non-breaking protocol change, set to 0 when the major version changes

        private const string commandState = "STATE";
        private const string commandRun = "RUN";
        private const string commandGetDataStore = "GET";
        private const string commandGetVariable = "GET2";
        private const string commandSet = "SET";
        private const string commandVersion = "VERSION";
        private ResponseSocket connection;

        private bool verbose;

        /// <summary>
        /// Create a new <see cref="ZMQCommunicationProtocol" /> instance which uses the
        /// specified connection stream.
        /// </summary>
        /// <param name="conn"></param>
        public ZMQComms(bool verbosity, string ipAddress, uint port)
        {
            // Create a TCP socket.
            connection = new ResponseSocket();
            connection.Bind("tcp://" + ipAddress + ":" + port.ToString());

            verbose = verbosity;
            Console.WriteLine($"Starting server on {connection.Options.LastEndpoint.ToString()}");
        }

        /// <summary>
        /// Wait for a command from the connected client.
        /// </summary>
        public void doCommands(ApsimEncapsulator apsim)
        {
            while (true)
            {
                var msg = connection.ReceiveMultipartMessage();
                try
                {
                    if (verbose) { Console.WriteLine($"Got multipart message with {msg.FrameCount} frames"); }
                    if (msg.FrameCount <= 0) { continue; }
                    var command = msg[0].ConvertToString();
                    if (verbose) { Console.WriteLine($"Command from client: {command}"); }
                    if (command == commandState)
                    {
                        SendString(apsim.runState.ToString());
                    }
                    else if (command == commandRun)
                    {
                        if (apsim.runState == ApsimEncapsulator.runStateT.idling)
                        {
                            var args = msg.FrameCount > 1 ? msg[1].ConvertToString().Split("\n") : null;
                            apsim.Run(args);
                            apsim.WaitForStateChange();
                            if (apsim.getErrors()?.Count > 0) {
                                throw new AggregateException("Simulation Error", apsim.getErrors());
                            }
                            SendString(apsim.runState.ToString());
                        } else if (apsim.runState == ApsimEncapsulator.runStateT.waiting) {
                            apsim.Proceed();
                            SendString(apsim.runState.ToString());
                        } else {
                            throw new Exception("Already running");
                        }
                    }
                    else if (command == commandGetVariable)
                    {
                        if (msg.FrameCount < 2) { throw new Exception($"Malformed GET: {msg.FrameCount} args"); }
                        byte[] result = apsim.getVariableFromModel(msg[1].ConvertToString());
                        SendBytes(result);
                    }
                    else if (command == commandGetDataStore)
                    {
                        if (msg.FrameCount < 2) { throw new Exception($"Malformed GET: {msg.FrameCount} args"); }
                        if (verbose) { Console.WriteLine("get from DS=" + msg[1].ConvertToString()); }
                        byte[] result = apsim.getVariableFromDS(msg[1].ConvertToString());
                        SendBytes(result);
                    }
                    else if (command == commandSet)
                    {
                        if (msg.FrameCount < 2) { throw new Exception($"Malformed SET: {msg.FrameCount} args"); }
                        apsim.setVariable(msg[1].ConvertToString().Split("\n"));
                        SendString("OK");
                    }
                    else if (command == commandVersion)
                    {
                        SendString(protocolVersionMajor.ToString() + "." + protocolVersionMinor.ToString());
                    }
                    else
                    {
                        throw new Exception($"Unknown command from client: '{command}'");
                    }
                }
                catch (Exception e)
                {
                    string msgBuf = "ERROR\n" + e.ToString();
                    if (verbose) {Console.WriteLine(msgBuf);}
                    SendString(msgBuf);
                }
            }
        }

        private void SendString(string s)
        {
            connection.SendFrame(s);
        }

        private void SendBytes(byte[] bytes)
        {
            connection.SendFrame(bytes);
        }

        public void Dispose()
        {
            connection?.Dispose();
        }
    }
}
