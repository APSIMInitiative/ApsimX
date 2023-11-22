using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace APSIM.Server.Cli
{
    /// <summary>
    /// Global command-line options for the apsim-server CLI.
    /// </summary>
    /// <remarks>
    /// fixme: need to revisit this
    /// </remarks>
    [Verb("listen", isDefault: true)]
    public class GlobalServerOptions
    {
        /// <summary>.apsimx file to hold in memory.</summary>
        [Option('f', "file", HelpText = ".apsimx file to hold in memory.", Required = true)]
        public string File { get; set; }

        /// <summary>Display verbose debugging info.</summary>
        [Option('v', "verbose", HelpText = "Display verbose debugging info")]
        public bool Verbose { get; set; }

        /// <summary>Keep the server alive after client disconnects.</summary>
        [Option('k', "keep-alive", HelpText = "Keep the server alive after client disconnects")]
        public bool KeepAlive { get; set; }

        /// <summary>Expect communications from a managed local client.</summary>
        [Option('m', "managed", HelpText = "Expect communications from a managed client")]
        public bool ManagedMode { get; set; }

        /// <summary>Expect communications from a native local client.</summary>
        [Option('n', "native", HelpText = "Expect communications from a native client")]
        public bool NativeMode { get; set; }

        /// <summary>Expect connections from a local client.</summary>
        [Option('l', "local", HelpText = "Expect connections from a local client")]
        public bool LocalMode { get; set; }

        /// <summary>Expect connections from a remote client.</summary>
        [Option('r', "remote", HelpText = "Expect connections from a remote client")]
        public bool RemoteMode { get; set; }

        /// <summary>Socket name. Only used when running in local mode.</summary>
        [Option('s', "socket-name", HelpText = "Socket name. Only used when running in local mode (--local)", Default = "testpipe")]
        public string SocketName { get; set; }

        /// <summary>Port number on which to listen for connections.</summary>
        [Option('p', "port", HelpText = "Port number on which to listen for connections. Only used when accepting connections over network", Default = 27746u)]
        public uint Port { get; set; }

        /// <summary>Maximum number of pending connections to allow.</summary>
        [Option('b', "backlog", HelpText = "Maximum number of pending connections to allow.", Default = (ushort)1)]
        public ushort Backlog { get; set; }

        /// <summary>IP Address on which to listen for connections.</summary>
        /// <remarks>If not set, will listen on 0.0.0.0.</remarks>
        [Option('a', "address", HelpText = "IP Address on which to listen for connections. Only used when accepting connections over network", Default = "0.0.0.0")]
        public string IPAddress { get; set; }

        /// <summary>
        /// Concrete examples shown in help text.
        /// </summary>
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal usage", new GlobalServerOptions() { File = "file.apsimx", ManagedMode = true });
                yield return new Example("Comms with a local native client (e.g. R program running on local computer)", new GlobalServerOptions() { File = "file.apsimx", NativeMode = true, LocalMode = true });
                yield return new Example("Comms with a native client over the network", new GlobalServerOptions() { File = "file.apsimx", NativeMode = true, RemoteMode = true });
            }
        }
    }
}
