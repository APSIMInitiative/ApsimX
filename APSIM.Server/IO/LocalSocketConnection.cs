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
    public class LocalSocketConnection : IConnectionManager, IDisposable
    {
        private bool verbose;

        private NamedPipeServerStream pipe;
        private ICommandManager comms;

        /// <summary>
        /// Create a new <see cref="LocalSocketConnection" /> instance.
        /// </summary>
        /// <param name="name">Name to use for the named pipe.</param>
        /// <param name="verbose">Print verbose diagnostics to stdout?</param>
        public LocalSocketConnection(string name, bool verbose)
        {
            pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1);
            comms = new NativeCommunicationProtocol(pipe);
            this.verbose = verbose;
        }

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
    }
}
