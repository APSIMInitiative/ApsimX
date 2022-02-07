using k8s;

namespace APSIM.Server.Cluster
{
    /// <summary>
    /// This class allows connection to the kubernetes API from within
    /// a running pod.
    /// </summary>
    internal class InPodClientGenerator : IKubernetesClientGenerator
    {
        /// <summary>
        /// Generate the kubernetes client.
        /// </summary>
        public Kubernetes CreateClient()
        {
            // var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var config = KubernetesClientConfiguration.InClusterConfig();
            return new Kubernetes(config);
        }
    }
}
