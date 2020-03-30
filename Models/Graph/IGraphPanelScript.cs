using Models.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    /// <summary>
    /// An interface for a graph panel script.
    /// </summary>
    public interface IGraphPanelScript
    {
        /// <summary>
        /// Gets a list of simulation names. One tab of graphs will be generated
        /// for each simulation.
        /// </summary>
        /// <param name="storage">Provides access to the datastore.</param>
        /// <param name="panel">Provides access to the graph panel and the simulations tree.</param>
        string[] GetSimulationNames(IStorageReader storage, GraphPanel panel);

        /// <summary>
        /// Called on each graph before it is drawn in a tab.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="simulationName">Simulation name for this tab.</param>
        void TransformGraph(Graph graph, string simulationName);
    }
}
