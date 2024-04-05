using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using APSIM.ZMQServer.IO;
using APSIM.Shared.Utilities;
using Models.Core;
using Models.Core.ApsimFile;
using Models.Core.Run;
using NetMQ;
using NetMQ.Sockets;

namespace APSIM.ZMQServer
{
    /// <summary>
    /// An APSIM Server.
    /// </summary>
    public class ApsimZMQServer
    {
        /// <summary>
        /// Server options.
        /// </summary>
        protected GlobalServerOptions options;

        protected ICommProtocol conn;

        protected ApsimEncapsulator apsimBlob;

        private RequestSocket connection = null;

        private string Identifier { get; set; }

        /// <summary>
        /// Create an <see cref="ApsimServer" /> instance.
        /// </summary>
        /// <param name="file">.apsimx file to be run.</param>
        public ApsimZMQServer(GlobalServerOptions options)
        {
            // store copy of options
            this.options = options;
            
            // open zmq connections
            Identifier = string.Format("tcp://{0}:{1}", options.IPAddress, options.Port);
            connection = new RequestSocket(Identifier);
            connection.SendFrame("connect");
            Console.WriteLine("Sent connect");
            var msg = connection.ReceiveFrameString();
            if (msg != "ok") { throw new Exception("Expected ok"); }

            // create the encapsulator
            apsimBlob = new ApsimEncapsulator(options);
        }

        protected ApsimZMQServer() { }

        /// <summary>
        /// Run the apsim server. This will block the calling thread.
        /// </summary>
        public virtual void Run()
        {
            try
            {
                if (options.Protocol == "oneshot")
                    conn = new OneshotComms(options);
                else if (options.Protocol == "interactive")
                    conn = new InteractiveComms(options);
                else
                    throw new Exception("Unknown comms protocol '" + options.Protocol + "'");
                conn.doCommands(apsimBlob);
            }
            catch (IOException)
            {
                // Broken pipe is handled further down.
                throw;
            }
            catch (Exception error)
            {
                // Other exceptions will usually be triggered by a
                // problem executing the command. This shouldn't cause
                // the server to crash.
                if (options.Verbose) Console.WriteLine("server ran with errors:\n" + error.ToString());
            }
            finally
            {
                apsimBlob.Close();
                conn.Dispose();
            }
        }
    }
}
