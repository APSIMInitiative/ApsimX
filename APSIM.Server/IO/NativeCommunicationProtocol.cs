using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using APSIM.Server.Commands;
using APSIM.Shared.Utilities;
using Models.Core.Run;

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

        private const string commandRun = "RUN";
        private const string commandRead = "READ";
        private const string ack = "ACK";
        private const string fin = "FIN";
        private Stream connection;

        // Assumes that we never care about the time of day, and that we won't have issues with timezone conversion changing the date.
        // We simply convert the DateTime to an ISO date string, e.g. "2022-03-04".
        private const string dateFormat = "yyyy-MM-dd";
        private static DateTimeFormatInfo dateFormatInfo = DateTimeFormatInfo.InvariantInfo;

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
                            Array data = reader.Result.AsEnumerable().Select(r => r[param]).ToArray();
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

        private void SendMessage(string message)
        {
            byte[] buffer = Encoding.Default.GetBytes(message);
            PipeUtilities.SendToPipe(connection, buffer);
        }

        public int ReadInt()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(connection);
            return BitConverter.ToInt32(buffer);
        }

        public double ReadDouble()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(connection);
            return BitConverter.ToDouble(buffer);
        }

        public double[] ReadDoubleArray()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(connection);
            const int bytesPerNumber = sizeof(double) / sizeof(byte);
            int length = buffer.Length / bytesPerNumber;
            double[] result = new double[length];
            for (int i = 0; i < length; i++)
                result[i] = BitConverter.ToDouble(new ArraySegment<byte>(buffer, i * bytesPerNumber, bytesPerNumber));
            return result;
        }

        public object ReadBool()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(connection);
            return BitConverter.ToBoolean(buffer);
        }

        public DateTime ReadDate()
        {
            return StringToDate(ReadString());
        }

        public string ReadString()
        {
            byte[] buffer = PipeUtilities.GetBytesFromPipe(connection);
            if (buffer == null)
                return null;
            return Encoding.Default.GetString(buffer);
        }

        private void SendArray(Array data)
        {
            PipeUtilities.SendToPipe(connection, GetBytes(data).ToArray());
        }

        private IEnumerable<byte> GetBytes(Array data)
        {
            if (data == null || data.Length < 1)
                return new byte[0];
            Type arrayType = data.GetValue(0).GetType();
            if (arrayType == typeof(int))
                return data.Cast<int>().SelectMany(IntBytes);
            else if (arrayType == typeof(double))
                return data.Cast<double>().SelectMany(DoubleBytes);
            else if (arrayType == typeof(bool))
                return data.Cast<bool>().SelectMany(BoolBytes);
            else if (arrayType == typeof(DateTime)) {
                return data.Cast<DateTime>().SelectMany(DateBytesWithLength);
            } else if (arrayType == typeof(string))
                return data.Cast<string>().SelectMany(StringBytesWithLength);
            else
                throw new NotImplementedException();
        }

        private IEnumerable<byte> IntBytes(int i)
        {
            return BitConverter.GetBytes(i);
        }

        private IEnumerable<byte> DoubleBytes(double d)
        {
            return BitConverter.GetBytes(d);
        }

        private IEnumerable<byte> BoolBytes(bool b)
        {
            return BitConverter.GetBytes(b);
        }

        private IEnumerable<byte> DateBytesWithLength(DateTime date)
        {
            return StringBytesWithLength(DateToString(date));
        }

        private IEnumerable<byte> StringBytes(string s)
        {
            return Encoding.Default.GetBytes(s);
        }

        private IEnumerable<byte> StringBytesWithLength(string s)
        {
            IEnumerable<byte> data = StringBytes(s);
            return IntBytes(data.Count()).Concat(data);
        }

        private string DateToString(DateTime date)
        {
            return date.ToString(dateFormat, dateFormatInfo);
        }

        private DateTime StringToDate(string str)
        {
            return DateTime.Parse(str, dateFormatInfo);
        }

        public void SendCommand(ICommand command)
        {
            throw new NotImplementedException();
        }
    }
}
