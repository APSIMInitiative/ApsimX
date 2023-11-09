using APSIM.Server.Commands;
using APSIM.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using static Models.Core.Overrides;

namespace APSIM.Server.IO
{
    /// <summary>
    /// This class handles the communications protocol with a native client.
    /// It doesn't make any particular assumptions about the connection medium.
    /// </summary>
    public class NativeCommunicationProtocol : ICommandManager
    {
        /// <summary>
        /// These are the parameter types supported by the comms protocol.
        /// </summary>
        private enum ParamType
        {
            Integer = 0,
            Double = 1,
            Boolean = 2,
            Date = 3,
            String = 4,
            IntArray = 5,
            DoubleArray = 6,
        }

        private const int protocolVersionMajor = 1; // Increment every time there is a breaking protocol change
        private const int protocolVersionMinor = 0; // Increment every time there is a non-breaking protocol change, set to 0 when the major version changes

        private const string commandRun = "RUN";
        private const string commandRead = "READ";
        private const string ack = "ACK";
        private const string fin = "FIN";
        private const string commandVersion = "VERSION";
        private Stream connection;

        /// <summary>
        /// Create a new <see cref="NativeCommunicationProtocol" /> instance which uses the
        /// specified connection stream.
        /// </summary>
        /// <param name="conn"></param>
        public NativeCommunicationProtocol(Stream conn)
        {
            connection = conn;
        }

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
                else if (input == commandRead)
                {
                    SendMessage(ack);
                    return ReadReadCommand();
                }
                else if (input == commandVersion)
                {
                    SendMessage(ack);
                    SendInt(protocolVersionMajor);
                    SendInt(protocolVersionMinor);
                    SendMessage(fin);
                }
                else
                {
                    // if (verbose)
                    //     Console.WriteLine($"Message from client: {input}");
                    SendMessage(ack);
                }
            }
            return null;
        }

        /// <summary>
        /// Invoked when a command finishes running.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="error">Error encountered by the command.</param>
        public void OnCommandFinished(ICommand command, Exception error = null)
        {
            try
            {
                if (error == null)
                {
                    // Need to check that ReadCommand columns all exist so that
                    // we can send error instead of FIN if necessary.
                    if (command is ReadCommand read)
                        foreach (string param in read.Parameters)
                            if (read.Result.Columns[param] == null)
                                throw new Exception($"Column {param} does not exist in table {read.Result.TableName}");

                    // Now send FIN - command has executed successfully.
                    SendMessage(fin);

                    // In the case of READ commands, we need to send through the results.
                    if (command is ReadCommand reader)
                    {
                        ValidateResponse(ReadString(), ack);
                        foreach (string param in reader.Parameters)
                        {
                            var data = reader.Result.AsEnumerable().Select(r => r[param]).ToArray();
                            var dataType = data.GetValue(0).GetType();
                            SendString(dataType.ToString());
                            SendArray(data);
                            ValidateResponse(ReadString(), ack);
                        }
                    }
                }
                else
                    SendMessage(error.ToString());
            }
            catch (Exception err)
            {
                SendMessage(err.ToString());
                throw;
            }
        }

        /// <summary>
        /// Receive a READ command over the socket.
        /// </summary>
        /// <remarks>
        /// The protocol is:
        /// 1. Receive READ command
        /// 2. Send ACK.
        /// 3. Receive table name.
        /// 4. Send ACK.
        /// 5. Receive parameters one at a time (send ACK after each).
        /// 6. Receive FIN.
        /// 7. Send one message per parameter name received. Receive ACK after each.
        /// </remarks>
        private ICommand ReadReadCommand()
        {
            // 3. Receive table name.
            string table = ReadString();
            // 4. Send ACK.
            SendMessage(ack);
            // 5. Receive parameters one at a time (send ACK after each).
            List<string> parameters = new List<string>();
            string parameter;
            while ( (parameter = ReadString()) != null && parameter != fin)
            {
                parameters.Add(parameter);
                SendMessage(ack);
            }

            return new ReadCommand(table, parameters);
        }

        public IEnumerable<Override> ReadChanges()
        {
            List<Override> replacements = new List<Override>();

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
                replacements.Add(new Override(path, paramValue, Override.MatchTypeEnum.NameAndType));
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
                case ParamType.DoubleArray:
                    return ReadDoubleArray();
                default:
                    throw new NotImplementedException($"Unknown parameter type {type}");
            }
        }

        private void ValidateResponse(string actual, string expected)
        {
            if (!string.Equals(actual, expected))
                throw new Exception($"Expected {expected} but received {actual}");
        }

        private void SendInt(int value)
        {
            PipeUtilities.SendIntToPipe(connection, value);
        }

        private void SendMessage(string message)
        {
            SendString(message);
        }

        public void SendString(string s)
        {
            PipeUtilities.SendStringToPipe(connection, s);
        }

        public int ReadInt()
        {
            return PipeUtilities.GetIntFromPipe(connection);
        }

        public double ReadDouble()
        {
            return PipeUtilities.GetDoubleFromPipe(connection);
        }

        public double[] ReadDoubleArray()
        {
            return PipeUtilities.GetDoubleArrayFromPipe(connection);
        }

        public object ReadBool()
        {
            return PipeUtilities.GetBoolFromPipe(connection);
        }

        public DateTime ReadDate()
        {
            return PipeUtilities.GetDateFromPipe(connection);
        }

        public string ReadString()
        {
            return PipeUtilities.GetStringFromPipe(connection);
        }

        private void SendArray(Array data)
        {
            PipeUtilities.SendArrayToPipe(connection, data);
        }

        public void SendCommand(ICommand command)
        {
            throw new NotImplementedException();
        }
    }
}
