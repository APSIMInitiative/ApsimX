using System;
using System.Collections.Generic;
using System.Data;
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
    /// This class handles the communications protocol with a managed client.
    /// It doesn't make any particular assumptions about the connection medium.
    /// </summary>
    public class ManagedCommunicationProtocol : ICommandManager
    {
        private Stream stream;
        private const string ack = "ACK_MANAGED";
        private const string fin = "FIN_MANAGED";

        /// <summary>
        /// Create a <see cref="ManagedCommunicationProtocol"/> instance.
        /// </summary>
        /// <param name="stream">The connection stream.</param>
        public ManagedCommunicationProtocol(Stream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Send a command to the connected client, and block until the
        /// client has acknowledged receipt of the command.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        public void SendCommand(ICommand command)
        {
            PipeUtilities.SendObjectToPipe(stream, command);

            // Server will send through ACK upon receipt of the command.
            object resp = Read();
            if (!(resp is string msg) || msg != ack)
                throw new Exception($"Unexpected response from server after sending command. Expected {ack}, got {resp}");

            // Server will send through another response upon completion of the command.
            // If command is a RUN command, this will just be a simple FIN.
            // If command is a READ command, this will be a DataTable.
            // todo: work out best way to approach these different cases.
            // for now, I'm just going to discard the result.
            object finResp = Read();
            if (finResp == null)
                throw new Exception($"Received null response from server upon job completion");
            if (finResp is Exception err)
                throw new Exception($"Command {command} ran with errors", err);
            if (command is RunCommand && (finResp as string) != fin)
                throw new Exception($"Unexpected response from server. Expected {fin}, got {finResp}");
            if (command is ReadCommand)
            {
                DataTable table = finResp as DataTable;
                if (table == null)
                    throw new Exception($"Unexpected response from server upon job completion. Expected DataTable, got {finResp}");
                Console.WriteLine($"Received table with {table.Columns.Count} columns and {table.Rows.Count} rows");
            }
        }

        /// <summary>
        /// Wait for a command from the conencted client.
        /// </summary>
        public ICommand WaitForCommand()
        {
            object resp = Read();
            PipeUtilities.SendObjectToPipe(stream, ack);
            if (resp is ICommand command)
                return command;
            if (resp is Exception exception)
                // fixme - could be another sort of error
                throw new Exception("Received exception while waiting for a command", exception);
            if (resp is string message)
            {
                // We've received a string (message); dump the message to stdout,
                // then wait for the next input. This shouldn't really happen,
                // but I'm going to leave this in here for now.
                // todo: should we send a response? Usually the other end will wait
                // for some sort of response after sending something...
                Console.WriteLine($"Received message: {message}");
                return WaitForCommand();
            }
            throw new Exception($"Unexpected input from pipe; expected a command, but got {resp}");
        }

        /// <summary>
        /// Called when a command finishes.
        /// The expectation is that the client will need to be signalled
        /// somehow when this occurs.
        /// </summary>
        /// <param name="command">The command that was run.</param>
        /// <param name="error">Error details (if command failed). If command succeeded, this will be null.</param>
        public void OnCommandFinished(ICommand command, Exception error = null)
        {
            if (error == null)
            {
                if (command is ReadCommand reader)
                {
                    foreach (string param in reader.Parameters)
                    {
                        if (reader.Result.Columns[param] == null)
                            throw new Exception($"Columns {param} does not exist in table {reader.Result.TableName}");
                        Array data = reader.Result.AsEnumerable().Select(r => r[param]).ToArray();
                        PipeUtilities.SendObjectToPipe(stream, data);
                    }
                }
                else
                    PipeUtilities.SendObjectToPipe(stream, fin);
            }
            else
                PipeUtilities.SendObjectToPipe(stream, error);
        }

        /// <summary>
        /// Read an object from the underlying stream.
        /// </summary>
        public object Read()
        {
            return PipeUtilities.GetObjectFromPipe(stream);
        }
    }
}
