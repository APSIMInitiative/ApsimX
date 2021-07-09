using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using APSIM.Server.Commands;
using APSIM.Shared.Utilities;
using Models.Core.Run;

namespace APSIM.Server.IO
{
    /// <summary>
    /// This class manages connections from an apsim server instance to a client over a network.
    /// </summary>
    public class NetworkSocketConnection : IConnectionManager, IDisposable
    {
        private bool verbose;
        private Socket server;
        private NetworkStream connection;
        private ICommandManager comms;

        /// <summary>
        /// Create a new <see cref="LocalSocketConnection" /> instance.
        /// </summary>
        /// <param name="verbose">Print verbose diagnostics to stdout?</param>
        /// <param name="port">Port on which to listen for connections.</param>
        public NetworkSocketConnection(bool verbose, uint port)
        {
            if (port > int.MaxValue)
                throw new ArgumentOutOfRangeException($"Cannot listen on port {port} (port number is too high)");
            this.verbose = verbose;

            IPAddress address = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint endpoint = new IPEndPoint(address, (int)port);

            // Create a TCP socket.
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            server.Bind(endpoint);

            // For now, we set max # pending connections to 1.
            // todo: this should probably be a user parameter.
            server.Listen(1);
        }

        /// <summary>
        /// Dispose of the network connection instance.
        /// </summary>
        public void Dispose()
        {
            if (connection != null)
                connection.Dispose();
            server.Dispose();
        }

        /// <summary>
        /// Wait for a client to connect (this will block until a client connects).
        /// </summary>
        public void WaitForConnection()
        {
            connection = new NetworkStream(server.Accept(), true);
            comms = new NativeCommunicationProtocol(connection);
        }

        /// <summary>
        /// Disconnect from the currently connected client.
        /// </summary>
        public void Disconnect()
        {
            // todo: check order of operations here.
            if (connection != null)
                connection.Dispose();
            if (server.Connected)
                server.Disconnect(true);
            else
                throw new InvalidOperationException("Unable to disconnect from client: no client is connected");
        }

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
