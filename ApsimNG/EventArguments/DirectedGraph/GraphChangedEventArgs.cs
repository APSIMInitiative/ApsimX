using System;
using System.Collections.Generic;
using APSIM.Shared.Graphing;

namespace ApsimNG.EventArguments.DirectedGraph
{
    public class GraphChangedEventArgs : EventArgs
    {
        public List<Node> Nodes { get; set; }
        public List<Arc> Arcs { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="arcs">Arcs.</param>
        /// <param name="nodes">Nodes.</param>
        public GraphChangedEventArgs(List<Arc> arcs, List<Node> nodes)
        {
            Nodes = nodes;
            Arcs = arcs;
        }
    }
}