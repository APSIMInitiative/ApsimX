using System;
using System.Collections.Generic;
using Models.Management;

namespace UserInterface.EventArguments.DirectedGraph
{
    public class GraphChangedEventArgs : EventArgs
    {
        public List<StateNode> Nodes { get; set; }
        public List<RuleAction> Arcs { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="arcs">Arcs.</param>
        /// <param name="nodes">Nodes.</param>
        public GraphChangedEventArgs(List<RuleAction> arcs, List<StateNode> nodes)
        {
            Nodes = nodes;
            Arcs = arcs;
        }
    }
}