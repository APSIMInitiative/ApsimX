using System;
using APSIM.Server.Commands;

namespace APSIM.Server.IO
{
    /// <summary>
    /// An interface for a socket connection for communication between
    /// the apsim server and a client. Implementators are responsible
    /// for managing a connection - connecting/disconnecting to clients,
    /// receiving commands, etc.
    /// </summary>
    public interface IConnectionManager : ICommandManager, IDisposable
    {
        /// <summary>
        /// Wait for a client to connect.
        /// </summary>
        void WaitForConnection();

        /// <summary>
        /// Disconnect from the currently connected client.
        /// </summary>
        void Disconnect();
    }
}
