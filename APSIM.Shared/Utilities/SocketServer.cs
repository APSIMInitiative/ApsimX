// -----------------------------------------------------------------------
// <copyright file="SocketServer.cs" company="APSIM Initiative">
//     Copyright (c) APSIM Initiative
// </copyright>
//-----------------------------------------------------------------------
namespace APSIM.Shared.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// An asynchronous socket server based on the MicroSoft one here:
    ///     https://msdn.microsoft.com/en-us/library/fx6588te(v=vs.110).aspx
    /// </summary>
    public class SocketServer
    {
        /// <summary>A container of commands</summary>
        private Dictionary<string, EventHandler<CommandArgs>> commands = new Dictionary<string, EventHandler<CommandArgs>>();

        /// <summary>Should the server keep listening for socket connections?</summary>
        private bool keepListening = true;

        /// <summary>Has the server stopped listening?</summary>
        private bool stoppedListening = false;

        /// <summary>Error event argument class.</summary>
        public class ErrorArgs : EventArgs
        {
            /// <summary>Error message.</summary>
            public string message;
        }

        /// <summary>Invoked when an error occurs.</summary>
        public event EventHandler<ErrorArgs> Error;


        /// <summary>Argument class passed to command handler.</summary>
        public class CommandArgs : EventArgs
        {
            /// <summary>The currently open socket. Can be used to send back command to client.</summary>
            public Socket socket;

            /// <summary>The object going with the command.</summary>
            public object obj;
        }

        /// <summary>Command object that clients send to server.</summary>
        [Serializable]
        public class CommandObject
        {
            /// <summary>Name of comamnd</summary>
            public string name;

            /// <summary>An optional object to go with the command.</summary>
            public object data;
        }

        /// <summary>Add a new command.</summary>
        /// <param name="commandName">The name of the command.</param>
        /// <param name="handler">The handler name.</param>
        public void AddCommand(string commandName, EventHandler<CommandArgs> handler)
        {
            commands.Add(commandName, handler);
        }

        /// <summary>Start listening for socket connections</summary>
        /// <param name="portNumber">Port number to listen on.</param>
        public void StartListening(int portNumber)
        {
            try
            {
                keepListening = true;

                // Establish the local endpoint for the socket.
                IPAddress ipAddress = IPAddress.Parse("127.0.0.1");
                IPEndPoint localEndPoint = new IPEndPoint(ipAddress, portNumber);

                // Create a TCP/IP socket.
                using (Socket ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Bind the socket to the local endpoint and listen for incoming connections.
                    ServerSocket.Bind(localEndPoint);
                    ServerSocket.Listen(1000);

                    while (keepListening)
                    {
                        Socket clientSocket = ServerSocket.Accept();
                        if (keepListening)
                            Task.Run(() => ProcessClient(clientSocket));
                        else
                            Send(clientSocket, "OK");
                    }
                }
            }
            catch (Exception err)
            {
                if (Error != null)
                    Error.Invoke(this, new ErrorArgs() { message = err.ToString() });
            }
            stoppedListening = true;
        }

        /// <summary>Stop listening for socket connections</summary>
        public void StopListening()
        {
            keepListening = false;
            // Open a socket connection with dummy data (0) so that the ServerSocket.Accept
            // method in 'StartListening' method will return an then the method exits cleanly.
            Send("127.0.0.1", 2222, 0);
            SpinWait.SpinUntil(() => stoppedListening);
        }

        /// <summary>Accept a socket connection</summary>
        /// <param name="obj">Socket parameters.</param>
        public void ProcessClient(object obj)
        {
            try
            {
                using (Socket clientSocket = obj as Socket)
                {
                    byte[] bytes = new byte[16384];
                    if (keepListening)
                    {
                        // Receive data from client
                        MemoryStream s = new MemoryStream();
                        int totalNumBytes = 0;
                        int numBytesExpected = 0;
                        bool allDone = false;
                        do
                        {
                            int NumBytesRead = clientSocket.Receive(bytes, SocketFlags.None);
                            s.Write(bytes, 0, NumBytesRead);
                            totalNumBytes += NumBytesRead;

                            if (numBytesExpected == 0 && totalNumBytes > 4)
                                numBytesExpected = BitConverter.ToInt32(bytes, 0);
                            if (numBytesExpected + 4 == totalNumBytes)
                                allDone = true;
                        }
                        while (!allDone);

                        // All done process command.
                        ProcessCommand(DecodeData(s.ToArray()), clientSocket);
                    }
                }
            }
            catch (Exception err)
            {
                if (Error != null)
                    Error.Invoke(this, new ErrorArgs() { message = err.ToString() });
            }
        }

        /// <summary>Process the command</summary>
        /// <param name="obj"></param>
        /// <param name="socket">The socket currently open.</param>
        private void ProcessCommand(object obj, Socket socket)
        {
            CommandObject command = obj as CommandObject;
            if (!commands.ContainsKey(command.name))
                throw new Exception("Cannot find a handler for command: " + command.name);
            CommandArgs args = new CommandArgs();
            args.socket = socket;
            args.obj = command.data;
            commands[command.name].Invoke(this, args);
        }

        /// <summary>Send data through socket.</summary>
        /// <param name="socket">The socket.</param>
        /// <param name="obj">Object to send</param>
        public void Send(Socket socket, object obj)
        {
            socket.Send(EncodeData(obj));
        }

        /// <summary>Encode the object into a series of bytes</summary>
        /// <param name="o">The object to encode</param>
        /// <returns>The encoded object as a byte array.</returns>
        public static byte[] EncodeData(object o)
        {
            MemoryStream memStream = ReflectionUtilities.BinarySerialise(o) as MemoryStream;
            byte[] bytes = new byte[memStream.Length + 4];
            BitConverter.GetBytes((int)memStream.Length).CopyTo(bytes, 0);
            memStream.ToArray().CopyTo(bytes, 4);
            return bytes;
        }

        /// <summary>Decode a byte array into an object.</summary>
        /// <param name="bytes">The byte array.</param>
        /// <returns>The newly created object</returns>
        public static object DecodeData(byte[] bytes)
        {
            MemoryStream memStream = new MemoryStream(bytes);
            memStream.Seek(4, SeekOrigin.Begin);
            return ReflectionUtilities.BinaryDeserialise(memStream);
        }

        /////////////////////////////////////////////////////////////////////////
        // The following method is useful for socket client applications
        /////////////////////////////////////////////////////////////////////////


        /// <summary>
        /// Send an object to the socket server, wait for a response and return the
        /// response as an object.
        /// </summary>
        /// <param name="serverName">The server name.</param>
        /// <param name="port">The port number.</param>
        /// <param name="obj">The object to send.</param>
        public static object Send(string serverName, int port, object obj)
        {
            using (TcpClient server = new TcpClient(serverName, Convert.ToInt32(port, CultureInfo.InvariantCulture)))
            {
                MemoryStream s = new MemoryStream();
                //do
                {
                    Byte[] bData = EncodeData(obj);
                    server.GetStream().Write(bData, 0, bData.Length);
                    Byte[] bytes = new Byte[65536];

                    // Loop to receive all the data sent by the client.
                    int numBytesExpected = 0;
                    int totalNumBytes = 0;
                    int i = 0;
                    int NumBytesRead;
                    bool allDone = false;
                    do
                    {
                        NumBytesRead = server.GetStream().Read(bytes, 0, bytes.Length);
                        s.Write(bytes, 0, NumBytesRead);
                        totalNumBytes += NumBytesRead;

                        if (numBytesExpected == 0 && totalNumBytes > 4)
                            numBytesExpected = BitConverter.ToInt32(bytes, 0);
                        if (numBytesExpected + 4 == totalNumBytes)
                            allDone = true;

                        i++;
                    }
                    while (NumBytesRead > 0 && !allDone);
                }
                //while (s.Length == 0);

                // Decode the bytes and return.
                if (s.Length > 0)
                    return DecodeData(s.ToArray());
                else
                    return null;
            }
        }


    }
}
