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

        /// <summary>
        /// Create an <see cref="ApsimServer" /> instance.
        /// </summary>
        /// <param name="file">.apsimx file to be run.</param>
        public ApsimZMQServer(GlobalServerOptions options)
        {
            this.options = options;
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
