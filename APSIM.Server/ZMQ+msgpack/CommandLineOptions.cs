using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace APSIM.ZMQServer
{
    /// <summary>
    /// Global command-line options 
    /// </summary>
    [Verb("listen", isDefault: true)]
    public class GlobalServerOptions
    {
        /// <summary>.apsimx file to hold in memory.</summary>
        [Option('P', "protocol", HelpText = "Control protocol to use - interactive or oneshot.", Default = "oneshot")]
        public string Protocol { get; set; }

        /// <summary>.apsimx file to hold in memory.</summary>
        [Option('f', "file", HelpText = ".apsimx file to hold in memory.", Required = true)]
        public string File { get; set; }

        /// <summary>Display verbose debugging info.</summary>
        [Option('v', "verbose", HelpText = "Display verbose debugging info")]
        public bool Verbose { get; set; }

        /// <summary>Port number on which to listen for connections.</summary>
        [Option('p', "port", HelpText = "Port number on which to listen for connections. 0 = choose ephemeral port", Default = 27746u)]
        public uint Port { get; set; }

        /// <summary>IP Address on which to listen for connections.</summary>
        /// <remarks>If not set, will listen on 0.0.0.0.</remarks>
        [Option('a', "address", HelpText = "IP Address on which to listen/connect to.", Default = "0.0.0.0")]
        public string IPAddress { get; set; }

        /// <summary>Number of vCPUs per worker node.</summary>
        [Option('c', "cpu-count", HelpText = "Number of vCPUs per worker node", Default = 1u)]
        public uint WorkerCpuCount { get; set; }
    }
}
