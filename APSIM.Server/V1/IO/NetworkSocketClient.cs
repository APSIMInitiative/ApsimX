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
    /// This class serves as a client which can send commands over a network.
    /// </summary>
    public class NetworkSocketClient : ICommandSink, IDisposable
    {
        private bool verbose;
        private Socket client;
        private NetworkStream connection;
        private ICommandManager comms;
        private Protocol connectionType;

        /// <summary>
        /// Create a new <see cref="LocalSocketConnection" /> instance.
        /// </summary>
        /// <param name="verbose">Print verbose diagnostics to stdout?</param>
        /// <param name="ipAddress">IP Address on which to listen for connections.</param>
        /// <param name="port">Port on which to listen for connections.</param>
        /// <param name="protocol">Communications protocol.</param>
        public NetworkSocketClient(bool verbose, string ipAddress, uint port, Protocol protocol)
        {
            if (port > int.MaxValue)
                throw new ArgumentOutOfRangeException($"Cannot listen on port {port} (port number is too high)");
            this.verbose = verbose;
            this.connectionType = protocol;

            if (string.IsNullOrWhiteSpace(ipAddress))
                ipAddress = "127.0.0.1";

            IPAddress address = IPAddress.Parse(ipAddress);
            IPEndPoint endpoint = new IPEndPoint(address, (int)port);

            // Create a TCP socket.
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            client.Connect(endpoint);
            connection = new NetworkStream(client);
            if (protocol == Protocol.Native)
                comms = new NativeCommunicationProtocol(connection);
            else if (protocol == Protocol.Managed)
                comms = new ManagedCommunicationProtocol(connection);
            else
                throw new NotImplementedException($"Unknown protocol {protocol}");
        }

        /// <summary>
        /// Dispose of unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            connection.Dispose();
            client.Dispose();
        }

        /// <summary>
        /// Send a command to the connected client, and block until the
        /// client has acknowledged receipt of the command.
        /// </summary>
        /// <param name="command">The command to be sent.</param>
        public void SendCommand(ICommand command) => comms.SendCommand(command);

        /// <summary>
        /// Read job output.
        /// </summary>
        /// <param name="command"></param>
        public DataTable ReadOutput(ReadCommand command)
        {
            if (connectionType != Protocol.Managed)
                throw new NotImplementedException("tbi");
            return ((ManagedCommunicationProtocol)comms).ReadOutput(command);
        }
    }
}
