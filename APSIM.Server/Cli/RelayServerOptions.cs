using CommandLine;

namespace APSIM.Server.Cli
{
    /// <summary>
    /// Options for a relay server.
    /// </summary>
    [Verb("relay")]
    public class RelayServerOptions : GlobalServerOptions
    {
        /// <summary>Number of vCPUs per worker node.</summary>
        [Option('c', "cpu-count", HelpText = "Number of vCPUs per worker node", Default = 1u)]
        public uint WorkerCpuCount { get; set; }

        /// <summary>Is the server running in a kubernetes pod?</summary>
        [Option("in-pod", HelpText = "Set this if the server is running in a kubernetes pod.", Default = false)]
        public bool InPod { get; set; }

        /// <summary>
        /// Kubernetes namespace in which this pod is running. If --in-pod is set, this is also required.
        /// </summary>
        [Option("namespace", HelpText = "Kubernetes namespace in which this pod is running. If --in-pod is set, this is also required.")]
        public string Namespace { get; set; }
    }
}
