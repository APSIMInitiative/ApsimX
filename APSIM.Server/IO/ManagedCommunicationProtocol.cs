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

        /// <summary>
        /// Create a <see cref="ManagedCommunicationProtocol"/> instance.
        /// </summary>
        /// <param name="stream">The connection stream.</param>
        public ManagedCommunicationProtocol(Stream stream)
        {
            this.stream = stream;
        }

        /// <summary>
        /// Send a command to the connected client.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        public void SendCommand(ICommand command)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Wait for a command from the conencted client.
        /// </summary>
        public ICommand WaitForCommand()
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
