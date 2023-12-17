using k8s;

namespace APSIM.Server.Cluster
{
    /// <summary>
    /// An interface for a class which produces a kubernetes client.
    /// </summary>
    internal class LocalhostClientGenerator : IKubernetesClientGenerator
    {
        /// <summary>
        /// Generate the kubernetes client.
        /// </summary>
        public Kubernetes CreateClient()
        {
            // var config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
            var config = new KubernetesClientConfiguration { Host = "http://127.0.0.1:8001" };
            return new Kubernetes(config);
        }
    }
}
