using System;
using System.Collections.Generic;
using CommandLine;
using CommandLine.Text;

namespace APSIM.Server.Cli
{
    /// <summary>
    /// Command-line options for Models.exe.
    /// </summary>
    public class ServerOptions
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

        /// <summary>This determines how data is sent/received over the socket.</summary>
        /// <remarks>
        /// When in managed mode, sent objects will be serialised. When in native mode, sent
        /// objects will be converted to a binary array. Actually, this kind of needs more thought.
        /// </remarks>
        public CommunicationMode Mode
        {
            get
            {
                if (ManagedMode)
                    return CommunicationMode.Managed;
                if (NativeMode)
                    return CommunicationMode.Native;
                if (NetworkMode)
                    return CommunicationMode.Network;
                throw new NotImplementedException();
            }
        }

        /// <summary>Expect communications from a managed local client.</summary>
        [Option('m', "managed", SetName = "comms-mode", HelpText = "Expect communications from a managed local client")]
        public bool ManagedMode { get; set; }

        /// <summary>Expect communications from a native local client.</summary>
        [Option('n', "native", SetName = "comms-mode", HelpText = "Expect communications from a native local client")]
        public bool NativeMode { get; set; }

        /// <summary>Expect communications from a native remote client over a network connection.</summary>
        [Option('r', "network", SetName = "comms-mode", HelpText = "Expect communications from a native client over a network connection")]
        public bool NetworkMode { get; set; }

        /// <summary>Port number on which to listen for connections.</summary>
        // todo: validation rules, maybe add a "network" comms mode???
        [Option('p', "port", HelpText = "Port number on which to listen for connections. Only used when accepting connections over network", Default = (uint)27746)]
        public uint Port { get; set; }

        /// <summary>IP Address on which to listen for connections.</summary>
        /// <value></value>
        [Option('a', "address", HelpText = "IP Address on which to listen for connections")]
        public string IPAddress { get; set; }

        /// <summary>
        /// Concrete examples shown in help text.
        /// </summary>
        [Usage]
        public static IEnumerable<Example> Examples
        {
            get
            {
                yield return new Example("Normal usage", new ServerOptions() { File = "file.apsimx", ManagedMode = true });
                yield return new Example("Comms with a local native client", new ServerOptions() { File = "file.apsimx", NativeMode = true });
                yield return new Example("Comms with a native client over the network", new ServerOptions() { File = "file.apsimx", NetworkMode = true });
            }
        }
    }
}
