using Models;

namespace Models.Interfaces
{
    /// <summary>
    /// This interface defines the communications between a soil arbitrator and
    /// and crop.
    /// </summary>
    public interface IVisualiseAsDirectedGraph
    {
        /// <summary>Get directed graph from model</summary>
        DirectedGraph DirectedGraphInfo { get; set; }
    }
}
