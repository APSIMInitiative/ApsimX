using APSIM.Shared.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Models.Core;
using System.Dynamic;
using Models;

namespace APSIM.ZMQServer.IO
{
    /// <summary>
    /// This class handles the communications protocol .
    /// </summary>
    public class InteractiveComms : ICommProtocol
    {
        private GlobalServerOptions options;

        /// <summary>
        /// Create a new <see cref="ZMQCommunicationProtocol" /> instance which uses the
        /// specified connection stream.
        /// </summary>
        /// <param name="conn"></param>
        public InteractiveComms(GlobalServerOptions _options)
        {
            options = _options;
        }

        /// <summary>
        /// Wait for a command from the connected clients.
        /// </summary>
        public void doCommands(ApsimEncapsulator apsim)
        {
            while (true)
            {
                try
                {
                    // double slash comments are not escaped - just send host/port
                    //string[] args = { "[Synchroniser].Script.Identifier = " + options.IPAddress + ":" + options.Port }; 
                    string[] args = { }; 
                    // for (int i=0; i<args.Length; i++){
                    Console.WriteLine(args);
                    apsim.Run(args);
                    apsim.WaitForStateChange();
                    if (apsim.getErrors()?.Count > 0)
                    {
                        throw new AggregateException("Simulation Error", apsim.getErrors());
                    }
                }
                catch (Exception ex)
                {
                    string msgBuf = "ERROR\n" + ex.ToString();
                    if (options.Verbose) { Console.WriteLine(msgBuf); }
                }
            }
        }
        public void Dispose()
        {
        }
    }
}
