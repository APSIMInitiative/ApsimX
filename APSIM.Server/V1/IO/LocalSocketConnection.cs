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
    /// This class encapsulates the comms from an apsim server instance to a native client.
    /// </summary>
    public class LocalSocketConnection : IConnectionManager, ICommandManager, IDisposable
    {
        private bool verbose;

        private NamedPipeServerStream pipe;
        private ICommandManager comms;

        /// <summary>
        /// Create a new <see cref="LocalSocketConnection" /> instance.
        /// </summary>
        /// <param name="name">Name to use for the named pipe.</param>
        /// <param name="verbose">Print verbose diagnostics to stdout?</param>
        /// <param name="protocol">The communciations protocol.</param>
        public LocalSocketConnection(string name, bool verbose, Protocol protocol)
        {
            pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1);
            this.verbose = verbose;

            if (protocol == Protocol.Native)
                comms = new NativeCommunicationProtocol(pipe);
            else if (protocol == Protocol.Managed)
                comms = new ManagedCommunicationProtocol(pipe);
            else
                throw new NotImplementedException($"Unknown protocol type {protocol}");
        }

        /// <summary>
        /// Dispose of unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            comms = null;
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
        public ICommand WaitForCommand() => comms.WaitForCommand();

        /// <summary>
        /// Invoked when a command finishes running.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="error">Error encountered by the command.</param>
        public void OnCommandFinished(ICommand command, Exception error = null) => comms.OnCommandFinished(command, error);

        /// <summary>
        /// Send a command to the connected client, and block until the
        /// client has acknowledged receipt of the command.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        public void SendCommand(ICommand command) => comms.SendCommand(command);
    }
}
