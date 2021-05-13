using System;
using APSIM.Server.Commands;

namespace APSIM.Server.IO
{
    /// <summary>
    /// An interface for a socket connection for communication between
    /// the apsim server and a client.
    /// </summary>
    public interface ISocketConnection : IDisposable
    {
        /// <summary>
        /// Wait for a client to connect.
        /// </summary>
        void WaitForConnection();
    
        /// <summary>
        /// Wait for a command from the conencted client.
        /// </summary>
        ICommand WaitForCommand();

        /// <summary>
        /// Disconnect from the currently connected client.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Called when a command finishes.
        /// The expectation is that the client will need to be signalled
        /// somehow when this occurs.
        /// </summary>
        /// <param name="error">Error details (if command failed). If command succeeded, this will be null.</param>
        void OnCommandFinished(Exception error = null);
    }
}
