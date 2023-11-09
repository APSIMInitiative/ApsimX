using k8s;

namespace APSIM.Server.Cluster
{
    /// <summary>
    /// An interface for a class which produces a kubernetes client.
    /// </summary>
    public interface IKubernetesClientGenerator
    {
        /// <summary>
        /// Generate the kubernetes client.
        /// </summary>
        Kubernetes CreateClient();
    }
}
