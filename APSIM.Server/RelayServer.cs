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
        private IEnumerable<V1Pod> pods;
        private string tempInputFiles;

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
            pods = InitialiseWorkers();

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
            foreach (V1Pod pod in pods)
            {
                if (string.IsNullOrEmpty(pod.Status.PodIP))
                    throw new NotImplementedException("Pod IP not set.");
                // Create a new socket connection to the pod.
                NetworkSocketConnection conn = new NetworkSocketConnection(relayOptions.Verbose, pod.Status.PodIP, portNo, Protocol.Native);

                // Relay the command to the pod.
                conn.SendCommand(command);
            }
        }

        /// <summary>
        /// Delete all worker nodes.
        /// </summary>
        private void RemoveWorkers()
        {
            if (pods == null)
            {
                WriteToLog("No pods to delete");
                return;
            }
            if (client == null)
                // The client is readonly so this shouldn't really be possible.
                throw new InvalidOperationException($"Unable to cleanup pods: client is null");

            WriteToLog("Deleting pods...");
            foreach (V1Pod pod in pods)
                // need to check this
                client.DeleteNamespacedPod(pod.Metadata.Name, podNamespace);

            pods = null;
        }

        /// <summary>
        /// Initialise the worker nodes.
        /// </summary>
        /// <param name="client">The kubernetes client to be used.</param>
        private IEnumerable<V1Pod> InitialiseWorkers()
        {
            WriteToLog("Initialising workers...");

            // Copy the input files to a writable location.
            EnsureInputsAreWritable();

            // Split apsimx file into smaller chunks.
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
            List<V1Pod> pods = new List<V1Pod>();
            foreach (string file in generatedFiles)
                pods.Add(CreatePod(file, $"worker-{i++}"));

            // Create pods in the current namespace.
            // todo: use async API
            pods = pods.Select(p => client.CreateNamespacedPod(p, podNamespace)).ToList();

            WriteToLog($"Created {pods.Count} pod{(pods.Count == 1 ? "" : "s")}.");

            WriteToLog($"Waiting for pods to start...");

            // Busy wait while any pods are still in the "Pending" phase.
            // This ensures that the returned pods have certain metadata
            // populated such as host IP address.
            while (!pods.Any(p => p.Status.Phase == "Pending"))
                pods = GetPods().ToList();

            return pods;
        }

        /// <summary>
        /// Copy the input files to a writable location.
        /// This will update the value of options.File.
        /// </summary>
        private void EnsureInputsAreWritable()
        {
            if (!File.Exists(relayOptions.File))
                throw new FileNotFoundException($"Input file {relayOptions.File} does not exist");
            string inputsDirectory = Path.GetDirectoryName(relayOptions.File);
            if (Directory.EnumerateDirectories(inputsDirectory).Any())
                throw new InvalidOperationException("Found a child directory inside inputs directory. Please use a flat list of files for now.");

            tempInputFiles = Path.Combine(Path.GetTempPath(), $"input-files-{Guid.NewGuid()}");
            Directory.CreateDirectory(tempInputFiles);
            foreach (string file in Directory.EnumerateFiles(inputsDirectory))
            {
                string rawFileName = Path.GetFileName(file);
                string newFileName = Path.Combine(tempInputFiles, rawFileName);
                string extension = Path.GetExtension(file);
                string[] dontCopy = new[] { ".db", ".db-wal", ".db-shm" };
                if (!dontCopy.Contains(extension))
                {
                    WriteToLog($"Copying {file} to {newFileName}");
                    try
                    {
                        File.Copy(file, newFileName);
                    }
                    catch (Exception err)
                    {
                        Process proc = null;
                        try
                        {
                            proc = Process.Start("/bin/chmod", $"a+x {inputsDirectory}");
                            proc.WaitForExit();
                        }
                        catch (Exception err2)
                        {
                            throw new AggregateException($"Unable to chmod OR copy file (stdout = {proc.StandardOutput}, stderr = {proc.StandardError})", new[] { err, err2 });
                        }
                        try
                        {
                            File.Copy(file, newFileName);
                        }
                        catch (Exception err2)
                        {
                            throw new AggregateException($"chmod worked, but didn't help", new[] { err, err2 });
                        }
                    }
                }
            }
            relayOptions.File = Path.Combine(tempInputFiles, Path.GetFileName(relayOptions.File));
            WriteToLog($"Moved input file to {relayOptions.File}");
        }

        /// <summary>
        /// Get all worker pods in the namespace.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<V1Pod> GetPods()
        {
            return client.ListNamespacedPod(podNamespace).Items;
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
        /// Create a pod for running the given file.
        /// </summary>
        /// <param name="file">The .apsimx file which the pod should run.</param>
        /// <param name="workerCpuCount">The number of vCPUs for the worker container in the pod. This should probably be equal to the number of simulations in the .apsimx file.</param>
        private V1Pod CreatePod(string file, string podName)
        {
            // todo: pod labels
            V1ObjectMeta metadata = new V1ObjectMeta(name: podName);
            V1Volume volume = InputFilesVolume(file);
            V1PodSpec spec = new V1PodSpec(
                containers: new[] { ApsimServerContainer($"{podName}-container", file, volume.Name) },
                volumes: new[] { volume }
            );
            return new V1Pod(apiVersion, Kind.Pod.ToString(), metadata, spec);
        }

        /// <summary>
        /// Create a V1Volume instance for the input file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        private V1Volume InputFilesVolume(string file)
        {
            V1HostPathVolumeSource hostVolume = new V1HostPathVolumeSource(
                path: Path.GetDirectoryName(file),
                type: "Directory"
            );
            return new V1Volume(
                name: $"{inputsVolumeName}-{Guid.NewGuid()}",
                hostPath: hostVolume
            );
        }

        /// <summary>
        /// Get the containers run in a worker node (pod).
        /// Currently this is a single apsiminitiative/apsimng-server container.
        /// </summary>
        /// <param name="name">Display name for the container.</parama>
        /// <param name="file">The input file on which the container should run.</param>
        /// <param name="inputsVolume">Name of the volume containing the input files.</param>
        private V1Container ApsimServerContainer(string name, string file, string inputsVolume)
        {
            string fileName = Path.GetFileName(file);
            const string mountPath = "/inputs";
            // fixme - this is hacky and nasty
            string[] args = new[]
            {
                "-c",
                // We run the server instances with the following settings:
                // -v: verbose mode
                // -k: keep-alive mode, because we don't maintain connections with the pods at all times.
                // -r: remote connection type, because we will be connecting over network socket
                // -n: native communications protocol, because I haven't implemented the managed version yet
                // -f: pointing to this particular input file
                // Listening on 0.0.0.0, using the same port no for all pods. This could be configurable
                // 
                // This little dance that we do here with the input files is to work around write
                // permissions (or lack thereof) on the volume mount.
                $"mkdir /input-files && cp {mountPath}/* /input-files/ && /apsim/apsim-server listen -vkrnf /input-files/{fileName} -a 0.0.0.0 -p {portNo}"
            };

            V1VolumeMount volume = new V1VolumeMount(
                mountPath: mountPath,
                name: inputsVolume
            );

            return new V1Container(
                name,
                image: imageName,
                command: new string[] { "/bin/sh" },
                args: args,
                volumeMounts: new List<V1VolumeMount>() { volume }
            );
        }

        /// <summary>
        /// Split an .apsimx file into smaller chunks, of the given size.
        /// </summary>
        /// <param name="inputFile">The input .apsimx file.</param>
        /// <param name="simsPerFile">The number of simulations to add to each generated file.</param>
        private static IEnumerable<string> SplitApsimXFile(string inputFile, uint simsPerFile)
        {
            // tbi - for now just return the original file
            string outputPath = Path.GetDirectoryName(inputFile);
            return GenerateApsimXFiles.SplitFile(inputFile, simsPerFile, outputPath, _ => {}, true);
        }

        /// <summary>
        /// Dispose of the job manager by deleting the namespace and all pods
        /// therein.
        /// </summary>
        public void Dispose()
        {
            RemoveWorkers();
            WriteToLog("Deleting namespace...");
            client.DeleteNamespace(podNamespace);
            client.Dispose();
            if (!string.IsNullOrEmpty(tempInputFiles) && Directory.Exists(tempInputFiles))
                Directory.Delete(tempInputFiles, true);
        }
    }
}
