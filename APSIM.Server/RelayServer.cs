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
            InitialiseWorkers();

            // tbi: go into relay mode
            WriteToLog("Starting relay server...");
            base.Run();
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
                NetworkSocketConnection conn = new NetworkSocketConnection(relayOptions.Verbose, pod.Status.PodIP, portNo, Protocol.Native);

                // Relay the command to the pod.
                conn.SendCommand(command);
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
        /// Initialise the worker nodes.
        /// </summary>
        /// <param name="client">The kubernetes client to be used.</param>
        private void InitialiseWorkers()
        {
            WriteToLog("Initialising workers...");

            // Split apsimx file into smaller chunks.
            string inputsPath = Path.GetDirectoryName(options.File);
            // Get a list of support files (.met files, .xlsx, ...). These are all
            // other input files required by the .apsimx file. They are just the
            // sibling files of the input file.
            IEnumerable<string> supportFiles = Directory.EnumerateFiles(inputsPath, "*", SearchOption.AllDirectories).Except(new[] { options.File });
            IEnumerable<string> generatedFiles = SplitApsimXFile(relayOptions.File, relayOptions.WorkerCpuCount);
            if (generatedFiles.Any())
            {
                int n = generatedFiles.Count();
                WriteToLog($"Split input file into {n} chunk{(n == 1 ? "" : "s")}.");
            }
            else
                throw new InvalidOperationException($"Input file {relayOptions.File} contains no simulations.");

            // If this is not running inside a pod, then we need to create a
            // namespace for the worker pods. Otherwise, the assumption is that
            // this pod is running inside the desired namespace.
            // Create a new namespace in which to store the pods.
            if (!relayOptions.InPod)
                client.CreateNamespace(CreateStandardNamespace());

            WriteToLog("Launching pods...");

            // Create pod templates.
            uint i = 0;
            Dictionary<string, string> pods = new Dictionary<string, string>();
            foreach (string file in generatedFiles)
            {
                string podName = $"worker-{i++}";
                pods.Add(podName, file);
                CreateWorkerPod(file, podName);
            }
            workers = pods.Select(k => k.Key).ToArray();
            WriteToLog($"Successfully created {pods.Count} pod{(pods.Count == 1 ? "" : "s")}.");

            // Wait for worker containers to launch.
            WaitForWorkersToLaunch(10 * 1000);

            // Send input files to pods.
            // The keys here are the pod names
            // The values are the pod's input file.
            // todo: create a struct to hold this?
            WriteToLog("Sending input files to pods...");
            foreach (KeyValuePair<string, string> pod in pods)
            {
                SendFilesToPod(pod.Key, supportFiles.Append(pod.Value), workerInputsPath);
                SendStartSignalToPod(pod.Key);
            }

            // Monitor the pods for some time.
            int seconds = 10;
            MonitorPods(seconds * 1000);
            EnsureWorkersHaveWrittenOutput();
        }

        private void EnsureWorkersHaveWrittenOutput()
        {
            foreach (string worker in workers)
                EnsureWorkerHasWrittenOutput(worker);
        }

        private void EnsureWorkerHasWrittenOutput(string podName)
        {
            string container = GetContainerName(podName);
            string log = GetLog(podNamespace, podName, container);
            if (string.IsNullOrEmpty(log))
                throw new Exception($"Pod {podName} has not written output after receiving start signal.");
        }

        /// <summary>
        /// Monitor the worker pods for the given duration, and throw
        /// if any of the pods have failed.
        /// </summary>
        /// <param name="duration">Time period for which to montior pods (in ms).</param>
        private void MonitorPods(int duration)
        {
            CancellationTokenSource source = new CancellationTokenSource(duration);
            while (!source.IsCancellationRequested)
                foreach (string worker in workers)
                    VerifyPodHealth(worker);
        }

        /// <summary>
        /// Send the "start" signal to the pod.
        /// </summary>
        /// <remarks>
        /// When we start a worker pod, it goes into a busy wait until we
        /// copy the input files into the pod. This function tells the pod
        /// to end its busy wait (presumably because the input files have)
        /// already been copied into the pod.
        /// </remarks>
        /// <param name="podName">Name of the pod.</param>
        private void SendStartSignalToPod(string podName)
        {
            // testme
            WriteToLog($"Sending start signal to pod {podName}");
            string file = Path.GetTempFileName();
            using (File.Create(file)) { }
            V1Pod pod = GetWorkerPod(podName);
            string container = GetContainerName(podName);
            client.CopyFileToPod(pod, container, file, containerStartFile);
            // ExecAsyncCallback action = (_, __, ___) => Task.CompletedTask;
            // string container = GetContainerName(podName);
            // string[] cmd = new[] { "touch", containerStartFile };
            // CancellationToken token = new CancellationTokenSource().Token;
            // client.NamespacedPodExecAsync(podName, podNamespace, container, cmd, false, action, token);
        }

        /// <summary>
        /// Send the given files to the pod at the specified location.
        /// </summary>
        /// <param name="podName">Name of the pod into which files will be copied.</param>
        /// <param name="files">Files to be copied into the pod.</param>
        /// <param name="destinationDirectory">Directory on the pod into which the files will be copied.</param>
        private void SendFilesToPod(string podName, IEnumerable<string> files, string destinationDirectory)
        {
            WriteToLog($"Sending inputs to pod {podName}...");
            foreach (string file in files)
            {
                WriteToLog($"Sending input file {file} to pod {podName}...");
                V1Pod pod = GetWorkerPod(podName);
                string destination = Path.Combine(destinationDirectory, Path.GetFileName(file));
                client.CopyFileToPod(pod, GetContainerName(podName), file, destination);
            }
        }

        private void WaitForWorkersToLaunch(int maxTimePerPod)
        {
            WriteToLog($"Waiting for pods to start...");
            CancellationTokenSource source = new CancellationTokenSource();

            foreach (string worker in workers)
            {
                source.CancelAfter(maxTimePerPod);
                VerifyPodHealth(worker, source.Token);
                WriteToLog($"Pod {worker} is now online and waiting for input files.");
            }
        }

        /// <summary>
        /// This function will monitor the job manager pod's health for the specified
        /// period of time. An exception will be thrown if the pod fails to start.
        /// </summary>
        private void VerifyPodHealth(string podName, CancellationToken cancellationToken)
        {
            WriteToLog("Verifying pod health...");
            while (!cancellationToken.IsCancellationRequested)
                VerifyPodHealth(podName);
        }

        /// <summary>
        /// Check that a pod is healthy. Throw if not.
        /// </summary>
        /// <param name="podName">Name of the pod to check.</param>
        private void VerifyPodHealth(string podName)
        {
            V1ContainerState state = GetPodState(podName);
            if (state.Running != null)
                return;
            if (state.Terminated != null)
            {
                // Get console output from the container.
                string log = GetLog(podNamespace, podName, GetContainerName(podName));
                throw new Exception($"Pod {podName} failed to start (Reason = {state.Terminated.Reason}, Message = {state.Terminated.Message}).\nContainer log:\n{log}");
            }
            if (state.Waiting != null && state.Waiting.Reason != "ContainerCreating")
                // todo: verify that this is correct...are there other waiting reasons???
                throw new Exception($"Worker pod {podName} failed to start. Reason={state.Waiting.Reason}. Message={state.Waiting.Message}");
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
        /// Create a namespace object with the appropriate metadata.
        /// </summary>
        private V1Namespace CreateStandardNamespace()
        {
            // https://kubernetes.io/docs/concepts/overview/working-with-objects/common-labels/
            Dictionary<string, string> labels = new Dictionary<string, string>();
            labels["app.kubernetes.io/name"] = appName;
            labels["app.kubernetes.io/instance"] = instanceName;
            labels["app.kubernetes.io/version"] = version;
            labels["app.kubernetes.io/component"] = component;
            labels["app.kubernetes.io/part-of"] = partOf;
            labels["app.kubernetes.io/managed-by"] = managedBy;
            labels["app.kubernetes.io/created-by"] = owner;

            V1ObjectMeta namespaceMetadata = new V1ObjectMeta(name: podNamespace, labels: labels);
            return new V1Namespace(apiVersion, Kind.Namespace.ToString(), namespaceMetadata);
        }

        /// <summary>
        /// Create and launch a worker pod which runs on the given input file.
        /// </summary>
        /// <param name="file">Input file for the pod.</param>
        /// <param name="podName">Name of the worker pod.</param>
        private void CreateWorkerPod(string file, string podName)
        {
            V1Pod template = CreateWorkerPodTemplate(file, podName);
            client.CreateNamespacedPod(template, podNamespace);
            WriteToLog($"Successfully launched pod {podName}.");
        }

        /// <summary>
        /// Create a pod for running the given file.
        /// </summary>
        /// <param name="file">The .apsimx file which the pod should run.</param>
        /// <param name="supportFiles">Other misc input files (e.g. met file) which are required to run the main .apsimx file.</param>
        /// <param name="workerCpuCount">The number of vCPUs for the worker container in the pod. This should probably be equal to the number of simulations in the .apsimx file.</param>
        private V1Pod CreateWorkerPodTemplate(string file, string podName)
        {
            // todo: pod labels
            V1ObjectMeta metadata = new V1ObjectMeta(name: podName);
            V1PodSpec spec = new V1PodSpec()
            {
                Containers = new[] { ApsimServerContainer(GetContainerName(podName), file) }
            };
            return new V1Pod(apiVersion, Kind.Pod.ToString(), metadata, spec);
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
        /// Get the containers run in a worker node (pod).
        /// Currently this is a single apsiminitiative/apsimng-server container.
        /// </summary>
        /// <param name="name">Display name for the container.</parama>
        /// <param name="file">The input file on which the container should run.</param>
        private V1Container ApsimServerContainer(string name, string file)
        {
            string fileName = Path.GetFileName(file);
            // fixme - this is hacky and nasty
            string[] args = new[]
            {
                "-c",
                $"mkdir -p {workerInputsPath} && until [ -f {containerStartFile} ]; do sleep 1; done; echo File upload complete, starting server && /apsim/apsim-server listen -vkrnf {workerInputsPath}/{fileName} -a 0.0.0.0 -p {portNo}"
            };

            return new V1Container(
                name,
                image: imageName,
                command: new string[] { "/bin/sh" },
                args: args
            );
        }

        /// <summary>
        /// Split an .apsimx file into smaller chunks, of the given size.
        /// </summary>
        /// <param name="inputFile">The input .apsimx file.</param>
        /// <param name="simsPerFile">The number of simulations to add to each generated file.</param>
        private static IEnumerable<string> SplitApsimXFile(string inputFile, uint simsPerFile)
        {
            string outputPath = Path.GetDirectoryName(inputFile);
            return GenerateApsimXFiles.SplitFile(inputFile, simsPerFile, outputPath, _ => {}, true);
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
