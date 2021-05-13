using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using APSIM.Server.Commands;
using APSIM.Shared.Utilities;
using Models.Core.Run;

namespace APSIM.Server.IO
{
    /// <summary>
    /// This class encapsulates the comms from an apsim server instance to a native client.
    /// </summary>
    public class NativeSocketConnection : ISocketConnection, IDisposable
    {
        private bool verbose;
        private const string commandRun = "RUN";
        private const string ack = "ACK";
        private const string fin = "FIN";

        private enum ParamType
        {
            Integer = 0,
            Double = 1,
            Boolean = 2,
            Date = 3,
            String = 4
        }

        private NamedPipeServerStream pipe;

        /// <summary>
        /// Create a new <see cref="NativeSocketConnection" /> instance.
        /// </summary>
        /// <param name="name">Name to use for the named pipe.</param>
        /// <param name="verbose">Print verbose diagnostics to stdout?</param>
        public NativeSocketConnection(string name, bool verbose)
        {
            pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1);
            this.verbose = verbose;
        }

        public void Dispose()
        {
            pipe.Dispose();
        }

        /// <summary>
        /// Wait for a client to connect.
        /// </summary>
        public void WaitForConnection() => pipe.WaitForConnection();

        /// <summary>
        /// Disconnect from the currently connected client.
        /// </summary>
        public void Disconnect() => pipe.Disconnect();

        /// <summary>
        /// Wait for a command from the conencted client.
        /// </summary>
        public ICommand WaitForCommand()
        {
            string input;
            while ( (input = ReadString()) != null)
            {
                if (input == commandRun)
                {
                    SendMessage(ack);
                    ICommand result = new RunCommand(ReadChanges());
                    return result;
                }
                else
                {
                    if (verbose)
                        Console.WriteLine($"Message from client: {input}");
                    SendMessage(ack);
                }
            }
            return null;
        }

        public void OnCommandFinished(Exception error = null)
        {
            if (error == null)
                SendMessage(fin);
            else
                SendMessage(error.ToString());
        }

        public IEnumerable<IReplacement> ReadChanges()
        {
            List<IReplacement> replacements = new List<IReplacement>();

            // For now, we assume the same parameter changes are applied to all simulations.
            object input;
            while ( (input = ReadString()) != null && input.ToString() != fin)
            {
                string path = (string)input;
                SendMessage(ack);
                
                int parameterType = ReadInt();
                SendMessage(ack);
                object paramValue = ReadParameter((ParamType)parameterType);
                SendMessage(ack);
                if (paramValue == null)
                    throw new NullReferenceException("paramValue is null");
                replacements.Add(new PropertyReplacement(path, paramValue));
            }
            SendMessage(ack);

            return replacements;
        }

        private object ReadParameter(ParamType type)
        {
            switch (type)
            {
                case ParamType.Integer:
                    return ReadInt();
                case ParamType.Double:
                    return ReadDouble();
                case ParamType.Boolean:
                    return ReadBool();
                case ParamType.Date:
                    return ReadDate();
                case ParamType.String:
                    return ReadString();
                default:
                    throw new NotImplementedException($"Unknown parameter type {type}");
            }
        }

        public void SendMessage(string message)
        {
            byte[] buffer = Encoding.Default.GetBytes(message);
            PipeUtilities.SendObjectToPipe(pipe, buffer);
        }

        public int ReadInt()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(pipe);
            return BitConverter.ToInt32(buffer);
        }

        public double ReadDouble()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(pipe);
            return BitConverter.ToDouble(buffer);
        }

        public object ReadBool()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(pipe);
            return BitConverter.ToBoolean(buffer);
        }

        public object ReadDate()
        {
            // tbi - need to give this one some thought.
            throw new NotImplementedException();
        }
    
        public string ReadString()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(pipe);
            if (buffer == null)
                return null;
            return Encoding.Default.GetString(buffer);
        }
    }
}
