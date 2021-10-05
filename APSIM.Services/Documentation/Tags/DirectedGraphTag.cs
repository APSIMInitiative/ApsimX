using System.Collections.Generic;
using APSIM.Services.Documentation;
using APSIM.Services.Graphing;

namespace APSIM.Services.Documentation.Tags
{
    /// <summary>
    /// A map which can be displayed in autodocs.
    /// </summary>
    public class DirectedGraphTag : ITag
    {
        /// <summary>
        /// The directed graph instance.
        /// </summary>
        public DirectedGraph Graph { get; private set; }

        /// <summary>
        /// Create a new <see cref="DirectedGraphTag"/> instance.
        /// </summary>
        /// <param name="graph">A directed graph instance.</param>
        public DirectedGraphTag(DirectedGraph graph)
        {
            Graph = graph;
        }
    }
}
