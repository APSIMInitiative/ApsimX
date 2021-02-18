using System;
using System.Collections.Generic;
using ApsimNG.Classes.DirectedGraph;
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
    public class AddNodeEventArgs : EventArgs
    {
        public StateNode Node { get; set; }
        public AddNodeEventArgs(StateNode node) => Node = node;
    }
    public class DelNodeEventArgs : EventArgs
    {
        public string nodeNameToDelete { get; set; }
    }
    public class AddArcEventArgs : EventArgs
    {
        public RuleAction Arc { get; set; }
    }
    public class DelArcEventArgs : EventArgs
    {
        public string arcNameToDelete { get; set; }
    }
    public class ObjectMovedArgs : EventArgs
    {
        /// <summary>
        /// The object which has been moved.
        /// </summary>
        public DGObject MovedObject { get; set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="obj">The object which has been moved.</param>
        public ObjectMovedArgs(DGObject obj)
        {
            MovedObject = obj;
        }
    }
}