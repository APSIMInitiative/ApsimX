using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using APSIM.Server.Cli;
using APSIM.Server.Cluster;
using APSIM.Server.Commands;
using APSIM.Server.IO;
using k8s;
using k8s.Models;
using Models.Core.Run;
using APSIM.Server.Extensions;

namespace APSIM.Server
{
    /// <summary>
    /// Job manager for a kubernetes cluster. This class essentially takes an
    /// .apsimx file of arbitrary size as input, splits it into multiple smaller
    /// chunks, and starts a kubernetes pod for each chunk. Each worker pod
    /// runs an apsim server instance on the specified chunk of the .apsimx
    /// file.
    /// 
    /// This class then listens for instructions over a socket connection, and
    /// essentially acts as a relay for the worker pods; whenever this class
    /// receives a command over the socket connection, it simply passes the
    /// command on to each worker pod.
    /// </summary>
    public class RelayServer : ApsimServer, IDisposable
    {
        // Constants
        private const string apiVersion = "v1";
        private enum Kind
        {
            Pod,
            Namespace,
            Deployment
        }
        private const string appName = "apsim-cluster";
        private const string version = "1.0";
        private const string component = "simulation";
        private const string partOf = "apsim";
        private const string managedBy = "ApsimClusterJobManager";
        private const string imageName = "apsiminitiative/apsimng-server";
        private const string inputsVolumeName = "apsim-inputs-files";
        private const string containerStartFile = "/start";
        private const string workerInputsPath = "/inputs";

        /// <summary>
        /// Port number used for socket connections to the worker pods.
        /// </summary>
        private const uint portNo = 27746;

        // State
        private readonly Kubernetes client;
        private readonly RelayServerOptions relayOptions;
        private readonly string owner = "drew"; // todo: This should be specified in options.
        private readonly Guid jobID;
        private readonly string instanceName;
        private readonly string podNamespace;

        /// <summary>
        /// Names of the worker pods.
        /// </summary>
        private IEnumerable<string> workers;

        /// <summary>
        /// Create a job manager instance.
        /// </summary>
        /// <param name="options">User options.</param>
        /// <param name="clientGenerator">Kubernetes client generator.</param>
        public RelayServer(RelayServerOptions options) : base()
        {
            this.options = (GlobalServerOptions)options;
            WriteToLog("Job manager started");
            this.relayOptions = options;
            jobID = Guid.NewGuid();
            IKubernetesClientGenerator clientGenerator;
            if (options.InPod)
            {
                instanceName = "job-manager";
                if (string.IsNullOrWhiteSpace(options.Namespace))
                    throw new ArgumentNullException("When running relay server in a kubernetes pod, namespace must be set.");
                podNamespace = options.Namespace;
                clientGenerator = new InPodClientGenerator();
            }
            else
            {
                instanceName = $"apsim-cluster-{jobID}";
                podNamespace = $"apsim-cluster-{jobID}";
                clientGenerator = new LocalhostClientGenerator();
            }
            client = clientGenerator.CreateClient();
        }

        public override void Run()
        {
            workers = FindWorkers();

            // tbi: go into relay mode
            WriteToLog("Starting relay server...");
            base.Run();
        }

        /// <summary>
        /// fixme!!!
        /// </summary>
        private IEnumerable<string> FindWorkers()
        {
            List<string> podNames = new List<string>();
            V1PodList pods = client.ListNamespacedPod(podNamespace);
            foreach (V1Pod pod in pods.Items)
            {
                string podName = pod.Name();
                if (!podName.Contains("job-manager"))
                    podNames.Add(podName);
            }
            return podNames;
        }

        /// <summary>
        /// We've received a command. Instead of running it, we instead
        /// relay the command to each worker pod.
        /// </summary>
        /// <param name="command">Command to be run.</param>
        /// <param name="connection">Connection on which we received the command.</param>
        protected override void RunCommand(ICommand command, IConnectionManager connection)
        {
            // Relay the command to all workers.
            foreach (string podName in workers)
            {
                V1Pod pod = GetWorkerPod(podName);
                if (string.IsNullOrEmpty(pod.Status.PodIP))
                    throw new NotImplementedException("Pod IP not set.");

                // Create a new socket connection to the pod.
                string ip = pod.Status.PodIP;
                WriteToLog($"Attempting connection to pod {podName} on {ip}:{portNo}");
                using (NetworkSocketConnection conn = new NetworkSocketConnection(relayOptions.Verbose, ip, portNo, Protocol.Managed))
                {
                    // Relay the command to the pod.
                    conn.SendCommand(command);
                }
            }
        }

        /// <summary>
        /// Get the worker pod with the given name.
        /// </summary>
        /// <param name="podName">Name of the worker pod.</param>
        private V1Pod GetWorkerPod(string podName)
        {
            return client.ReadNamespacedPod(podName, podNamespace);
        }

        /// <summary>
        /// Get the state of the given worker pod. Can throw but will never return null.
        /// </summary>
        /// <param name="podName">Name of the pod.</param>
        private V1ContainerState GetPodState(string podName)
        {
            V1Pod pod = GetWorkerPod(podName);
            string container = GetContainerName(podName);
            V1ContainerState state = pod.Status.ContainerStatuses.FirstOrDefault(c => c.Name == container)?.State;
            if (state == null)
                throw new Exception($"Unable to read state of pod {podName} - pod has no container state for the {container} container");
            return state;
        }

        /// <summary>
        /// Read console output from a particular container in a pod.
        /// </summary>
        /// <param name="podNamespace">Namespace of the pod.</param>
        /// <param name="podName">Pod name.</param>
        /// <param name="containerName">Container name.</param>
        /// <returns></returns>
        private string GetLog(string podNamespace, string podName, string containerName)
        {
            using (Stream logStream = client.ReadNamespacedPodLog(podName, podNamespace, containerName, previous: true))
                using (StreamReader reader = new StreamReader(logStream))
                    return reader.ReadToEnd();
        }

        /// <summary>
        /// Get the name of the apsim-server container running in a given pod.
        /// </summary>
        /// <param name="podName">Name of the pod.</param>
        private string GetContainerName(string podName)
        {
            return $"{podName}-container";
        }

        /// <summary>
        /// Dispose of the job manager by deleting the namespace and all pods
        /// therein.
        /// </summary>
        public void Dispose()
        {
            // RemoveWorkers();
            WriteToLog("Deleting namespace...");
            client.DeleteNamespace(podNamespace);
            client.Dispose();
        }
    }
}
